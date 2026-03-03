using System.Threading.Tasks;

/// <summary>
/// Провайдер шагов с платформы (HealthConnect, HealthKit).
/// Реализации пытаются подключиться при первом запросе.
/// </summary>
public interface IPlatformStepsProvider
{
    bool IsConnected { get; }
    
    /// <summary>
    /// Попытаться подключиться и получить шаги за сегодня.
    /// </summary>
    Task<(bool connected, int steps)> TryGetStepsTodayAsync();
}
