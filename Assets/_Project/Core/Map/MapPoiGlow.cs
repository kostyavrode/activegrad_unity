using UnityEngine;

[DisallowMultipleComponent]
public class MapPoiGlow : MonoBehaviour
{
    [SerializeField] private Material _material;
    [SerializeField] private float _radius = 0.55f;
    [SerializeField] private float _height = 1.1f;
    [SerializeField] private bool _faceCamera = true;

    private Transform _glowTransform;
    private Camera _camera;

    private void Awake()
    {
        EnsureGlow();
    }

    private void LateUpdate()
    {
        if (_glowTransform == null)
            return;

        _glowTransform.localPosition = new Vector3(0f, _height, 0f);

        if (!_faceCamera)
            return;

        _camera ??= Camera.main;
        if (_camera == null)
            return;

        var toCamera = _camera.transform.position - _glowTransform.position;
        toCamera.y = 0f;
        if (toCamera.sqrMagnitude > 0.001f)
            _glowTransform.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
    }

    private void EnsureGlow()
    {
        if (_glowTransform != null)
            return;

        if (_material == null)
            _material = Resources.Load<Material>("Map/Materials/POIGlow");

        var glowObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        glowObject.name = "POIGlow";
        glowObject.transform.SetParent(transform, false);
        glowObject.transform.localScale = Vector3.one * _radius;

        var collider = glowObject.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        var renderer = glowObject.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = _material;
        MapMeshRenderOptimizer.Apply(renderer, receiveShadows: false);

        _glowTransform = glowObject.transform;
    }
}
