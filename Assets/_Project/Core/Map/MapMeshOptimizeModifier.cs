using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.MeshGeneration.Modifiers;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "ActiveGrad/Map/Map Mesh Optimize Modifier")]
public class MapMeshOptimizeModifier : GameObjectModifier
{
    public override void Run(VectorEntity ve, UnityTile tile)
    {
        if (ve?.MeshRenderer == null)
            return;

        MapMeshRenderOptimizer.Apply(ve.MeshRenderer);
    }
}

public static class MapMeshRenderOptimizer
{
    public static void Apply(Renderer renderer)
    {
        if (renderer == null)
            return;

        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        renderer.allowOcclusionWhenDynamic = false;
    }

    public static void ApplyHierarchy(Transform root)
    {
        if (root == null)
            return;

        var renderers = root.GetComponentsInChildren<Renderer>(true);
        for (var i = 0; i < renderers.Length; i++)
            Apply(renderers[i]);
    }
}
