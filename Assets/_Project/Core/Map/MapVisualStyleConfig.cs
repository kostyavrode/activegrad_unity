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
}
