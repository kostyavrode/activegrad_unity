using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Пост-обработка для карты (SampleScene): Bloom, Vignette, ColorAdjustments, Tonemapping(Neutral).
/// Если глобального Volume нет — создаёт свой с runtime-профилем.
/// Если есть — добавляет в его runtime-копию профиля только ОТСУТСТВУЮЩИЕ эффекты (ассет не меняется).
/// Переключатель: MapPostFxBootstrap.Enabled (PlayerPrefs "PostFxEnabled").
/// </summary>
public static class MapPostFxBootstrap
{
    private const string MainSceneName = "SampleScene";
    private const string PrefsKey = "PostFxEnabled";
    private const string RuntimeVolumeName = "[RuntimePostFx]";

    private static Volume _runtimeVolume;
    private static int _patchedVolumeId;

    public static bool Enabled
    {
        get => PlayerPrefs.GetInt(PrefsKey, 1) != 0;
        set
        {
            PlayerPrefs.SetInt(PrefsKey, value ? 1 : 0);
            PlayerPrefs.Save();
            ApplyToActiveScene();
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplyToActiveScene();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Apply(scene);

    public static void ApplyToActiveScene() => Apply(SceneManager.GetActiveScene());

    private static void Apply(Scene scene)
    {
        if (!scene.IsValid() || scene.name != MainSceneName)
            return;

        var enabled = Enabled;
        var volume = FindGlobalVolume();

        if (volume == null)
        {
            if (enabled)
                volume = CreateRuntimeVolume(scene);
        }
        else if (volume != _runtimeVolume && _patchedVolumeId != volume.GetInstanceID())
        {
            EnsureOverrides(volume.profile); // runtime-копия sharedProfile
            _patchedVolumeId = volume.GetInstanceID();
        }

        if (_runtimeVolume != null)
            _runtimeVolume.enabled = enabled;

        SetCameraPostProcessing(enabled);
    }

    private static Volume FindGlobalVolume()
    {
        var volumes = Object.FindObjectsByType<Volume>(FindObjectsSortMode.None);
        for (var i = 0; i < volumes.Length; i++)
        {
            var v = volumes[i];
            if (v != null && v.isGlobal && (v.enabled || v == _runtimeVolume))
                return v;
        }
        return null;
    }

    private static Volume CreateRuntimeVolume(Scene scene)
    {
        var go = new GameObject(RuntimeVolumeName);
        if (go.scene != scene)
            SceneManager.MoveGameObjectToScene(go, scene);

        var volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 0f;

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "RuntimeMapPostFx";
        EnsureOverrides(profile);
        volume.sharedProfile = profile;

        _runtimeVolume = volume;
        return volume;
    }

    private static void EnsureOverrides(VolumeProfile profile)
    {
        if (profile == null)
            return;

        if (!profile.Has<Bloom>())
        {
            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.value = 0.6f;
            bloom.threshold.value = 1.0f;
            bloom.highQualityFiltering.value = false;
        }

        if (!profile.Has<Vignette>())
        {
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.value = 0.25f;
            vignette.smoothness.value = 0.4f;
        }

        if (!profile.Has<ColorAdjustments>())
        {
            var color = profile.Add<ColorAdjustments>(true);
            color.postExposure.value = 0f;
            color.contrast.value = 0f;
            color.saturation.value = 8f;
            color.colorFilter.value = new Color(1f, 0.975f, 0.94f, 1f);
        }

        if (!profile.Has<Tonemapping>())
        {
            var tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.value = TonemappingMode.Neutral;
        }
    }

    private static void SetCameraPostProcessing(bool enabled)
    {
        var cam = Camera.main;
        if (cam == null)
            return;

        var data = cam.GetUniversalAdditionalCameraData();
        if (data != null)
            data.renderPostProcessing = enabled;
    }
}
