using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.MeshGeneration.Modifiers;
using UnityEngine;

[CreateAssetMenu(menuName = "ActiveGrad/Map/Stylized Material Modifier")]
public class MapStylizedMaterialModifier : GameObjectModifier
{
    [SerializeField] private Material[] _materials;

    public void Configure(Material[] materials)
    {
        _materials = materials;
    }

    public override void Run(VectorEntity ve, UnityTile tile)
    {
        if (ve?.MeshRenderer == null || _materials == null || _materials.Length == 0)
            return;

        var subMeshCount = ve.MeshFilter != null && ve.MeshFilter.sharedMesh != null
            ? ve.MeshFilter.sharedMesh.subMeshCount
            : 1;

        var count = Mathf.Min(subMeshCount, _materials.Length);
        var mats = new Material[count];
        for (var i = 0; i < count; i++)
            mats[i] = _materials[Mathf.Min(i, _materials.Length - 1)];

        ve.MeshRenderer.sharedMaterials = mats;
        MapMeshRenderOptimizer.Apply(ve.MeshRenderer, receiveShadows: false);
    }
}
