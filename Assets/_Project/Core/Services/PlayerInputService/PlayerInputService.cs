using UnityEngine;
using Zenject;
using System;
using UnityEngine.InputSystem;

public class PlayerInputService : ITickable, IDisposable
{
    private readonly Camera _mainCamera;
    private readonly SightsUpdater _sightsUpdater;
    private readonly UIManager _uiManager;
    private readonly GameEventService _gameEventService;
    private bool _isEnabled = true;

    private Vector2 _touchStartPosition;
    private bool _isDragging;
    private const float DragThreshold = 15f;

    // Настройка прямоугольников в ВЕРХНИХ углах
    [Header("Top-Left Corner Settings")]
    private readonly bool _enableTopLeftCorner = true;
    private readonly Vector2 _topLeftCornerSize = new Vector2(250f, 250f); // width, height
    private readonly Vector2 _topLeftCornerOffset = new Vector2(0f, 0f);    // x offset from left, y offset from TOP
    
    [Header("Top-Right Corner Settings")]
    private readonly bool _enableTopRightCorner = true;
    private readonly Vector2 _topRightCornerSize = new Vector2(250f, 600f); // width, height
    private readonly Vector2 _topRightCornerOffset = new Vector2(0f, 0f);   // x offset from right, y offset from TOP
    
    // Прямоугольники
    private Rect _topLeftRect;
    private Rect _topRightRect;

    [Inject]
    public PlayerInputService(Camera mainCamera, SightsUpdater sightsUpdater, UIManager uiManager, GameEventService gameEventService)
    {
        _mainCamera = mainCamera;
        _sightsUpdater = sightsUpdater;
        _uiManager = uiManager;
        _gameEventService = gameEventService;
        
        InitializeRects();
    }

    private void InitializeRects()
    {
        // Левый ВЕРХНИЙ угол - Y считается от ВЕРХНЕГО края экрана
        _topLeftRect = new Rect(
            _topLeftCornerOffset.x,
            _topLeftCornerOffset.y,  // Y от верхнего края (0 = верх экрана)
            _topLeftCornerSize.x,
            _topLeftCornerSize.y
        );
        
        // Правый ВЕРХНИЙ угол
        _topRightRect = new Rect(
            Screen.width - _topRightCornerSize.x - _topRightCornerOffset.x,
            _topRightCornerOffset.y,  // Y от верхнего края (0 = верх экрана)
            _topRightCornerSize.x,
            _topRightCornerSize.y
        );
    }

    public void Tick()
    {
        if (!_isEnabled) return;
        if (!_uiManager.IsActiveWindow<MenuWindow>()) return;

        var canvas = GameObject.FindGameObjectWithTag("Canvas");
        if (canvas != null && canvas.transform.childCount > 1) return;
        
        UpdateRectsPosition();

#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouseInput();
#else
        HandleTouchInput();
#endif
    }
    
    private void UpdateRectsPosition()
    {
        if (_enableTopRightCorner)
        {
            _topRightRect.x = Screen.width - _topRightCornerSize.x - _topRightCornerOffset.x;
            _topRightRect.y = _topRightCornerOffset.y;  // Сохраняем Y от верхнего края
        }
        
        if (_enableTopLeftCorner)
        {
            _topLeftRect.x = _topLeftCornerOffset.x;
            _topLeftRect.y = _topLeftCornerOffset.y;  // Сохраняем Y от верхнего края
        }
    }

    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _touchStartPosition = Input.mousePosition;
            _isDragging = false;
        }

        if (Input.GetMouseButton(0) && !_isDragging)
        {
            if (Vector2.Distance(Input.mousePosition, _touchStartPosition) > DragThreshold)
                _isDragging = true;
        }

        if (Input.GetMouseButtonUp(0) && !_isDragging)
        {
            Vector2 mousePosition = Input.mousePosition;
            // Преобразуем координаты мыши в UI координаты (Y от верхнего края)
            Vector2 uiPosition = new Vector2(mousePosition.x, Screen.height - mousePosition.y);
            
            if (IsPointInTopCorners(uiPosition))
                return;
                
            Ray ray = _mainCamera.ScreenPointToRay(mousePosition);
            CheckHit(ray);
        }
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount != 1)
            return;

        Touch touch = Input.GetTouch(0);

        if (touch.phase == UnityEngine.TouchPhase.Began)
        {
            _touchStartPosition = touch.position;
            _isDragging = false;
        }
        else if (touch.phase == UnityEngine.TouchPhase.Moved)
        {
            if (Vector2.Distance(touch.position, _touchStartPosition) > DragThreshold)
                _isDragging = true;
        }
        else if (touch.phase == UnityEngine.TouchPhase.Ended && !_isDragging)
        {
            Vector2 uiPosition = new Vector2(touch.position.x, Screen.height - touch.position.y);
            
            if (IsPointInTopCorners(uiPosition))
                return;
                
            Ray ray = _mainCamera.ScreenPointToRay(touch.position);
            CheckHit(ray);
        }
    }

    private bool IsPointInTopCorners(Vector2 screenPoint)
    {
        if (_enableTopLeftCorner && _topLeftRect.Contains(screenPoint))
        {
            Debug.Log($"Click blocked by TOP-LEFT corner area: {_topLeftRect}");
            return true;
        }
        
        if (_enableTopRightCorner && _topRightRect.Contains(screenPoint))
        {
            Debug.Log($"Click blocked by TOP-RIGHT corner area: {_topRightRect}");
            return true;
        }
        
        return false;
    }

    private void CheckHit(Ray ray)
    {
        if (Physics.Raycast(ray, out var hit))
        {
            GameObject clickedObject = hit.collider.gameObject;
            OnObjectClicked(clickedObject);
        }
    }

    private void OnObjectClicked(GameObject obj)
    {
        if (obj.TryGetComponent(out SightObject sightObject))
        {   
            _sightsUpdater.CreateSightDetailsPopup(sightObject.GetSightInfo());
        }
        else if (obj.TryGetComponent(out PartnerStoreObject partnerStoreObject))
        {
            _sightsUpdater.CreatePartnerStoreDetailsPopup(partnerStoreObject.GetStoreID());
        }
        else if (obj.TryGetComponent(out GameEventObject gameEventObject))
        {
            _gameEventService.HandleEventClicked(gameEventObject.GetEventId());
        }
    }
    
    public void SetEnabled(bool enabled) => _isEnabled = enabled;
    
    public void DrawDebugRects()
    {
        #if UNITY_EDITOR
        Debug.Log($"TOP-LEFT Rect: {_topLeftRect}");
        Debug.Log($"TOP-RIGHT Rect: {_topRightRect}");
        #endif
    }
    
    public void Dispose()
    {
        // Cleanup if needed
    }
}