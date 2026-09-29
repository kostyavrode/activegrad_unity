using System;

public interface IStepsService
{
    /// <summary>
    /// Текущее количество шагов за сегодня (локальные сутки)
    /// </summary>
    int StepsToday { get; }

    /// <summary>
    /// true, если шаги берутся с шагомера устройства
    /// </summary>
    bool IsHealthConnected { get; }

    /// <summary>
    /// Состояние доступа к шагомеру
    /// </summary>
    StepsAccess Access { get; }

    /// <summary>
    /// Вызывается при обновлении количества шагов
    /// </summary>
    event Action<int> OnStepsChanged;

    /// <summary>
    /// Вызывается при изменении доступа к шагомеру
    /// </summary>
    event Action<StepsAccess> OnAccessChanged;

    /// <summary>
    /// Запросить доступ к шагомеру (например, по кнопке в квесте)
    /// </summary>
    void RequestAccess();

    /// <summary>
    /// Добавить шаги для отладки (Up+N)
    /// </summary>
    void AddDebugSteps(int amount);
}
