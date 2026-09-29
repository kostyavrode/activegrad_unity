using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Глобальный линейный туман для шейдера ActiveGrad/StylizedMatcap (_AG_LinearFogColor/_AG_LinearFogParams).
/// Цвет и дистанции задаются здесь одни на всю сцену, в материалах — только ползунок Fog Intensity.
/// Опционально синхронизирует встроенный туман Unity (Linear), чтобы URP Lit объекты совпадали по тону.
/// Если в SampleScene компонента нет — создаётся автоматически из текущих RenderSettings.
/// Чтобы видеть туман в редакторе без Play — добавь компонент на любой объект сцены.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class StylizedFogController : MonoBehaviour
{
    private const string MainSceneName = "SampleScene";
    private const float DefaultStart = 60f;
    private const float DefaultEnd = 160f;

    private static readonly int FogColorId = Shader.PropertyToID("_AG_LinearFogColor");
    private static readonly int FogParamsId = Shader.PropertyToID("_AG_LinearFogParams");

    [SerializeField] private Color _color = new(0.8f, 0.87f, 0.95f, 1f);
    [SerializeField, Min(0f)] private float _start = DefaultStart;
    [SerializeField, Min(0f)] private float _end = DefaultEnd;
    [SerializeField] private bool _syncRenderSettingsFog = true;

    public Color Color
    {
        get => _color;
        set { _color = value; Apply(); }
    }

    public float Start
    {
        get => _start;
        set { _start = Mathf.Max(0f, value); Apply(); }
    }

    public float End
    {
        get => _end;
        set { _end = Mathf.Max(0f, value); Apply(); }
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

        if (FindAnyObjectByType<StylizedFogController>() != null)
            return;

        var go = new GameObject("[StylizedFog]");
        if (go.scene != scene)
            SceneManager.MoveGameObjectToScene(go, scene);

        var controller = go.AddComponent<StylizedFogController>();
        controller._color = RenderSettings.fogColor;
        if (RenderSettings.fogMode == FogMode.Linear && RenderSettings.fogEndDistance > RenderSettings.fogStartDistance)
        {
            controller._start = RenderSettings.fogStartDistance;
            controller._end = RenderSettings.fogEndDistance;
        }
        controller.Apply();
    }

    private void OnEnable() => Apply();

    private void OnValidate() => Apply();

    private void OnDisable()
    {
        Shader.SetGlobalVector(FogParamsId, Vector4.zero);
    }

    public void Apply()
    {
        if (!isActiveAndEnabled)
            return;

        var end = Mathf.Max(_end, _start + 0.01f);
        Shader.SetGlobalColor(FogColorId, _color);
        Shader.SetGlobalVector(FogParamsId, new Vector4(_start, end, 1f / (end - _start), 1f));

        if (!_syncRenderSettingsFog)
            return;

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = _color;
        RenderSettings.fogStartDistance = _start;
        RenderSettings.fogEndDistance = end;
    }
}
