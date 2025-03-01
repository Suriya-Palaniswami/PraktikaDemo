using UnityEngine;
using UnityEngine.Events;

namespace Modules.UIService.External
{
    public interface IUIService
    {
        public UnityEvent MainScreenLoaded { get; }
        public UnityEvent WelcomeScreenLoaded { get; }
        public UnityEvent EndCall { get; }
        public UnityEvent StartRecording { get; }
        public UnityEvent StopRecording { get; }
        public UnityEvent ShowVideo { get; }

    }
}