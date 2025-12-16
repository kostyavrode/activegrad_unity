using System;

/// <summary>
/// Статический класс для событий отметки достопримечательностей
/// Используется системой квестов для отслеживания прогресса
/// </summary>
public static class SightMarkedEvent
{
    /// <summary>
    /// Событие вызывается когда игрок отмечается в достопримечательности
    /// Параметр: ID достопримечательности (pageId из Wikipedia)
    /// </summary>
    public static event Action<int> OnSightMarked; // sightId
    
    /// <summary>
    /// Вызвать событие отметки места
    /// Вызывается из APIService после успешной отметки
    /// </summary>
    public static void Invoke(int sightId)
    {
        OnSightMarked?.Invoke(sightId);
        UnityEngine.Debug.Log($"[SightMarkedEvent] Sight {sightId} marked");
    }
}

