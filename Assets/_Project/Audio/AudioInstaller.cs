using UnityEngine;
using Zenject;

public class AudioInstaller : MonoInstaller
{
    [SerializeField] private AudioClip mainTheme;

    public override void InstallBindings()
    {
        Container.Bind<AudioClip>().WithId("MainTheme").FromInstance(mainTheme).AsSingle();
        Container.BindInterfacesAndSelfTo<AudioManager>().AsSingle();
    }
}