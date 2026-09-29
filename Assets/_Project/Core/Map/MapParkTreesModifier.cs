using System;
using System.Collections.Generic;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.MeshGeneration.Modifiers;
using UnityEngine;
using UnityEngine.Rendering;
using Random = System.Random;

/// <summary>
/// Засаживает полигоны парков low-poly деревьями. Все деревья сущности склеиваются в один меш
/// (один draw call на тайл/чанк), цвета — в цветах вершин (шейдер StylizedMatcap, Vertex Color = 1).
/// Расстановка детерминирована по тайлу: при перезагрузке деревья стоят на тех же местах.
/// </summary>
[CreateAssetMenu(menuName = "ActiveGrad/Map/Park Trees Modifier")]
public class MapParkTreesModifier : GameObjectModifier
{
    private const string ChildName = "ParkTrees";

    [SerializeField] private Material _material;
    [SerializeField, Min(10f)] private float _squareMetersPerTree = 1260f;
    [SerializeField, Min(1)] private int _maxTreesPerEntity = 40;
    [SerializeField] private Vector2 _heightMeters = new(14f, 22f);
    [SerializeField, Range(0f, 1f)] private float _coniferChance = 0.22f;
    [SerializeField] private bool _castShadows;

    // Листва: один доминирующий зелёный и редкие близкие оттенки.
    private static readonly (float weight, Color color)[] CrownPalette =
    {
        (70f, new Color(0.47f, 0.72f, 0.40f)),
        (14f, new Color(0.42f, 0.67f, 0.38f)),
        (11f, new Color(0.54f, 0.76f, 0.42f)),
        (5f, new Color(0.63f, 0.73f, 0.41f)),
    };

    private static readonly Color ConiferColor = new(0.34f, 0.58f, 0.42f);
    private static readonly Color TrunkColor = new(0.58f, 0.46f, 0.36f);

    private static TreePart[] _roundTree;
    private static TreePart[] _coniferTree;

    private readonly List<Vector3> _vertices = new();
    private readonly List<Vector3> _normals = new();
    private readonly List<Color> _colors = new();
    private readonly List<int> _triangles = new();

    public void Configure(Material material)
    {
        _material = material;
    }

    public override void Run(VectorEntity ve, UnityTile tile)
    {
        if (ve?.GameObject == null || ve.Mesh == null || _material == null)
            return;

        var child = GetOrCreateChild(ve.GameObject.transform);
        var filter = child.GetComponent<MeshFilter>();

        var scale = tile != null ? tile.TileScale : 1f;
        var seed = StableSeed(tile, ve.Mesh.vertexCount);

        if (!BuildTrees(ve.Mesh, scale, seed))
        {
            child.gameObject.SetActive(false);
            return;
        }

        var mesh = filter.sharedMesh;
        if (mesh == null)
        {
            mesh = new Mesh { name = ChildName };
            filter.sharedMesh = mesh;
        }

        mesh.Clear();
        mesh.indexFormat = _vertices.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        mesh.SetVertices(_vertices);
        mesh.SetNormals(_normals);
        mesh.SetColors(_colors);
        mesh.SetTriangles(_triangles, 0);
        mesh.RecalculateBounds();

        child.gameObject.SetActive(true);
    }

    public override void OnPoolItem(VectorEntity vectorEntity)
    {
        var child = vectorEntity?.GameObject != null ? vectorEntity.GameObject.transform.Find(ChildName) : null;
        if (child != null)
            child.gameObject.SetActive(false);
    }

    private Transform GetOrCreateChild(Transform parent)
    {
        var child = parent.Find(ChildName);
        if (child == null)
        {
            var go = new GameObject(ChildName);
            child = go.transform;
            child.SetParent(parent, false);
            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>();
        }

        var renderer = child.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = _material;
        renderer.shadowCastingMode = _castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        return child;
    }

    private bool BuildTrees(Mesh source, float scale, int seed)
    {
        _vertices.Clear();
        _normals.Clear();
        _colors.Clear();
        _triangles.Clear();

        var srcVertices = source.vertices;
        var srcTriangles = source.GetTriangles(0);
        if (srcTriangles.Length < 3)
            return false;

        // Кумулятивная площадь треугольников — для равномерного распределения по полигонам.
        var triCount = srcTriangles.Length / 3;
        var cumulative = new float[triCount];
        var totalArea = 0f;
        for (var t = 0; t < triCount; t++)
        {
            var a = srcVertices[srcTriangles[t * 3]];
            var b = srcVertices[srcTriangles[t * 3 + 1]];
            var c = srcVertices[srcTriangles[t * 3 + 2]];
            totalArea += Vector3.Cross(b - a, c - a).magnitude * 0.5f;
            cumulative[t] = totalArea;
        }

        var areaMeters = totalArea / Mathf.Max(scale * scale, 1e-6f);
        var random = new Random(seed);
        var exact = areaMeters / _squareMetersPerTree;
        var count = Mathf.Min(_maxTreesPerEntity, (int)exact + (random.NextDouble() < exact % 1f ? 1 : 0));
        if (count <= 0)
            return false;

        EnsureTemplates();

        for (var i = 0; i < count; i++)
        {
            var t = Array.BinarySearch(cumulative, (float)(random.NextDouble() * totalArea));
            if (t < 0)
                t = Mathf.Min(~t, triCount - 1);

            var a = srcVertices[srcTriangles[t * 3]];
            var b = srcVertices[srcTriangles[t * 3 + 1]];
            var c = srcVertices[srcTriangles[t * 3 + 2]];
            var position = RandomPointInTriangle(random, a, b, c);

            var height = Mathf.Lerp(_heightMeters.x, _heightMeters.y, (float)random.NextDouble()) * scale;
            var rotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);
            var isConifer = random.NextDouble() < _coniferChance;
            var crown = isConifer ? ConiferColor : PickCrownColor(random);

            AppendTree(isConifer ? _coniferTree : _roundTree, position, rotation, height, crown);
        }

        return _vertices.Count > 0;
    }

    private static Vector3 RandomPointInTriangle(Random random, Vector3 a, Vector3 b, Vector3 c)
    {
        var u = (float)random.NextDouble();
        var v = (float)random.NextDouble();
        if (u + v > 1f)
        {
            u = 1f - u;
            v = 1f - v;
        }

        // Лёгкое стягивание к центру треугольника, чтобы деревья реже вылезали на края парка.
        var point = a + (b - a) * u + (c - a) * v;
        var center = (a + b + c) / 3f;
        return Vector3.Lerp(point, center, 0.15f);
    }

    private static Color PickCrownColor(Random random)
    {
        var total = 0f;
        for (var i = 0; i < CrownPalette.Length; i++)
            total += CrownPalette[i].weight;

        var target = (float)random.NextDouble() * total;
        for (var i = 0; i < CrownPalette.Length; i++)
        {
            target -= CrownPalette[i].weight;
            if (target < 0f)
                return CrownPalette[i].color;
        }

        return CrownPalette[0].color;
    }

    private void AppendTree(TreePart[] parts, Vector3 position, Quaternion rotation, float height, Color crownColor)
    {
        for (var p = 0; p < parts.Length; p++)
        {
            var part = parts[p];
            var color = part.IsTrunk ? TrunkColor : crownColor;
            // Цвета вершин не конвертируются Unity автоматически — переводим палитру (sRGB) в linear.
            if (QualitySettings.activeColorSpace == ColorSpace.Linear)
                color = color.linear;
            var start = _vertices.Count;

            for (var i = 0; i < part.Vertices.Length; i++)
            {
                _vertices.Add(position + rotation * (part.Vertices[i] * height));
                _normals.Add(rotation * part.Normals[i]);
                _colors.Add(color);
            }

            for (var i = 0; i < part.Triangles.Length; i++)
                _triangles.Add(start + part.Triangles[i]);
        }
    }

    private static int StableSeed(UnityTile tile, int salt)
    {
        unchecked
        {
            var hash = 17;
            if (tile != null)
            {
                var id = tile.CanonicalTileId;
                hash = hash * 31 + id.X;
                hash = hash * 31 + id.Y;
                hash = hash * 31 + id.Z;
            }
            return hash * 31 + salt;
        }
    }

    // ---------- Шаблоны деревьев (высота = 1) ----------

    private struct TreePart
    {
        public Vector3[] Vertices;
        public Vector3[] Normals;
        public int[] Triangles;
        public bool IsTrunk;
    }

    private static void EnsureTemplates()
    {
        if (_roundTree != null)
            return;

        var trunk = Cylinder(6, 0.045f, 0.035f, 0f, 0.42f, true);

        _roundTree = new[]
        {
            trunk,
            Sphere(8, 6, new Vector3(0f, 0.62f, 0f), new Vector3(0.3f, 0.36f, 0.3f)),
        };

        _coniferTree = new[]
        {
            Cylinder(6, 0.04f, 0.03f, 0f, 0.25f, true),
            Cone(8, 0.26f, 0.18f, 0.62f),
            Cone(8, 0.2f, 0.45f, 1.0f),
        };
    }

    private static TreePart Sphere(int segments, int rings, Vector3 center, Vector3 radius)
    {
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var triangles = new List<int>();

        for (var r = 0; r <= rings; r++)
        {
            var phi = Mathf.PI * r / rings;
            for (var s = 0; s <= segments; s++)
            {
                var theta = 2f * Mathf.PI * s / segments;
                var n = new Vector3(Mathf.Sin(phi) * Mathf.Cos(theta), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(theta));
                vertices.Add(center + Vector3.Scale(n, radius));
                normals.Add(n);
            }
        }

        var stride = segments + 1;
        for (var r = 0; r < rings; r++)
        {
            for (var s = 0; s < segments; s++)
            {
                var i0 = r * stride + s;
                var i1 = i0 + stride;
                triangles.Add(i0); triangles.Add(i0 + 1); triangles.Add(i1);
                triangles.Add(i1); triangles.Add(i0 + 1); triangles.Add(i1 + 1);
            }
        }

        return new TreePart { Vertices = vertices.ToArray(), Normals = normals.ToArray(), Triangles = triangles.ToArray() };
    }

    private static TreePart Cylinder(int segments, float bottomRadius, float topRadius, float bottomY, float topY, bool isTrunk)
    {
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var triangles = new List<int>();

        for (var s = 0; s <= segments; s++)
        {
            var theta = 2f * Mathf.PI * s / segments;
            var dir = new Vector3(Mathf.Cos(theta), 0f, Mathf.Sin(theta));
            vertices.Add(dir * bottomRadius + Vector3.up * bottomY);
            vertices.Add(dir * topRadius + Vector3.up * topY);
            normals.Add(dir);
            normals.Add(dir);
        }

        for (var s = 0; s < segments; s++)
        {
            var i = s * 2;
            triangles.Add(i); triangles.Add(i + 1); triangles.Add(i + 2);
            triangles.Add(i + 2); triangles.Add(i + 1); triangles.Add(i + 3);
        }

        return new TreePart { Vertices = vertices.ToArray(), Normals = normals.ToArray(), Triangles = triangles.ToArray(), IsTrunk = isTrunk };
    }

    private static TreePart Cone(int segments, float radius, float bottomY, float topY)
    {
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var triangles = new List<int>();

        var slope = radius / Mathf.Max(topY - bottomY, 1e-4f);
        for (var s = 0; s <= segments; s++)
        {
            var theta = 2f * Mathf.PI * s / segments;
            var dir = new Vector3(Mathf.Cos(theta), 0f, Mathf.Sin(theta));
            var normal = (dir + Vector3.up * slope).normalized;
            vertices.Add(dir * radius + Vector3.up * bottomY);
            normals.Add(normal);
            vertices.Add(Vector3.up * topY);
            normals.Add(normal);
        }

        // Нижняя «юбка» конуса.
        var centerIndex = vertices.Count;
        vertices.Add(Vector3.up * bottomY);
        normals.Add(Vector3.down);

        for (var s = 0; s < segments; s++)
        {
            var i = s * 2;
            triangles.Add(i); triangles.Add(i + 1); triangles.Add(i + 2);
            triangles.Add(centerIndex); triangles.Add(i); triangles.Add(i + 2);
        }

        return new TreePart { Vertices = vertices.ToArray(), Normals = normals.ToArray(), Triangles = triangles.ToArray() };
    }
}
