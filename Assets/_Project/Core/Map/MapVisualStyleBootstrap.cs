using System;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Modifiers;
using UnityEngine;
using Zenject;

public class MapVisualStyleBootstrap : IInitializable, IDisposable
{
    private readonly AbstractMap _map;
    private readonly MapVisualStyleConfig _config;

    private MapStylizedMaterialModifier _buildingMaterialModifier;
    private MapStylizedMaterialModifier _roadMaterialModifier;
    private MapStylizedMaterialModifier _landuseMaterialModifier;
    private MapStylizedMaterialModifier _waterMaterialModifier;

    public MapVisualStyleBootstrap(AbstractMap map, MapVisualStyleConfig config)
    {
        _map = map;
        _config = config;
    }

    public void Initialize()
    {
        MapVisualGlobals.Apply(_config);
        _map.OnInitialized += OnMapInitialized;
    }

    public void Dispose()
    {
        _map.OnInitialized -= OnMapInitialized;
    }

    private void OnMapInitialized()
    {
        if (_map.VectorData == null || _config == null)
            return;

        MapVisualGlobals.Apply(_config);

        foreach (var subLayer in _map.VectorData.GetAllFeatureSubLayers())
        {
            var layerName = subLayer.coreOptions.layerName;
            switch (layerName)
            {
                case "building":
                    ApplyBuildingLayer(subLayer);
                    break;
                case "road":
                    ApplyGroundLayer(subLayer, _config.RoadMaterial, ref _roadMaterialModifier);
                    break;
                case "landuse":
                    ApplyGroundLayer(subLayer, _config.LanduseMaterial, ref _landuseMaterialModifier);
                    break;
                case "water":
                    ApplyGroundLayer(subLayer, _config.WaterMaterial, ref _waterMaterialModifier);
                    break;
            }
        }
    }

    private void ApplyBuildingLayer(VectorSubLayerProperties subLayer)
    {
        if (!_config.ApplyBuildingMaterials || _config.BuildingRoofMaterial == null)
            return;

        var wallMaterial = _config.BuildingWallMaterial != null
            ? _config.BuildingWallMaterial
            : _config.BuildingRoofMaterial;

        AssignMaterialOptions(subLayer, _config.BuildingRoofMaterial, wallMaterial);
        EnsureMaterialModifier(subLayer, ref _buildingMaterialModifier, new[] { _config.BuildingRoofMaterial, wallMaterial });
    }

    private void ApplyGroundLayer(VectorSubLayerProperties subLayer, Material material, ref MapStylizedMaterialModifier modifier)
    {
        if (!_config.ApplyGroundMaterials || material == null)
            return;

        AssignMaterialOptions(subLayer, material, material);
        EnsureMaterialModifier(subLayer, ref modifier, new[] { material });
    }

    private static void AssignMaterialOptions(VectorSubLayerProperties subLayer, Material topMaterial, Material sideMaterial)
    {
        var options = subLayer.materialOptions;
        EnsureMaterialSlot(options.materials, 0, topMaterial);
        EnsureMaterialSlot(options.materials, 1, sideMaterial);

        if (options.customStyleOptions?.materials != null)
        {
            EnsureMaterialSlot(options.customStyleOptions.materials, 0, topMaterial);
            EnsureMaterialSlot(options.customStyleOptions.materials, 1, sideMaterial);
        }
    }

    private static void EnsureMaterialSlot(MaterialList[] materials, int index, Material material)
    {
        if (materials == null || index >= materials.Length || material == null)
            return;

        if (materials[index].Materials == null || materials[index].Materials.Length == 0)
            materials[index].Materials = new[] { material };
        else
            materials[index].Materials[0] = material;
    }

    private static void EnsureMaterialModifier(
        VectorSubLayerProperties subLayer,
        ref MapStylizedMaterialModifier modifier,
        Material[] materials)
    {
        if (materials == null || materials.Length == 0)
            return;

        modifier ??= ScriptableObject.CreateInstance<MapStylizedMaterialModifier>();
        modifier.Configure(materials);

        var alreadyAdded = subLayer.BehaviorModifiers
            .GetGameObjectModifier(existing => existing is MapStylizedMaterialModifier)
            .Count > 0;

        if (!alreadyAdded)
            subLayer.BehaviorModifiers.AddGameObjectModifier(modifier);
    }
}
