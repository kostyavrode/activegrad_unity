using UnityEngine;
using Zenject;

public class AudioManager : IInitializable
{
    private readonly AudioSettings _settings;
    private AudioSource _musicSource;
    private AudioSource _sfxSource;

    public AudioManager(AudioSettings settings)
    {
        _settings = settings;
    }

    public void Initialize()
    {
        var go = new GameObject("AudioManager");
        UnityEngine.Object.DontDestroyOnLoad(go);

        _musicSource = go.AddComponent<AudioSource>();
        _musicSource.loop = true;
        _musicSource.volume = _settings.MusicVolume;

        _sfxSource = go.AddComponent<AudioSource>();
        _sfxSource.loop = false;
        _sfxSource.volume = _settings.SfxVolume;
    }

    public void SetMusicVolume(float value)
    {
        _musicSource.volume = value;
    }

    public void SetSfxVolume(float value)
    {
        _sfxSource.volume = value;
    }
}