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
/// Заодно переписывает UV0 стен под процедурные окна шейдера: на каждую стену — целое число окон и этажей.
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

    [Header("Windows (UV0 стен)")]
    [SerializeField, Min(0.5f)] private float _windowStepMeters = 3.2f;
    [SerializeField, Min(0.5f)] private float _floorHeightMeters = 3f;

    public override void Run(VectorFeatureUnity feature, MeshData md, UnityTile tile = null)
    {
        if (md?.Vertices == null || md.Vertices.Count == 0)
            return;

        FitWindowUv(md, tile != null ? tile.TileScale : 1f);

        if (_palette == null || _palette.Length == 0)
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

    // Стена из HeightModifier — четвёрка вершин: верх-лево, верх-право, низ-лево, низ-право.
    // Пишем UV так, чтобы 1 единица = одно окно по горизонтали и один этаж по вертикали,
    // а на стену приходилось целое их число. Слишком короткие стены (скосы углов) остаются без окон.
    private void FitWindowUv(MeshData md, float scale)
    {
        if (md.Normals == null || md.Normals.Count != md.Vertices.Count
            || md.UV == null || md.UV.Count == 0 || md.UV[0].Count != md.Vertices.Count)
            return;

        var step = Mathf.Max(0.01f, _windowStepMeters * scale);
        var floor = Mathf.Max(0.01f, _floorHeightMeters * scale);
        var uv = md.UV[0];
        var count = md.Vertices.Count;

        for (var i = 0; i + 3 < count;)
        {
            if (!IsWallQuad(md, i))
            {
                i++;
                continue;
            }

            var topLeft = md.Vertices[i];
            var topRight = md.Vertices[i + 1];
            var length = new Vector2(topRight.x - topLeft.x, topRight.z - topLeft.z).magnitude;
            var height = topLeft.y - md.Vertices[i + 2].y;

            var windows = Mathf.FloorToInt(length / step + 0.35f);
            var floors = windows > 0 ? Mathf.Max(1, Mathf.RoundToInt(height / floor)) : 0;

            uv[i] = new Vector2(0f, floors);
            uv[i + 1] = new Vector2(windows, floors);
            uv[i + 2] = new Vector2(0f, 0f);
            uv[i + 3] = new Vector2(windows, 0f);
            i += 4;
        }
    }

    private static bool IsWallQuad(MeshData md, int i)
    {
        for (var k = 0; k < 4; k++)
        {
            if (Mathf.Abs(md.Normals[i + k].y) > 0.1f)
                return false;
        }

        return SameXZ(md.Vertices[i], md.Vertices[i + 2])
               && SameXZ(md.Vertices[i + 1], md.Vertices[i + 3])
               && md.Vertices[i].y > md.Vertices[i + 2].y;
    }

    private static bool SameXZ(Vector3 a, Vector3 b)
    {
        return Mathf.Abs(a.x - b.x) < 1e-3f && Mathf.Abs(a.z - b.z) < 1e-3f;
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
