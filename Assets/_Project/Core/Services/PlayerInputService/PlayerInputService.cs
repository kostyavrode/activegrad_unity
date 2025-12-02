using UnityEngine;
using Zenject;
using System;
using UnityEngine.InputSystem;

public class PlayerInputService : ITickable, IDisposable
{
    private readonly Camera _mainCamera;
    private readonly SightsUpdater _sightsUpdater;
    private readonly UIManager _uiManager;
    private bool _isEnabled = true;

    [Inject]
    public PlayerInputService(Camera mainCamera, SightsUpdater sightsUpdater, UIManager uiManager)
    {
        _mainCamera = mainCamera;
        _sightsUpdater = sightsUpdater;
        _uiManager = uiManager;
        Debug.Log("PlayerInputService initialized");
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
    }
    
    public void SetEnabled(bool enabled) => _isEnabled = enabled;
    
    public void Dispose()
    {
        // Cleanup if needed
    }
}