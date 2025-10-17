using Zenject;

public class UIMediatorInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container.BindInterfacesTo<LoginMediator>().AsSingle();
        Container.BindInterfacesTo<RegisterMediator>().AsSingle();
        //Container.BindInterfacesTo<MenuMediator>().AsSingle();
        Container.BindInterfacesTo<CharacterCustomizationMediator>().AsSingle();
    }
}