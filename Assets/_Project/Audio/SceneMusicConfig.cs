using System;
using UnityEngine;

[CreateAssetMenu(fileName = "SceneMusicConfig", menuName = "Audio/Scene Music Config")]
public class SceneMusicConfig : ScriptableObject
{
    [Serializable]
    public struct SceneTrack
    {
        public string SceneName;
        public AudioClip Clip;
    }

    [SerializeField] private SceneTrack[] _tracks;

    public bool TryGetTrack(string sceneName, out AudioClip clip)
    {
        clip = null;
        if (_tracks == null || string.IsNullOrEmpty(sceneName))
            return false;

        for (var i = 0; i < _tracks.Length; i++)
        {
            if (_tracks[i].SceneName == sceneName)
            {
                clip = _tracks[i].Clip;
                return clip != null;
            }
        }

        return false;
    }
}
