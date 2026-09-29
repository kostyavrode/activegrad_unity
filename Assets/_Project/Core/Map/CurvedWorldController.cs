using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// «Круглый» мир для шейдера ActiveGrad/StylizedMatcap: всё дальше плоского радиуса от персонажа
/// (без персонажа — от точки, куда смотрит камера) плавно уходит вниз (y -= (d - r)^2 * strength). Вблизи персонажа мир плоский,
/// поэтому объекты на обычных URP-шейдерах (персонаж, маркеры рядом) не расходятся с землёй.
/// Расширяет culling-пирамиду камеры, чтобы загнутые вниз дальние объекты не пропадали у горизонта.
/// В SampleScene создаётся автоматически; добавь на объект сцены, чтобы видеть эффект в редакторе.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[DefaultExecutionOrder(-1000)] // раньше SpawnOnMap/GameEventService: они используют центр изгиба в своём Update
public class CurvedWorldController : MonoBehaviour
{
    private const string MainSceneName = "SampleScene";

    private static readonly int CurveCenterId = Shader.PropertyToID("_AG_CurveCenter");
    private static readonly int CurveParamsId = Shader.PropertyToID("_AG_CurveParams");

    [SerializeField] private bool _enabled = true;
    [SerializeField, Min(0f)] private float _flatRadius = 45f;
    [SerializeField, Range(0f, 0.01f)] private float _strength = 0.0022f;
    [SerializeField, Range(0f, 30f)] private float _cullingPaddingDegrees = 12f;
    [SerializeField] private float _groundHeight;
    [SerializeField] private Camera _camera;

    // Текущие параметры изгиба — для объектов на обычных шейдерах (маркеры, события мини-игр).
    private static bool _curveActive;
    private static Vector3 _curveCenter;
    private static float _curveRadius;
    private static float _curveStrength;

    /// <summary>
    /// Насколько мир опущен в этой мировой точке (та же формула, что в шейдере StylizedMatcap).
    /// </summary>
    public static float DropAt(Vector3 worldPosition)
    {
        if (!_curveActive)
            return 0f;

        var dx = worldPosition.x - _curveCenter.x;
        var dz = worldPosition.z - _curveCenter.z;
        var t = Mathf.Max(0f, Mathf.Sqrt(dx * dx + dz * dz) - _curveRadius);
        return t * t * _curveStrength;
    }

    /// <summary>
    /// Опускает объект вместе с изогнутой землёй. Вызывать сразу после того, как
    /// позиция объекта выставлена заново (каждый кадр), иначе смещение накопится.
    /// </summary>
    public static void Bend(Transform target)
    {
        if (target == null || !_curveActive)
            return;

        var drop = DropAt(target.position);
        if (drop > 0f)
            target.position += Vector3.down * drop;
    }

    public bool CurveEnabled
    {
        get => _enabled;
        set { _enabled = value; Apply(); }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryAttach(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => TryAttach(scene);

    private static void TryAttach(Scene scene)
    {
        if (!scene.IsValid() || scene.name != MainSceneName)
            return;

        if (FindAnyObjectByType<CurvedWorldController>() != null)
            return;

        var go = new GameObject("[CurvedWorld]");
        if (go.scene != scene)
            SceneManager.MoveGameObjectToScene(go, scene);
        go.AddComponent<CurvedWorldController>();
    }

    private void OnEnable() => Apply();

    private void OnValidate() => Apply();

    private void OnDisable()
    {
        Shader.SetGlobalVector(CurveCenterId, Vector4.zero);
        _curveActive = false;
        var cam = ResolveCamera();
        if (cam != null)
            cam.ResetCullingMatrix();
    }

    // Центр изгиба обновляем в начале кадра: шейдер и маркеры используют одно и то же значение.
    private void Update() => Apply();

    // Culling-матрица — после того как камера сдвинулась в этом кадре.
    private void LateUpdate() => UpdateCulling(ResolveCamera());

    private void Apply()
    {
        if (!isActiveAndEnabled)
            return;

        var cam = ResolveCamera();
        if (!_enabled || cam == null)
        {
            _curveActive = false;
            Shader.SetGlobalVector(CurveCenterId, Vector4.zero);
            if (cam != null)
                cam.ResetCullingMatrix();
            return;
        }

        var center = FocusPoint(cam);
        Shader.SetGlobalVector(CurveCenterId, new Vector4(center.x, center.y, center.z, 1f));
        Shader.SetGlobalVector(CurveParamsId, new Vector4(_flatRadius, _strength, 0f, 0f));

        _curveActive = true;
        _curveCenter = center;
        _curveRadius = _flatRadius;
        _curveStrength = _strength;

        UpdateCulling(cam);
    }

    private void UpdateCulling(Camera cam)
    {
        if (cam == null)
            return;

        if (!_enabled)
        {
            cam.ResetCullingMatrix();
            return;
        }

        if (_cullingPaddingDegrees > 0f && !cam.orthographic)
        {
            var fov = Mathf.Min(cam.fieldOfView + _cullingPaddingDegrees * 2f, 170f);
            var projection = Matrix4x4.Perspective(fov, cam.aspect, cam.nearClipPlane, cam.farClipPlane);
            cam.cullingMatrix = projection * cam.worldToCameraMatrix;
        }
        else
        {
            cam.ResetCullingMatrix();
        }
    }

    private Camera ResolveCamera()
    {
        if (_camera == null)
            _camera = Camera.main;
        return _camera;
    }

    // Центр изгиба — под персонажем: он всегда в плоской зоне и не «повисает» над загнутой землёй.
    // Камера смотрит выше персонажа (CameraController.height) и может наклоняться к горизонту,
    // поэтому луч из камеры даёт точку далеко за ним — её используем только если персонажа нет.
    private Vector3 FocusPoint(Camera cam)
    {
        var player = CharacterController3D.Active;
        if (player != null)
        {
            var p0 = player.transform.position;
            return new Vector3(p0.x, _groundHeight, p0.z);
        }

        var ray = new Ray(cam.transform.position, cam.transform.forward);
        var plane = new Plane(Vector3.up, new Vector3(0f, _groundHeight, 0f));
        if (plane.Raycast(ray, out var distance) && distance < cam.farClipPlane)
            return ray.GetPoint(distance);

        var p = cam.transform.position;
        return new Vector3(p.x, _groundHeight, p.z);
    }
}
