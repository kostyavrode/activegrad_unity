using UnityEngine;
using Zenject;

public class ProjectInstaller : MonoInstaller
{
    [SerializeField] private MonoBehaviour coroutineRunner;
    [SerializeField] private PopupView popupPrefab;
    [SerializeField] private SightDetailsView sightDetailsPrefab;
    [SerializeField] private PartnerStoreDetailsView partnerStoreDetailsPrefab;
    [SerializeField] private AudioSource _audioRootPrefab;
    [SerializeField] private AudioClip _defaultUiClickClip;
    [SerializeField] private AudioClip _defaultUiCloseClip;
    [SerializeField] private SceneMusicConfig _sceneMusicConfig;

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
        Container.BindFactory<SightDetailsView, SightDetailsView.Factory>()
            .FromComponentInNewPrefab(sightDetailsPrefab)
            .UnderTransformGroup("SightDetails");
        Container.BindFactory<PartnerStoreDetailsView, PartnerStoreDetailsView.Factory>()
            .FromComponentInNewPrefab(partnerStoreDetailsPrefab)
            .UnderTransformGroup("PartnerStoreDetails");
        
    }

    private void BindAudio()
    {
        var root = Container.InstantiatePrefab(_audioRootPrefab);
        Object.DontDestroyOnLoad(root);

        var sources = root.GetComponentsInChildren<AudioSource>();
        if (sources.Length == 0)
        {
            Debug.LogError("[ProjectInstaller] AUDIO_ROOT must contain at least one AudioSource for music.");
            return;
        }

        Container.BindInstance(sources[0]).WithId("Music").AsCached();
        Container.BindInstance(sources.Length > 1 ? sources[1] : sources[0]).WithId("Sfx").AsCached();
        if (_defaultUiClickClip != null)
            Container.BindInstance(_defaultUiClickClip).WithId("UiClick").AsCached();
        if (_defaultUiCloseClip != null)
            Container.BindInstance(_defaultUiCloseClip).WithId("UiClose").AsCached();
        if (_sceneMusicConfig != null)
            Container.BindInstance(_sceneMusicConfig).AsSingle();

        Container.Bind<AudioSettings>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<AudioManager>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<UiClickSoundAutoBinder>().AsSingle().NonLazy();
    }
}