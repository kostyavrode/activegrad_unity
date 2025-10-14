using Zenject;

public class OtherInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container.Bind<ILocationProvider>().To<GPSLocationProvider>().AsSingle();

        Container.BindInterfacesAndSelfTo<LocationService>().AsSingle();
        
        Container.Bind<MapService>().AsSingle().WithArguments("7955252a-2f7b-4c01-968f-19e1c095f7b5").NonLazy();
        
        Container.BindInterfacesAndSelfTo<CharacterService>().AsSingle().NonLazy();
        
        Container.BindInterfacesTo<ProfileMediator>().AsSingle();

        Container.BindInterfacesTo<QuestMediator>().AsSingle();
        
        Container.BindInterfacesTo<MenuMediator>().AsSingle();

    }
}