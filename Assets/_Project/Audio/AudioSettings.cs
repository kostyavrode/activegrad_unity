using UnityEngine;

public class AudioSettings
{
    private const string MusicKey = "MusicVolume";
    private const string SfxKey = "SfxVolume";
    private const string MusicMutedKey = "MusicMuted";
    private const string SfxMutedKey = "SfxMuted";

    public float MusicVolume { get; private set; }
    public float SfxVolume { get; private set; }
    public bool MusicMuted { get; private set; }
    public bool SfxMuted { get; private set; }

    public AudioSettings()
    {
        MusicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicKey, 0.8f));
        SfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxKey, 0.8f));
        MusicMuted = PlayerPrefs.GetInt(MusicMutedKey, 0) == 1;
        SfxMuted = PlayerPrefs.GetInt(SfxMutedKey, 0) == 1;
    }

    public void SetMusicVolume(float value)
    {
        MusicVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MusicKey, MusicVolume);
        PlayerPrefs.Save();
    }

    public void SetSfxVolume(float value)
    {
        SfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(SfxKey, SfxVolume);
        PlayerPrefs.Save();
    }

    public void SetMusicMuted(bool isMuted)
    {
        MusicMuted = isMuted;
        PlayerPrefs.SetInt(MusicMutedKey, isMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetSfxMuted(bool isMuted)
    {
        SfxMuted = isMuted;
        PlayerPrefs.SetInt(SfxMutedKey, isMuted ? 1 : 0);
        PlayerPrefs.Save();
    }
}