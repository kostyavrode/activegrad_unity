using UnityEngine;
using Zenject;

public class ProjectInstaller : MonoInstaller
{
    [SerializeField] private MonoBehaviour coroutineRunner;
    [SerializeField] private PopupView popupPrefab;
    [SerializeField] private AudioSource _audioRootPrefab;

    public override void InstallBindings()
    {
        // AudioManager
        
        BindAudio();
        
        //Container.BindInterfacesTo<AudioManager>().AsSingle().NonLazy();

        
        Container.Bind<APIService>().AsSingle().WithArguments(coroutineRunner).NonLazy();
        
        Container.Bind<SceneLoader>().AsSingle().NonLazy();
        
        Container.Bind<UIManager>().AsSingle().NonLazy();
        
        //Container.Bind<AchievementsService>().AsSingle();
        
        Container.Bind<UserDataService>().AsSingle().NonLazy();
        
        Container.Bind<CharacterPreviewService>().AsSingle().NonLazy();
        
        Container.BindInterfacesTo<PopupService>().AsSingle();
        Container.BindFactory<PopupView, PopupView.Factory>()
            .FromComponentInNewPrefab(popupPrefab)
            .UnderTransformGroup("Popups");
        
    }

    private void BindAudio()
    {
        var root = Container.InstantiatePrefab(_audioRootPrefab);
        Object.DontDestroyOnLoad(root);

        var sources = root.GetComponentsInChildren<AudioSource>();

        Container.BindInstance(sources[0]).WithId("Music").AsSingle();
        if (sources.Length > 1)
            //Container.BindInstance(sources[1]).WithId("Sfx").AsSingle();

        Container.Bind<AudioSettings>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<AudioManager>().AsSingle().NonLazy();
    }
}