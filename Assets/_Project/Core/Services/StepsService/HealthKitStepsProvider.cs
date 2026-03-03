using System.Threading.Tasks;

#if UNITY_IOS && !UNITY_EDITOR

/// <summary>
/// Провайдер шагов для iOS (HealthKit).
/// TODO: Интеграция с HealthKit через нативный плагин.
/// Пока возвращает (false, 0) — используется внутренний счётчик.
/// </summary>
public class HealthKitStepsProvider : IPlatformStepsProvider
{
    public bool IsConnected { get; private set; }

    public async Task<(bool connected, int steps)> TryGetStepsTodayAsync()
    {
        await Task.Yield();

        // TODO: Вызов нативного iOS плагина для HKHealthStore,
        // HKSampleType.QuantityType для HKQuantityTypeIdentifierStepCount,
        // predicateForSamples с началом дня
        IsConnected = false;
        return (false, 0);
    }
}

#else

using UnityEngine;

public class HealthKitStepsProvider : IPlatformStepsProvider
{
    public bool IsConnected => false;
    public Task<(bool connected, int steps)> TryGetStepsTodayAsync() => Task.FromResult((false, 0));
}

#endif
