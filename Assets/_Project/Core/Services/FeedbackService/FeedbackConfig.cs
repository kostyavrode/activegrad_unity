using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FeedbackConfig", menuName = "ActiveGrad/Feedback Config")]
public class FeedbackConfig : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public FeedbackType type;
        [Tooltip("Если пусто — используется дефолтный UI-клик с указанным pitch")]
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.1f, 3f)] public float pitch = 1f;
        [Range(0f, 0.5f)] public float pitchJitter = 0.05f;
        public HapticType haptic = HapticType.Light;
        public bool hapticEnabled = true;
    }

    [SerializeField] private List<Entry> _entries = new List<Entry>();

    public IReadOnlyList<Entry> Entries => _entries;

    public bool TryGet(FeedbackType type, out Entry entry)
    {
        for (var i = 0; i < _entries.Count; i++)
        {
            if (_entries[i] != null && _entries[i].type == type)
            {
                entry = _entries[i];
                return true;
            }
        }

        entry = null;
        return false;
    }
}
