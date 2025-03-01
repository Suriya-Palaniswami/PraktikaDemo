using UnityEngine;
using Zenject;
using Modules.UIService.External;
using Modules.UIService.Internal;
using Modules.EnvironmentService.External;
using Modules.EnvironmentService.Internal;

public class ServicesInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        // First bind EnvironmentService since UIService depends on it
        Container.Bind<IEnvironmentService>()
            .To<EnvironmentService>()
            .FromComponentInHierarchy()
            .AsSingle();

        // Then bind UIService
        Container.Bind<IUIService>()
            .To<UIService>()
            .FromComponentInHierarchy()
            .AsSingle();
    }
} 