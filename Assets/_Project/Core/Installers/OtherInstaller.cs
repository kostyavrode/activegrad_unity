using Mapbox.Examples;
using Mapbox.Unity.Map;
using Mapbox.Unity.Map.Interfaces;
using UnityEngine;
using Zenject;

public class OtherInstaller : MonoInstaller
{
    [SerializeField] private QuestItemView _questItemPrefab;
    [SerializeField] private Transform _questListParent;
    [SerializeField] private CoroutineRunner _coroutineRunner;
    [SerializeField] private SightItemView SightItemPrefab;
    [SerializeField] private AbstractMap _map;
    [SerializeField] private SpawnOnMap _spawnOnMap;
    [SerializeField] private Camera _mainCamera;
    public override void InstallBindings()
    {
        Container.BindInterfacesAndSelfTo<GPSLocationProvider>().AsSingle();
        
        Container.BindInterfacesAndSelfTo<LocationService>().AsSingle();
        
        Container.Bind<MapService>().AsSingle().WithArguments("7955252a-2f7b-4c01-968f-19e1c095f7b5").NonLazy();
        
        Container.BindInterfacesAndSelfTo<CharacterService>().AsSingle().NonLazy();
        
        Container.BindInterfacesTo<ProfileMediator>().AsSingle();

        Container.BindInterfacesTo<QuestMediator>().AsSingle();
        
        Container.BindInterfacesTo<MenuMediator>().AsSingle();
        
        Container.BindInterfacesTo<SettingsMediator>().AsSingle();
        
        Container.BindInterfacesTo<SightsMediator>().AsSingle();

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

        Container.BindInterfacesAndSelfTo<AbstractMap>().FromComponentInHierarchy(_map).AsSingle();

        Container.BindInterfacesAndSelfTo<SpawnOnMap>().FromComponentInHierarchy(_spawnOnMap).AsSingle();
        
        Container.Bind<Camera>().FromComponentInHierarchy(_mainCamera).AsSingle();
        
        Container.BindInterfacesAndSelfTo<PlayerInputService>().AsSingle().NonLazy();
        
        
    }
}