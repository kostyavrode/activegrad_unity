using System.Collections.Generic;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.MeshGeneration.Modifiers;
using UnityEngine;

[CreateAssetMenu(menuName = "ActiveGrad/Map/Building Footprint Chamfer Modifier")]
public class MapBuildingFootprintChamferModifier : MeshModifier
{
    [SerializeField] private float _chamferMeters = 0.35f;

    public void Configure(float chamferMeters)
    {
        _chamferMeters = Mathf.Max(0.05f, chamferMeters);
    }

    public override void Run(VectorFeatureUnity feature, MeshData md, UnityTile tile = null)
    {
        if (feature?.Points == null || feature.Points.Count == 0)
            return;

        var offset = _chamferMeters * (tile != null ? tile.TileScale : 1f);
        if (offset <= 0.001f)
            return;

        for (var ring = 0; ring < feature.Points.Count; ring++)
            ChamferRing(feature.Points[ring], offset);
    }

    private static void ChamferRing(List<Vector3> ring, float offset)
    {
        if (ring == null || ring.Count < 3)
            return;

        var count = ring.Count;
        var isClosed = count > 1 && (ring[0] - ring[count - 1]).sqrMagnitude < 0.0001f;
        var uniqueCount = isClosed ? count - 1 : count;
        if (uniqueCount < 3)
            return;

        var result = new List<Vector3>(uniqueCount * 2);

        for (var i = 0; i < uniqueCount; i++)
        {
            var prev = ring[(i - 1 + uniqueCount) % uniqueCount];
            var curr = ring[i];
            var next = ring[(i + 1) % uniqueCount];

            var toPrev = prev - curr;
            var toNext = next - curr;
            toPrev.y = 0f;
            toNext.y = 0f;

            var lenPrev = toPrev.magnitude;
            var lenNext = toNext.magnitude;
            if (lenPrev < 0.001f || lenNext < 0.001f)
            {
                result.Add(curr);
                continue;
            }

            var maxOffset = Mathf.Min(offset, lenPrev * 0.45f, lenNext * 0.45f);
            if (maxOffset < offset * 0.2f)
            {
                result.Add(curr);
                continue;
            }

            result.Add(curr + toPrev / lenPrev * maxOffset);
            result.Add(curr + toNext / lenNext * maxOffset);
        }

        ring.Clear();
        ring.AddRange(result);

        if (isClosed && ring.Count > 0)
            ring.Add(ring[0]);
    }
}
