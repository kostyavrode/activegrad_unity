using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Zenject;

public class AudioManager : IInitializable
{
    private const int SfxPoolSize = 6;
    private const float SfxClipCooldown = 0.04f;
    private const float MusicCrossfadeDuration = 0.5f;

    private readonly AudioSettings _settings;
    private readonly AudioSource _musicSource;
    private readonly AudioSource _sfxSource;
    private readonly AudioClip _defaultUiClickClip;
    private readonly AudioClip _defaultUiCloseClip;

    // Музыка: два источника для кроссфейда. Итоговая громкость = настройки * вес * дак.
    private readonly AudioSource[] _musicSources = new AudioSource[2];
    private readonly float[] _musicWeights = { 1f, 0f };
    private readonly Tween[] _musicFadeTweens = new Tween[2];
    private int _activeMusicIndex;
    private float _duckFactor = 1f;
    private Sequence _duckSequence;

    // Пул SFX
    private readonly List<AudioSource> _sfxPool = new List<AudioSource>(SfxPoolSize);
    private readonly List<float> _sfxPoolVolumeScales = new List<float>(SfxPoolSize);
    private int _nextPoolIndex;
    private readonly Dictionary<AudioClip, float> _lastPlayTimeByClip = new Dictionary<AudioClip, float>();

    private bool _initialized;

    public AudioClip DefaultUiClickClip => _defaultUiClickClip;
    public AudioClip DefaultUiCloseClip => _defaultUiCloseClip;

    public AudioManager(
        AudioSettings settings,
        [Inject(Id = "Music")] AudioSource musicSource,
        [Inject(Id = "Sfx")] AudioSource sfxSource,
        [InjectOptional(Id = "UiClick")] AudioClip defaultUiClickClip,
        [InjectOptional(Id = "UiClose")] AudioClip defaultUiCloseClip)
    {
        _settings = settings;
        _musicSource = musicSource;
        _sfxSource = sfxSource;
        _defaultUiClickClip = defaultUiClickClip;
        _defaultUiCloseClip = defaultUiCloseClip;
    }

    public void Initialize()
    {
        EnsureInitialized();
        ApplyVolumes();
    }

    private void EnsureInitialized()
    {
        if (_initialized)
            return;
        _initialized = true;

        _musicSource.loop = true;
        if (_sfxSource != _musicSource)
            _sfxSource.loop = false;

        // Второй музыкальный источник рядом с основным
        _musicSources[0] = _musicSource;
        var secondMusic = _musicSource.gameObject.AddComponent<AudioSource>();
        CopySourceSettings(_musicSource, secondMusic);
        secondMusic.loop = true;
        secondMusic.playOnAwake = false;
        _musicSources[1] = secondMusic;
        _activeMusicIndex = 0;
        _musicWeights[0] = 1f;
        _musicWeights[1] = 0f;

        // Пул SFX как дочерние объекты Sfx-источника
        for (var i = 0; i < SfxPoolSize; i++)
        {
            var go = new GameObject("SfxPool_" + i);
            go.transform.SetParent(_sfxSource.transform, false);
            var source = go.AddComponent<AudioSource>();
            CopySourceSettings(_sfxSource, source);
            source.loop = false;
            source.playOnAwake = false;
            _sfxPool.Add(source);
            _sfxPoolVolumeScales.Add(1f);
        }
    }

    private static void CopySourceSettings(AudioSource from, AudioSource to)
    {
        to.outputAudioMixerGroup = from.outputAudioMixerGroup;
        to.spatialBlend = from.spatialBlend;
        to.priority = from.priority;
        to.bypassEffects = from.bypassEffects;
        to.bypassListenerEffects = from.bypassListenerEffects;
        to.bypassReverbZones = from.bypassReverbZones;
        to.ignoreListenerPause = from.ignoreListenerPause;
    }

    public void SetMusicVolume(float value)
    {
        _settings.SetMusicVolume(value);
        ApplyVolumes();
    }

    public void SetSfxVolume(float value)
    {
        _settings.SetSfxVolume(value);
        ApplyVolumes();
    }

    public void SetMusicMuted(bool isMuted)
    {
        _settings.SetMusicMuted(isMuted);
        ApplyVolumes();
    }

    public void SetSfxMuted(bool isMuted)
    {
        _settings.SetSfxMuted(isMuted);
        ApplyVolumes();
    }

    public void PlayMusic(AudioClip clip, bool restartIfSameClip = false)
    {
        if (clip == null)
            return;

        EnsureInitialized();

        var active = _musicSources[_activeMusicIndex];
        var isSameClip = active.clip == clip;
        if (isSameClip && active.isPlaying && !restartIfSameClip)
            return;

        if (isSameClip || !active.isPlaying)
        {
            // Нечего кроссфейдить — просто запускаем на активном источнике
            var other = 1 - _activeMusicIndex;
            KillMusicFade(other);
            _musicSources[other].Stop();
            _musicWeights[other] = 0f;

            KillMusicFade(_activeMusicIndex);
            active.clip = clip;
            if (!isSameClip)
                _musicWeights[_activeMusicIndex] = 0f; // плавный fade-in из тишины
            active.Play();
            FadeMusicWeight(_activeMusicIndex, 1f, isSameClip ? 0f : MusicCrossfadeDuration, null);
            ApplyVolumes();
            return;
        }

        // Кроссфейд: старый затухает, новый нарастает
        var oldIndex = _activeMusicIndex;
        var newIndex = 1 - oldIndex;
        var newSource = _musicSources[newIndex];

        KillMusicFade(newIndex);
        newSource.clip = clip;
        newSource.loop = true;
        _musicWeights[newIndex] = 0f;
        newSource.Play();
        _activeMusicIndex = newIndex;

        var oldSource = _musicSources[oldIndex];
        FadeMusicWeight(oldIndex, 0f, MusicCrossfadeDuration, () => oldSource.Stop());
        FadeMusicWeight(newIndex, 1f, MusicCrossfadeDuration, null);
        ApplyVolumes();
    }

    public void StopMusic()
    {
        EnsureInitialized();
        for (var i = 0; i < _musicSources.Length; i++)
        {
            KillMusicFade(i);
            _musicSources[i].Stop();
            _musicWeights[i] = i == _activeMusicIndex ? 1f : 0f;
        }
        ApplyVolumes();
    }

    /// <summary>Временно приглушить музыку (например, на награду/левелап) и вернуть обратно.</summary>
    public void Duck(float amount = 0.35f, float duration = 1.2f)
    {
        var target = Mathf.Clamp01(amount);
        var fadeTime = Mathf.Min(0.15f, duration * 0.25f);
        var hold = Mathf.Max(0f, duration - fadeTime * 3f);

        _duckSequence?.Kill();
        _duckSequence = DOTween.Sequence()
            .Append(DOTween.To(() => _duckFactor, x => { _duckFactor = x; ApplyMusicVolumes(); }, target, fadeTime))
            .AppendInterval(hold)
            .Append(DOTween.To(() => _duckFactor, x => { _duckFactor = x; ApplyMusicVolumes(); }, 1f, fadeTime * 2f))
            .OnKill(() => { _duckFactor = 1f; ApplyMusicVolumes(); })
            .SetUpdate(true);
    }

    public void PlaySfx(AudioClip clip, float volumeScale = 1f)
    {
        PlaySfx(clip, volumeScale, 1f);
    }

    /// <summary>Проиграть клип через пул SFX с заданным pitch.</summary>
    public void PlaySfx(AudioClip clip, float volumeScale, float pitch)
    {
        if (clip == null)
            return;

        EnsureInitialized();

        // Анти-спам: один и тот же клип не чаще раза в 40мс
        var now = Time.unscaledTime;
        if (_lastPlayTimeByClip.TryGetValue(clip, out var lastTime) && now - lastTime < SfxClipCooldown)
            return;
        _lastPlayTimeByClip[clip] = now;

        var index = GetFreePoolIndex();
        var source = _sfxPool[index];
        var scale = Mathf.Clamp01(volumeScale);
        _sfxPoolVolumeScales[index] = scale;

        source.Stop();
        source.clip = clip;
        source.pitch = Mathf.Clamp(pitch, 0.1f, 3f);
        source.volume = GetSfxVolume() * scale;
        source.Play();
    }

    public void PlayUiClick(AudioClip clip = null)
    {
        var targetClip = clip != null ? clip : _defaultUiClickClip;
        if (targetClip == null)
            return;

        PlaySfx(targetClip);
    }

    public void PlayUiClose(AudioClip clip = null)
    {
        var targetClip = clip != null ? clip : _defaultUiCloseClip;
        if (targetClip == null)
        {
            PlayUiClick();
            return;
        }

        PlaySfx(targetClip);
    }

    private int GetFreePoolIndex()
    {
        for (var i = 0; i < _sfxPool.Count; i++)
        {
            var idx = (_nextPoolIndex + i) % _sfxPool.Count;
            if (_sfxPool[idx] != null && !_sfxPool[idx].isPlaying)
            {
                _nextPoolIndex = (idx + 1) % _sfxPool.Count;
                return idx;
            }
        }

        // Все заняты — перебиваем по кругу (самый старый)
        var stolen = _nextPoolIndex;
        _nextPoolIndex = (_nextPoolIndex + 1) % _sfxPool.Count;
        return stolen;
    }

    private void FadeMusicWeight(int index, float target, float duration, TweenCallback onComplete)
    {
        KillMusicFade(index);
        if (duration <= 0f)
        {
            _musicWeights[index] = target;
            ApplyMusicVolumes();
            onComplete?.Invoke();
            return;
        }

        var tween = DOTween.To(() => _musicWeights[index], x => { _musicWeights[index] = x; ApplyMusicVolumes(); }, target, duration)
            .SetEase(Ease.Linear)
            .SetUpdate(true);
        if (onComplete != null)
            tween.OnComplete(onComplete);
        _musicFadeTweens[index] = tween;
    }

    private void KillMusicFade(int index)
    {
        if (_musicFadeTweens[index] != null && _musicFadeTweens[index].IsActive())
            _musicFadeTweens[index].Kill();
        _musicFadeTweens[index] = null;
    }

    private float GetMusicVolume() => _settings.MusicMuted ? 0f : _settings.MusicVolume;
    private float GetSfxVolume() => _settings.SfxMuted ? 0f : _settings.SfxVolume;

    private void ApplyMusicVolumes()
    {
        var baseVolume = GetMusicVolume() * _duckFactor;
        for (var i = 0; i < _musicSources.Length; i++)
        {
            if (_musicSources[i] != null)
                _musicSources[i].volume = baseVolume * _musicWeights[i];
        }

        if (_musicSources[0] == null && _musicSource != null)
            _musicSource.volume = baseVolume;
    }

    private void ApplyVolumes()
    {
        ApplyMusicVolumes();

        var sfxVolume = GetSfxVolume();
        // Если Music и Sfx — один и тот же источник, не перетираем громкость музыки
        if (_sfxSource != null && _sfxSource != _musicSource)
            _sfxSource.volume = sfxVolume;

        for (var i = 0; i < _sfxPool.Count; i++)
        {
            if (_sfxPool[i] != null)
                _sfxPool[i].volume = sfxVolume * _sfxPoolVolumeScales[i];
        }
    }
}
