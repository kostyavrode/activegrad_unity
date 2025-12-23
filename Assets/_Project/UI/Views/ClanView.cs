using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class ClanView : MonoBehaviour
{
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _descriptionText;
    [SerializeField] private TMP_Text _memberCountText;
    [SerializeField] private TMP_Text _landmarksCountText;
    [SerializeField] private TMP_Text _createdByText;
    [SerializeField] private TMP_Text _createdAtText;
    [SerializeField] private Button _joinButton;
    [SerializeField] private Button _leaveButton;
    
    public event Action<int> OnJoinClicked;
    public event Action OnLeaveClicked;
    
    private int _clanId;
    private bool _isMyClan;
    
    private void Awake()
    {
        if (_joinButton != null)
        {
            _joinButton.onClick.AddListener(HandleJoinClick);
        }
        if (_leaveButton != null)
        {
            _leaveButton.onClick.AddListener(HandleLeaveClick);
        }
    }
    
    private void OnDestroy()
    {
        if (_joinButton != null)
        {
            _joinButton.onClick.RemoveAllListeners();
        }
        if (_leaveButton != null)
        {
            _leaveButton.onClick.RemoveAllListeners();
        }
    }
    
    public void Init(ClanData clanData, bool isMyClan = false)
    {
        _clanId = clanData.id;
        _isMyClan = isMyClan;
        
        _nameText.text = clanData.name;
        _descriptionText.text = string.IsNullOrEmpty(clanData.description) ? "Нет описания" : clanData.description;
        _memberCountText.text = $"Участников: {clanData.member_count}";
        _landmarksCountText.text = $"Захвачено достопримечательностей: {clanData.captured_landmarks_count}";
        _createdByText.text = $"Создатель: {clanData.created_by_username}";
        
        if (!string.IsNullOrEmpty(clanData.created_at))
        {
            if (DateTime.TryParse(clanData.created_at, out DateTime date))
            {
                _createdAtText.text = $"Создан: {date:dd.MM.yyyy}";
            }
            else
            {
                _createdAtText.text = $"Создан: {clanData.created_at}";
            }
        }
        else
        {
            _createdAtText.text = "";
        }
        
        // Показываем кнопки в зависимости от того, мой ли это клан
        if (_joinButton != null)
        {
            _joinButton.gameObject.SetActive(!isMyClan);
        }
        if (_leaveButton != null)
        {
            _leaveButton.gameObject.SetActive(isMyClan);
        }
    }
    
    private void HandleJoinClick()
    {
        OnJoinClicked?.Invoke(_clanId);
    }
    
    private void HandleLeaveClick()
    {
        OnLeaveClicked?.Invoke();
    }
    
    public class Factory : PlaceholderFactory<ClanView> { }
}

