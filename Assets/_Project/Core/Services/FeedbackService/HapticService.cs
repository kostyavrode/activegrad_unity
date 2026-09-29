using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Вибрация. Android — через Vibrator/VibrationEffect, iOS — Handheld.Vibrate (только сильные типы), Editor — no-op.
/// </summary>
public class HapticService
{
    private const string EnabledPrefsKey = "HapticsEnabled";
    private const float MinInterval = 0.03f;

    private bool _enabled;
    private float _lastHapticTime = -1f;

#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject _vibrator;
    private AndroidJavaClass _vibrationEffectClass;
    private readonly Dictionary<int, AndroidJavaObject> _predefinedEffects = new Dictionary<int, AndroidJavaObject>();
    private int _sdkInt;
    private bool _androidInitialized;
    private bool _androidAvailable;
#endif

    // Никогда не true: ссылка на Handheld.Vibrate нужна, чтобы Unity добавил разрешение VIBRATE в манифест.
    private static bool _forceVibratePermissionReference;

    public HapticService()
    {
        _enabled = PlayerPrefs.GetInt(EnabledPrefsKey, 1) == 1;
    }

    public bool Enabled
    {
        get => _enabled;
        set
        {
            _enabled = value;
            PlayerPrefs.SetInt(EnabledPrefsKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public void Play(HapticType type)
    {
        if (!_enabled)
            return;

        var now = Time.unscaledTime;
        if (_lastHapticTime >= 0f && now - _lastHapticTime < MinInterval)
            return;
        _lastHapticTime = now;

#if UNITY_ANDROID || UNITY_IOS
        if (_forceVibratePermissionReference)
            Handheld.Vibrate();
#endif

#if UNITY_EDITOR
        // no-op
#elif UNITY_ANDROID
        PlayAndroid(type);
#elif UNITY_IOS
        if (type == HapticType.Heavy || type == HapticType.Success || type == HapticType.Error)
            Handheld.Vibrate();
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private void EnsureAndroid()
    {
        if (_androidInitialized)
            return;
        _androidInitialized = true;

        try
        {
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                _sdkInt = version.GetStatic<int>("SDK_INT");

            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            }

            _androidAvailable = _vibrator != null && _vibrator.Call<bool>("hasVibrator");

            if (_androidAvailable && _sdkInt >= 26)
                _vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[HapticService] Android vibrator init failed: " + e.Message);
            _androidAvailable = false;
        }
    }

    private void PlayAndroid(HapticType type)
    {
        EnsureAndroid();
        if (!_androidAvailable)
            return;

        try
        {
            if (_sdkInt >= 29)
            {
                var effect = GetPredefinedEffect(GetPredefinedId(type));
                if (effect != null)
                    _vibrator.Call("vibrate", effect);
            }
            else if (_sdkInt >= 26)
            {
                GetOneShotParams(type, out var ms, out var amplitude);
                using (var effect = _vibrationEffectClass.CallStatic<AndroidJavaObject>("createOneShot", ms, amplitude))
                    _vibrator.Call("vibrate", effect);
            }
            else
            {
                GetOneShotParams(type, out var ms, out _);
                _vibrator.Call("vibrate", ms);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[HapticService] Vibrate failed: " + e.Message);
        }
    }

    private AndroidJavaObject GetPredefinedEffect(int id)
    {
        if (_predefinedEffects.TryGetValue(id, out var cached))
            return cached;

        var effect = _vibrationEffectClass.CallStatic<AndroidJavaObject>("createPredefined", id);
        _predefinedEffects[id] = effect;
        return effect;
    }

    private static int GetPredefinedId(HapticType type)
    {
        switch (type)
        {
            case HapticType.Light: return 2;   // EFFECT_TICK
            case HapticType.Medium: return 0;  // EFFECT_CLICK
            case HapticType.Heavy: return 5;   // EFFECT_HEAVY_CLICK
            case HapticType.Success:
            case HapticType.Error: return 1;   // EFFECT_DOUBLE_CLICK
            default: return 0;
        }
    }

    private static void GetOneShotParams(HapticType type, out long ms, out int amplitude)
    {
        switch (type)
        {
            case HapticType.Light: ms = 10; amplitude = 60; break;
            case HapticType.Medium: ms = 20; amplitude = 120; break;
            case HapticType.Heavy: ms = 35; amplitude = 255; break;
            case HapticType.Success: ms = 30; amplitude = 160; break;
            case HapticType.Error: ms = 45; amplitude = 200; break;
            default: ms = 15; amplitude = 100; break;
        }
    }
#endif
}
