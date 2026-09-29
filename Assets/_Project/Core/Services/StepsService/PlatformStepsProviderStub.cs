using System;
using System.Threading.Tasks;

/// <summary>
/// Заглушка для Editor и платформ без шагомера. Шаги можно добавлять отладочной комбинацией Up+N.
/// </summary>
public class PlatformStepsProviderStub : IPlatformStepsProvider
{
    public bool IsConnected => false;

    public StepsAccess Access => StepsAccess.Unavailable;

    public void RequestAccess(Action<StepsAccess> onResult) => onResult?.Invoke(StepsAccess.Unavailable);

    public void Refresh() { }

    public Task<(bool connected, int steps)> TryGetStepsTodayAsync()
    {
        return Task.FromResult((false, 0));
    }
}
