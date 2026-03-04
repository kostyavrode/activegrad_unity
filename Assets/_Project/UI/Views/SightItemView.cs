using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SightItemView : MonoBehaviour
{
    public TMP_Text Title;
    public TMP_Text Distance;
    
    public Image Image;
    public Button Button;
    
    public int PageId;
    
    public Action<int> OnClicked;

    private void Awake()
    {
        Button.onClick.AddListener(() => OnClicked?.Invoke(PageId));
    }

    private void OnDisable()
    {
        Button.onClick.RemoveAllListeners();
    }

    public void SetImage(Sprite sprite)
    {
        if (this == null || Image == null) return;
        Image.sprite = sprite;
    }
}