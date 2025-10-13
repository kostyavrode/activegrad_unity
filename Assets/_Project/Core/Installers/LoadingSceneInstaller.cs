using UnityEngine;
using Zenject;

public class LoadingSceneInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        //Container.Bind<SceneLoader>().AsSingle().NonLazy();
    }
}
