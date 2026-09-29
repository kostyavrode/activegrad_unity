using System;
using System.Collections.Generic;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.MeshGeneration.Modifiers;
using UnityEngine;

/// <summary>
/// Даёт каждому зданию лёгкий оттенок из взвешенной палитры и пишет его в UV1 всех вершин здания.
/// Шейдер StylizedMatcap читает UV1: x = теплота (-1 холоднее .. +1 теплее), y = светлота (-1 .. +1).
/// Амплитуда оттенков задаётся в материале (Warmth/Lightness Range), здесь — только «какой» и «как часто».
/// Выбор детерминирован по ID фичи: при перезагрузке тайла здание сохраняет свой цвет.
/// </summary>
[CreateAssetMenu(menuName = "ActiveGrad/Map/Building Tint Modifier")]
public class MapBuildingTintModifier : MeshModifier
{
    [Serializable]
    public struct TintEntry
    {
        [Min(0f)] public float Weight;
        [Range(-1f, 1f)] public float Warmth;
        [Range(-1f, 1f)] public float Lightness;

        public TintEntry(float weight, float warmth, float lightness)
        {
            Weight = weight;
            Warmth = warmth;
            Lightness = lightness;
        }
    }

    // Базовый цвет сильно доминирует, остальные — редкие и близкие к нему оттенки.
    [SerializeField] private TintEntry[] _palette =
    {
        new(76f, 0f, 0f),       // базовый
        new(7f, 0.6f, 0.3f),    // тёплый кремовый
        new(6f, -0.7f, 0f),     // чуть холоднее
        new(5f, 1f, -0.2f),     // песочный
        new(4f, 0f, 0.8f),      // светлее
        new(2f, 0.2f, -0.7f),   // чуть темнее
    };

    public override void Run(VectorFeatureUnity feature, MeshData md, UnityTile tile = null)
    {
        if (md?.Vertices == null || md.Vertices.Count == 0 || _palette == null || _palette.Length == 0)
            return;

        var id = feature?.Data != null ? feature.Data.Id : 0UL;
        if (id == 0UL)
            id = FallbackId(md);

        var tint = Pick(Hash01(id));
        var value = new Vector2(tint.Warmth, tint.Lightness);

        while (md.UV.Count < 2)
            md.UV.Add(new List<Vector2>(md.Vertices.Count));

        var uv1 = md.UV[1];
        uv1.Clear();
        for (var i = 0; i < md.Vertices.Count; i++)
            uv1.Add(value);
    }

    private TintEntry Pick(float t)
    {
        var total = 0f;
        for (var i = 0; i < _palette.Length; i++)
            total += _palette[i].Weight;

        if (total <= 0f)
            return _palette[0];

        var target = t * total;
        for (var i = 0; i < _palette.Length; i++)
        {
            target -= _palette[i].Weight;
            if (target < 0f)
                return _palette[i];
        }

        return _palette[_palette.Length - 1];
    }

    // Без ID — хэш от округлённого центра здания (стабилен при перезагрузке тайла).
    private static ulong FallbackId(MeshData md)
    {
        var sum = Vector3.zero;
        for (var i = 0; i < md.Vertices.Count; i++)
            sum += md.Vertices[i];
        sum /= md.Vertices.Count;
        unchecked
        {
            return (ulong)Mathf.RoundToInt(sum.x * 10f) * 73856093UL ^ (ulong)Mathf.RoundToInt(sum.z * 10f) * 19349663UL;
        }
    }

    // SplitMix64 → [0, 1)
    private static float Hash01(ulong x)
    {
        unchecked
        {
            x += 0x9E3779B97F4A7C15UL;
            x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
            x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
            x ^= x >> 31;
        }
        return (x >> 40) / (float)(1UL << 24);
    }
}
