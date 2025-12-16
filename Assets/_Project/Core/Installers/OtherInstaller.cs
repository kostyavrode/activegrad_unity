using Mapbox.Examples;
using Mapbox.Unity.Map;
using Mapbox.Unity.Map.Interfaces;
using UnityEngine;
using UnityEngine.Rendering;
using Zenject;

public class OtherInstaller : MonoInstaller
{
    [SerializeField] private QuestItemView _questItemPrefab;
    [SerializeField] private Transform _questListParent;
    [SerializeField] private CoroutineRunner _coroutineRunner;
    [SerializeField] private SightItemView SightItemPrefab;
    [SerializeField] private OtherPlayerProfileView _otherPlayerProfilePrefab;
    [SerializeField] private OtherPlayerSightsDetailsView _otherPlayerSightsDetailsViewPrefab;
    [SerializeField] private ShopItemView _shopItemPrefab;
    [SerializeField] private AbstractMap _map;
    [SerializeField] private SpawnOnMap _spawnOnMap;
    [SerializeField] private Camera _mainCamera;
    public override void InstallBindings()
    {
        Container.BindInterfacesAndSelfTo<GPSLocationProvider>().AsSingle();
        
        Container.BindInterfacesAndSelfTo<GamePopupService>().AsSingle().NonLazy();
        
        Container.BindInterfacesAndSelfTo<LocationService>().AsSingle();
        
        Container.Bind<MapService>().AsSingle().WithArguments("7955252a-2f7b-4c01-968f-19e1c095f7b5").NonLazy();
        
        Container.BindInterfacesAndSelfTo<CharacterService>().AsSingle().NonLazy();
        
        Container.BindInterfacesTo<ProfileMediator>().AsSingle();

        Container.BindInterfacesTo<QuestMediator>().AsSingle();
        
        Container.BindInterfacesTo<MenuMediator>().AsSingle();
        
        Container.BindInterfacesTo<SettingsMediator>().AsSingle();
        
        Container.BindInterfacesTo<SightsMediator>().AsSingle();

        Container.BindInterfacesTo<PlayerSearchMediator>().AsSingle();
        
        Container.BindInterfacesTo<ShopMediator>().AsSingle();

        Container.BindFactory<QuestItemView, QuestItemView.Factory>()
            .FromComponentInNewPrefab(_questItemPrefab)
            .UnderTransform(_questListParent);
        
        Container.Bind<CoroutineRunner>().FromInstance(_coroutineRunner).AsSingle();
        
        Container.BindInterfacesAndSelfTo<CameraController>().FromComponentInHierarchy().AsSingle();
        
        Container.Bind<WikipediaApiClient>().AsSingle().NonLazy();
        
        Container.Bind<ISightService>().To<SightService>().AsSingle().NonLazy();
        
        Container.BindInterfacesAndSelfTo<SightsUpdater>().AsSingle().NonLazy();
        
        Container.BindFactory<SightItemView, SightItemFactory>()
            .FromComponentInNewPrefab(SightItemPrefab)
            .AsTransient();
        
        Container.Bind<IImageLoadService>()
            .To<ImageLoadService>()
            .AsSingle();
        
        Container.BindFactory<OtherPlayerProfileView, OtherPlayerProfileView.Factory>()
            .FromComponentInNewPrefab(_otherPlayerProfilePrefab)
            .AsTransient();
        
        Container.BindInterfacesAndSelfTo<AbstractMap>().FromComponentInHierarchy(_map).AsSingle();

        Container.BindInterfacesAndSelfTo<SpawnOnMap>().FromComponentInHierarchy(_spawnOnMap).AsSingle();
        
        Container.Bind<Camera>().FromComponentInHierarchy(_mainCamera).AsSingle();
        
        Container.BindInterfacesAndSelfTo<PlayerInputService>().AsSingle().NonLazy();
        
        Container.BindFactory<OtherPlayerSightsDetailsView, OtherPlayerSightsDetailsView.Factory>().FromComponentInNewPrefab(_otherPlayerSightsDetailsViewPrefab).AsTransient();
        
        Container.BindFactory<ShopItemView, ShopItemView.Factory>().FromComponentInNewPrefab(_shopItemPrefab).AsTransient();
        
        BindQuests();
    }

    private void BindQuests()
    {
        Container.BindInterfacesAndSelfTo<QuestCompletionService>().AsSingle().NonLazy();
        
        
        Container.BindFactory<SightMarkQuestCondition, SightMarkQuestCondition.Factory>()
            .AsTransient();
        
        Container.BindFactory<StepsCondition, StepsCondition.Factory>()
            .AsTransient();
    }
    
    public override void Start()
    {
        base.Start();
        
        Debug.Log("[OtherInstaller] Registering quest condition factories...");
        
        var questService = Container.Resolve<QuestCompletionService>();
        
        
        questService.RegisterConditionFactory("mark_sights", () =>
        {
            return Container.Instantiate<SightMarkQuestCondition>();
        });
        
        questService.RegisterConditionFactory("steps", () =>
        {
            return Container.Instantiate<StepsCondition>();
        });

        // questService.RegisterConditionFactory("collect_coins", () => Container.Instantiate<CollectCoinsCondition>());
    }
}