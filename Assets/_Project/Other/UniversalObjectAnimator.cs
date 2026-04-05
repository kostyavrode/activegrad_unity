using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public class UniversalObjectAnimator : MonoBehaviour
{
    [Header("Основные настройки")]
    [SerializeField] private bool animateOnStart = true;
    [SerializeField] private bool randomizeOnStart = true;
    
    [Header("Вращение (Rotation)")]
    [SerializeField] private bool enableRotation = true;
    [SerializeField] private Vector2 rotationSpeedRange = new Vector2(30f, 180f);
    [SerializeField] private bool rotateX = false;
    [SerializeField] private bool rotateY = true;
    [SerializeField] private bool rotateZ = false;
    [SerializeField] private RotateMode rotateMode = RotateMode.FastBeyond360;
    
    [Header("Левитация (Vertical Movement)")]
    [SerializeField] private bool enableLevitation = true;
    [SerializeField] private Vector2 levitationSpeedRange = new Vector2(1f, 3f);
    [SerializeField] private Vector2 levitationHeightRange = new Vector2(0.5f, 1.5f);
    [SerializeField] private Ease levitationEase = Ease.InOutSine;
    
    [Header("Scale (Scale)")]
    [SerializeField] private bool enableScaleAnimation = true;
    [SerializeField] private Vector2 scaleSpeedRange = new Vector2(0.5f, 2f);
    [SerializeField] private Vector2 scaleMultiplierRange = new Vector2(0.8f, 1.2f);
    [SerializeField] private Ease scaleEase = Ease.InOutSine;
    
    [Header("Дополнительные эффекты")]
    [SerializeField] private bool enableColorPulse = false;
    [SerializeField] private Vector2 colorPulseSpeedRange = new Vector2(0.5f, 2f);
    [SerializeField] private Gradient colorGradient;
    [SerializeField] private List<Renderer> targetRenderers = new List<Renderer>();
    
    [Header("Path Movement (Движение по пути)")]
    [SerializeField] private bool enablePathMovement = false;
    [SerializeField] private Vector3[] pathPoints;
    [SerializeField] private Vector2 pathSpeedRange = new Vector2(2f, 5f);
    [SerializeField] private bool loopPath = true;
    [SerializeField] private PathType pathType = PathType.CatmullRom;
    
    [Header("Пульсация (Pulsation)")]
    [SerializeField] private bool enablePulsation = false;
    [SerializeField] private Vector2 pulsationSpeedRange = new Vector2(1f, 4f);
    [SerializeField] private float pulsationIntensity = 0.1f;
    
    [Header("Random Delays")]
    [SerializeField] private Vector2 startDelayRange = new Vector2(0f, 0.5f);
    
    // Приватные переменные
    private Vector3 originalPosition;
    private Vector3 originalScale;
    private Quaternion originalRotation;
    private Color[] originalColors;
    private Sequence mainSequence;
    private Tween rotationTween;
    private Tween levitationTween;
    private Tween scaleTween;
    private Tween colorTween;
    private Tween pathTween;
    private float currentRotationSpeed;
    private float currentLevitationSpeed;
    private float currentScaleSpeed;
    
    private void Start()
    {
        InitializeOriginalValues();
        
        if (animateOnStart)
        {
            if (randomizeOnStart)
                RandomizeAndStart();
            else
                StartAnimation();
        }
    }
    
    private void InitializeOriginalValues()
    {
        originalPosition = transform.position;
        originalScale = transform.localScale;
        originalRotation = transform.rotation;
        
        if (enableColorPulse && targetRenderers.Count > 0)
        {
            originalColors = new Color[targetRenderers.Count];
            for (int i = 0; i < targetRenderers.Count; i++)
            {
                if (targetRenderers[i] != null && targetRenderers[i].material != null)
                    originalColors[i] = targetRenderers[i].material.color;
            }
        }
    }
    
    [ContextMenu("Randomize And Start")]
    public void RandomizeAndStart()
    {
        StopAllAnimations();
        GenerateRandomValues();
        StartAnimation();
    }
    
    [ContextMenu("Start Animation")]
    public void StartAnimation()
    {
        StopAllAnimations();
        
        float startDelay = Random.Range(startDelayRange.x, startDelayRange.y);
        
        // Создаем основную последовательность
        mainSequence = DOTween.Sequence();
        mainSequence.AppendInterval(startDelay);
        mainSequence.OnComplete(() => {
            StartRotation();
            StartLevitation();
            StartScaleAnimation();
            StartColorPulse();
            StartPathMovement();
            StartPulsation();
        });
        mainSequence.Play();
    }
    
    [ContextMenu("Stop All Animations")]
    public void StopAllAnimations()
    {
        mainSequence?.Kill();
        rotationTween?.Kill();
        levitationTween?.Kill();
        scaleTween?.Kill();
        colorTween?.Kill();
        pathTween?.Kill();
        
        // Возвращаем в исходное состояние
        transform.position = originalPosition;
        transform.localScale = originalScale;
        transform.rotation = originalRotation;
        
        if (originalColors != null)
        {
            for (int i = 0; i < targetRenderers.Count; i++)
            {
                if (targetRenderers[i] != null && targetRenderers[i].material != null && i < originalColors.Length)
                    targetRenderers[i].material.color = originalColors[i];
            }
        }
    }
    
    private void GenerateRandomValues()
    {
        currentRotationSpeed = Random.Range(rotationSpeedRange.x, rotationSpeedRange.y);
        currentLevitationSpeed = Random.Range(levitationSpeedRange.x, levitationSpeedRange.y);
        currentScaleSpeed = Random.Range(scaleSpeedRange.x, scaleSpeedRange.y);
    }
    
    private void StartRotation()
    {
        if (!enableRotation) return;
        
        Vector3 rotationAxis = new Vector3(
            rotateX ? 1f : 0f,
            rotateY ? 1f : 0f,
            rotateZ ? 1f : 0f
        ).normalized;
        
        if (rotationAxis == Vector3.zero) return;
        
        rotationTween = transform.DORotate(
            rotationAxis * 360f, 
            360f / currentRotationSpeed, 
            rotateMode
        ).SetLoops(-1, LoopType.Incremental).SetEase(Ease.Linear);
    }
    
    private void StartLevitation()
    {
        if (!enableLevitation) return;
        
        float height = Random.Range(levitationHeightRange.x, levitationHeightRange.y);
        float duration = 1f / currentLevitationSpeed;
        
        levitationTween = transform.DOMoveY(
            originalPosition.y + height, 
            duration
        ).SetLoops(-1, LoopType.Yoyo).SetEase(levitationEase).SetRelative(false);
    }
    
    private void StartScaleAnimation()
    {
        if (!enableScaleAnimation) return;
        
        float multiplier = Random.Range(scaleMultiplierRange.x, scaleMultiplierRange.y);
        float duration = 1f / currentScaleSpeed;
        
        scaleTween = transform.DOScale(
            originalScale * multiplier, 
            duration
        ).SetLoops(-1, LoopType.Yoyo).SetEase(scaleEase);
    }
    
    private void StartColorPulse()
    {
        if (!enableColorPulse || targetRenderers.Count == 0 || colorGradient == null) return;
        
        float speed = Random.Range(colorPulseSpeedRange.x, colorPulseSpeedRange.y);
        float duration = 1f / speed;
        
        // Создаем анимацию цвета для каждого рендерера
        foreach (var renderer in targetRenderers)
        {
            if (renderer != null && renderer.material != null)
            {
                renderer.material.DOColor(colorGradient.Evaluate(0f), duration)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);
            }
        }
    }
    
    private void StartPathMovement()
    {
        if (!enablePathMovement || pathPoints == null || pathPoints.Length < 2) return;
        
        float speed = Random.Range(pathSpeedRange.x, pathSpeedRange.y);
        float duration = Vector3.Distance(pathPoints[0], pathPoints[pathPoints.Length - 1]) / speed;
        
        pathTween = transform.DOPath(
            pathPoints, 
            duration, 
            pathType
        ).SetOptions(false).SetLookAt(0.01f).SetLoops(loopPath ? -1 : 0, LoopType.Restart).SetEase(Ease.Linear);
    }
    
    private void StartPulsation()
    {
        if (!enablePulsation) return;
        
        float speed = Random.Range(pulsationSpeedRange.x, pulsationSpeedRange.y);
        float duration = 1f / speed;
        
        Sequence pulseSequence = DOTween.Sequence();
        pulseSequence.Append(transform.DOScale(originalScale * (1f + pulsationIntensity), duration / 2f).SetEase(Ease.OutQuad));
        pulseSequence.Append(transform.DOScale(originalScale, duration / 2f).SetEase(Ease.InQuad));
        pulseSequence.SetLoops(-1);
    }
    
    [ContextMenu("Regenerate Random Values")]
    public void RegenerateRandomValues()
    {
        GenerateRandomValues();
        if (mainSequence != null && mainSequence.IsActive())
        {
            StopAllAnimations();
            StartAnimation();
        }
    }
    
    [ContextMenu("Set Default Path Points")]
    public void SetDefaultPathPoints()
    {
        pathPoints = new Vector3[]
        {
            transform.position,
            transform.position + Vector3.right * 2f,
            transform.position + Vector3.forward * 2f,
            transform.position + Vector3.left * 2f,
            transform.position + Vector3.back * 2f,
            transform.position
        };
    }
    
    private void OnDrawGizmosSelected()
    {
        // Визуализация пути в редакторе
        if (enablePathMovement && pathPoints != null && pathPoints.Length > 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < pathPoints.Length; i++)
            {
                Gizmos.DrawSphere(pathPoints[i], 0.1f);
                if (i < pathPoints.Length - 1)
                    Gizmos.DrawLine(pathPoints[i], pathPoints[i + 1]);
            }
        }
        
        // Визуализация высоты левитации
        if (enableLevitation)
        {
            Gizmos.color = Color.cyan;
            float maxHeight = originalPosition.y + levitationHeightRange.y;
            float minHeight = originalPosition.y + levitationHeightRange.x;
            Gizmos.DrawWireSphere(new Vector3(originalPosition.x, maxHeight, originalPosition.z), 0.1f);
            Gizmos.DrawWireSphere(new Vector3(originalPosition.x, minHeight, originalPosition.z), 0.1f);
            Gizmos.DrawLine(new Vector3(originalPosition.x, minHeight, originalPosition.z), 
                           new Vector3(originalPosition.x, maxHeight, originalPosition.z));
        }
    }
    
    private void OnDestroy()
    {
        StopAllAnimations();
    }
}