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
    private readonly ClanPageView.Factory _clanPageViewFactory;
    private readonly CreateClanView.Factory _createClanViewFactory;
    private readonly IPopupService _popupService;

    private List<ClanView> _spawnedClanViews = new List<ClanView>();
    private ClanData _myClan;
    private CreateClanView _currentCreateClanView;
    private ClanPageView _currentClanPageView;

    public ClansMediator(
        ClansWindow clansWindow,
        UIManager uiManager,
        APIService apiService,
        ClanView.Factory clanViewFactory,
        ClanPageView.Factory clanPageViewFactory,
        CreateClanView.Factory createClanViewFactory,
        IPopupService popupService)
    {
        _clansWindow = clansWindow;
        _uiManager = uiManager;
        _apiService = apiService;
        _clanViewFactory = clanViewFactory;
        _clanPageViewFactory = clanPageViewFactory;
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

        CloseClanPageView();
        CloseCreateClanView();
        DestroyAllClanViews();
    }

    // ── Tabs ────────────────────────────────────────────────────────────────

    private async void LoadTopClans()
    {
        _clansWindow.ClearContent();
        DestroyAllClanViews();
        _clansWindow.ShowSearchPanel(false);

        var (success, response) = await _apiService.GetTopClans();

        if (success && response?.top_clans != null)
        {
            foreach (var clanData in response.top_clans)
                SpawnClanView(clanData, isMyClan: false);
        }
        else
        {
            _popupService.ShowError("Не удалось загрузить топ-10 кланов");
        }
    }

    private void HandleSearchTabClicked()
    {
        _clansWindow.ClearContent();
        DestroyAllClanViews();
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
        DestroyAllClanViews();

        var (success, response) = await _apiService.SearchClans(query);

        if (success && response?.clans != null)
        {
            if (response.clans.Length == 0)
            {
                _popupService.ShowInfo("Кланы не найдены");
                return;
            }

            foreach (var clanData in response.clans)
                SpawnClanView(clanData, isMyClan: false);
        }
        else
        {
            _popupService.ShowError("Не удалось выполнить поиск");
        }
    }

    private async void LoadMyClan()
    {
        _clansWindow.ClearContent();
        DestroyAllClanViews();
        _clansWindow.ShowSearchPanel(false);

        var (success, clan) = await _apiService.GetMyClan();

        if (success)
        {
            if (clan != null)
            {
                _myClan = clan;
                SpawnClanView(clan, isMyClan: true);
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

    // ── ClanView spawning ────────────────────────────────────────────────────

    private void SpawnClanView(ClanData clan, bool isMyClan)
    {
        var view = _clanViewFactory.Create();
        view.transform.SetParent(_clansWindow.Content, false);
        view.Init(clan, isMyClan);

        if (isMyClan)
            view.OnLeaveClicked += HandleLeaveClan;
        else
            view.OnJoinClicked += HandleJoinClan;

        view.OnOpenPageClicked += HandleOpenClanPage;
        _spawnedClanViews.Add(view);
    }

    private void DestroyAllClanViews()
    {
        foreach (var view in _spawnedClanViews)
        {
            if (view == null) continue;
            view.OnJoinClicked -= HandleJoinClan;
            view.OnLeaveClicked -= HandleLeaveClan;
            view.OnOpenPageClicked -= HandleOpenClanPage;
            if (view.gameObject != null)
                UnityEngine.Object.Destroy(view.gameObject);
        }
        _spawnedClanViews.Clear();
    }

    // ── ClanPageView ─────────────────────────────────────────────────────────

    private void HandleOpenClanPage(ClanData clan)
    {
        CloseClanPageView();

        bool isMyClan = _myClan != null && _myClan.id == clan.id;

        _currentClanPageView = _clanPageViewFactory.Create();
        _currentClanPageView.transform.SetParent(GameObject.FindGameObjectWithTag("Canvas").transform, false);
        _currentClanPageView.Init(clan, isMyClan);

        _currentClanPageView.OnBackClicked += HandleClanPageBack;
        _currentClanPageView.OnJoinClicked += HandleClanPageJoin;
        _currentClanPageView.OnLeaveClicked += HandleClanPageLeave;
    }

    private async void HandleClanPageJoin(int clanId)
    {
        var (success, response) = await _apiService.JoinClan(clanId);

        if (success && response != null)
        {
            _popupService.ShowSuccess(response.message ?? "Вы успешно вступили в клан!");
            await RefreshMyClan();
            _currentClanPageView?.SetIsMyClan(true);
        }
        else
        {
            _popupService.ShowError("Не удалось вступить в клан");
        }
    }

    private async void HandleClanPageLeave()
    {
        var (success, message) = await _apiService.LeaveClan();

        if (success)
        {
            _popupService.ShowSuccess(message ?? "Вы покинули клан");
            _myClan = null;
            _currentClanPageView?.SetIsMyClan(false);
        }
        else
        {
            _popupService.ShowError(message ?? "Не удалось покинуть клан");
        }
    }

    private void HandleClanPageBack()
    {
        CloseClanPageView();
    }

    private void CloseClanPageView()
    {
        if (_currentClanPageView == null) return;
        _currentClanPageView.OnBackClicked -= HandleClanPageBack;
        _currentClanPageView.OnJoinClicked -= HandleClanPageJoin;
        _currentClanPageView.OnLeaveClicked -= HandleClanPageLeave;
        _currentClanPageView.Close();
        _currentClanPageView = null;
    }

    // ── CreateClanView ───────────────────────────────────────────────────────

    private void HandleCreateClanTabClicked()
    {
        CloseCreateClanView();

        _currentCreateClanView = _createClanViewFactory.Create();
        _currentCreateClanView.transform.SetParent(GameObject.FindGameObjectWithTag("Canvas").transform, false);
        _currentCreateClanView.Reset();

        _currentCreateClanView.OnCreateClicked += HandleCreateClan;
        _currentCreateClanView.OnCancelClicked += HandleCancelCreateClan;
    }

    private async void HandleCreateClan(string name, string description)
    {
        var (success, response) = await _apiService.CreateClan(name, description);

        if (success && response != null)
        {
            _popupService.ShowSuccess(response.message ?? "Клан успешно создан!");
            CloseCreateClanView();
            await RefreshMyClan();
            LoadMyClan();
        }
        else
        {
            if (_currentCreateClanView == null) return;

            var errorResponse = _apiService.GetLastErrorResponse();
            if (errorResponse?.errors != null && errorResponse.errors.Count > 0)
            {
                foreach (var error in errorResponse.errors)
                {
                    if (error.Value?.Length > 0)
                    {
                        _currentCreateClanView.ShowValidationError(error.Key, error.Value);
                        break;
                    }
                }
            }
            else if (!string.IsNullOrEmpty(errorResponse?.error))
            {
                _currentCreateClanView.ShowError(errorResponse.error);
            }
            else if (!string.IsNullOrEmpty(errorResponse?.message))
            {
                _currentCreateClanView.ShowError(errorResponse.message);
            }
            else
            {
                _currentCreateClanView.ShowError("Не удалось создать клан");
            }
        }
    }

    private void HandleCancelCreateClan() => CloseCreateClanView();

    private void CloseCreateClanView()
    {
        if (_currentCreateClanView == null) return;
        _currentCreateClanView.OnCreateClicked -= HandleCreateClan;
        _currentCreateClanView.OnCancelClicked -= HandleCancelCreateClan;
        _currentCreateClanView.Close();
        _currentCreateClanView = null;
    }

    // ── Join / Leave (из списка кланов) ─────────────────────────────────────

    private async void HandleJoinClan(int clanId)
    {
        var (success, response) = await _apiService.JoinClan(clanId);

        if (success && response != null)
        {
            _popupService.ShowSuccess(response.message ?? "Вы успешно вступили в клан!");
            await RefreshMyClan();
            LoadTopClans();
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
        if (success) _myClan = clan;
    }

    private void HandleBackButtonClicked() => _uiManager.Back();
}
