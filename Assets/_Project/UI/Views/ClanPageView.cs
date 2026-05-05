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

    [Header("Members")]
    [SerializeField] private Transform _membersContent;

    [Header("Actions")]
    [SerializeField] private Button _backButton;
    [SerializeField] private Button _joinButton;
    [SerializeField] private Button _leaveButton;

    public event Action OnBackClicked;
    public event Action<int> OnJoinClicked;
    public event Action OnLeaveClicked;
    public event Action<int> OnMemberClicked;

    public int ClanId { get; private set; }

    private void Awake()
    {
        _backButton.onClick.AddListener(() => OnBackClicked?.Invoke());
        if (_joinButton != null) _joinButton.onClick.AddListener(() => OnJoinClicked?.Invoke(ClanId));
        if (_leaveButton != null) _leaveButton.onClick.AddListener(() => OnLeaveClicked?.Invoke());
    }

    private void OnDestroy()
    {
        _backButton.onClick.RemoveAllListeners();
        if (_joinButton != null) _joinButton.onClick.RemoveAllListeners();
        if (_leaveButton != null) _leaveButton.onClick.RemoveAllListeners();
    }

    // playerClan — клан игрока (null если не в клане)
    public void Init(ClanData clan, ClanData playerClan)
    {
        ClanId = clan.id;

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

        RefreshButtons(ClanId, playerClan);
    }

    public void ShowMembers(ClanMember[] members, ClanMemberView.Factory factory)
    {
        if (_membersContent == null) return;

        for (int i = _membersContent.childCount - 1; i >= 0; i--)
            Destroy(_membersContent.GetChild(i).gameObject);

        foreach (var member in members)
        {
            var view = factory.Create();
            view.transform.SetParent(_membersContent, false);
            view.Init(member);
            view.OnClicked += id => OnMemberClicked?.Invoke(id);
        }
    }

    // Вызывается после join/leave чтобы обновить кнопки без пересоздания вью
    public void RefreshButtons(int clanId, ClanData playerClan)
    {
        bool isMyClan = playerClan != null && playerClan.id == clanId;
        bool isInAnotherClan = playerClan != null && playerClan.id != clanId;

        if (_joinButton != null) _joinButton.gameObject.SetActive(!isMyClan && !isInAnotherClan);
        if (_leaveButton != null) _leaveButton.gameObject.SetActive(isMyClan);
    }

    public void Close() => Destroy(gameObject);

    public class Factory : PlaceholderFactory<ClanPageView> { }
}
