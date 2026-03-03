using System.Threading.Tasks;
using UnityEngine;

#if UNITY_ANDROID && !UNITY_EDITOR

/// <summary>
/// Провайдер шагов для Android (Health Connect).
/// TODO: Интеграция с Health Connect SDK для получения шагов за день.
/// Пока возвращает (false, 0) — используется внутренний счётчик.
/// </summary>
public class HealthConnectStepsProvider : IPlatformStepsProvider
{
    public bool IsConnected { get; private set; }

    public async Task<(bool connected, int steps)> TryGetStepsTodayAsync()
    {
        await Task.Yield();

        // TODO: Запрос разрешений Health Connect и получение шагов за сегодня
        // Использование: androidx.health.connect.client
        // DataType: STEPS
        // TimeRangeFilter: сегодня
        IsConnected = false;
        return (false, 0);
    }
}

#else

public class HealthConnectStepsProvider : IPlatformStepsProvider
{
    public bool IsConnected => false;
    public Task<(bool connected, int steps)> TryGetStepsTodayAsync() => Task.FromResult((false, 0));
}

#endif
