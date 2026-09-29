using UnityEngine;
using UnityEngine.Rendering;
using Zenject;

public class CharacterController3D : MonoBehaviour
{
    private static readonly int VertHash = Animator.StringToHash("Vert");
    private static readonly string[] IdleVariantTriggers = { "Idle2", "IdleVariant" };

    [SerializeField] private Animator animator;
    [SerializeField] private float rotationSpeed = 8f;
    [SerializeField] private float movementThreshold = 0.001f;
    [SerializeField] private float animationBlendSpeed = 5f;   // макс. скорость изменения Vert (ед./сек)
    [SerializeField] private float vertSmoothTime = 0.18f;
    [SerializeField] private float minMovingVert = 0.35f;
    [SerializeField] private Vector2 idleVariantInterval = new Vector2(6f, 12f);
    [SerializeField] private bool stepDust = true;
    [SerializeField] private float dustMaxRate = 9f;

    /// <summary>Текущий активный персонаж игрока (для эффектов близости и т.п.).</summary>
    public static CharacterController3D Active { get; private set; }

    [InjectOptional] private SmoothMapMovementService _mapMovement;

    private Vector3 _targetDirection;
    private bool _isMoving = false;
    private bool _shouldKeepWalking = false;
    private float _currentVertValue = 0f;
    private float _vertVelocity;

    private float _externalSpeed01 = 1f;
    private float _externalSpeedUntil = -1f;
    private const float ExternalSpeedTimeout = 0.5f;

    private int _idleVariantHash;
    private float _idleTimer;

    private ParticleSystem _dust;
    private float _dustRate = -1f;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        CacheIdleVariant();
        ResetIdleTimer();
    }

    private void OnEnable()
    {
        Active = this;
    }

    private void OnDisable()
    {
        if (Active == this)
            Active = null;
    }

    private void Start()
    {
        // В Start, чтобы MapShadowHelper/CharacterRimApplier (вызываются сразу после Instantiate) не трогали пыль.
        if (stepDust)
            CreateDust();
    }

    private void Update()
    {
        bool shouldMove = (_isMoving || _shouldKeepWalking) && _targetDirection.magnitude > movementThreshold;
        float targetVertValue = shouldMove ? Mathf.Max(minMovingVert, GetSpeed01()) : 0f;

        _currentVertValue = Mathf.SmoothDamp(_currentVertValue, targetVertValue, ref _vertVelocity,
            vertSmoothTime, animationBlendSpeed, Time.deltaTime);
        if (_currentVertValue < 0.001f && targetVertValue <= 0f)
        {
            _currentVertValue = 0f;
            _vertVelocity = 0f;
        }

        if (animator != null)
        {
            animator.SetFloat(VertHash, _currentVertValue);
        }

        if (shouldMove)
        {
            Quaternion targetRotation = Quaternion.LookRotation(_targetDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }

        UpdateIdleVariant(shouldMove);
        UpdateDust();
    }

    public void Move(Vector3 direction, bool keepWalking = false)
    {
        _shouldKeepWalking = keepWalking;

        if (direction.magnitude > movementThreshold)
        {
            _targetDirection = direction.normalized;
            _isMoving = true;
        }
        else if (!keepWalking)
        {
            _isMoving = false;
        }
    }

    /// <summary>
    /// Интенсивность ходьбы 0..1 (масштабирует Vert, при движении не ниже minMovingVert).
    /// Если не вызывать — берётся SmoothMapMovementService.CurrentSpeed01 (если внедрён), иначе 1.
    /// </summary>
    public void SetSpeed01(float speed01)
    {
        _externalSpeed01 = Mathf.Clamp01(speed01);
        _externalSpeedUntil = Time.time + ExternalSpeedTimeout;
    }

    private float GetSpeed01()
    {
        if (Time.time <= _externalSpeedUntil)
            return _externalSpeed01;

        if (_mapMovement != null)
            return _mapMovement.CurrentSpeed01;

        return 1f;
    }

    // ─────────────────────────── Idle variety ───────────────────────────

    private void CacheIdleVariant()
    {
        _idleVariantHash = 0;
        if (animator == null || animator.runtimeAnimatorController == null)
            return;

        var parameters = animator.parameters;
        for (var t = 0; t < IdleVariantTriggers.Length && _idleVariantHash == 0; t++)
        {
            for (var i = 0; i < parameters.Length; i++)
            {
                var p = parameters[i];
                if (p.type == AnimatorControllerParameterType.Trigger && p.name == IdleVariantTriggers[t])
                {
                    _idleVariantHash = p.nameHash;
                    break;
                }
            }
        }
    }

    private void ResetIdleTimer()
    {
        _idleTimer = Random.Range(idleVariantInterval.x, idleVariantInterval.y);
    }

    private void UpdateIdleVariant(bool shouldMove)
    {
        if (_idleVariantHash == 0 || animator == null)
            return;

        if (shouldMove || _currentVertValue > 0.05f)
        {
            ResetIdleTimer();
            return;
        }

        _idleTimer -= Time.deltaTime;
        if (_idleTimer <= 0f)
        {
            animator.SetTrigger(_idleVariantHash);
            ResetIdleTimer();
        }
    }

    // ─────────────────────────── Step dust ───────────────────────────

    private void CreateDust()
    {
        var material = JuiceFxMaterials.ParticleAlpha;
        if (material == null)
            return;

        var h = MeasureHeight();

        var go = new GameObject("StepDust");
        go.layer = gameObject.layer;
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 1f;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(h * 0.05f, h * 0.15f);
        main.startSize = new ParticleSystem.MinMaxCurve(h * 0.10f, h * 0.20f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.78f, 0.76f, 0.72f, 0.35f),
            new Color(0.90f, 0.88f, 0.85f, 0.25f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Local;
        main.maxParticles = 32;

        var emission = ps.emission;
        emission.rateOverTime = 0f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = h * 0.08f;
        shape.rotation = new Vector3(90f, 0f, 0f); // круг лежит на земле, частицы расходятся в стороны

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(0f);
        velocity.y = new ParticleSystem.MinMaxCurve(h * 0.08f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(gradient);

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.6f, 1f, 1.4f));

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        ps.Play();
        _dust = ps;
        _dustRate = 0f;
    }

    private float MeasureHeight()
    {
        var renderers = GetComponentsInChildren<Renderer>();
        var hasBounds = false;
        var bounds = new Bounds();
        for (var i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r == null || r is ParticleSystemRenderer)
                continue;
            if (!hasBounds) { bounds = r.bounds; hasBounds = true; }
            else bounds.Encapsulate(r.bounds);
        }

        var h = hasBounds ? bounds.size.y : 0f;
        return h > 0.01f ? h : 1f;
    }

    private void UpdateDust()
    {
        if (_dust == null)
            return;

        var rate = _currentVertValue > 0.2f ? _currentVertValue * dustMaxRate : 0f;
        if (Mathf.Abs(rate - _dustRate) < 0.25f && !(rate == 0f && _dustRate != 0f))
            return;

        _dustRate = rate;
        var emission = _dust.emission;
        emission.rateOverTime = rate;
    }
}
