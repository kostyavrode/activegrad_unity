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

    // Настраиваемые прямоугольники
    private Rect _topLeftRect;
    private Rect _topRightRect;
    
    // Параметры прямоугольников (можно изменить здесь)
    private readonly Vector2 _topLeftRectSize = new Vector2(200f, 200f);
    private readonly Vector2 _topRightRectSize = new Vector2(200f, 200f);
    private readonly Vector2 _topLeftRectOffset = new Vector2(0f, 0f); // Смещение от левого верхнего угла
    private readonly Vector2 _topRightRectOffset = new Vector2(0f, 0f); // Смещение от правого верхнего угла
    
    // Флаги включения/выключения прямоугольников
    private readonly bool _enableTopLeftRect = true;
    private readonly bool _enableTopRightRect = true;

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
        _topLeftRect = new Rect(
            _topLeftRectOffset.x,
            _topLeftRectOffset.y,
            _topLeftRectSize.x,
            _topLeftRectSize.y
        );
        
        _topRightRect = new Rect(
            Screen.width - _topRightRectSize.x - _topRightRectOffset.x,
            _topRightRectOffset.y,
            _topRightRectSize.x,
            _topRightRectSize.y
        );
    }

    public void Tick()
    {
        if (!_isEnabled) return;
        if (!_uiManager.IsActiveWindow<MenuWindow>()) return;

        var canvas = GameObject.FindGameObjectWithTag("Canvas");
        if (canvas != null && canvas.transform.childCount > 1) return;
        
        // Обновляем позицию правого прямоугольника при изменении размера экрана
        UpdateRectsPosition();

#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouseInput();
#else
        HandleTouchInput();
#endif
    }
    
    private void UpdateRectsPosition()
    {
        // Обновляем позицию правого прямоугольника при изменении размера экрана
        if (_enableTopRightRect)
        {
            _topRightRect.x = Screen.width - _topRightRect.width - _topRightRectOffset.x;
            _topRightRect.y = _topRightRectOffset.y;
        }
        
        // Левый прямоугольник тоже может менять позицию при изменении оффсета
        if (_enableTopLeftRect)
        {
            _topLeftRect.x = _topLeftRectOffset.x;
            _topLeftRect.y = _topLeftRectOffset.y;
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
            // Проверяем, не попал ли клик в UI прямоугольники
            if (IsPointInUIBlocks(Input.mousePosition))
            {
                Debug.Log("UI tapped");
            }
                return;
                
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
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
            // Проверяем, не попал ли клик в UI прямоугольники
            if (IsPointInUIBlocks(touch.position))
                return;
                
            Ray ray = _mainCamera.ScreenPointToRay(touch.position);
            CheckHit(ray);
        }
    }

    private bool IsPointInUIBlocks(Vector2 screenPoint)
    {
        if (_enableTopLeftRect && _topLeftRect.Contains(screenPoint))
            return true;
            
        if (_enableTopRightRect && _topRightRect.Contains(screenPoint))
            return true;
            
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
    
    // Опциональные публичные методы для динамической настройки (если понадобятся)
    public void SetTopLeftRectActive(bool active)
    {
        // Для возможности динамического изменения через рефлексию или другие системы
        var field = GetType().GetField("_enableTopLeftRect", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null && field.IsInitOnly == false)
        {
            field.SetValue(this, active);
        }
    }
    
    public void SetTopRightRectActive(bool active)
    {
        var field = GetType().GetField("_enableTopRightRect", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null && field.IsInitOnly == false)
        {
            field.SetValue(this, active);
        }
    }
    
    public void Dispose()
    {
        // Cleanup if needed
    }
}