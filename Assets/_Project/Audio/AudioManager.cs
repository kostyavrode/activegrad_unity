using UnityEngine;
using Zenject;
using System;
using Object = UnityEngine.Object;

public class AudioManager : IInitializable, IDisposable
{
    private AudioSource _musicSource;
    private AudioSource _sfxSource;

    private readonly AudioClip _mainTheme;

    public AudioManager([Inject(Id = "MainTheme")] AudioClip mainTheme)
    {
        _mainTheme = mainTheme;
    }

    public void Initialize()
    {
        var go = new GameObject("AudioManager");
        Object.DontDestroyOnLoad(go);

        _musicSource = go.AddComponent<AudioSource>();
        _musicSource.loop = true;
        _musicSource.volume = PlayerPrefs.GetFloat("MusicVolume", 0.8f);

        _sfxSource = go.AddComponent<AudioSource>();
        _sfxSource.loop = false;
        _sfxSource.volume = PlayerPrefs.GetFloat("SfxVolume", 0.8f);

        // Запускаем фоновую музыку
        PlayMusic(_mainTheme);
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        _musicSource.clip = clip;
        _musicSource.Play();
    }

    public void PlaySfx(AudioClip clip)
    {
        if (clip == null) return;
        _sfxSource.PlayOneShot(clip);
    }

    public void SetMusicVolume(float value)
    {
        _musicSource.volume = value;
        PlayerPrefs.SetFloat("MusicVolume", value);
    }

    public void SetSfxVolume(float value)
    {
        _sfxSource.volume = value;
        PlayerPrefs.SetFloat("SfxVolume", value);
    }

    public void Dispose()
    {
        PlayerPrefs.Save();
    }
}