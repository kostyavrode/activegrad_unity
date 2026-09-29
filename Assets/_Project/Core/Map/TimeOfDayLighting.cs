using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Подкрашивает основной направленный свет по локальному времени суток
/// (утро — тёплый, полдень — белый, вечер — оранжевый, ночь — холодный и тусклее).
/// Пересчёт раз в минуту с плавным переходом. Автоматически создаётся в SampleScene.
/// </summary>
[DisallowMultipleComponent]
public class TimeOfDayLighting : MonoBehaviour
{
    private const string MainSceneName = "SampleScene";
    private const float UpdateInterval = 60f;
    private const float BlendDuration = 4f;

    private struct Key
    {
        public readonly float Hour;
        public readonly Color Tint;
        public readonly float Intensity;

        public Key(float hour, Color tint, float intensity)
        {
            Hour = hour;
            Tint = tint;
            Intensity = intensity;
        }
    }

    // Ночь — не темнота, а мягкий «голубой час»: игра про прогулки, в неё играют и вечером.
    private static readonly Key[] Keys =
    {
        new Key(0f,    new Color(0.80f, 0.86f, 1.00f), 0.80f),
        new Key(5f,    new Color(0.82f, 0.87f, 1.00f), 0.82f),
        new Key(7f,    new Color(1.00f, 0.88f, 0.76f), 0.92f),
        new Key(9.5f,  new Color(1.00f, 0.96f, 0.90f), 0.98f),
        new Key(12.5f, new Color(1.00f, 0.99f, 0.97f), 1.00f),
        new Key(16f,   new Color(1.00f, 0.96f, 0.89f), 1.00f),
        new Key(18.5f, new Color(1.00f, 0.84f, 0.68f), 0.94f),
        new Key(20.5f, new Color(0.90f, 0.84f, 0.98f), 0.86f),
        new Key(22f,   new Color(0.82f, 0.86f, 1.00f), 0.82f),
        new Key(24f,   new Color(0.80f, 0.86f, 1.00f), 0.80f),
    };

    private Light _light;
    private Color _baseColor;
    private float _baseIntensity;
    private Color _fromColor;
    private Color _toColor;
    private float _fromIntensity;
    private float _toIntensity;
    private float _blend = 1f;
    private float _nextEvaluate;

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

        if (FindAnyObjectByType<TimeOfDayLighting>() != null)
            return;

        var go = new GameObject("[TimeOfDayLighting]");
        if (go.scene != scene)
            SceneManager.MoveGameObjectToScene(go, scene);
        go.AddComponent<TimeOfDayLighting>();
    }

    private void Start()
    {
        _light = FindSun();
        if (_light == null)
        {
            enabled = false;
            return;
        }

        _baseColor = _light.color;
        _baseIntensity = _light.intensity;

        Evaluate(out _toColor, out _toIntensity);
        _light.color = _toColor;
        _light.intensity = _toIntensity;
        _blend = 1f;
        _nextEvaluate = Time.unscaledTime + UpdateInterval;
    }

    private void Update()
    {
        if (_light == null)
        {
            enabled = false;
            return;
        }

        if (Time.unscaledTime >= _nextEvaluate)
        {
            _nextEvaluate = Time.unscaledTime + UpdateInterval;
            _fromColor = _light.color;
            _fromIntensity = _light.intensity;
            Evaluate(out _toColor, out _toIntensity);
            _blend = 0f;
        }

        if (_blend < 1f)
        {
            _blend = Mathf.Min(1f, _blend + Time.unscaledDeltaTime / BlendDuration);
            var s = Mathf.SmoothStep(0f, 1f, _blend);
            _light.color = Color.Lerp(_fromColor, _toColor, s);
            _light.intensity = Mathf.Lerp(_fromIntensity, _toIntensity, s);
        }
    }

    private void Evaluate(out Color color, out float intensity)
    {
        var hour = (float)DateTime.Now.TimeOfDay.TotalHours;
        Sample(hour, out var tint, out var mul);

        color = new Color(_baseColor.r * tint.r, _baseColor.g * tint.g, _baseColor.b * tint.b, 1f);
        intensity = _baseIntensity * mul;
    }

    private static void Sample(float hour, out Color tint, out float intensity)
    {
        hour = Mathf.Repeat(hour, 24f);
        for (var i = 0; i < Keys.Length - 1; i++)
        {
            var a = Keys[i];
            var b = Keys[i + 1];
            if (hour >= a.Hour && hour <= b.Hour)
            {
                var t = Mathf.InverseLerp(a.Hour, b.Hour, hour);
                tint = Color.Lerp(a.Tint, b.Tint, t);
                intensity = Mathf.Lerp(a.Intensity, b.Intensity, t);
                return;
            }
        }

        tint = Keys[0].Tint;
        intensity = Keys[0].Intensity;
    }

    private static Light FindSun()
    {
        var sun = RenderSettings.sun;
        if (sun != null && sun.type == LightType.Directional && sun.isActiveAndEnabled)
            return sun;

        var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        Light best = null;
        for (var i = 0; i < lights.Length; i++)
        {
            var l = lights[i];
            if (l == null || l.type != LightType.Directional || !l.isActiveAndEnabled)
                continue;
            if (best == null || l.intensity > best.intensity)
                best = l;
        }
        return best;
    }
}
