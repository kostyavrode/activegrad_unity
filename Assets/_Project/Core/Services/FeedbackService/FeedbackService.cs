using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class FeedbackService : IFeedbackService
{
    private readonly AudioManager _audioManager;
    private readonly HapticService _haptics;
    private readonly FeedbackConfig _config;
    private readonly Dictionary<FeedbackType, FeedbackConfig.Entry> _defaults;

    public FeedbackService(
        AudioManager audioManager,
        HapticService haptics,
        [InjectOptional] FeedbackConfig config)
    {
        _audioManager = audioManager;
        _haptics = haptics;
        _config = config;
        _defaults = BuildDefaults();
    }

    public void Play(FeedbackType type, Vector3? worldOrScreenPos = null)
    {
        // worldOrScreenPos зарезервирован под визуальные эффекты
        FeedbackConfig.Entry entry = null;
        if (_config == null || !_config.TryGet(type, out entry))
            _defaults.TryGetValue(type, out entry);

        if (entry == null)
            return;

        var clip = entry.clip != null ? entry.clip : _audioManager.DefaultUiClickClip;
        if (clip != null)
            _audioManager.PlaySfx(clip, entry.volume, JitterPitch(entry.pitch, entry.pitchJitter));

        if (entry.hapticEnabled)
            _haptics.Play(entry.haptic);

        if (type == FeedbackType.Reward)
            _audioManager.Duck(0.4f, 1.0f);
        else if (type == FeedbackType.LevelUp)
            _audioManager.Duck(0.3f, 1.6f);
    }

    public void Haptic(HapticType type)
    {
        _haptics.Play(type);
    }

    public void PlaySfx(AudioClip clip, float pitchJitter = 0.05f, float pitch = 1f)
    {
        if (clip == null)
            return;

        _audioManager.PlaySfx(clip, 1f, JitterPitch(pitch, pitchJitter));
    }

    private static float JitterPitch(float pitch, float jitter)
    {
        if (jitter <= 0f)
            return pitch;
        return pitch * (1f + Random.Range(-jitter, jitter));
    }

    private static Dictionary<FeedbackType, FeedbackConfig.Entry> BuildDefaults()
    {
        return new Dictionary<FeedbackType, FeedbackConfig.Entry>
        {
            { FeedbackType.Tap, Make(FeedbackType.Tap, 1.0f, 1f, HapticType.Light) },
            { FeedbackType.Close, Make(FeedbackType.Close, 0.9f, 1f, HapticType.Light) },
            { FeedbackType.Success, Make(FeedbackType.Success, 1.15f, 1f, HapticType.Success) },
            { FeedbackType.Error, Make(FeedbackType.Error, 0.75f, 1f, HapticType.Error) },
            { FeedbackType.Reward, Make(FeedbackType.Reward, 1.2f, 1f, HapticType.Medium) },
            { FeedbackType.CoinTick, Make(FeedbackType.CoinTick, 1.5f, 0.35f, HapticType.Light, 0.08f) },
            { FeedbackType.LevelUp, Make(FeedbackType.LevelUp, 1.3f, 1f, HapticType.Heavy) },
            { FeedbackType.Hit, Make(FeedbackType.Hit, 1.1f, 0.9f, HapticType.Light) },
            { FeedbackType.Perfect, Make(FeedbackType.Perfect, 1.35f, 1f, HapticType.Medium) },
            { FeedbackType.Miss, Make(FeedbackType.Miss, 0.7f, 0.8f, HapticType.Error) },
            { FeedbackType.Discover, Make(FeedbackType.Discover, 1.25f, 1f, HapticType.Medium) },
        };
    }

    private static FeedbackConfig.Entry Make(FeedbackType type, float pitch, float volume, HapticType haptic, float jitter = 0.05f)
    {
        return new FeedbackConfig.Entry
        {
            type = type,
            clip = null,
            volume = volume,
            pitch = pitch,
            pitchJitter = jitter,
            haptic = haptic,
            hapticEnabled = true
        };
    }
}
