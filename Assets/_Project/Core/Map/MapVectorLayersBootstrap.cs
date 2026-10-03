using System.Collections.Generic;
using System.Linq;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Modifiers;
using UnityEngine;
using Zenject;

/// <summary>
/// Добавляет в карту стилизованные векторные слои поверх растровой подложки:
/// дороги (3 группы по ширине), воду и парки (+ деревья). Все на шейдере ActiveGrad/StylizedMatcap.
/// Слои лежат на земле (roof-only экструзия на пару сантиметров + depth offset в материале).
/// Дороги — полупрозрачные, чтобы просвечивали надписи растровой карты.
/// Настройки и материалы — Resources/Map/MapVisualStyleConfig.
/// Работает и до, и после инициализации карты: Mapbox сам подхватит/перерисует добавленные слои.
/// </summary>
public class MapVectorLayersBootstrap : IInitializable
{
    private const string ConfigPath = "Map/MapVisualStyleConfig";
    private const string LayerPrefix = "AG ";

    // Высоты слоёв в метрах — почти на земле, чтобы слои не «парили». От z-fighting с тайлом
    // защищает depth offset в материалах (Overlay on Ground), а не подъём геометрии.
    private const float WaterHeight = 0.02f;
    private const float ParkHeight = 0.03f;
    private const float PathHeight = 0.04f;
    private const float StreetHeight = 0.04f;
    private const float MajorRoadHeight = 0.04f;

    private readonly AbstractMap _map;

    public MapVectorLayersBootstrap(AbstractMap map)
    {
        _map = map;
    }

    public void Initialize()
    {
        var config = Resources.Load<MapVisualStyleConfig>(ConfigPath);
        if (config == null || !config.AddVectorGroundLayers || _map == null || _map.VectorData == null)
            return;

        var optimize = ScriptableObject.CreateInstance<MapMeshOptimizeModifier>();
        optimize.Configure(receiveShadows: true);

        if (config.WaterMaterial != null)
            AddPolygonLayer("Water", "water", null, config.WaterMaterial, WaterHeight, optimize);

        if (config.LanduseMaterial != null)
        {
            GameObjectModifier trees = null;
            if (config.PlantParkTrees && config.TreeMaterial != null)
            {
                var treesModifier = ScriptableObject.CreateInstance<MapParkTreesModifier>();
                treesModifier.Configure(config.TreeMaterial);
                trees = treesModifier;
            }

            AddPolygonLayer("Parks", "landuse", "park,grass,wood,scrub,cemetery",
                config.LanduseMaterial, ParkHeight, optimize, trees);
        }

        if (config.RoadMaterial != null)
        {
            AddRoadLayer("Paths", "path,pedestrian,track", config.PathWidthMeters, PathHeight, config.RoadMaterial, optimize);
            AddRoadLayer("Streets", "street,service", config.StreetWidthMeters, StreetHeight, config.RoadMaterial, optimize);
            AddRoadLayer("Major Roads", "motorway,trunk,primary,secondary,tertiary", config.MajorRoadWidthMeters,
                MajorRoadHeight, config.RoadMaterial, optimize);
        }
    }

    private void AddRoadLayer(string name, string classes, float widthMeters, float height, Material material,
        GameObjectModifier optimize)
    {
        if (HasLayer(name))
            return;

        var layer = PresetSubLayerPropertiesFetcher.GetSubLayerProperties(PresetFeatureType.Roads);
        layer.coreOptions.layerName = "road";
        layer.lineGeometryOptions.Width = widthMeters;
        Configure(layer, name, classes, material, height, optimize, null);
        _map.VectorData.AddFeatureSubLayer(layer);
    }

    private void AddPolygonLayer(string name, string dataLayer, string classes, Material material, float height,
        GameObjectModifier optimize, GameObjectModifier extra = null)
    {
        if (HasLayer(name))
            return;

        var layer = PresetSubLayerPropertiesFetcher.GetSubLayerProperties(PresetFeatureType.Landuse);
        layer.coreOptions.layerName = dataLayer;
        Configure(layer, name, classes, material, height, optimize, extra);
        _map.VectorData.AddFeatureSubLayer(layer);
    }

    private static void Configure(VectorSubLayerProperties layer, string name, string classes, Material material,
        float height, GameObjectModifier optimize, GameObjectModifier extra)
    {
        layer.coreOptions.sublayerName = LayerPrefix + name;
        layer.coreOptions.combineMeshes = true;
        layer.coreOptions.snapToTerrain = true;
        layer.buildingsWithUniqueIds = false;
        layer.colliderOptions.colliderType = ColliderType.None;
        // Слои из кода не получают performanceOptions (они есть только у сериализованных) —
        // без этого весь тайл строится за один кадр и даёт рывок при подгрузке во время ходьбы.
        layer.performanceOptions = new LayerPerformanceOptions { isEnabled = true, entityPerCoroutine = 10 };

        layer.extrusionOptions.extrusionType = ExtrusionType.AbsoluteHeight;
        layer.extrusionOptions.extrusionGeometryType = ExtrusionGeometryType.RoofOnly;
        layer.extrusionOptions.maximumHeight = height;
        layer.extrusionOptions.minimumHeight = 0f;

        layer.materialOptions.style = StyleTypes.Custom;
        layer.materialOptions.customStyleOptions = new CustomStyleBundle
        {
            texturingType = UvMapType.Tiled,
            materials = new[]
            {
                new MaterialList { Materials = new[] { material } },
                new MaterialList { Materials = new[] { material } },
            },
        };

        if (!string.IsNullOrEmpty(classes))
            layer.filterOptions.AddStringFilterContains("class", classes);

        layer.MeshModifiers ??= new List<MeshModifier>();
        layer.GoModifiers ??= new List<GameObjectModifier>();
        layer.GoModifiers.Add(optimize);
        if (extra != null)
            layer.GoModifiers.Add(extra);
    }

    private bool HasLayer(string name)
    {
        return _map.VectorData.GetAllFeatureSubLayers()
            .Any(layer => layer.coreOptions.sublayerName == LayerPrefix + name);
    }
}
