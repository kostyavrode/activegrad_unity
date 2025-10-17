using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class UIInstaller : MonoInstaller
{
    [SerializeField] private List<MonoBehaviour> sceneWindows;

    public override void InstallBindings()
    {
        var windows = GetSceneWindows();

        Container.Bind<List<IWindow>>().FromInstance(windows).AsSingle();
        
        foreach (var window in windows)
        {
            Container.Bind(window.GetType()).FromInstance(window as MonoBehaviour).AsSingle();
        }

        Container.Bind<IInitializable>().To<UIInitializer>().AsSingle().NonLazy();
    }


    public List<IWindow> GetSceneWindows()
    {
        var windows = new List<IWindow>();
        foreach (var mono in sceneWindows)
        {
            if (mono is IWindow window)
                windows.Add(window);
        }
        return windows;
    }
}