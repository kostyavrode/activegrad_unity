using UnityEngine;

public class AudioSettings
{
    private const string MusicKey = "MusicVolume";
    private const string SfxKey = "SfxVolume";

    public float MusicVolume { get; private set; }
    public float SfxVolume { get; private set; }

    public AudioSettings()
    {
        MusicVolume = PlayerPrefs.GetFloat(MusicKey, 0.8f);
        SfxVolume = PlayerPrefs.GetFloat(SfxKey, 0.8f);
    }

    public void SetMusicVolume(float value)
    {
        MusicVolume = value;
        PlayerPrefs.SetFloat(MusicKey, value);
        PlayerPrefs.Save();
    }

    public void SetSfxVolume(float value)
    {
        SfxVolume = value;
        PlayerPrefs.SetFloat(SfxKey, value);
        PlayerPrefs.Save();
    }
}