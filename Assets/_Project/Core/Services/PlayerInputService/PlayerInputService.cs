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
        
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
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