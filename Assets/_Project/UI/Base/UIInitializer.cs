using System.Collections.Generic;
using Zenject;

public class UIInitializer : IInitializable
{
    private readonly UIManager _uiManager;
    private readonly List<IWindow> _sceneWindows;

    public UIInitializer(UIManager uiManager, List<IWindow> sceneWindows)
    {
        _uiManager = uiManager;
        _sceneWindows = sceneWindows;
    }

    public void Initialize()
    {
        _uiManager.RegisterSceneWindows(_sceneWindows);

        if (_uiManager.HasWindow<LoginWindow>())
            _uiManager.Show<LoginWindow>();
        else if (_uiManager.HasWindow<MenuWindow>())
            _uiManager.Show<MenuWindow>();
    }
}
