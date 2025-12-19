using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Zenject;

public class PromoCodesMediator : IInitializable, IDisposable
{
    private readonly PromoCodesWindow _promoCodesWindow;
    private readonly UIManager _uiManager;
    private readonly APIService _apiService;
    private readonly IImageLoadService _imageLoadService;
    private readonly PromoCodeView.Factory _promoCodeViewFactory;
    private readonly IClipboardService _clipboardService;
    
    private readonly List<PromoCodeView> _spawnedViews = new List<PromoCodeView>();

    public PromoCodesMediator(
        PromoCodesWindow promoCodesWindow,
        UIManager uiManager,
        APIService apiService,
        IImageLoadService imageLoadService,
        PromoCodeView.Factory promoCodeViewFactory,
        IClipboardService clipboardService)
    {
        _promoCodesWindow = promoCodesWindow;
        _uiManager = uiManager;
        _apiService = apiService;
        _imageLoadService = imageLoadService;
        _promoCodeViewFactory = promoCodeViewFactory;
        _clipboardService = clipboardService;
    }
    
    public void Initialize()
    {
        _promoCodesWindow.OnWindowOpened += LoadPromoCodes;
        _promoCodesWindow.OnBackClicked += HandleBackButtonClicked;
    }

    public void Dispose()
    {
        _promoCodesWindow.OnWindowOpened -= LoadPromoCodes;
        _promoCodesWindow.OnBackClicked -= HandleBackButtonClicked;
        DestroyPromoCodeViews();
    }

    private async void LoadPromoCodes()
    {
        DestroyPromoCodeViews();
        
        var (success, response) = await _apiService.GetPromoCodes();
        
        if (!success || response == null || response.promo_codes == null)
        {
            return;
        }

        for (int i = 0; i < response.promo_codes.Length; i++)
        {
            var promoCodeData = response.promo_codes[i];
            var promoCodeView = _promoCodeViewFactory.Create();
            
            promoCodeView.transform.SetParent(_promoCodesWindow.ResultContent, false);
            
            promoCodeView.Init(promoCodeData.promo_code, promoCodeData.quest_title);
            
            promoCodeView.OnCopyButtonClicked += (code) => _clipboardService.CopyToClipboard(code);
            
            _spawnedViews.Add(promoCodeView);
            
            if (!string.IsNullOrEmpty(promoCodeData.quest_image_url))
            {
                _ = LoadAndApplyImageAsync(promoCodeView, promoCodeData.quest_image_url);
            }
        }
    }

    private async Task LoadAndApplyImageAsync(PromoCodeView view, string imageUrl)
    {
        Sprite sprite = await _imageLoadService.LoadSpriteAsync(imageUrl);
        
        if (sprite == null || view == null)
            return;

        view.SetImage(sprite);
    }

    private void DestroyPromoCodeViews()
    {
        foreach (var view in _spawnedViews)
        {
            if (view != null)
            {
                GameObject.Destroy(view.gameObject);
            }
        }
        _spawnedViews.Clear();
    }

    private void HandleBackButtonClicked()
    {
        _uiManager.Back();
    }
}
