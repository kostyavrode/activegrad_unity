using System;

public interface IStepsService
{
    /// <summary>
    /// Текущее количество шагов за сегодня
    /// </summary>
    int StepsToday { get; }
    
    /// <summary>
    /// true если шаги берутся из HealthConnect (Android) или HealthKit (iOS)
    /// </summary>
    bool IsHealthConnected { get; }
    
    /// <summary>
    /// Вызывается при обновлении количества шагов (каждые ~3 сек)
    /// </summary>
    event Action<int> OnStepsChanged;

    /// <summary>
    /// Добавить шаги для отладки (Up+N)
    /// </summary>
    void AddDebugSteps(int amount);
}
