using UnityEngine;
using UnityEngine.Rendering;

public static class MapShadowHelper
{
    public static void EnableCastShadows(Transform root)
    {
        if (root == null)
            return;

        var renderers = root.GetComponentsInChildren<Renderer>(true);
        for (var i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            if (renderer == null)
                continue;

            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }
    }
}
