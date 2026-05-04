using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject-словарь id → Sprite для ресурсов и предметов инвентаря.
/// Заполняется в инспекторе. Создать: ПКМ → Create → Config → IconConfig
/// </summary>
[CreateAssetMenu(menuName = "Config/IconConfig", fileName = "IconConfig")]
public class IconConfig : ScriptableObject
{
    [Serializable]
    public struct IconEntry
    {
        [Tooltip("id ресурса или предмета с сервера (например: metal, wood, sword)")]
        public string id;
        public Sprite sprite;
    }

    [SerializeField] private IconEntry[] _entries;

    private Dictionary<string, Sprite> _cache;

    /// <summary>
    /// Возвращает спрайт по id. Если не найден — null.
    /// </summary>
    public Sprite GetSprite(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        if (_cache == null)
            BuildCache();

        return _cache.TryGetValue(id, out var sprite) ? sprite : null;
    }

    private void BuildCache()
    {
        _cache = new Dictionary<string, Sprite>(_entries?.Length ?? 0);
        if (_entries == null) return;

        foreach (var entry in _entries)
        {
            if (!string.IsNullOrEmpty(entry.id) && entry.sprite != null)
                _cache[entry.id] = entry.sprite;
        }
    }

    // Сбрасываем кэш при изменении в редакторе
    private void OnValidate() => _cache = null;
}
