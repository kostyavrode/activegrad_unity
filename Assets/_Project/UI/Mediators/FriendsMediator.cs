using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Zenject;

public class FriendsMediator : IInitializable, IDisposable
{
    private readonly FriendsWindow _friendsWindow;
    private readonly UIManager _uiManager;
    private readonly APIService _apiService;
    private readonly FriendView.Factory _friendViewFactory;
    private readonly FriendRequestView.Factory _friendRequestViewFactory;
    private readonly IPopupService _popupService;
    private readonly GamePopupService _gamePopupService;
    
    private readonly List<FriendView> _spawnedFriendViews = new List<FriendView>();
    private readonly List<FriendRequestView> _spawnedRequestViews = new List<FriendRequestView>();
    
    public FriendsMediator(
        FriendsWindow friendsWindow,
        UIManager uiManager,
        APIService apiService,
        FriendView.Factory friendViewFactory,
        FriendRequestView.Factory friendRequestViewFactory,
        IPopupService popupService,
        GamePopupService gamePopupService)
    {
        _friendsWindow = friendsWindow;
        _uiManager = uiManager;
        _apiService = apiService;
        _friendViewFactory = friendViewFactory;
        _friendRequestViewFactory = friendRequestViewFactory;
        _popupService = popupService;
        _gamePopupService = gamePopupService;
    }
    
    public void Initialize()
    {
        _friendsWindow.OnWindowOpened += HandleWindowOpened;
        _friendsWindow.OnBackClicked += HandleBackButtonClicked;
        _friendsWindow.OnFriendsTabClicked += HandleFriendsTab;
        _friendsWindow.OnPendingTabClicked += HandlePendingTab;
        _friendsWindow.OnSentTabClicked += HandleSentTab;
    }

    public void Dispose()
    {
        _friendsWindow.OnWindowOpened -= HandleWindowOpened;
        _friendsWindow.OnBackClicked -= HandleBackButtonClicked;
        _friendsWindow.OnFriendsTabClicked -= HandleFriendsTab;
        _friendsWindow.OnPendingTabClicked -= HandlePendingTab;
        _friendsWindow.OnSentTabClicked -= HandleSentTab;
        DestroyAllViews();
    }

    // ── Tab handlers ─────────────────────────────────────────────────────────

    private void HandleWindowOpened() => LoadFriends();
    private void HandleFriendsTab()   => LoadFriends();
    private void HandlePendingTab()   => LoadPendingRequests();
    private void HandleSentTab()      => LoadSentRequests();

    // ── Data loading ─────────────────────────────────────────────────────────

    private async void LoadFriends()
    {
        _friendsWindow.ClearContent();
        DestroyAllViews();

        var (success, response) = await _apiService.GetFriends();

        if (!success || response == null || response.friends == null)
            return;

        for (int i = 0; i < response.friends.Length; i++)
        {
            var friendData = response.friends[i];
            var friendView = _friendViewFactory.Create();

            friendView.transform.SetParent(_friendsWindow.Content, false);
            friendView.Init(
                friendData.id,
                friendData.username,
                friendData.first_name,
                friendData.last_name,
                friendData.level
            );
            friendView.OnRemoveClicked += HandleRemoveFriend;
            friendView.OnProfileClicked += HandleFriendProfileClicked;

            _spawnedFriendViews.Add(friendView);
        }

        _friendsWindow.PlayTabAnimation();
    }

    private async void LoadPendingRequests()
    {
        _friendsWindow.ClearContent();
        DestroyAllViews();

        var (success, response) = await _apiService.GetPendingFriendRequests();

        if (!success || response == null || response.pending_requests == null)
            return;

        for (int i = 0; i < response.pending_requests.Length; i++)
        {
            var requestData = response.pending_requests[i];
            var requestView = _friendRequestViewFactory.Create();

            requestView.transform.SetParent(_friendsWindow.Content, false);
            requestView.InitAsPending(
                requestData.id,
                requestData.from_user.username,
                requestData.from_user.first_name,
                requestData.from_user.last_name,
                requestData.from_user.level
            );
            requestView.OnAcceptClicked += HandleAcceptRequest;
            requestView.OnRejectClicked += HandleRejectRequest;

            _spawnedRequestViews.Add(requestView);
        }

        _friendsWindow.PlayTabAnimation();
    }

    private async void LoadSentRequests()
    {
        _friendsWindow.ClearContent();
        DestroyAllViews();

        var (success, response) = await _apiService.GetSentFriendRequests();

        if (!success || response == null || response.sent_requests == null)
            return;

        for (int i = 0; i < response.sent_requests.Length; i++)
        {
            var requestData = response.sent_requests[i];
            var requestView = _friendRequestViewFactory.Create();

            requestView.transform.SetParent(_friendsWindow.Content, false);
            requestView.InitAsSent(
                requestData.id,
                requestData.to_user.username,
                requestData.to_user.first_name,
                requestData.to_user.last_name,
                requestData.to_user.level,
                requestData.status
            );

            _spawnedRequestViews.Add(requestView);
        }

        _friendsWindow.PlayTabAnimation();
    }

    private async void HandleRemoveFriend(int friendId)
    {
        var (success, message) = await _apiService.RemoveFriend(friendId);
        
        if (success)
        {
            LoadFriends();
        }
        else
        {
            _popupService.ShowError(message);
        }
    }

    private async void HandleAcceptRequest(int requestId)
    {
        var (success, response) = await _apiService.AcceptFriendRequest(requestId);
        
        if (success)
        {
            LoadPendingRequests();
            LoadFriends();
        }
        else
        {
            _popupService.ShowError("Failed to accept friend request");
        }
    }

    private async void HandleRejectRequest(int requestId)
    {
        var (success, message) = await _apiService.RejectFriendRequest(requestId);
        
        if (success)
        {
            LoadPendingRequests();
        }
        else
        {
            _popupService.ShowError(message);
        }
    }

    private void DestroyAllViews()
    {
        foreach (var view in _spawnedFriendViews)
        {
            if (view != null)
            {
                GameObject.Destroy(view.gameObject);
            }
        }
        _spawnedFriendViews.Clear();
        
        foreach (var view in _spawnedRequestViews)
        {
            if (view != null)
            {
                GameObject.Destroy(view.gameObject);
            }
        }
        _spawnedRequestViews.Clear();
    }

    private async void HandleFriendProfileClicked(int friendId)
    {
        var (success, message) = await _apiService.SearchPlayer(friendId);
        
        if (success)
        {
            _gamePopupService.CreateOtherPlayerProfilePopup(message);
        }
        else
        {
            _popupService.ShowError(message);
        }
    }

    private void HandleBackButtonClicked()
    {
        _uiManager.Back();
    }
}

