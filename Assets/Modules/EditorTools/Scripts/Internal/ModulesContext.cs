using UnityEngine;
using System.Linq;
using Zenject;

namespace Modules.EditorTools.Internal
{
    public class ModulesContext : SceneContext
    {
        public int modulesCount;

        private void Awake()
        {
            SetInstallers(); // Ensure installers are set before Zenject starts
        }
        [ContextMenu("Set Installers")]
        public int SetInstallers()
        {
            var modules = GetComponentsInChildren<MonoInstaller>(true).ToList();
            modulesCount = modules.Count;
            Installers = modules;
            return modulesCount;
        }
    }
}
