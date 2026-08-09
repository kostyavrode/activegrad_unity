using System.Collections.Generic;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.MeshGeneration.Modifiers;
using UnityEngine;

[CreateAssetMenu(menuName = "ActiveGrad/Map/Building Roof Softener Modifier")]
public class MapBuildingRoofSoftenerModifier : MeshModifier
{
    [SerializeField] private float _insetMeters = 0.35f;
    [SerializeField] private float _dropMeters = 0.28f;

    public void Configure(float insetMeters, float dropMeters)
    {
        _insetMeters = Mathf.Max(0.05f, insetMeters);
        _dropMeters = Mathf.Max(0.05f, dropMeters);
    }

    public override void Run(VectorFeatureUnity feature, MeshData md, UnityTile tile = null)
    {
        if (md?.Triangles == null || md.Triangles.Count == 0 || md.Triangles[0].Count < 3)
            return;

        var scale = tile != null ? tile.TileScale : 1f;
        var inset = _insetMeters * scale;
        var drop = _dropMeters * scale;
        if (inset <= 0.001f || drop <= 0.001f)
            return;

        SoftenRoofParapet(md, inset, drop);
    }

    private static void SoftenRoofParapet(MeshData md, float inset, float drop)
    {
        var roofTris = md.Triangles[0];
        var maxY = float.MinValue;
        for (var i = 0; i < md.Vertices.Count; i++)
        {
            if (md.Vertices[i].y > maxY)
                maxY = md.Vertices[i].y;
        }

        const float yEpsilon = 0.08f;
        var edgeCounts = new Dictionary<(int, int), int>();

        for (var t = 0; t < roofTris.Count; t += 3)
        {
            AddUndirectedEdge(edgeCounts, roofTris[t], roofTris[t + 1]);
            AddUndirectedEdge(edgeCounts, roofTris[t + 1], roofTris[t + 2]);
            AddUndirectedEdge(edgeCounts, roofTris[t + 2], roofTris[t]);
        }

        var boundaryEdges = new List<(int a, int b)>();
        foreach (var pair in edgeCounts)
        {
            if (pair.Value == 1)
                boundaryEdges.Add(pair.Key);
        }

        if (boundaryEdges.Count == 0)
            return;

        var boundaryVerts = new HashSet<int>();
        for (var i = 0; i < boundaryEdges.Count; i++)
        {
            boundaryVerts.Add(boundaryEdges[i].a);
            boundaryVerts.Add(boundaryEdges[i].b);
        }

        var centroidX = 0f;
        var centroidZ = 0f;
        var boundaryCount = 0;
        foreach (var index in boundaryVerts)
        {
            var vertex = md.Vertices[index];
            if (vertex.y < maxY - yEpsilon)
                continue;

            centroidX += vertex.x;
            centroidZ += vertex.z;
            boundaryCount++;
        }

        if (boundaryCount == 0)
            return;

        centroidX /= boundaryCount;
        centroidZ /= boundaryCount;

        EnsureUvList(md);
        EnsureTangents(md);

        var roofInnerByOuter = new Dictionary<int, int>();
        var bevelInnerByOuter = new Dictionary<int, int>();

        foreach (var outerIndex in boundaryVerts)
        {
            var outer = md.Vertices[outerIndex];
            if (outer.y < maxY - yEpsilon)
                continue;

            var roofInner = ComputeInsetPoint(outer, centroidX, centroidZ, inset, outer.y);
            var bevelInner = ComputeInsetPoint(outer, centroidX, centroidZ, inset, outer.y - drop);

            roofInnerByOuter[outerIndex] = AddVertex(md, roofInner, Vector3.up, outerIndex);
            bevelInnerByOuter[outerIndex] = AddVertex(md, bevelInner, Vector3.up, outerIndex);
        }

        for (var t = 0; t < roofTris.Count; t++)
        {
            if (roofInnerByOuter.TryGetValue(roofTris[t], out var roofInnerIndex))
                roofTris[t] = roofInnerIndex;
        }

        if (md.Triangles.Count < 2)
            md.Triangles.Add(new List<int>());

        var wallTris = md.Triangles[1];
        for (var i = 0; i < boundaryEdges.Count; i++)
        {
            var a = boundaryEdges[i].a;
            var b = boundaryEdges[i].b;
            if (!roofInnerByOuter.TryGetValue(a, out var roofInnerA)
                || !roofInnerByOuter.TryGetValue(b, out var roofInnerB)
                || !bevelInnerByOuter.TryGetValue(a, out var bevelInnerA)
                || !bevelInnerByOuter.TryGetValue(b, out var bevelInnerB))
                continue;

            var outerA = md.Vertices[a];
            var outerB = md.Vertices[b];
            var bevelA = md.Vertices[bevelInnerA];
            var bevelB = md.Vertices[bevelInnerB];

            var bevelNormal = Vector3.Cross(outerB - outerA, bevelA - outerA).normalized;
            if (bevelNormal.sqrMagnitude < 0.0001f)
                bevelNormal = Vector3.up;

            SetNormal(md, a, bevelNormal);
            SetNormal(md, b, bevelNormal);
            SetNormal(md, bevelInnerA, bevelNormal);
            SetNormal(md, bevelInnerB, bevelNormal);

            wallTris.Add(a);
            wallTris.Add(b);
            wallTris.Add(bevelInnerB);

            wallTris.Add(a);
            wallTris.Add(bevelInnerB);
            wallTris.Add(bevelInnerA);

            var capNormal = Vector3.Cross(bevelB - bevelA, md.Vertices[roofInnerA] - bevelA).normalized;
            if (capNormal.sqrMagnitude < 0.0001f)
                capNormal = Vector3.up;

            SetNormal(md, roofInnerA, capNormal);
            SetNormal(md, roofInnerB, capNormal);

            wallTris.Add(bevelInnerA);
            wallTris.Add(bevelInnerB);
            wallTris.Add(roofInnerB);

            wallTris.Add(bevelInnerA);
            wallTris.Add(roofInnerB);
            wallTris.Add(roofInnerA);
        }
    }

    private static Vector3 ComputeInsetPoint(Vector3 outer, float centroidX, float centroidZ, float inset, float y)
    {
        var toCenterX = centroidX - outer.x;
        var toCenterZ = centroidZ - outer.z;
        var planarLen = Mathf.Sqrt(toCenterX * toCenterX + toCenterZ * toCenterZ);

        if (planarLen < 0.001f)
            return new Vector3(outer.x, y, outer.z);

        var move = Mathf.Min(inset, planarLen * 0.42f);
        return new Vector3(
            outer.x + toCenterX / planarLen * move,
            y,
            outer.z + toCenterZ / planarLen * move);
    }

    private static int AddVertex(MeshData md, Vector3 position, Vector3 normal, int uvSourceIndex)
    {
        var index = md.Vertices.Count;
        md.Vertices.Add(position);
        md.Normals.Add(normal);
        md.UV[0].Add(md.UV[0][uvSourceIndex]);
        md.Tangents.Add(md.Tangents[uvSourceIndex]);
        return index;
    }

    private static void SetNormal(MeshData md, int index, Vector3 normal)
    {
        if (index >= 0 && index < md.Normals.Count)
            md.Normals[index] = normal;
    }

    private static void AddUndirectedEdge(Dictionary<(int, int), int> edgeCounts, int a, int b)
    {
        if (a > b)
            (a, b) = (b, a);

        var key = (a, b);
        edgeCounts[key] = edgeCounts.GetValueOrDefault(key) + 1;
    }

    private static void EnsureUvList(MeshData md)
    {
        if (md.UV == null)
            md.UV = new List<List<Vector2>>();

        if (md.UV.Count == 0)
            md.UV.Add(new List<Vector2>());

        while (md.UV[0].Count < md.Vertices.Count)
            md.UV[0].Add(Vector2.zero);
    }

    private static void EnsureTangents(MeshData md)
    {
        if (md.Tangents == null)
            md.Tangents = new List<Vector4>();

        while (md.Tangents.Count < md.Vertices.Count)
            md.Tangents.Add(new Vector4(1f, 0f, 0f, 1f));
    }
}
