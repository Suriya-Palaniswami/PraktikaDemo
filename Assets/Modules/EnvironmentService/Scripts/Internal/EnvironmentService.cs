using UnityEngine;
using Modules.EnvironmentService.External;
using UnityEngine.Events;

namespace Modules.EnvironmentService.Internal
{
    public class EnvironmentService : MonoBehaviour, IEnvironmentService
    {
        // Singleton pattern for easy access
        public static EnvironmentService Instance { get; private set; }
        
        [SerializeField] private GameObject mobileEnvironmentPrefab;
        [SerializeField] private GameObject modernRoomPrefab;

        private GameObject mobileEnvironment;
        private GameObject modernRoom;

        public UnityEvent MobileEnvironmentLoaded { get; } = new();
        public UnityEvent LivingRoomLoaded { get; } = new();

        private void Awake()
        {
            // Simple singleton setup
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            LoadMobileEnvironment();
        }

        public void LoadLivingRoom()
        {
            if (modernRoom == null)
            {
                modernRoom = Instantiate(modernRoomPrefab);
            }
            
            modernRoom.SetActive(true);
            LivingRoomLoaded.Invoke();
        }

        public void LoadMobileEnvironment()
        {
            if (mobileEnvironment == null)
            {
                mobileEnvironment = Instantiate(mobileEnvironmentPrefab);
            }

            if (modernRoom != null)
            { 
                modernRoom.SetActive(false);
            }
            
            mobileEnvironment.SetActive(true);
            MobileEnvironmentLoaded.Invoke();
        }
    }
}