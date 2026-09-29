using System;
using System.Threading.Tasks;
using UnityEngine;

#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;

/// <summary>
/// Шаги через CoreMotion (CMPedometer). iOS хранит историю за 7 дней,
/// так что шаги за время, когда игра была закрыта, тоже засчитываются.
/// Нативная часть: Assets/Plugins/iOS/AGPedometerPlugin.mm.
/// </summary>
public class IosPedometerProvider : IPlatformStepsProvider
{
    [DllImport("__Internal")] private static extern bool _AGPedometer_IsAvailable();
    [DllImport("__Internal")] private static extern int _AGPedometer_AuthorizationStatus();
    [DllImport("__Internal")] private static extern void _AGPedometer_Start();
    [DllImport("__Internal")] private static extern void _AGPedometer_Refresh();
    [DllImport("__Internal")] private static extern int _AGPedometer_GetStepsToday();

    private bool _started;

    public bool IsConnected => _started && Access == StepsAccess.Granted;

    public StepsAccess Access
    {
        get
        {
            if (!_AGPedometer_IsAvailable())
                return StepsAccess.Unavailable;

            switch (_AGPedometer_AuthorizationStatus())
            {
                case 0: return StepsAccess.NotDetermined;
                case 3: return StepsAccess.Granted;
                default: return StepsAccess.Denied;
            }
        }
    }

    public void RequestAccess(Action<StepsAccess> onResult)
    {
        if (!_AGPedometer_IsAvailable())
        {
            onResult?.Invoke(StepsAccess.Unavailable);
            return;
        }

        // Первый запрос истории шагов сам показывает системный диалог «Движение и фитнес»
        _AGPedometer_Start();
        _started = true;
        onResult?.Invoke(Access);
    }

    public void Refresh()
    {
        if (_started)
            _AGPedometer_Refresh();
    }

    public Task<(bool connected, int steps)> TryGetStepsTodayAsync()
    {
        if (!_started || Access != StepsAccess.Granted)
            return Task.FromResult((false, 0));

        var steps = _AGPedometer_GetStepsToday();
        return Task.FromResult(steps < 0 ? (false, 0) : (true, steps));
    }
}

#else

public class IosPedometerProvider : PlatformStepsProviderStub { }

#endif
