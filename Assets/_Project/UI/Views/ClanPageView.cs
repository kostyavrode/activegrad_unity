using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class ClanPageView : MonoBehaviour
{
    [Header("Info")]
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _descriptionText;
    [SerializeField] private TMP_Text _memberCountText;
    [SerializeField] private TMP_Text _landmarksCountText;
    [SerializeField] private TMP_Text _createdByText;
    [SerializeField] private TMP_Text _createdAtText;

    [Header("Actions")]
    [SerializeField] private Button _backButton;
    [SerializeField] private Button _joinButton;
    [SerializeField] private Button _leaveButton;

    public event Action OnBackClicked;
    public event Action<int> OnJoinClicked;
    public event Action OnLeaveClicked;

    private int _clanId;

    private void Awake()
    {
        _backButton.onClick.AddListener(() => OnBackClicked?.Invoke());
        if (_joinButton != null) _joinButton.onClick.AddListener(() => OnJoinClicked?.Invoke(_clanId));
        if (_leaveButton != null) _leaveButton.onClick.AddListener(() => OnLeaveClicked?.Invoke());
    }

    private void OnDestroy()
    {
        _backButton.onClick.RemoveAllListeners();
        if (_joinButton != null) _joinButton.onClick.RemoveAllListeners();
        if (_leaveButton != null) _leaveButton.onClick.RemoveAllListeners();
    }

    public void Init(ClanData clan, bool isMyClan)
    {
        _clanId = clan.id;

        _nameText.text = clan.name;
        _descriptionText.text = string.IsNullOrEmpty(clan.description) ? "Нет описания" : clan.description;
        if (_memberCountText != null) _memberCountText.text = clan.member_count.ToString();
        if (_landmarksCountText != null) _landmarksCountText.text = clan.captured_landmarks_count.ToString();
        if (_createdByText != null) _createdByText.text = clan.created_by_username;

        if (_createdAtText != null)
        {
            if (!string.IsNullOrEmpty(clan.created_at) &&
                System.DateTime.TryParse(clan.created_at, out var date))
                _createdAtText.text = date.ToString("dd.MM.yyyy");
            else
                _createdAtText.text = clan.created_at ?? "";
        }

        if (_joinButton != null) _joinButton.gameObject.SetActive(!isMyClan);
        if (_leaveButton != null) _leaveButton.gameObject.SetActive(isMyClan);
    }

    public void SetIsMyClan(bool isMyClan)
    {
        if (_joinButton != null) _joinButton.gameObject.SetActive(!isMyClan);
        if (_leaveButton != null) _leaveButton.gameObject.SetActive(isMyClan);
    }

    public void Close() => Destroy(gameObject);

    public class Factory : PlaceholderFactory<ClanPageView> { }
}
