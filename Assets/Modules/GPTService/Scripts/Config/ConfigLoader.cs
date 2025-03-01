using UnityEngine;

namespace Modules.GPTService.Config
{
    public static class ConfigLoader
    {
        private static APIConfig _config;
        
        public static APIConfig GetConfig()
        {
            if (_config == null)
            {
                // Load the config file from Resources
                var textAsset = Resources.Load<TextAsset>("Config/api_config");
                if (textAsset != null)
                {
                    _config = JsonUtility.FromJson<APIConfig>(textAsset.text);
                }
                else
                {
                    Debug.LogError("Failed to load api_config.json from Resources/Config/");
                    _config = new APIConfig();
                }
            }
            return _config;
        }
    }
} 