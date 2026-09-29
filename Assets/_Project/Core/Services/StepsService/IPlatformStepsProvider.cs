using System;
using System.Threading.Tasks;

public enum StepsAccess
{
    /// <summary>На устройстве нет шагомера.</summary>
    Unavailable,
    /// <summary>Разрешение ещё не запрашивали.</summary>
    NotDetermined,
    /// <summary>Пользователь запретил доступ.</summary>
    Denied,
    /// <summary>Шаги считаются.</summary>
    Granted
}

/// <summary>
/// Источник шагов с устройства. Данные копятся самим телефоном,
/// поэтому шаги засчитываются, даже когда игра закрыта.
/// </summary>
public interface IPlatformStepsProvider
{
    bool IsConnected { get; }

    StepsAccess Access { get; }

    /// <summary>Запросить разрешение (если нужно) и начать сбор шагов.</summary>
    void RequestAccess(Action<StepsAccess> onResult);

    /// <summary>Перечитать данные после возврата игры из фона.</summary>
    void Refresh();

    /// <summary>Шаги за сегодня. connected = false, если данных нет.</summary>
    Task<(bool connected, int steps)> TryGetStepsTodayAsync();
}
