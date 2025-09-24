using UnityEngine;
using Zenject;

public class ProjectInstaller : MonoInstaller
{
    [SerializeField] private MonoBehaviour coroutineRunner;

    public override void InstallBindings()
    {
        // AudioManager
        //Container.Bind<AudioManager>().AsSingle().NonLazy();

        // APIService — нужен MonoBehaviour для корутин
        Container.Bind<APIService>().AsSingle().WithArguments(coroutineRunner).NonLazy();

        // UIManager
        Container.Bind<UIManager>().AsSingle().NonLazy();

        // Другие сервисы
        //Container.Bind<AchievementsService>().AsSingle();
    }
}