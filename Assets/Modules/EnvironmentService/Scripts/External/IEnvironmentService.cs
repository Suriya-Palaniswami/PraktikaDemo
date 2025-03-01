using UnityEngine;
using UnityEngine.Events;

namespace Modules.EnvironmentService.External
{
    public interface IEnvironmentService
    {
        public UnityEvent MobileEnvironmentLoaded { get; }
        public UnityEvent LivingRoomLoaded { get; }

        public void LoadLivingRoom();
    }
}