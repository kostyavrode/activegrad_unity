using UnityEngine;
using Zenject;

public class AudioManager : IInitializable
{
    private readonly AudioSettings _settings;
    private readonly AudioSource _musicSource;
    private readonly AudioSource _sfxSource;
    private readonly AudioClip _defaultUiClickClip;
    private readonly AudioClip _defaultUiCloseClip;

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
        _musicSource.loop = true;
        _sfxSource.loop = false;
        ApplyVolumes();
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

        var isSameClip = _musicSource.clip == clip;
        if (isSameClip && _musicSource.isPlaying && !restartIfSameClip)
            return;

        _musicSource.clip = clip;
        _musicSource.Play();
    }

    public void StopMusic()
    {
        _musicSource.Stop();
    }

    public void PlaySfx(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null)
            return;

        _sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
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

    private void ApplyVolumes()
    {
        _musicSource.volume = _settings.MusicMuted ? 0f : _settings.MusicVolume;
        _sfxSource.volume = _settings.SfxMuted ? 0f : _settings.SfxVolume;
    }
}