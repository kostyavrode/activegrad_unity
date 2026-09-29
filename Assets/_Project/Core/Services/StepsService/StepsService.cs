using System;
using System.Threading.Tasks;
using UnityEngine;
using Zenject;

/// <summary>
/// Шаги за сегодня. Источник — шагомер устройства (Android TYPE_STEP_COUNTER / iOS CoreMotion),
/// который считает шаги и когда игра закрыта. Последнее значение кэшируется и синхронизируется с сервером.
/// </summary>
public class StepsService : IInitializable, IDisposable, ITickable, IStepsService
{
    private readonly IPlatformStepsProvider _platformProvider;
    private readonly APIService _apiService;

    private const float UpdateIntervalSec = 3f;
    private const float ServerSyncIntervalSec = 300f;
    private const string StepsPrefsKey = "StepsService_Steps";
    private const string StepsDateKey = "StepsService_Date";

    private int _stepsToday;
    private int _debugSteps;
    private string _storedDate;
    private bool _isRunning;
    private bool _debugComboWasActive;
    private StepsAccess _lastAccess = StepsAccess.NotDetermined;

    private int _lastSyncedSteps = -1;
    private float _lastSyncTime = float.MinValue;
    private bool _isSyncing;

    public int StepsToday => _stepsToday;
    public bool IsHealthConnected => _platformProvider.IsConnected;
    public StepsAccess Access => _platformProvider.Access;

    public event Action<int> OnStepsChanged;
    public event Action<StepsAccess> OnAccessChanged;

    public StepsService(IPlatformStepsProvider platformProvider, [InjectOptional] APIService apiService = null)
    {
        _platformProvider = platformProvider ?? new PlatformStepsProviderStub();
        _apiService = apiService;
    }

    public void Initialize()
    {
        LoadStoredSteps();
        EnsureDailyReset();

        _isRunning = true;
        Application.focusChanged += HandleFocusChanged;

        OnStepsChanged?.Invoke(_stepsToday);
        RequestAccess();
        _ = RunUpdateLoop();
    }

    public void Dispose()
    {
        _isRunning = false;
        Application.focusChanged -= HandleFocusChanged;
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

    public void RequestAccess()
    {
        _platformProvider.RequestAccess(access =>
        {
            NotifyAccess(access);
            _ = UpdateSteps();
        });
    }

    public void AddDebugSteps(int amount)
    {
        _debugSteps += amount;
        SetSteps(_stepsToday + amount);
    }

    private void HandleFocusChanged(bool hasFocus)
    {
        if (!_isRunning)
            return;

        if (hasFocus)
        {
            // Пока игра была свёрнута, шагомер продолжал считать — сразу подтягиваем свежие данные
            _platformProvider.Refresh();
            _ = UpdateSteps();
        }
        else
        {
            SaveSteps();
            _ = SyncWithServer(force: true);
        }
    }

    private async Task RunUpdateLoop()
    {
        while (_isRunning)
        {
            await UpdateSteps();
            await Task.Delay((int)(UpdateIntervalSec * 1000));
        }
    }

    private async Task UpdateSteps()
    {
        if (!_isRunning)
            return;

        EnsureDailyReset();
        NotifyAccess(_platformProvider.Access);

        var (connected, platformSteps) = await _platformProvider.TryGetStepsTodayAsync();
        if (connected)
        {
            // Шагомер — главный источник; за день значение не должно уменьшаться
            SetSteps(Mathf.Max(platformSteps + _debugSteps, _stepsToday));
        }

        _ = SyncWithServer(force: false);
    }

    private void SetSteps(int steps)
    {
        if (steps == _stepsToday)
            return;

        _stepsToday = steps;
        SaveSteps();
        OnStepsChanged?.Invoke(_stepsToday);
    }

    private void NotifyAccess(StepsAccess access)
    {
        if (access == _lastAccess)
            return;

        _lastAccess = access;
        Debug.Log($"[StepsService] Step counter access: {access}");
        OnAccessChanged?.Invoke(access);
    }

    private async Task SyncWithServer(bool force)
    {
        if (_apiService == null || !_apiService.IsLoggedIn || _isSyncing || _stepsToday == _lastSyncedSteps)
            return;

        if (!force && Time.realtimeSinceStartup - _lastSyncTime < ServerSyncIntervalSec)
            return;

        _isSyncing = true;
        _lastSyncTime = Time.realtimeSinceStartup;
        var steps = _stepsToday;

        try
        {
            var (ok, message) = await _apiService.UpdateDailySteps(steps);
            if (ok)
                _lastSyncedSteps = steps;
            else
                Debug.LogWarning($"[StepsService] Failed to sync steps: {message}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[StepsService] Steps sync error: {e.Message}");
        }
        finally
        {
            _isSyncing = false;
        }
    }

    private void LoadStoredSteps()
    {
        _storedDate = PlayerPrefs.GetString(StepsDateKey, "");
        _stepsToday = PlayerPrefs.GetInt(StepsPrefsKey, 0);
    }

    private void EnsureDailyReset()
    {
        string today = DateTime.Now.ToString("yyyy-MM-dd");
        if (_storedDate == today)
            return;

        bool wasEmpty = string.IsNullOrEmpty(_storedDate);
        _storedDate = today;
        _debugSteps = 0;
        _lastSyncedSteps = -1;

        if (!wasEmpty && _stepsToday != 0)
        {
            _stepsToday = 0;
            OnStepsChanged?.Invoke(_stepsToday);
            Debug.Log("[StepsService] Daily reset - steps cleared");
        }

        SaveSteps();
    }

    private void SaveSteps()
    {
        if (string.IsNullOrEmpty(_storedDate))
            return;

        PlayerPrefs.SetInt(StepsPrefsKey, _stepsToday);
        PlayerPrefs.SetString(StepsDateKey, _storedDate);
        PlayerPrefs.Save();
    }
}
