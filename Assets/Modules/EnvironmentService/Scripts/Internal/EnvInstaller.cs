// UIServiceInstaller.cs
using Modules.EnvironmentService.External;
using Modules.EnvironmentService.Internal;
using Modules.UIService.External;
using Modules.UIService.Internal;
using Zenject;


public class EnvInstaller : MonoInstaller
{
    public EnvironmentService environmentService; // assign this via the Inspector

    public override void InstallBindings()
    {
        Container.Bind<IEnvironmentService>().FromInstance(environmentService).AsSingle();
    }
}
