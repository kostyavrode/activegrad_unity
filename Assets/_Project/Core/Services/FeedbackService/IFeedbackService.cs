using UnityEngine;

/// <summary>
/// Единая точка "ощущений": звук + вибрация (+ опционально эффекты) для игровых событий.
/// </summary>
public interface IFeedbackService
{
    /// <summary>Проиграть звук и вибрацию, настроенные для типа события.</summary>
    void Play(FeedbackType type, Vector3? worldOrScreenPos = null);

    /// <summary>Только вибрация.</summary>
    void Haptic(HapticType type);

    /// <summary>Проиграть клип через пул SFX со случайным разбросом pitch.</summary>
    void PlaySfx(AudioClip clip, float pitchJitter = 0.05f, float pitch = 1f);
}
