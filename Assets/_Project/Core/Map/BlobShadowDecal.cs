using UnityEngine;

[DisallowMultipleComponent]
public class BlobShadowDecal : MonoBehaviour
{
    [SerializeField] private Material _material;
    [SerializeField] private float _size = 1.4f;
    [SerializeField] private float _heightOffset = 0.04f;
    [SerializeField] private bool _followTarget = true;

    private Transform _shadowTransform;
    private Transform _followTransform;

    public void Initialize(Transform followTransform, Material material = null, float size = 1.4f)
    {
        _followTransform = followTransform;
        if (material != null)
            _material = material;
        _size = size;
        EnsureShadow();
    }

    private void Awake()
    {
        _followTransform = transform;
        EnsureShadow();
    }

    private void LateUpdate()
    {
        if (!_followTarget || _shadowTransform == null || _followTransform == null)
            return;

        var position = _followTransform.position;
        position.y += _heightOffset;
        _shadowTransform.position = position;
    }

    private void EnsureShadow()
    {
        if (_shadowTransform != null)
            return;

        if (_material == null)
            _material = Resources.Load<Material>("Map/Materials/BlobShadow");

        var shadowObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
        shadowObject.name = "BlobShadow";
        shadowObject.transform.SetParent(transform, false);
        shadowObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        shadowObject.transform.localScale = new Vector3(_size, _size, 1f);

        var collider = shadowObject.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        var renderer = shadowObject.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = _material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

        _shadowTransform = shadowObject.transform;
    }
}
