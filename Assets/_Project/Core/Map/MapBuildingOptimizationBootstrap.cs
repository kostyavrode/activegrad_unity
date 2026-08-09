using System;
using System.Collections;
using System.Linq;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Factories;
using Mapbox.Unity.MeshGeneration.Interfaces;
using Mapbox.Unity.MeshGeneration.Modifiers;
using UnityEngine;
using Zenject;

public class MapBuildingOptimizationBootstrap : IInitializable, IDisposable
{
    private const float BuildingChamferMeters = 0.75f;
    private const float RoofInsetMeters = 0.4f;
    private const float RoofDropMeters = 0.32f;

    private readonly AbstractMap _map;
    private readonly CoroutineRunner _coroutineRunner;
    private MapMeshOptimizeModifier _buildingModifier;
    private MapMeshOptimizeModifier _groundModifier;
    private MapBuildingFootprintChamferModifier _chamferModifier;
    private MapBuildingRoofSoftenerModifier _roofSoftenerModifier;
    private bool _buildingMeshStyleApplied;

    public MapBuildingOptimizationBootstrap(AbstractMap map, CoroutineRunner coroutineRunner)
    {
        _map = map;
        _coroutineRunner = coroutineRunner;
    }

    public void Initialize()
    {
        _chamferModifier ??= CreateChamferModifier();
        _roofSoftenerModifier ??= CreateRoofSoftenerModifier();
        _map.OnInitialized += OnMapInitialized;
        _coroutineRunner.StartCoroutine(ApplyBuildingChamferWhenReady());
    }

    public void Dispose()
    {
        _map.OnInitialized -= OnMapInitialized;
    }

    private void OnMapInitialized()
    {
        ApplyBuildingOptimizations();
    }

    private IEnumerator ApplyBuildingChamferWhenReady()
    {
        yield return null;
        ApplyBuildingOptimizations();

        if (!_buildingMeshStyleApplied)
        {
            yield return null;
            ApplyBuildingOptimizations();
        }
    }

    private void ApplyBuildingOptimizations()
    {
        if (_map.VectorData == null)
            return;

        _buildingModifier ??= CreateModifier(receiveShadows: false);
        _groundModifier ??= CreateModifier(receiveShadows: true);

        var vectorLayer = _map.VectorData as VectorLayer;

        foreach (var subLayer in _map.VectorData.GetAllFeatureSubLayers())
        {
            var isBuilding = subLayer.coreOptions.layerName == "building";
            if (isBuilding)
            {
                subLayer.Modeling.EnableCombiningMeshes(true);
                subLayer.Modeling.ColliderOptions.SetFeatureCollider(ColliderType.None);
                TryApplyBuildingMeshStyle(vectorLayer, subLayer);
                EnsureOptimizeModifier(subLayer, _buildingModifier);
            }
            else
            {
                EnsureOptimizeModifier(subLayer, _groundModifier);
            }
        }
    }

    private void TryApplyBuildingMeshStyle(VectorLayer vectorLayer, VectorSubLayerProperties subLayer)
    {
        if (vectorLayer?.Factory == null)
            return;

        var visualizer = vectorLayer.Factory.FindVectorLayerVisualizer(subLayer) as VectorLayerVisualizer;
        if (visualizer?.DefaultModifierStack?.MeshModifiers == null)
            return;

        var stack = visualizer.DefaultModifierStack.MeshModifiers;
        var hadStyle = stack.Any(modifier => modifier is MapBuildingFootprintChamferModifier)
                       && stack.Any(modifier => modifier is MapBuildingRoofSoftenerModifier);

        stack.RemoveAll(modifier => modifier is MapBuildingFootprintChamferModifier
                                    || modifier is MapBuildingRoofSoftenerModifier);
        stack.Insert(0, _chamferModifier);
        stack.Add(_roofSoftenerModifier);

        _buildingMeshStyleApplied = true;

        if (hadStyle)
            return;

        RedrawBuildingLayer(vectorLayer.Factory, visualizer);
    }

    private void RedrawBuildingLayer(VectorTileFactory factory, VectorLayerVisualizer visualizer)
    {
        var tiles = _map.MapVisualizer?.ActiveTiles;
        if (tiles == null || tiles.Count == 0)
            return;

        var tileList = tiles.Values.ToList();
        for (var i = 0; i < tileList.Count; i++)
            factory.UnregisterLayer(tileList[i], visualizer);

        for (var i = 0; i < tileList.Count; i++)
            factory.RedrawSubLayer(tileList[i], visualizer);
    }

    private static MapBuildingFootprintChamferModifier CreateChamferModifier()
    {
        var modifier = ScriptableObject.CreateInstance<MapBuildingFootprintChamferModifier>();
        modifier.Configure(BuildingChamferMeters);
        return modifier;
    }

    private static MapBuildingRoofSoftenerModifier CreateRoofSoftenerModifier()
    {
        var modifier = ScriptableObject.CreateInstance<MapBuildingRoofSoftenerModifier>();
        modifier.Configure(RoofInsetMeters, RoofDropMeters);
        return modifier;
    }

    private static MapMeshOptimizeModifier CreateModifier(bool receiveShadows)
    {
        var modifier = ScriptableObject.CreateInstance<MapMeshOptimizeModifier>();
        modifier.Configure(receiveShadows);
        return modifier;
    }

    private void EnsureOptimizeModifier(VectorSubLayerProperties subLayer, MapMeshOptimizeModifier modifier)
    {
        var alreadyAdded = subLayer.BehaviorModifiers
            .GetGameObjectModifier(existing => existing is MapMeshOptimizeModifier)
            .Any();

        if (!alreadyAdded)
            subLayer.BehaviorModifiers.AddGameObjectModifier(modifier);
    }
}
