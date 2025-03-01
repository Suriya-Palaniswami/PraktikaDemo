// UIServiceInstaller.cs
using Modules.UIService.External;
using Modules.UIService.Internal;
using Zenject;
public class UIServiceInstaller : MonoInstaller
{
    public UIService uiService; // assign this via the Inspector

    public override void InstallBindings()
    {
        Container.Bind<IUIService>().FromInstance(uiService).AsSingle();
    }
}
