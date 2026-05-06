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

    [Inject]
    public PlayerInputService(Camera mainCamera, SightsUpdater sightsUpdater, UIManager uiManager, GameEventService gameEventService)
    {
        _mainCamera = mainCamera;
        _sightsUpdater = sightsUpdater;
        _uiManager = uiManager;
        _gameEventService = gameEventService;
    }

    public void Tick()
    {
        if (!_isEnabled) return;
        if (!_uiManager.IsActiveWindow<MenuWindow>()) return;

        var canvas = GameObject.FindGameObjectWithTag("Canvas");
        if (canvas != null && canvas.transform.childCount > 1) return;

#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouseInput();
#else
        HandleTouchInput();
#endif
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
            Ray ray = _mainCamera.ScreenPointToRay(touch.position);
            CheckHit(ray);
        }
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
    
    public void Dispose()
    {
        // Cleanup if needed
    }
}