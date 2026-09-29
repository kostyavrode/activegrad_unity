using System;
using System.Threading.Tasks;
using UnityEngine;

#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;

/// <summary>
/// Шаги с аппаратного шагомера Android (TYPE_STEP_COUNTER).
/// Нативная часть: Assets/Plugins/Android/ActiveGradSteps.androidlib.
/// </summary>
public class AndroidStepCounterProvider : IPlatformStepsProvider
{
    private const string BridgeClass = "com.activegrad.steps.StepCounterBridge";
    private const string ActivityRecognition = "android.permission.ACTIVITY_RECOGNITION";
    private const string DeniedForeverKey = "Steps_PermissionDeniedForever";

    private AndroidJavaClass _bridge;
    private AndroidJavaObject _context;
    private bool _started;
    private bool _requestInProgress;

    public bool IsConnected => _started;

    public StepsAccess Access
    {
        get
        {
            if (!EnsureBridge() || !_bridge.CallStatic<bool>("isSensorAvailable", _context))
                return StepsAccess.Unavailable;

            if (_bridge.CallStatic<bool>("hasPermission", _context))
                return StepsAccess.Granted;

            return PlayerPrefs.GetInt(DeniedForeverKey, 0) == 1 ? StepsAccess.Denied : StepsAccess.NotDetermined;
        }
    }

    public void RequestAccess(Action<StepsAccess> onResult)
    {
        var access = Access;
        if (access == StepsAccess.Granted)
        {
            Start();
            onResult?.Invoke(StepsAccess.Granted);
            return;
        }

        if (access == StepsAccess.Unavailable || access == StepsAccess.Denied || _requestInProgress)
        {
            onResult?.Invoke(access);
            return;
        }

        _requestInProgress = true;
        var callbacks = new PermissionCallbacks();
        callbacks.PermissionGranted += _ =>
        {
            _requestInProgress = false;
            PlayerPrefs.DeleteKey(DeniedForeverKey);
            Start();
            onResult?.Invoke(StepsAccess.Granted);
        };
        callbacks.PermissionDenied += _ =>
        {
            _requestInProgress = false;
            onResult?.Invoke(StepsAccess.NotDetermined);
        };
        callbacks.PermissionDeniedAndDontAskAgain += _ =>
        {
            _requestInProgress = false;
            PlayerPrefs.SetInt(DeniedForeverKey, 1);
            PlayerPrefs.Save();
            onResult?.Invoke(StepsAccess.Denied);
        };

        Permission.RequestUserPermission(ActivityRecognition, callbacks);
    }

    public void Refresh()
    {
        // Разрешение могли выдать в настройках, пока игра была свёрнута
        if (!_started && Access == StepsAccess.Granted)
        {
            PlayerPrefs.DeleteKey(DeniedForeverKey);
            Start();
        }
    }

    public Task<(bool connected, int steps)> TryGetStepsTodayAsync()
    {
        if (!_started)
        {
            Refresh();
            if (!_started)
                return Task.FromResult((false, 0));
        }

        try
        {
            var steps = _bridge.CallStatic<long>("getStepsToday", _context);
            return Task.FromResult((true, (int)Math.Min(steps, int.MaxValue)));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AndroidStepCounterProvider] getStepsToday failed: {e.Message}");
            return Task.FromResult((false, 0));
        }
    }

    private void Start()
    {
        if (_started || !EnsureBridge())
            return;

        try
        {
            _started = _bridge.CallStatic<bool>("start", _context);
            Debug.Log($"[AndroidStepCounterProvider] Step counter started: {_started}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AndroidStepCounterProvider] start failed: {e.Message}");
        }
    }

    private bool EnsureBridge()
    {
        if (_bridge != null)
            return true;

        try
        {
            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            _context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            _bridge = new AndroidJavaClass(BridgeClass);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[AndroidStepCounterProvider] Native plugin not found: {e.Message}");
            _bridge = null;
            return false;
        }
    }
}

#else

public class AndroidStepCounterProvider : PlatformStepsProviderStub { }

#endif
