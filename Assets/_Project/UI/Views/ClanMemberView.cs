using TMPro;
using UnityEngine;
using Zenject;

public class ClanMemberView : MonoBehaviour
{
    [SerializeField] private TMP_Text _usernameText;
    [SerializeField] private TMP_Text _levelText;
    [SerializeField] private TMP_Text _joinedAtText;

    public void Init(ClanMember member)
    {
        if (_usernameText != null) _usernameText.text = member.username;
        if (_levelText != null) _levelText.text = member.level.ToString();

        if (_joinedAtText != null)
        {
            if (!string.IsNullOrEmpty(member.joined_at) &&
                System.DateTime.TryParse(member.joined_at, out var date))
                _joinedAtText.text = "присоединился: "+date.ToString("dd.MM.yyyy");
            else
                _joinedAtText.text = member.joined_at ?? "";
        }
    }

    public class Factory : PlaceholderFactory<ClanMemberView> { }
}
