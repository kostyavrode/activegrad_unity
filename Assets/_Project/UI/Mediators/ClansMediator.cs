using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Zenject;

public class ClansMediator : IInitializable, IDisposable
{
    private readonly ClansWindow _clansWindow;
    private readonly UIManager _uiManager;
    private readonly APIService _apiService;
    private readonly ClanView.Factory _clanViewFactory;
    private readonly CreateClanView.Factory _createClanViewFactory;
    private readonly IPopupService _popupService;
    
    private List<ClanView> _spawnedClanViews = new List<ClanView>();
    private ClanData _myClan;
    private CreateClanView _currentCreateClanView;
    
    public ClansMediator(
        ClansWindow clansWindow,
        UIManager uiManager,
        APIService apiService,
        ClanView.Factory clanViewFactory,
        CreateClanView.Factory createClanViewFactory,
        IPopupService popupService)
    {
        _clansWindow = clansWindow;
        _uiManager = uiManager;
        _apiService = apiService;
        _clanViewFactory = clanViewFactory;
        _createClanViewFactory = createClanViewFactory;
        _popupService = popupService;
    }
    
    public void Initialize()
    {
        _clansWindow.OnWindowOpened += LoadTopClans;
        _clansWindow.OnBackClicked += HandleBackButtonClicked;
        _clansWindow.OnTopClansTabClicked += LoadTopClans;
        _clansWindow.OnSearchTabClicked += HandleSearchTabClicked;
        _clansWindow.OnMyClanTabClicked += LoadMyClan;
        _clansWindow.OnCreateClanTabClicked += HandleCreateClanTabClicked;
        _clansWindow.OnSearchClicked += HandleSearchClicked;
    }
    
    public void Dispose()
    {
        _clansWindow.OnWindowOpened -= LoadTopClans;
        _clansWindow.OnBackClicked -= HandleBackButtonClicked;
        _clansWindow.OnTopClansTabClicked -= LoadTopClans;
        _clansWindow.OnSearchTabClicked -= HandleSearchTabClicked;
        _clansWindow.OnMyClanTabClicked -= LoadMyClan;
        _clansWindow.OnCreateClanTabClicked -= HandleCreateClanTabClicked;
        _clansWindow.OnSearchClicked -= HandleSearchClicked;
        
        if (_currentCreateClanView != null)
        {
            _currentCreateClanView.OnCreateClicked -= HandleCreateClan;
            _currentCreateClanView.OnCancelClicked -= HandleCancelCreateClan;
            if (_currentCreateClanView.gameObject != null)
            {
                UnityEngine.Object.Destroy(_currentCreateClanView.gameObject);
            }
        }
        
        DestroyAllViews();
    }
    
    private async void LoadTopClans()
    {
        _clansWindow.ClearContent();
        DestroyAllViews();
        _clansWindow.ShowSearchPanel(false);
        
        var (success, response) = await _apiService.GetTopClans();
        
        if (success && response != null && response.top_clans != null)
        {
            foreach (var clanData in response.top_clans)
            {
                var clanView = _clanViewFactory.Create();
                clanView.transform.SetParent(_clansWindow.Content, false);
                clanView.Init(clanData, false);
                clanView.OnJoinClicked += HandleJoinClan;
                _spawnedClanViews.Add(clanView);
            }
        }
        else
        {
            _popupService.ShowError("Не удалось загрузить топ-10 кланов");
        }
    }
    
    private void HandleSearchTabClicked()
    {
        _clansWindow.ClearContent();
        DestroyAllViews();
        _clansWindow.ShowSearchPanel(true);
    }
    
    private async void HandleSearchClicked(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            _popupService.ShowError("Введите название клана для поиска");
            return;
        }
        
        _clansWindow.ClearContent();
        DestroyAllViews();
        
        var (success, response) = await _apiService.SearchClans(query);
        
        if (success && response != null && response.clans != null)
        {
            if (response.clans.Length == 0)
            {
                _popupService.ShowInfo("Кланы не найдены");
                return;
            }
            
            foreach (var clanData in response.clans)
            {
                var clanView = _clanViewFactory.Create();
                clanView.transform.SetParent(_clansWindow.Content, false);
                clanView.Init(clanData, false);
                clanView.OnJoinClicked += HandleJoinClan;
                _spawnedClanViews.Add(clanView);
            }
        }
        else
        {
            _popupService.ShowError("Не удалось выполнить поиск");
        }
    }
    
    private async void LoadMyClan()
    {
        _clansWindow.ClearContent();
        DestroyAllViews();
        _clansWindow.ShowSearchPanel(false);
        
        var (success, clan) = await _apiService.GetMyClan();
        
        if (success)
        {
            if (clan != null)
            {
                _myClan = clan;
                var clanView = _clanViewFactory.Create();
                clanView.transform.SetParent(_clansWindow.Content, false);
                clanView.Init(clan, true);
                clanView.OnLeaveClicked += HandleLeaveClan;
                _spawnedClanViews.Add(clanView);
            }
            else
            {
                _popupService.ShowInfo("Вы не состоите в клане");
            }
        }
        else
        {
            _popupService.ShowError("Не удалось загрузить информацию о вашем клане");
        }
    }
    
    private async void HandleJoinClan(int clanId)
    {
        var (success, response) = await _apiService.JoinClan(clanId);
        
        if (success && response != null)
        {
            _popupService.ShowSuccess(response.message ?? "Вы успешно вступили в клан!");
            // Обновляем информацию о клане
            await RefreshMyClan();
            // Перезагружаем текущую вкладку
            if (_clansWindow.Content.childCount > 0)
            {
                LoadTopClans(); // Перезагружаем топ-10 или поиск
            }
        }
        else
        {
            _popupService.ShowError("Не удалось вступить в клан");
        }
    }
    
    private async void HandleLeaveClan()
    {
        var (success, message) = await _apiService.LeaveClan();
        
        if (success)
        {
            _popupService.ShowSuccess(message ?? "Вы покинули клан");
            _myClan = null;
            // Обновляем информацию о клане
            await RefreshMyClan();
            // Перезагружаем вкладку "Мой клан"
            LoadMyClan();
        }
        else
        {
            _popupService.ShowError(message ?? "Не удалось покинуть клан");
        }
    }
    
    private async Task RefreshMyClan()
    {
        var (success, clan) = await _apiService.GetMyClan();
        if (success)
        {
            _myClan = clan;
        }
    }
    
    private void DestroyAllViews()
    {
        foreach (var view in _spawnedClanViews)
        {
            if (view != null)
            {
                view.OnJoinClicked -= HandleJoinClan;
                view.OnLeaveClicked -= HandleLeaveClan;
                if (view.gameObject != null)
                {
                    UnityEngine.Object.Destroy(view.gameObject);
                }
            }
        }
        _spawnedClanViews.Clear();
    }
    
    private void HandleCreateClanTabClicked()
    {
        // Удаляем предыдущее окно, если оно есть
        if (_currentCreateClanView != null)
        {
            _currentCreateClanView.OnCreateClicked -= HandleCreateClan;
            _currentCreateClanView.OnCancelClicked -= HandleCancelCreateClan;
            UnityEngine.Object.Destroy(_currentCreateClanView.gameObject);
        }
        
        // Создаем новое окно через Factory
        _currentCreateClanView = _createClanViewFactory.Create();
        _currentCreateClanView.transform.SetParent(GameObject.FindGameObjectWithTag("Canvas").transform, false);
        _currentCreateClanView.Reset();
        
        // Подписываемся на события
        _currentCreateClanView.OnCreateClicked += HandleCreateClan;
        _currentCreateClanView.OnCancelClicked += HandleCancelCreateClan;
    }
    
    private async void HandleCreateClan(string name, string description)
    {
        var (success, response) = await _apiService.CreateClan(name, description);
        
        if (success && response != null)
        {
            _popupService.ShowSuccess(response.message ?? "Клан успешно создан!");
            if (_currentCreateClanView != null)
            {
                _currentCreateClanView.OnCreateClicked -= HandleCreateClan;
                _currentCreateClanView.OnCancelClicked -= HandleCancelCreateClan;
                _currentCreateClanView.Close();
                _currentCreateClanView = null;
            }
            
            // Обновляем информацию о клане
            await RefreshMyClan();
            
            // Переключаемся на вкладку "Мой клан" и загружаем информацию
            LoadMyClan();
        }
        else
        {
            // Обработка ошибок валидации
            var errorResponse = _apiService.GetLastErrorResponse();
            if (_currentCreateClanView != null)
            {
                if (errorResponse != null && errorResponse.errors != null && errorResponse.errors.Count > 0)
                {
                    // Показываем ошибки валидации
                    foreach (var error in errorResponse.errors)
                    {
                        if (error.Value != null && error.Value.Length > 0)
                        {
                            _currentCreateClanView.ShowValidationError(error.Key, error.Value);
                            break; // Показываем только первую ошибку
                        }
                    }
                }
                else if (errorResponse != null && !string.IsNullOrEmpty(errorResponse.error))
                {
                    _currentCreateClanView.ShowError(errorResponse.error);
                }
                else if (errorResponse != null && !string.IsNullOrEmpty(errorResponse.message))
                {
                    _currentCreateClanView.ShowError(errorResponse.message);
                }
                else
                {
                    _currentCreateClanView.ShowError("Не удалось создать клан");
                }
            }
        }
    }
    
    private void HandleCancelCreateClan()
    {
        if (_currentCreateClanView != null)
        {
            _currentCreateClanView.OnCreateClicked -= HandleCreateClan;
            _currentCreateClanView.OnCancelClicked -= HandleCancelCreateClan;
            _currentCreateClanView.Close();
            _currentCreateClanView = null;
        }
    }
    
    private void HandleBackButtonClicked()
    {
        _uiManager.Back();
    }
}

