using Modules.EnvironmentService.External;
using Modules.UIService.External;
using UnityEngine;
using Zenject;

namespace Modules.Installer
{
    public class AppInstaller : MonoInstaller
    {
        public Modules.EnvironmentService.Internal.EnvironmentService environmentService;
        public Modules.UIService.Internal.UIService uiService;

        public override void InstallBindings()
        {
            Container.Bind<IEnvironmentService>().FromInstance(environmentService).AsSingle();
            Container.Bind<IUIService>().FromInstance(uiService).AsSingle();
        }
    }
}