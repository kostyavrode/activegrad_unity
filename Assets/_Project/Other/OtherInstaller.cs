using UnityEngine;
using Zenject;

public class OtherInstaller : MonoInstaller
{
    [SerializeField] private QuestItemView _questItemPrefab;
    [SerializeField] private Transform _questListParent;
    [SerializeField] private CoroutineRunner _coroutineRunner;
    public override void InstallBindings()
    {
        Container.BindInterfacesAndSelfTo<GPSLocationProvider>().AsSingle();


        Container.BindInterfacesAndSelfTo<LocationService>().AsSingle();
        
        Container.Bind<MapService>().AsSingle().WithArguments("7955252a-2f7b-4c01-968f-19e1c095f7b5").NonLazy();
        
        Container.BindInterfacesAndSelfTo<CharacterService>().AsSingle().NonLazy();
        
        Container.BindInterfacesTo<ProfileMediator>().AsSingle();

        Container.BindInterfacesTo<QuestMediator>().AsSingle();
        
        Container.BindInterfacesTo<MenuMediator>().AsSingle();

        Container.BindFactory<QuestItemView, QuestItemView.Factory>()
            .FromComponentInNewPrefab(_questItemPrefab)
            .UnderTransform(_questListParent);
        Container.Bind<CoroutineRunner>().FromInstance(_coroutineRunner).AsSingle();
        
        Container.BindInterfacesAndSelfTo<CameraController>().FromComponentInHierarchy().AsSingle();

    }
}