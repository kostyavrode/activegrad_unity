using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Заглушка провайдера для Editor и платформ без HealthConnect/HealthKit.
/// Всегда возвращает (false, 0).
/// </summary>
public class PlatformStepsProviderStub : IPlatformStepsProvider
{
    public bool IsConnected => false;

    public Task<(bool connected, int steps)> TryGetStepsTodayAsync()
    {
        return Task.FromResult((false, 0));
    }
}
