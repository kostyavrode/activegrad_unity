using System;
using UnityEngine;
using Zenject;

public class ProfileMediator : IInitializable, IDisposable
{
    private readonly ProfileWindow _profileWindow;
    private readonly UserDataService _userDataService;
    private readonly UIManager _uiManager;
    private readonly APIService _apiService;
    private readonly IPopupService _popupService;

    public ProfileMediator(ProfileWindow profileWindow, UserDataService userDataService, UIManager uiManager, APIService apiService, IPopupService popupService)
    {
        _profileWindow = profileWindow;
        _userDataService = userDataService;
        _uiManager = uiManager;
        _apiService = apiService;
        _popupService = popupService;
    }
    
    public void Initialize()
    {
        _profileWindow.OnBackClicked += HandleBackClicked;
        _profileWindow.OnWindowOpened += LoadInfo;
        _profileWindow.OnUpgradeStatClicked += HandleUpgradeStatClicked;
    }

    public void Dispose()
    {
        _profileWindow.OnBackClicked -= HandleBackClicked;
        _profileWindow.OnWindowOpened -= LoadInfo;
        _profileWindow.OnUpgradeStatClicked -= HandleUpgradeStatClicked;
    }

    private void LoadInfo()
    {
        RefreshProfileInfo();
        _profileWindow.SetStats(_userDataService.Strength, _userDataService.Intelligence, _userDataService.Agility, _userDataService.StatUpgradePoints);
        var _ = RefreshStatsFromServer();
        if (string.IsNullOrEmpty(_userDataService.FirstName) && string.IsNullOrEmpty(_userDataService.LastName))
        {
            var _2 = FetchProfileNamesAndRefresh();
        }
    }

    private async System.Threading.Tasks.Task FetchProfileNamesAndRefresh()
    {
        if (await _apiService.FetchProfileNamesAsync(_userDataService.ID))
        {
            RefreshProfileInfo();
        }
    }

    private async System.Threading.Tasks.Task RefreshStatsFromServer()
    {
        var (success, response) = await _apiService.GetPlayerStats();
        if (success && response?.player_stats != null)
        {
            var ps = response.player_stats;
            _userDataService.SetStats(ps.strength, ps.intelligence, ps.agility, ps.stat_upgrade_points);
            _profileWindow.SetStats(ps.strength, ps.intelligence, ps.agility, ps.stat_upgrade_points);
        }
    }

    private void RefreshProfileInfo()
    {
        string[] info = new string[7];
        info[0] = _userDataService.Username;
        info[1] = _userDataService.FirstName;
        info[2] = _userDataService.LastName;
        info[3] = _userDataService.Level.ToString();
        info[4] = _userDataService.Experience.ToString();
        info[5] = _userDataService.DateOfStart ?? "";
        info[6] = _userDataService.ID.ToString();
        _profileWindow.SetInfo(info);
    }

    private async void HandleUpgradeStatClicked(string statType)
    {
        if (_userDataService.StatUpgradePoints <= 0)
        {
            _popupService.ShowError("Нет очков прокачки");
            return;
        }

        var (success, response) = await _apiService.UpgradeStat(statType);
        if (success && response != null && response.success && response.player_stats != null)
        {
            var ps = response.player_stats;
            _userDataService.SetStats(ps.strength, ps.intelligence, ps.agility, ps.stat_upgrade_points);
            _profileWindow.SetStats(ps.strength, ps.intelligence, ps.agility, ps.stat_upgrade_points);
            string statName = statType == "strength" ? "Сила" : statType == "intelligence" ? "Интеллект" : "Ловкость";
            _popupService.ShowSuccess($"{statName} увеличена до {response.new_value}");
        }
        else if (!success)
        {
            _popupService.ShowError("Не удалось прокачать. Нет очков или ошибка сервера.");
        }
    }
    
    private void HandleBackClicked()
    {
        _uiManager.Back();
    }
}
