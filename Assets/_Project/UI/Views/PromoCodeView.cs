using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class PromoCodeView : MonoBehaviour
{
    [SerializeField] private Button _copyButton;
    [SerializeField] private TMP_Text _promoCodeText;
    [SerializeField] private TMP_Text _headerText;
    [SerializeField] private Image _image;
    
    public event Action<string> OnCopyButtonClicked; 

    private void Awake()
    {
        _copyButton.onClick.AddListener(HandleCopyButtonClick);
    }

    private void OnDestroy()
    {
        _copyButton.onClick.RemoveAllListeners();
    }

    public void Init(string promoCode, string headerText)
    {
        _promoCodeText.text = promoCode;
        _headerText.text = headerText;
    }

    public void SetImage(Sprite sprite)
    {
        _image.sprite = sprite;
    }

    private void HandleCopyButtonClick()
    {
        OnCopyButtonClicked?.Invoke(_promoCodeText.text);
    }
    
    public class Factory : PlaceholderFactory<PromoCodeView> { }
}
