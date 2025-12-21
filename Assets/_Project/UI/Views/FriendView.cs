using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class FriendView : MonoBehaviour
{
    [SerializeField] private TMP_Text _usernameText;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _levelText;
    [SerializeField] private Button _removeButton;
    [SerializeField] private Button _profileButton;
    
    public event Action<int> OnRemoveClicked;
    public event Action<int> OnProfileClicked;
    
    private int _friendId;
    
    private void Awake()
    {
        _removeButton.onClick.AddListener(HandleRemoveClick);
        if (_profileButton != null)
        {
            _profileButton.onClick.AddListener(HandleProfileClick);
        }
    }
    
    private void OnDestroy()
    {
        _removeButton.onClick.RemoveAllListeners();
        if (_profileButton != null)
        {
            _profileButton.onClick.RemoveAllListeners();
        }
    }
    
    public void Init(int friendId, string username, string firstName, string lastName, int level)
    {
        _friendId = friendId;
        _usernameText.text = username;
        _nameText.text = $"{firstName} {lastName}";
        _levelText.text = $"Level: {level}";
    }
    
    private void HandleRemoveClick()
    {
        OnRemoveClicked?.Invoke(_friendId);
    }
    
    private void HandleProfileClick()
    {
        OnProfileClicked?.Invoke(_friendId);
    }
    
    public class Factory : PlaceholderFactory<FriendView> { }
}

