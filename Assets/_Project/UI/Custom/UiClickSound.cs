using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;

public class UiClickSound : MonoBehaviour, IPointerClickHandler
{
    private AudioManager _audioManager;
    private IFeedbackService _feedback;
    [SerializeField] private bool _useCloseSound;
    [SerializeField] private AudioClip _overrideClip;

    [Inject]
    public void Construct(AudioManager audioManager, [InjectOptional] IFeedbackService feedback)
    {
        _audioManager = audioManager;
        _feedback = feedback;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isActiveAndEnabled)
            return;

        // Звук — через AudioManager (как раньше), фидбек — только вибрация, чтобы не было двойного звука
        if (_useCloseSound)
            _audioManager?.PlayUiClose(_overrideClip);
        else
            _audioManager?.PlayUiClick(_overrideClip);

        _feedback?.Haptic(HapticType.Light);
    }

    public void Configure(bool useCloseSound)
    {
        _useCloseSound = useCloseSound;
    }
}
