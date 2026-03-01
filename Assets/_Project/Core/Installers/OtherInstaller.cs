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
    [SerializeField] private PromoCodeView _promoCodeViewPrefab;
    [SerializeField] private FriendView _friendViewPrefab;
    [SerializeField] private FriendRequestView _friendRequestViewPrefab;
    [SerializeField] private ClanView _clanViewPrefab;
    [SerializeField] private CreateClanView _createClanViewPrefab;
    [SerializeField] private GameEventView _gameEventViewPrefab;
    [SerializeField] private ResourceItemView _resourceItemPrefab;
    [SerializeField] private InventoryItemView _inventoryItemPrefab;
    [SerializeField] private CraftRecipeView _craftRecipePrefab;
    [SerializeField] private GameObject _gameEventMarkerPrefab;
    [SerializeField] private AbstractMap _map;
    [SerializeField] private SpawnOnMap _spawnOnMap;
    [SerializeField] private Camera _mainCamera;
    public override void InstallBindings()
    {
        Container.BindInterfacesAndSelfTo<GPSLocationProvider>().AsSingle();
        
        Container.BindInterfacesAndSelfTo<GamePopupService>().AsSingle().NonLazy();
        
        Container.BindInterfacesAndSelfTo<LocationService>().AsSingle();
        
        Container.BindInterfacesAndSelfTo<MapService>().AsSingle().WithArguments("7955252a-2f7b-4c01-968f-19e1c095f7b5").NonLazy();
        
        Container.BindInterfacesAndSelfTo<SmoothMapMovementService>().AsSingle().NonLazy();
        
        Container.BindInterfacesAndSelfTo<CharacterService>().AsSingle().NonLazy();
        
        Debug.Log("[OtherInstaller] CharacterService registered as ITickable");
        
        Container.BindInterfacesTo<ProfileMediator>().AsSingle();

        Container.BindInterfacesTo<QuestMediator>().AsSingle();
        
        Container.BindInterfacesTo<MenuMediator>().AsSingle();
        
        Container.BindInterfacesTo<SettingsMediator>().AsSingle();
        
        Container.BindInterfacesTo<SightsMediator>().AsSingle();

        Container.BindInterfacesTo<PlayerSearchMediator>().AsSingle();
        
        Container.BindInterfacesTo<ShopMediator>().AsSingle();
        
        Container.BindInterfacesTo<PromoCodesMediator>().AsSingle();
        
        Container.BindInterfacesTo<FriendsMediator>().AsSingle();

        Container.BindInterfacesTo<ClansMediator>().AsSingle();

        Container.Bind<IInventoryService>().To<InventoryService>().AsSingle();
        Container.BindInterfacesTo<InventoryMediator>().AsSingle();

        Container.BindFactory<QuestItemView, QuestItemView.Factory>()
            .FromComponentInNewPrefab(_questItemPrefab)
            .UnderTransform(_questListParent);
        
        Container.Bind<CoroutineRunner>().FromInstance(_coroutineRunner).AsSingle();
        
        Container.BindInterfacesAndSelfTo<CameraController>().FromComponentInHierarchy().AsSingle();
        
        Container.Bind<WikipediaApiClient>().AsSingle().NonLazy();
        
        Container.Bind<ISightService>().To<SightService>().AsSingle().NonLazy();
        
        Container.BindInterfacesAndSelfTo<SightsUpdater>().AsSingle().NonLazy();
        
        Container.BindInterfacesAndSelfTo<MiniGamesService>().AsSingle();
        
        Container.Bind<GameObject>().WithId("GameEventMarker").FromInstance(_gameEventMarkerPrefab).AsSingle();
        Container.BindInterfacesAndSelfTo<GameEventService>().AsSingle().NonLazy();
        
        Container.BindFactory<GameEventView, GameEventView.Factory>()
            .FromComponentInNewPrefab(_gameEventViewPrefab)
            .AsTransient();
        
        Container.BindFactory<SightItemView, SightItemFactory>()
            .FromComponentInNewPrefab(SightItemPrefab)
            .AsTransient();
        
        Container.Bind<IImageLoadService>()
            .To<ImageLoadService>()
            .AsSingle();
        
        Container.Bind<IClipboardService>()
            .To<ClipboardService>()
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

        Container.BindFactory<ResourceItemView, ResourceItemView.Factory>()
            .FromComponentInNewPrefab(_resourceItemPrefab)
            .AsTransient();
        Container.BindFactory<InventoryItemView, InventoryItemView.Factory>()
            .FromComponentInNewPrefab(_inventoryItemPrefab)
            .AsTransient();
        Container.BindFactory<CraftRecipeView, CraftRecipeView.Factory>()
            .FromComponentInNewPrefab(_craftRecipePrefab)
            .AsTransient();
        
        Container.BindFactory<PromoCodeView, PromoCodeView.Factory>().FromComponentInNewPrefab(_promoCodeViewPrefab).AsTransient();
        
        Container.BindFactory<FriendView, FriendView.Factory>().FromComponentInNewPrefab(_friendViewPrefab).AsTransient();
        
        Container.BindFactory<FriendRequestView, FriendRequestView.Factory>().FromComponentInNewPrefab(_friendRequestViewPrefab).AsTransient();
        
        Container.BindFactory<ClanView, ClanView.Factory>().FromComponentInNewPrefab(_clanViewPrefab).AsTransient();
        
        Container.BindFactory<CreateClanView, CreateClanView.Factory>().FromComponentInNewPrefab(_createClanViewPrefab).AsTransient();
        
#if UNITY_EDITOR
        Container.BindInterfacesAndSelfTo<EditorGPSTestController>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();
#endif
        
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