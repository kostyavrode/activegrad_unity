using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class FriendRequestView : MonoBehaviour
{
    [SerializeField] private TMP_Text _usernameText;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _levelText;
    [SerializeField] private Button _acceptButton;
    [SerializeField] private Button _rejectButton;
    [SerializeField] private GameObject _acceptRejectButtons;
    [SerializeField] private TMP_Text _statusText;
    
    public event Action<int> OnAcceptClicked;
    public event Action<int> OnRejectClicked;
    
    private int _requestId;
    private bool _isPending;
    
    private void Awake()
    {
        _acceptButton.onClick.AddListener(HandleAcceptClick);
        _rejectButton.onClick.AddListener(HandleRejectClick);
    }
    
    private void OnDestroy()
    {
        _acceptButton.onClick.RemoveAllListeners();
        _rejectButton.onClick.RemoveAllListeners();
    }
    
    public void InitAsPending(int requestId, string username, string firstName, string lastName, int level)
    {
        _requestId = requestId;
        _isPending = true;
        _usernameText.text = username;
        _nameText.text = $"{firstName} {lastName}";
        _levelText.text = $"Level: {level}";
        _acceptRejectButtons.SetActive(true);
        _statusText.gameObject.SetActive(false);
    }
    
    public void InitAsSent(int requestId, string username, string firstName, string lastName, int level, string status)
    {
        _requestId = requestId;
        _isPending = false;
        _usernameText.text = username;
        _nameText.text = $"{firstName} {lastName}";
        _levelText.text = $"Level: {level}";
        _acceptRejectButtons.SetActive(false);
        _statusText.gameObject.SetActive(true);
        _statusText.text = $"Status: {status}";
    }
    
    private void HandleAcceptClick()
    {
        if (_isPending)
        {
            OnAcceptClicked?.Invoke(_requestId);
        }
    }
    
    private void HandleRejectClick()
    {
        if (_isPending)
        {
            OnRejectClicked?.Invoke(_requestId);
        }
    }
    
    public class Factory : PlaceholderFactory<FriendRequestView> { }
}

