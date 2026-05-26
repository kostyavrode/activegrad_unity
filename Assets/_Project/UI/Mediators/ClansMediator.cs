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
    private readonly ClanMemberView.Factory _clanMemberViewFactory;
    private readonly CreateClanView.Factory _createClanViewFactory;
    private readonly IPopupService _popupService;
    private readonly GamePopupService _gamePopupService;

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
        ClanMemberView.Factory clanMemberViewFactory,
        CreateClanView.Factory createClanViewFactory,
        IPopupService popupService,
        GamePopupService gamePopupService)
    {
        _clansWindow = clansWindow;
        _uiManager = uiManager;
        _apiService = apiService;
        _clanViewFactory = clanViewFactory;
        _clanPageViewFactory = clanPageViewFactory;
        _clanMemberViewFactory = clanMemberViewFactory;
        _createClanViewFactory = createClanViewFactory;
        _popupService = popupService;
        _gamePopupService = gamePopupService;
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
        _clansWindow.SetCreateClanButtonActive(true);

        var (success, response) = await _apiService.GetTopClans();

        if (success && response?.top_clans != null)
        {
            foreach (var clanData in response.top_clans)
                SpawnClanView(clanData, isMyClan: false);

            _clansWindow.PlayTabAnimation(false);
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
        _clansWindow.SetCreateClanButtonActive(false);
        _clansWindow.PlayTabAnimation(true);
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

            // Убираем панель поиска и возвращаем ScrollView — иначе она перекрывает результаты
            _clansWindow.ShowSearchPanel(false);

            foreach (var clanData in response.clans)
                SpawnClanView(clanData, isMyClan: false);

            _clansWindow.PlayTabAnimation(false);
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
        _clansWindow.SetCreateClanButtonActive(true);

        var (success, clan) = await _apiService.GetMyClan();

        if (success)
        {
            if (clan != null)
            {
                // /player/{id} возвращает неполный ClanData — обогащаем через SearchClans
                clan = await EnrichClanData(clan);
                _myClan = clan;
                SpawnClanView(clan, isMyClan: true);
                _clansWindow.PlayTabAnimation(false);
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

    private async void HandleOpenClanPage(ClanData clan)
    {
        CloseClanPageView();

        _currentClanPageView = _clanPageViewFactory.Create();
        _currentClanPageView.transform.SetParent(GameObject.FindGameObjectWithTag("Canvas").transform, false);
        // Показываем страницу сразу с текущим _myClan (может быть null если вкладка «Мой клан» не открывалась)
        _currentClanPageView.Init(clan, _myClan);

        _currentClanPageView.OnBackClicked += HandleClanPageBack;
        _currentClanPageView.OnJoinClicked += HandleClanPageJoin;
        _currentClanPageView.OnLeaveClicked += HandleClanPageLeave;
        _currentClanPageView.OnMemberClicked += HandleMemberClicked;

        // Запускаем оба запроса параллельно — без дополнительной задержки
        var membersTask = _apiService.GetClanMembers(clan.id);
        await RefreshMyClan(); // выполняется пока membersTask тоже идёт

        if (_currentClanPageView == null) return;

        // Обновляем кнопки с актуальными данными — теперь _myClan точно загружен
        _currentClanPageView.RefreshButtons(clan.id, _myClan);

        var (success, response) = await membersTask;

        if (_currentClanPageView == null) return;

        if (success && response?.members != null)
            _currentClanPageView.ShowMembers(response.members, _clanMemberViewFactory);
        else
            _popupService.ShowError("Не удалось загрузить участников клана");
    }

    private async void HandleClanPageJoin(int clanId)
    {
        var (success, response) = await _apiService.JoinClan(clanId);

        if (success && response != null)
        {
            _popupService.ShowSuccess(response.message ?? "Вы успешно вступили в клан!");
            await RefreshMyClan();
            _currentClanPageView?.RefreshButtons(clanId, _myClan);
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
            _currentClanPageView?.RefreshButtons(_currentClanPageView.ClanId, _myClan);
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

    private async void HandleMemberClicked(int playerId)
    {
        var (success, message) = await _apiService.SearchPlayer(playerId);

        if (success)
            _gamePopupService.CreateOtherPlayerProfilePopup(message);
        else
            _popupService.ShowError(message);
    }

    private void CloseClanPageView()
    {
        if (_currentClanPageView == null) return;
        _currentClanPageView.OnBackClicked -= HandleClanPageBack;
        _currentClanPageView.OnJoinClicked -= HandleClanPageJoin;
        _currentClanPageView.OnLeaveClicked -= HandleClanPageLeave;
        _currentClanPageView.OnMemberClicked -= HandleMemberClicked;
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
        if (success && clan != null)
            _myClan = await EnrichClanData(clan);
    }

    // /player/{id} возвращает неполный ClanData (без created_at, created_by_username).
    // Ищем клан по имени через SearchClans и возвращаем полную версию, если нашли.
    private async Task<ClanData> EnrichClanData(ClanData clan)
    {
        if (!string.IsNullOrEmpty(clan.created_by_username))
            return clan;

        var (success, response) = await _apiService.SearchClans(clan.name);
        if (!success || response?.clans == null)
            return clan;

        foreach (var full in response.clans)
        {
            if (full.id == clan.id)
                return full;
        }

        return clan;
    }

    private void HandleBackButtonClicked() => _uiManager.Back();
}
