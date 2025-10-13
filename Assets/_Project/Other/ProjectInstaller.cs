using UnityEngine;
using Zenject;

public class ProjectInstaller : MonoInstaller
{
    [SerializeField] private MonoBehaviour coroutineRunner;

    public override void InstallBindings()
    {
        // AudioManager
        //Container.Bind<AudioManager>().AsSingle().NonLazy();
        
        Container.Bind<APIService>().AsSingle().WithArguments(coroutineRunner).NonLazy();
        
        Container.Bind<SceneLoader>().AsSingle().NonLazy();
        
        Container.Bind<UIManager>().AsSingle().NonLazy();
        
        //Container.Bind<AchievementsService>().AsSingle();
        
        Container.Bind<UserDataService>().AsSingle().NonLazy();
        
        Container.Bind<CharacterPreviewService>().AsSingle().NonLazy();
    }
}