using UnityEngine;
using UnityEngine.Events;
using Modules.EnvironmentService.External;
using Modules.EnvironmentService.Internal;
using Modules.UIService.Internal;

public class CharacterService : MonoBehaviour
{
    [SerializeField] private GameObject characterPrefab;
    private GameObject instantiatedCharacter;
    
    // Event to notify when character is ready with its AudioSource
    public UnityEvent<AudioSource> OnCharacterAudioSourceReady { get; } = new();
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Listen to either EnvironmentService or UIService for room loading
        var environmentService = EnvironmentService.Instance;
        var uiService = FindObjectOfType<UIService>();
        
        if (environmentService != null)
        {
            environmentService.LivingRoomLoaded.AddListener(OnLivingRoomLoaded);
            Debug.Log("CharacterService: Listening to LivingRoomLoaded event");
        }
        else
        {
            Debug.LogError("CharacterService: EnvironmentService not found!");
        }
        
        if (uiService != null)
        {
            uiService.MainScreenLoaded.AddListener(OnMainScreenLoaded);
            Debug.Log("CharacterService: Listening to MainScreenLoaded event");
        }
        else
        {
            Debug.LogError("CharacterService: UIService not found!");
        }
    }
    
    private void OnLivingRoomLoaded()
    {
        InstantiateCharacter();
    }
    
    private void OnMainScreenLoaded()
    {
        InstantiateCharacter();
    }
    
    private void InstantiateCharacter()
    {
        if (characterPrefab == null)
        {
            Debug.LogError("CharacterService: Character prefab not assigned!");
            return;
        }
        
        // Only instantiate if we haven't already
        if (instantiatedCharacter == null)
        {
            Debug.Log("CharacterService: Instantiating character");
            instantiatedCharacter = Instantiate(characterPrefab);
            
            // Get the AudioSource from the character
            var audioSource = instantiatedCharacter.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                OnCharacterAudioSourceReady.Invoke(audioSource);
                Debug.Log("CharacterService: Character AudioSource ready");
            }
            else
            {
                Debug.LogError("CharacterService: No AudioSource found on character prefab!");
            }
        }
    }

    public AudioSource GetCurrentAudioSource()
    {
        if (instantiatedCharacter != null)
        {
            return instantiatedCharacter.GetComponent<AudioSource>();
        }
        return null;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
