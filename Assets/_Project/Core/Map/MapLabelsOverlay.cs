using System;
using System.Collections.Generic;
using System.Linq;
using Mapbox.Map;
using Mapbox.Unity;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Data;
using UnityEngine;
using UnityEngine.Rendering;
using Zenject;

/// <summary>
/// 1) Слой надписей поверх 3D: для каждого тайла карты грузит растровый тайл второго стиля Mapbox Studio
///    («только надписи», прозрачный фон) и рисует его шейдером ActiveGrad/MapLabelsOverlay поверх зданий и дорог.
/// 2) Разбивает плоский тайл земли (4 вершины) на сетку — иначе изгиб мира (CurvedWorldController)
///    применяется только в углах тайла, и земля внутри тайла расходится со зданиями, надписями и персонажем.
/// Настройки — Resources/Map/MapVisualStyleConfig (LabelsStyleUrl, LabelsMaterial, SubdivideGroundTiles).
/// </summary>
public class MapLabelsOverlay : IInitializable, IDisposable
{
    private const string ConfigPath = "Map/MapVisualStyleConfig";
    private const string ChildName = "MapLabels";

    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

    private readonly AbstractMap _map;
    private readonly Dictionary<UnityTile, LabelState> _states = new();

    private MapVisualStyleConfig _config;
    private bool _labelsEnabled;
    private bool _subscribed;

    private sealed class LabelState
    {
        public MeshRenderer Renderer;
        public MaterialPropertyBlock Block;
        public Texture2D Texture;
        public CanonicalTileId RequestedId;
        public bool HasRequest;
        public RetinaRasterTile Raster;
    }

    public MapLabelsOverlay(AbstractMap map)
    {
        _map = map;
    }

    public void Initialize()
    {
        _config = Resources.Load<MapVisualStyleConfig>(ConfigPath);
        if (_config == null || _map == null)
            return;

        _labelsEnabled = !string.IsNullOrWhiteSpace(_config.LabelsStyleUrl) && _config.LabelsMaterial != null;
        if (!_labelsEnabled && !_config.SubdivideGroundTiles)
            return;

        _map.OnTileFinished += OnTile;
        _map.OnInitialized += SubscribeVisualizer;
        SubscribeVisualizer();

        // Тайлы, загруженные до нас.
        var tiles = _map.MapVisualizer?.ActiveTiles;
        if (tiles != null)
        {
            foreach (var tile in tiles.Values.ToList())
                OnTile(tile);
        }
    }

    public void Dispose()
    {
        if (_map != null)
        {
            _map.OnTileFinished -= OnTile;
            _map.OnInitialized -= SubscribeVisualizer;
            if (_subscribed && _map.MapVisualizer != null)
                _map.MapVisualizer.OnTileImageProcessingFinished -= OnTile;
        }

        foreach (var state in _states.Values)
        {
            state.Raster?.Cancel();
            DestroyTexture(state);
        }
        _states.Clear();
    }

    private void SubscribeVisualizer()
    {
        if (_subscribed || _map.MapVisualizer == null)
            return;

        // Раньше OnTileFinished: сетка земли и запрос надписей стартуют сразу после загрузки подложки.
        _map.MapVisualizer.OnTileImageProcessingFinished += OnTile;
        _subscribed = true;
    }

    private void OnTile(UnityTile tile)
    {
        if (tile == null || tile.MeshFilter == null)
            return;

        if (_config.SubdivideGroundTiles)
            SubdivideGround(tile, _config.GroundGridResolution);

        if (_labelsEnabled)
            RequestLabels(tile);
    }

    // ---------------- Надписи ----------------

    private void RequestLabels(UnityTile tile)
    {
        var state = GetOrCreateState(tile);
        var id = tile.CanonicalTileId;

        // Тайл тот же и уже загружен/грузится — ничего не делаем (обработчик вызывается несколько раз).
        if (state.HasRequest && state.RequestedId.Equals(id))
            return;

        state.Raster?.Cancel();
        state.Renderer.enabled = false;
        DestroyTexture(state);

        state.RequestedId = id;
        state.HasRequest = true;

        var raster = new RetinaRasterTile();
        state.Raster = raster;
        raster.Initialize(new Tile.Parameters
        {
            Id = id,
            TilesetId = _config.LabelsStyleUrl,
            Fs = MapboxAccess.Instance,
        }, () => OnLabelsLoaded(tile, state, raster, id));
    }

    private void OnLabelsLoaded(UnityTile tile, LabelState state, RetinaRasterTile raster, CanonicalTileId id)
    {
        // Тайл успели переиспользовать под другой участок карты — ответ устарел.
        if (tile == null || state.Raster != raster || !tile.CanonicalTileId.Equals(id))
            return;

        state.Raster = null;

        if (raster.HasError || raster.Data == null || raster.Data.Length == 0)
        {
            if (raster.HasError)
                Debug.LogWarning($"[MapLabelsOverlay] Не удалось загрузить надписи для тайла {id}: {string.Join("; ", raster.Exceptions.Select(e => e.Message))}");
            return;
        }

        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, true, false)
        {
            name = "MapLabels_" + id,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Trilinear,
            anisoLevel = 2,
        };

        if (!texture.LoadImage(raster.Data, true))
        {
            UnityEngine.Object.Destroy(texture);
            return;
        }

        state.Texture = texture;
        state.Block.SetTexture(BaseMapId, texture);
        state.Renderer.SetPropertyBlock(state.Block);
        state.Renderer.enabled = true;
    }

    private LabelState GetOrCreateState(UnityTile tile)
    {
        if (_states.TryGetValue(tile, out var state) && state.Renderer != null)
        {
            // Меш тайла мог быть пересоздан (сетка) — держим ссылку актуальной.
            var filter = state.Renderer.GetComponent<MeshFilter>();
            if (filter.sharedMesh != tile.MeshFilter.sharedMesh)
                filter.sharedMesh = tile.MeshFilter.sharedMesh;
            return state;
        }

        var child = tile.transform.Find(ChildName);
        if (child == null)
        {
            var go = new GameObject(ChildName);
            child = go.transform;
            child.SetParent(tile.transform, false);
            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>();
        }

        child.GetComponent<MeshFilter>().sharedMesh = tile.MeshFilter.sharedMesh;

        var renderer = child.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = _config.LabelsMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.enabled = false;

        state = new LabelState { Renderer = renderer, Block = new MaterialPropertyBlock() };
        _states[tile] = state;
        return state;
    }

    private static void DestroyTexture(LabelState state)
    {
        if (state.Texture == null)
            return;

        UnityEngine.Object.Destroy(state.Texture);
        state.Texture = null;
    }

    // ---------------- Сетка земли ----------------

    /// <summary>
    /// Плоский тайл Mapbox — квад из 4 вершин (FlatTerrainStrategy пишет его в собственный меш тайла
    /// и больше не трогает). Заменяем содержимое этого меша сеткой N×N с теми же границами и UV.
    /// </summary>
    private static void SubdivideGround(UnityTile tile, int resolution)
    {
        var mesh = tile.MeshFilter.sharedMesh;
        if (mesh == null || mesh.vertexCount != 4)
            return;

        var corners = mesh.vertices;
        var cornerUvs = mesh.uv;
        if (corners.Length != 4 || cornerUvs.Length != 4)
            return;

        resolution = Mathf.Clamp(resolution, 2, 32);
        var side = resolution + 1;
        var vertices = new Vector3[side * side];
        var normals = new Vector3[side * side];
        var uvs = new Vector2[side * side];

        // Билинейно между углами 0-1 (ближняя кромка) и 3-2 (дальняя) — сохраняет раскладку и UV Mapbox.
        for (var j = 0; j < side; j++)
        {
            var t = j / (float)resolution;
            for (var i = 0; i < side; i++)
            {
                var s = i / (float)resolution;
                var index = j * side + i;
                vertices[index] = Vector3.Lerp(Vector3.Lerp(corners[0], corners[1], s), Vector3.Lerp(corners[3], corners[2], s), t);
                uvs[index] = Vector2.Lerp(Vector2.Lerp(cornerUvs[0], cornerUvs[1], s), Vector2.Lerp(cornerUvs[3], cornerUvs[2], s), t);
                normals[index] = Vector3.up;
            }
        }

        // Тот же порядок обхода, что у исходного квада (0,1,2 / 0,2,3).
        var triangles = new int[resolution * resolution * 6];
        var k = 0;
        for (var j = 0; j < resolution; j++)
        {
            for (var i = 0; i < resolution; i++)
            {
                var a = j * side + i;
                var b = a + 1;
                var c = a + side + 1;
                var d = a + side;
                triangles[k++] = a; triangles[k++] = b; triangles[k++] = c;
                triangles[k++] = a; triangles[k++] = c; triangles[k++] = d;
            }
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
    }
}
