using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class UiClickSoundAutoBinder : ITickable
{
    private readonly DiContainer _container;
    private float _nextRefreshTime;

    public UiClickSoundAutoBinder(DiContainer container)
    {
        _container = container;
    }

    public void Tick()
    {
        if (Time.unscaledTime < _nextRefreshTime)
            return;

        _nextRefreshTime = Time.unscaledTime + 1f;
        BindOnAllButtons();
    }

    private void BindOnAllButtons()
    {
        var buttons = Object.FindObjectsOfType<Button>(true);
        for (var i = 0; i < buttons.Length; i++)
        {
            var button = buttons[i];
            if (button == null)
                continue;

            if (button.gameObject.GetComponent<UiClickSound>() != null)
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
