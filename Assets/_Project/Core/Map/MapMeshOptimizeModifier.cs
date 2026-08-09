using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.MeshGeneration.Modifiers;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "ActiveGrad/Map/Map Mesh Optimize Modifier")]
public class MapMeshOptimizeModifier : GameObjectModifier
{
    [SerializeField] private bool _receiveShadows;

    public void Configure(bool receiveShadows)
    {
        _receiveShadows = receiveShadows;
    }

    public override void Run(VectorEntity ve, UnityTile tile)
    {
        if (ve?.MeshRenderer == null)
            return;

        MapMeshRenderOptimizer.Apply(ve.MeshRenderer, _receiveShadows);
    }
}

public static class MapMeshRenderOptimizer
{
    public static void Apply(Renderer renderer, bool receiveShadows = false)
    {
        if (renderer == null)
            return;

        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = receiveShadows;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        renderer.allowOcclusionWhenDynamic = false;
    }

    public static void ApplyHierarchy(Transform root, bool receiveShadows = false)
    {
        if (root == null)
            return;

        var renderers = root.GetComponentsInChildren<Renderer>(true);
        for (var i = 0; i < renderers.Length; i++)
            Apply(renderers[i], receiveShadows);
    }
}
