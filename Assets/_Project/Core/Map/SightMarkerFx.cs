using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Zenject;

/// <summary>
/// "Живость" маркера достопримечательности: покачивание, лёгкая пульсация,
/// усиление рядом с игроком и эффект открытия (PlayDiscover).
/// Добавляется автоматически из SightObject.SetPageID.
/// SpawnOnMap каждый кадр перезаписывает позицию/масштаб маркера в Update,
/// поэтому эффект накладывается поверх в LateUpdate.
/// </summary>
[DisallowMultipleComponent]
public class SightMarkerFx : MonoBehaviour
{
    private static readonly Dictionary<int, SightMarkerFx> Registry = new();

    /// <summary>Радиус (в мировых единицах, по XZ), в котором маркер "оживляется" сильнее.</summary>
    public static float ProximityRadius = 30f;

    [SerializeField] private float _bobAmplitude01 = 0.04f;   // доля от высоты маркера
    [SerializeField] private float _bobSpeed = 1.8f;
    [SerializeField] private float _pulseAmount = 0.025f;
    [SerializeField] private float _pulseSpeed = 2.4f;
    [SerializeField] private float _nearSpeedMultiplier = 2.2f;
    [SerializeField] private float _nearStrengthMultiplier = 1.8f;

    private const float PunchDuration = 0.65f;
    private const float PunchStrength = 0.4f;

    private int _pageId;
    private bool _registered;
    private Renderer[] _renderers;
    private float _localHeight = -1f;
    private float _phase;
    private float _nearBlend;
    private float _punchStart = -1f;

    private Vector3 _basePos;
    private Vector3 _baseScale;
    private Vector3 _lastAppliedPos;
    private Vector3 _lastAppliedScale;
    private bool _hasApplied;

    public int PageId => _pageId;

    public void Init(int pageId)
    {
        if (_registered && Registry.TryGetValue(_pageId, out var old) && old == this)
            Registry.Remove(_pageId);

        _pageId = pageId;
        Registry[pageId] = this;
        _registered = true;
    }

    private void Awake()
    {
        _phase = Random.value * 10f;
        CacheRenderers();
    }

    private void OnDestroy()
    {
        if (_registered && Registry.TryGetValue(_pageId, out var current) && current == this)
            Registry.Remove(_pageId);
    }

    private void CacheRenderers()
    {
        var all = GetComponentsInChildren<Renderer>(true);
        var count = 0;
        for (var i = 0; i < all.Length; i++)
            if (!(all[i] is ParticleSystemRenderer)) count++;

        _renderers = new Renderer[count];
        var n = 0;
        for (var i = 0; i < all.Length; i++)
            if (!(all[i] is ParticleSystemRenderer)) _renderers[n++] = all[i];
    }

    private bool IsAnyRendererVisible()
    {
        if (_renderers == null || _renderers.Length == 0)
            return true;

        for (var i = 0; i < _renderers.Length; i++)
        {
            var r = _renderers[i];
            if (r != null && r.isVisible)
                return true;
        }
        return false;
    }

    private float GetWorldHeight()
    {
        if (_renderers == null || _renderers.Length == 0)
            return Mathf.Max(0.1f, transform.lossyScale.y);

        var hasBounds = false;
        var bounds = new Bounds();
        for (var i = 0; i < _renderers.Length; i++)
        {
            var r = _renderers[i];
            if (r == null) continue;
            if (!hasBounds) { bounds = r.bounds; hasBounds = true; }
            else bounds.Encapsulate(r.bounds);
        }
        return hasBounds ? Mathf.Max(0.1f, bounds.size.y) : Mathf.Max(0.1f, transform.lossyScale.y);
    }

    private void LateUpdate()
    {
        var t = transform;
        var pos = t.localPosition;
        var scl = t.localScale;

        // Если SpawnOnMap не перезаписал трансформ в этом кадре — восстанавливаем базу, чтобы не накапливать смещение.
        if (_hasApplied && pos == _lastAppliedPos) pos = _basePos;
        if (_hasApplied && scl == _lastAppliedScale) scl = _baseScale;
        _basePos = pos;
        _baseScale = scl;

        var punching = _punchStart >= 0f;
        if (!punching && !IsAnyRendererVisible())
        {
            if (_hasApplied)
            {
                t.localPosition = pos;
                t.localScale = scl;
                _hasApplied = false;
            }
            return;
        }

        if (_localHeight < 0f)
        {
            var sy = Mathf.Abs(t.lossyScale.y);
            _localHeight = sy > 0.0001f ? GetWorldHeight() / sy : 1f;
        }

        var dt = Time.deltaTime;

        var nearTarget = 0f;
        if (TryGetPlayerPosition(out var playerPos))
        {
            var d = playerPos - t.position;
            d.y = 0f;
            var r = ProximityRadius;
            nearTarget = d.sqrMagnitude < r * r ? 1f : 0f;
        }
        _nearBlend = Mathf.MoveTowards(_nearBlend, nearTarget, dt * 2f);

        var speedMul = Mathf.Lerp(1f, _nearSpeedMultiplier, _nearBlend);
        var strengthMul = Mathf.Lerp(1f, _nearStrengthMultiplier, _nearBlend);
        _phase += dt * speedMul;

        var bob01 = 0.5f + 0.5f * Mathf.Sin(_phase * _bobSpeed * 2f);
        var worldHeight = _localHeight * Mathf.Abs(scl.y);
        var offsetY = worldHeight * _bobAmplitude01 * strengthMul * bob01;

        var pulse = 1f + _pulseAmount * strengthMul * Mathf.Sin(_phase * _pulseSpeed * 2f + 1.3f);

        if (punching)
        {
            var p = (Time.time - _punchStart) / PunchDuration;
            if (p >= 1f)
                _punchStart = -1f;
            else
                pulse *= 1f + PunchStrength * Mathf.Sin(p * Mathf.PI * 3f) * (1f - p);
        }

        var newPos = new Vector3(pos.x, pos.y + offsetY, pos.z);
        var newScale = scl * pulse;
        t.localPosition = newPos;
        t.localScale = newScale;
        _lastAppliedPos = newPos;
        _lastAppliedScale = newScale;
        _hasApplied = true;
    }

    private static bool TryGetPlayerPosition(out Vector3 position)
    {
        var player = CharacterController3D.Active;
        if (player != null)
        {
            position = player.transform.position;
            return true;
        }

        var cam = Camera.main;
        if (cam != null)
        {
            position = cam.transform.position;
            return true;
        }

        position = default;
        return false;
    }

    // ─────────────────────────── Discover ───────────────────────────

    /// <summary>Эффект "открытия" достопримечательности: punch, луч света, частицы и FeedbackType.Discover.</summary>
    public static void PlayDiscover(int pageId)
    {
        Vector3 fxPos;
        if (Registry.TryGetValue(pageId, out var fx) && fx != null)
        {
            fxPos = fx.transform.position;
            fx.PlayDiscoverLocal();
        }
        else
        {
            fxPos = TryGetPlayerPosition(out var p) ? p : Vector3.zero;
        }

        var feedback = ResolveFeedback();
        if (feedback != null)
        {
            try { feedback.Play(FeedbackType.Discover, fxPos); }
            catch (System.Exception e) { Debug.LogException(e); }
        }
    }

    private void PlayDiscoverLocal()
    {
        _punchStart = Time.time;

        var height = GetWorldHeight();
        var basePos = transform.position;

        SpawnBeam(transform, basePos, height);
        SpawnBurst(basePos + Vector3.up * (height * 0.5f), height);
    }

    private static IFeedbackService ResolveFeedback()
    {
        try
        {
            var ctx = ProjectContext.Instance;
            return ctx != null ? ctx.Container.TryResolve<IFeedbackService>() : null;
        }
        catch
        {
            return null;
        }
    }

    private static void SpawnBeam(Transform follow, Vector3 position, float height)
    {
        var material = JuiceFxMaterials.BeamAdditive;
        var mesh = JuiceFxMaterials.BeamMesh;
        if (material == null || mesh == null)
            return;

        var root = new GameObject("DiscoverBeam");
        root.transform.position = position;
        root.transform.localScale = new Vector3(height * 0.55f, height * 5f, height * 0.55f);

        var outer = CreateBeamPart(root.transform, mesh, material, 1f);
        var inner = CreateBeamPart(root.transform, mesh, material, 0.35f);

        var beam = root.AddComponent<DiscoverBeamFx>();
        beam.Setup(follow,
            new[] { outer, inner },
            new[] { new Color(1.3f, 1.1f, 0.55f, 0.35f), new Color(1.6f, 1.5f, 1.1f, 0.9f) });
    }

    private static Renderer CreateBeamPart(Transform parent, Mesh mesh, Material material, float widthScale)
    {
        var go = new GameObject("BeamPart");
        go.transform.SetParent(parent, false);
        go.transform.localScale = new Vector3(widthScale, 1f, widthScale);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = material;
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.lightProbeUsage = LightProbeUsage.Off;
        mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
        return mr;
    }

    private static void SpawnBurst(Vector3 position, float height)
    {
        var material = JuiceFxMaterials.ParticleAdditive;
        if (material == null)
            return;

        var go = new GameObject("DiscoverBurst");
        go.transform.position = position;

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 1f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(height * 1.2f, height * 2.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(height * 0.06f, height * 0.16f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.35f, 1f), new Color(1f, 1f, 0.9f, 1f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Local;
        main.maxParticles = 48;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)36) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = height * 0.25f;

        var limit = ps.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.limit = height * 0.4f;
        limit.dampen = 0.12f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(gradient);

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f));

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        ps.Play();
    }
}

/// <summary>Анимация луча открытия: появление, расширение, затухание, самоуничтожение.</summary>
public class DiscoverBeamFx : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private const float Duration = 1.6f;

    private Transform _follow;
    private Renderer[] _renderers;
    private Color[] _colors;
    private MaterialPropertyBlock _mpb;
    private Vector3 _baseScale;
    private float _time;

    public void Setup(Transform follow, Renderer[] renderers, Color[] colors)
    {
        _follow = follow;
        _renderers = renderers;
        _colors = colors;
        _mpb = new MaterialPropertyBlock();
        _baseScale = transform.localScale;
        Apply(0f);
    }

    private void Update()
    {
        _time += Time.deltaTime;
        var p = _time / Duration;
        if (p >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        if (_follow != null && _follow.gameObject.activeInHierarchy)
        {
            var pos = _follow.position;
            transform.position = pos;
        }

        Apply(p);
    }

    private void Apply(float p)
    {
        if (_renderers == null)
            return;

        var appear = Mathf.Clamp01(_time / 0.15f);
        var easedAppear = 1f - (1f - appear) * (1f - appear) * (1f - appear);
        var fade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.3f, 1f, p));
        var alpha = easedAppear * fade;

        var width = Mathf.Lerp(0.2f, 1f, easedAppear) * (1f + 0.25f * p);
        var heightMul = Mathf.Lerp(0.5f, 1f, easedAppear);
        transform.localScale = new Vector3(_baseScale.x * width, _baseScale.y * heightMul, _baseScale.z * width);

        for (var i = 0; i < _renderers.Length; i++)
        {
            var r = _renderers[i];
            if (r == null) continue;
            var c = _colors[i];
            c.a *= alpha;
            _mpb.SetColor(BaseColorId, c);
            _mpb.SetColor(ColorId, c);
            r.SetPropertyBlock(_mpb);
        }
    }
}

/// <summary>Общие процедурные материалы/текстуры/меши для "juice"-эффектов (создаются лениво, один раз).</summary>
public static class JuiceFxMaterials
{
    private static Shader _shader;
    private static bool _shaderSearched;
    private static Texture2D _softDot;
    private static Material _particleAlpha;
    private static Material _particleAdditive;
    private static Material _beamAdditive;
    private static Mesh _beamMesh;

    public static Shader ParticleShader
    {
        get
        {
            if (!_shaderSearched)
            {
                _shaderSearched = true;
                _shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (_shader == null)
                    _shader = Shader.Find("Sprites/Default");
            }
            return _shader;
        }
    }

    public static Texture2D SoftDot
    {
        get
        {
            if (_softDot != null)
                return _softDot;

            const int size = 64;
            _softDot = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "JuiceSoftDot",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave
            };
            var pixels = new Color32[size * size];
            var half = (size - 1) * 0.5f;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var dx = (x - half) / half;
                var dy = (y - half) / half;
                var d = Mathf.Sqrt(dx * dx + dy * dy);
                var a = Mathf.Clamp01(1f - d);
                a = a * a * (3f - 2f * a);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
            _softDot.SetPixels32(pixels);
            _softDot.Apply(false, true);
            return _softDot;
        }
    }

    /// <summary>Мягкие альфа-частицы (пыль). null, если шейдер не найден.</summary>
    public static Material ParticleAlpha => _particleAlpha != null ? _particleAlpha : _particleAlpha = Create("JuiceParticleAlpha", false, SoftDot, false);

    /// <summary>Аддитивные частицы (искры). null, если шейдер не найден.</summary>
    public static Material ParticleAdditive => _particleAdditive != null ? _particleAdditive : _particleAdditive = Create("JuiceParticleAdditive", true, SoftDot, false);

    /// <summary>Аддитивный двусторонний материал луча (прозрачность из вершинных цветов меша).</summary>
    public static Material BeamAdditive => _beamAdditive != null ? _beamAdditive : _beamAdditive = Create("JuiceBeamAdditive", true, null, true);

    public static Mesh BeamMesh => _beamMesh != null ? _beamMesh : _beamMesh = BuildBeamMesh();

    private static Material Create(string name, bool additive, Texture texture, bool doubleSided)
    {
        var shader = ParticleShader;
        if (shader == null)
            return null;

        var m = new Material(shader) { name = name, hideFlags = HideFlags.DontSave };

        if (shader.name.StartsWith("Universal Render Pipeline"))
        {
            m.SetFloat("_Surface", 1f);                    // Transparent
            m.SetFloat("_Blend", additive ? 2f : 0f);      // 2 = Additive, 0 = Alpha
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_Cull", doubleSided ? (float)CullMode.Off : (float)CullMode.Back);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_ALPHATEST_ON");
        }

        m.renderQueue = (int)RenderQueue.Transparent;
        if (texture != null)
            m.mainTexture = texture;
        m.color = Color.white;
        return m;
    }

    private static Mesh BuildBeamMesh()
    {
        const int segments = 16;
        var vertices = new Vector3[(segments + 1) * 2];
        var colors = new Color[vertices.Length];
        var uvs = new Vector2[vertices.Length];
        var triangles = new int[segments * 6];

        for (var i = 0; i <= segments; i++)
        {
            var u = (float)i / segments;
            var angle = u * Mathf.PI * 2f;
            var x = Mathf.Cos(angle) * 0.5f;
            var z = Mathf.Sin(angle) * 0.5f;

            vertices[i * 2] = new Vector3(x, 0f, z);
            vertices[i * 2 + 1] = new Vector3(x, 1f, z);
            colors[i * 2] = new Color(1f, 1f, 1f, 1f);
            colors[i * 2 + 1] = new Color(1f, 1f, 1f, 0f);
            uvs[i * 2] = new Vector2(u, 0f);
            uvs[i * 2 + 1] = new Vector2(u, 1f);
        }

        for (var i = 0; i < segments; i++)
        {
            var b0 = i * 2;
            var t0 = b0 + 1;
            var b1 = b0 + 2;
            var t1 = b0 + 3;
            var ti = i * 6;
            triangles[ti] = b0;
            triangles[ti + 1] = t0;
            triangles[ti + 2] = b1;
            triangles[ti + 3] = b1;
            triangles[ti + 4] = t0;
            triangles[ti + 5] = t1;
        }

        var mesh = new Mesh { name = "JuiceBeam", hideFlags = HideFlags.DontSave };
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        return mesh;
    }
}
