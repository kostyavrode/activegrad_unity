using UnityEngine;
using Zenject;

public class PopupService : IPopupService
{
    private readonly PopupView.Factory _popupFactory;
    private Canvas _uiCanvas;

    public PopupService(PopupView.Factory popupFactory)
    {
        _popupFactory = popupFactory;
        _uiCanvas = GameObject.FindObjectOfType<Canvas>();
    }

    public void ShowError(string message)
    {
        CreatePopup(message, Color.red);
    }

    public void ShowInfo(string message)
    {
        CreatePopup(message, Color.blue);
    }

    public void ShowSuccess(string message)
    {
        CreatePopup(message, Color.green);
    }

    private void CreatePopup(string message, Color color)
    {
        var popup = _popupFactory.Create();
        popup.transform.SetParent(_uiCanvas.transform, false);
        popup.Setup(message, color);
    }
    
}