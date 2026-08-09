using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;

public class UiClickSound : MonoBehaviour, IPointerClickHandler
{
    private AudioManager _audioManager;
    [SerializeField] private bool _useCloseSound;
    [SerializeField] private AudioClip _overrideClip;

    [Inject]
    public void Construct(AudioManager audioManager)
    {
        _audioManager = audioManager;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isActiveAndEnabled)
            return;

        if (_useCloseSound)
            _audioManager?.PlayUiClose(_overrideClip);
        else
            _audioManager?.PlayUiClick(_overrideClip);
    }

    public void Configure(bool useCloseSound)
    {
        _useCloseSound = useCloseSound;
    }
}
