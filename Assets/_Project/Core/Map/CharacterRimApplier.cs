using UnityEngine;

[DisallowMultipleComponent]
public class CharacterRimApplier : MonoBehaviour
{
    [SerializeField] private Color _rimColor = new(1f, 0.92f, 0.75f, 1f);
    [SerializeField] private float _rimStrength = 0.55f;
    [SerializeField] private float _rimPower = 3.5f;

    private static Shader _rimShader;

    private void Awake()
    {
        ApplyToHierarchy(transform);
    }

    public static void ApplyToHierarchy(Transform root, Color? rimColor = null, float rimStrength = 0.55f)
    {
        _rimShader ??= Shader.Find("ActiveGrad/CharacterRimLit");
        if (_rimShader == null)
            return;

        var renderers = root.GetComponentsInChildren<Renderer>(true);
        for (var i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            if (renderer is ParticleSystemRenderer)
                continue;

            var sourceMaterials = renderer.sharedMaterials;
            var converted = new Material[sourceMaterials.Length];

            for (var m = 0; m < sourceMaterials.Length; m++)
            {
                var source = sourceMaterials[m];
                if (source == null)
                    continue;

                var rimMaterial = new Material(_rimShader)
                {
                    name = source.name + "_Rim"
                };

                if (source.HasProperty("_BaseColor"))
                    rimMaterial.SetColor("_BaseColor", source.GetColor("_BaseColor"));
                else if (source.HasProperty("_Color"))
                    rimMaterial.SetColor("_BaseColor", source.GetColor("_Color"));
                else
                    rimMaterial.SetColor("_BaseColor", Color.white);

                if (source.HasProperty("_BaseMap"))
                    rimMaterial.SetTexture("_MainTex", source.GetTexture("_BaseMap"));
                else if (source.HasProperty("_MainTex"))
                    rimMaterial.SetTexture("_MainTex", source.GetTexture("_MainTex"));

                rimMaterial.SetColor("_RimColor", rimColor ?? new Color(1f, 0.92f, 0.75f, 1f));
                rimMaterial.SetFloat("_RimStrength", rimStrength);
                rimMaterial.SetFloat("_RimPower", 3.5f);
                converted[m] = rimMaterial;
            }

            renderer.sharedMaterials = converted;
        }
    }

    private void ApplyToHierarchy(Transform root)
    {
        ApplyToHierarchy(root, _rimColor, _rimStrength);
    }
}
