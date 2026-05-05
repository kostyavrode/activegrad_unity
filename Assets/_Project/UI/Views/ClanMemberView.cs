using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class ClanMemberView : MonoBehaviour
{
    [SerializeField] private TMP_Text _usernameText;
    [SerializeField] private TMP_Text _levelText;
    [SerializeField] private TMP_Text _joinedAtText;
    [SerializeField] private Button _profileButton;

    public event Action<int> OnClicked;

    private int _playerId;

    private void Awake()
    {
        if (_profileButton != null)
            _profileButton.onClick.AddListener(() => OnClicked?.Invoke(_playerId));
    }

    private void OnDestroy()
    {
        if (_profileButton != null)
            _profileButton.onClick.RemoveAllListeners();
    }

    public void Init(ClanMember member)
    {
        _playerId = member.id;

        if (_usernameText != null) _usernameText.text = member.username;
        if (_levelText != null) _levelText.text = member.level.ToString();

        if (_joinedAtText != null)
        {
            if (!string.IsNullOrEmpty(member.joined_at) &&
                System.DateTime.TryParse(member.joined_at, out var date))
                _joinedAtText.text = "присоединился: " + date.ToString("dd.MM.yyyy");
            else
                _joinedAtText.text = member.joined_at ?? "";
        }
    }

    public class Factory : PlaceholderFactory<ClanMemberView> { }
}
