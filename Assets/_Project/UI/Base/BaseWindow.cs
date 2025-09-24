using UnityEngine;

public abstract class BaseWindow : MonoBehaviour, IWindow
{
    [SerializeField] private CanvasGroup canvasGroup;
    
    public bool IsVisible { get; private set; }
    
    public virtual void Show()
    {
        IsVisible = true;
        canvasGroup.alpha = 1;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
        
        gameObject.SetActive(true);
        
        OnShow();
    }

    public void Hide()
    {
        IsVisible = false;
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        
        OnHide();
        
        gameObject.SetActive(false);
    }
    
    protected virtual void OnShow() { }
    protected virtual void OnHide() { }
}
