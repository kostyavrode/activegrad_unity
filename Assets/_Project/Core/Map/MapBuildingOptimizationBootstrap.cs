using System;
using System.Linq;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Modifiers;
using UnityEngine;
using Zenject;

public class MapBuildingOptimizationBootstrap : IInitializable, IDisposable
{
    private readonly AbstractMap _map;
    private MapMeshOptimizeModifier _buildingModifier;
    private MapMeshOptimizeModifier _groundModifier;

    public MapBuildingOptimizationBootstrap(AbstractMap map)
    {
        _map = map;
    }

    public void Initialize()
    {
        _map.OnInitialized += OnMapInitialized;
    }

    public void Dispose()
    {
        _map.OnInitialized -= OnMapInitialized;
    }

    private void OnMapInitialized()
    {
        if (_map.VectorData == null)
            return;

        _buildingModifier ??= CreateModifier(receiveShadows: false);
        _groundModifier ??= CreateModifier(receiveShadows: true);

        foreach (var subLayer in _map.VectorData.GetAllFeatureSubLayers())
        {
            var isBuilding = subLayer.coreOptions.layerName == "building";
            if (isBuilding)
            {
                subLayer.Modeling.EnableCombiningMeshes(true);
                subLayer.Modeling.ColliderOptions.SetFeatureCollider(ColliderType.None);
                EnsureOptimizeModifier(subLayer, _buildingModifier);
            }
            else
            {
                EnsureOptimizeModifier(subLayer, _groundModifier);
            }
        }
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
