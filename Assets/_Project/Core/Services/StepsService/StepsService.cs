using System;
using System.Threading.Tasks;
using UnityEngine;
using Zenject;

public class StepsService : IInitializable, IDisposable, ITickable, IStepsService
{
    private readonly IPlatformStepsProvider _platformProvider;

    private const float UpdateIntervalSec = 3f;
    private const string StepsPrefsKey = "StepsService_Steps";
    private const string StepsDateKey = "StepsService_Date";

    private int _stepsToday;
    private int _stepsAtSessionStart;
    private int _internalStepsThisSession;
    private string _lastStoredDate;
    private bool _isRunning;
    private Task _updateTask;

    // Accelerometer step detection
    private float _accelMagnitudeLowPass;
    private const float LowPassFactor = 0.8f;
    private const float StepThreshold = 1.2f;
    private const float StepCooldownSec = 0.4f;
    private float _lastStepTime;
    private bool _wasAboveThreshold;

    private bool _debugComboWasActive;

    public int StepsToday => _stepsToday;
    public bool IsHealthConnected => _platformProvider?.IsConnected ?? false;
    public event Action<int> OnStepsChanged;

    public StepsService(IPlatformStepsProvider platformProvider)
    {
        _platformProvider = platformProvider ?? new PlatformStepsProviderStub();
    }

    public void Initialize()
    {
        LoadStoredSteps();
        EnsureDailyReset();
        _isRunning = true;
        OnStepsChanged?.Invoke(_stepsToday);
        _updateTask = RunUpdateLoop();
    }

    public void Dispose()
    {
        _isRunning = false;
        SaveSteps();
    }

    public void Tick()
    {
        bool up = Input.GetKey(KeyCode.UpArrow);
        bool n = Input.GetKey(KeyCode.N);
        if (up && n)
        {
            if (!_debugComboWasActive)
            {
                _debugComboWasActive = true;
                AddDebugSteps(100);
                Debug.Log("[StepsService] Debug: +100 steps (Up+N)");
            }
        }
        else
        {
            _debugComboWasActive = false;
        }
    }

    public void AddDebugSteps(int amount)
    {
        _internalStepsThisSession += amount;
        _stepsToday = _stepsAtSessionStart + _internalStepsThisSession;
        SaveSteps();
        OnStepsChanged?.Invoke(_stepsToday);
    }

    private void LoadStoredSteps()
    {
        _lastStoredDate = PlayerPrefs.GetString(StepsDateKey, "");
        _internalStepsThisSession = 0;
        _stepsAtSessionStart = PlayerPrefs.GetInt(StepsPrefsKey, 0);
        _stepsToday = _stepsAtSessionStart;
    }

    private void EnsureDailyReset()
    {
        string today = DateTime.Now.ToString("yyyy-MM-dd");
        if (!string.IsNullOrEmpty(_lastStoredDate) && _lastStoredDate != today)
        {
            _stepsToday = 0;
            _stepsAtSessionStart = 0;
            _internalStepsThisSession = 0;
            _lastStoredDate = today;
            SaveSteps();
            Debug.Log("[StepsService] Daily reset - steps cleared");
        }
        else if (string.IsNullOrEmpty(_lastStoredDate))
        {
            _lastStoredDate = today;
        }
    }

    private void SaveSteps()
    {
        if (string.IsNullOrEmpty(_lastStoredDate)) return;
        PlayerPrefs.SetInt(StepsPrefsKey, _stepsToday);
        PlayerPrefs.SetString(StepsDateKey, _lastStoredDate);
        PlayerPrefs.Save();
    }

    private async Task RunUpdateLoop()
    {
        while (_isRunning)
        {
            await Task.Delay((int)(UpdateIntervalSec * 1000));
            if (!_isRunning) break;
            await UpdateSteps();
        }
    }

    private async Task UpdateSteps()
    {
        EnsureDailyReset();

        int newSteps;
        var (connected, platformSteps) = await _platformProvider.TryGetStepsTodayAsync();

        if (connected)
        {
            newSteps = platformSteps;
        }
        else
        {
            UpdateAccelerometerSteps();
            newSteps = _stepsAtSessionStart + _internalStepsThisSession;
        }

        if (newSteps != _stepsToday)
        {
            _stepsToday = newSteps;
            SaveSteps();
            OnStepsChanged?.Invoke(_stepsToday);
        }
    }

    private void UpdateAccelerometerSteps()
    {
        Vector3 accel = Input.acceleration;
        float magnitude = accel.magnitude;

        _accelMagnitudeLowPass = LowPassFactor * _accelMagnitudeLowPass + (1f - LowPassFactor) * magnitude;

        float now = Time.realtimeSinceStartup;
        if (_accelMagnitudeLowPass > StepThreshold)
        {
            if (!_wasAboveThreshold && (now - _lastStepTime) > StepCooldownSec)
            {
                _internalStepsThisSession++;
                _lastStepTime = now;
            }
            _wasAboveThreshold = true;
        }
        else
        {
            _wasAboveThreshold = false;
        }
    }
}
