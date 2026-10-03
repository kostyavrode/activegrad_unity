using UnityEngine;

[CreateAssetMenu(menuName = "ActiveGrad/Map/Visual Style Config")]
public class MapVisualStyleConfig : ScriptableObject
{
    [Header("Buildings")]
    public Material BuildingRoofMaterial;
    public Material BuildingWallMaterial;

    [Header("Ground")]
    public Material RoadMaterial;
    public Material LanduseMaterial;
    public Material WaterMaterial;

    [Header("Actors")]
    public Material BlobShadowMaterial;
    public Material PoiGlowMaterial;

    [Header("Globals")]
    [Range(0f, 1f)] public float DayNightBlend;
    public bool ApplyBuildingMaterials = true;
    public bool ApplyGroundMaterials = true;

    [Header("Vector Ground (MapVectorLayersBootstrap)")]
    public bool AddVectorGroundLayers = true;
    [Min(0.5f)] public float MajorRoadWidthMeters = 12f;
    [Min(0.5f)] public float StreetWidthMeters = 7f;
    [Min(0.5f)] public float PathWidthMeters = 2.5f;

    [Header("Park Trees")]
    public bool PlantParkTrees = true;
    public Material TreeMaterial;

    [Header("Labels Overlay (MapLabelsOverlay)")]
    [Tooltip("Стиль Mapbox Studio «только надписи» с прозрачным фоном, например mapbox://styles/<user>/<styleId> (можно .../draft). Пусто — слой выключен.")]
    public string LabelsStyleUrl = "";
    public Material LabelsMaterial;

    [Header("Ground Tiles")]
    [Tooltip("Разбить плоский тайл земли (4 вершины) на сетку, чтобы изгиб мира был плавным, а надписи/здания/персонаж лежали на земле.")]
    public bool SubdivideGroundTiles = true;
    [Range(2, 32)] public int GroundGridResolution = 12;
}
