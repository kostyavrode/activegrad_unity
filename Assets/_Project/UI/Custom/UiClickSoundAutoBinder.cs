using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;

/// <summary>
/// Навешивает звук клика и «пружинку» на все кнопки, включая созданные позже (элементы списков).
/// Оптимизировано под телефоны: поиск без сортировки, уже обработанные кнопки пропускаются,
/// частый опрос — только первые секунды после загрузки сцены, дальше — редкий.
/// </summary>
public class UiClickSoundAutoBinder : ITickable
{
    private const float FastInterval = 1f;
    private const float SlowInterval = 3f;
    private const float FastPhaseDuration = 8f;
    private const int MaxTrackedButtons = 4096;

    private readonly DiContainer _container;
    private readonly HashSet<int> _processed = new();
    private float _nextRefreshTime;
    private float _fastPhaseUntil;

    public UiClickSoundAutoBinder(DiContainer container)
    {
        _container = container;
        SceneManager.sceneLoaded += OnSceneLoaded;
        _fastPhaseUntil = Time.unscaledTime + FastPhaseDuration;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _fastPhaseUntil = Time.unscaledTime + FastPhaseDuration;
        _nextRefreshTime = 0f;
        if (_processed.Count > MaxTrackedButtons)
            _processed.Clear();
    }

    public void Tick()
    {
        var now = Time.unscaledTime;
        if (now < _nextRefreshTime)
            return;

        _nextRefreshTime = now + (now < _fastPhaseUntil ? FastInterval : SlowInterval);
        BindOnAllButtons();
    }

    private void BindOnAllButtons()
    {
        var buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (var i = 0; i < buttons.Length; i++)
        {
            var button = buttons[i];
            if (button == null || !_processed.Add(button.GetInstanceID()))
                continue;

            var go = button.gameObject;

            // Пружинка нажатия — только если нет своего Animator (иначе будет конфликт по scale)
            if (go.GetComponent<UIPressable>() == null && go.GetComponent<Animator>() == null
                && go.GetComponent<UIButtonAttentionPulse>() == null
                && UIPressable.IsSuitable(go.transform as RectTransform))
                go.AddComponent<UIPressable>();

            if (go.GetComponent<UiClickSound>() != null)
                continue;

            var clickSound = button.gameObject.AddComponent<UiClickSound>();
            _container.Inject(clickSound);
            clickSound.Configure(IsCloseButton(button));
        }
    }

    private static bool IsCloseButton(Button button)
    {
        var objName = button.name.ToLowerInvariant();
        if (objName.Contains("close") || objName.Contains("back") || objName.Contains("exit"))
            return true;

        var current = button.transform;
        while (current != null)
        {
            var parentName = current.name.ToLowerInvariant();
            if (parentName.Contains("close") || parentName.Contains("back") || parentName.Contains("exit"))
                return true;
            current = current.parent;
        }

        return false;
    }
}
