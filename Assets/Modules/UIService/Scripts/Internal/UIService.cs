using UnityEngine;
using Modules.UIService.External;
using UnityEngine.Events;
using UnityEngine.UI;
using Unity.VisualScripting;
using Modules.EnvironmentService.External;
using Modules.EnvironmentService.Internal;
using Modules.GPTService.External;

namespace Modules.UIService.Internal
{
    public class UIService : MonoBehaviour, IUIService
    {
        // Direct reference to the environment service
        private IEnvironmentService environmentService;
        
        // Reference to GPT service for recording functionality
        private IGPTService gptService;
        
        // Track recording state
        private bool isRecording = false;
        
        // Colors for mic button states
        private Color recordingColor = Color.green;
        private Color notRecordingColor = Color.red;

        private GameObject welcomeScreen;
        private GameObject mainScreen;

        public UIReferences uiReferences;

        public UnityEvent MainScreenLoaded { get; } = new();
        public UnityEvent WelcomeScreenLoaded { get; } = new();

        public UnityEvent EndCall { get; } = new();
        public UnityEvent StartRecording { get; } = new();
        public UnityEvent StopRecording { get; } = new();
        public UnityEvent ShowVideo { get; } = new();

        // Add a field for the WebCamDisplay
        private WebCamDisplay webCamDisplay;

        private void Awake()
        {
            // Only create WebCamDisplay component
            webCamDisplay = gameObject.AddComponent<WebCamDisplay>();
        }

        private void Start()
        {
            Debug.Log("UIService Start called");
            
            // Get reference to environment service using the full namespace path
            environmentService = Modules.EnvironmentService.Internal.EnvironmentService.Instance;
            
            // Find GPT service
            gptService = FindObjectOfType<Modules.GPTService.Internal.GPTService>();
            
            if (gptService == null)
            {
                Debug.LogError("GPTService not found! Make sure it exists in the scene.");
            }
            else
            {
                // Subscribe to GPT service events
                SubscribeToGPTEvents();
            }
            
            if (environmentService != null)
            {
                Debug.Log("Found EnvironmentService, adding listener for MobileEnvironmentLoaded");
                environmentService.MobileEnvironmentLoaded.AddListener(OnEnvironmentInitialized);
                
                // Call OnEnvironmentInitialized immediately if mobile environment is already loaded
                if (FindObjectOfType<UIReferences>() != null)
                {
                    Debug.Log("Mobile environment already exists, initializing UI");
                    OnEnvironmentInitialized();
                }
                else
                {
                    Debug.Log("Waiting for mobile environment to be loaded");
                }
            }
            else
            {
                Debug.LogError("EnvironmentService not found! Make sure it exists in the scene.");
                
                // Alternative: Try to find it in the scene
                var envService = FindObjectOfType<Modules.EnvironmentService.Internal.EnvironmentService>();
                if (envService != null)
                {
                    environmentService = envService;
                    environmentService.MobileEnvironmentLoaded.AddListener(OnEnvironmentInitialized);
                    Debug.Log("Found EnvironmentService through FindObjectOfType");
                }
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from GPT service events when this object is destroyed
            if (gptService != null)
            {
                UnsubscribeFromGPTEvents();
            }
        }
        
        private void SubscribeToGPTEvents()
        {
            gptService.OnAudioRecordingStarted.AddListener(OnGPTRecordingStarted);
            gptService.OnAudioRecordingStopped.AddListener(OnGPTRecordingStopped);
            gptService.OnGPTThinking.AddListener(OnGPTThinking);
            gptService.OnGPTResponseReceived.AddListener(OnGPTResponseReceived);
            gptService.OnTextToSpeechStarted.AddListener(OnTextToSpeechStarted);
            gptService.OnTextToSpeechCompleted.AddListener(OnTextToSpeechCompleted);
            gptService.OnError.AddListener(OnGPTError);
        }
        
        private void UnsubscribeFromGPTEvents()
        {
            gptService.OnAudioRecordingStarted.RemoveListener(OnGPTRecordingStarted);
            gptService.OnAudioRecordingStopped.RemoveListener(OnGPTRecordingStopped);
            gptService.OnGPTThinking.RemoveListener(OnGPTThinking);
            gptService.OnGPTResponseReceived.RemoveListener(OnGPTResponseReceived);
            gptService.OnTextToSpeechStarted.RemoveListener(OnTextToSpeechStarted);
            gptService.OnTextToSpeechCompleted.RemoveListener(OnTextToSpeechCompleted);
            gptService.OnError.RemoveListener(OnGPTError);
        }
        
        // GPT Service event handlers
        private void OnGPTRecordingStarted()
        {
            // Show mic emoji when recording starts
            if (uiReferences.MicEmoji != null)
            {
                uiReferences.MicEmoji.gameObject.SetActive(true);
            }
            
            // Hide thought emojis
            if (uiReferences.ThoughtEmoji != null)
            {
                uiReferences.ThoughtEmoji.gameObject.SetActive(false);
            }
            
            if (uiReferences.ThinkCloud != null)
            {
                uiReferences.ThinkCloud.gameObject.SetActive(false);
            }
            
            Debug.Log("GPT Recording Started - Showing Mic Emoji");
        }
        
        private void OnGPTRecordingStopped()
        {
            // Keep mic emoji visible until thinking starts
            Debug.Log("GPT Recording Stopped");
        }
        
        private void OnGPTThinking()
        {
            // Hide mic emoji
            if (uiReferences.MicEmoji != null)
            {
                uiReferences.MicEmoji.gameObject.SetActive(false);
            }
            
            // Show thought emojis
            if (uiReferences.ThoughtEmoji != null)
            {
                uiReferences.ThoughtEmoji.gameObject.SetActive(true);
            }
            
            if (uiReferences.ThinkCloud != null)
            {
                uiReferences.ThinkCloud.gameObject.SetActive(true);
            }
            
            Debug.Log("GPT Thinking - Showing Thought Emojis");
        }
        
        private void OnGPTResponseReceived(string response)
        {
            // Hide all emojis when response is received
            HideAllEmojis();
            
            Debug.Log("GPT Response Received - Hiding Emojis");
        }
        
        private void OnTextToSpeechStarted()
        {
            // Keep emojis hidden during speech
            Debug.Log("TTS Started");
        }
        
        private void OnTextToSpeechCompleted()
        {
            // Ensure all emojis are hidden when speech is complete
            HideAllEmojis();
            
            Debug.Log("TTS Completed");
        }
        
        private void OnGPTError(string errorMessage)
        {
            // Hide all emojis on error
            HideAllEmojis();
            
            Debug.LogError("GPT Error: " + errorMessage);
        }
        
        private void HideAllEmojis()
        {
            // Hide all emoji UI elements
            if (uiReferences.MicEmoji != null)
            {
                uiReferences.MicEmoji.gameObject.SetActive(false);
            }
            
            if (uiReferences.ThoughtEmoji != null)
            {
                uiReferences.ThoughtEmoji.gameObject.SetActive(false);
            }
            
            if (uiReferences.ThinkCloud != null)
            {
                uiReferences.ThinkCloud.gameObject.SetActive(false);
            }
        }

        private void OnEnvironmentInitialized()
        {
            Debug.Log("OnEnvironmentInitialized called - attempting to find UIReferences");
            
            // Find UIReferences in the scene after mobile environment is instantiated
            uiReferences = FindObjectOfType<UIReferences>();
            
            if (uiReferences == null)
            {
                Debug.LogError("UIReferences not found in instantiated mobile environment!");
                return;
            }
            
            Debug.Log("UIReferences found successfully");
            
            welcomeScreen = uiReferences.WelcomeScreen;
            mainScreen = uiReferences.MainScreen;
            
            // Log screen references
            Debug.Log($"Welcome Screen reference: {(welcomeScreen != null ? "Found" : "Missing")}");
            Debug.Log($"Main Screen reference: {(mainScreen != null ? "Found" : "Missing")}");
            
            // Initially hide both screens
            if (welcomeScreen != null) welcomeScreen.SetActive(false);
            if (mainScreen != null) mainScreen.SetActive(false);
            
            // Set initial mic button color
            if (uiReferences.Mic != null)
            {
                uiReferences.Mic.color = notRecordingColor;
                Debug.Log("Mic button color initialized");
            }
            else
            {
                Debug.LogWarning("Mic reference is missing");
            }
            
            // Initially hide all emoji UI elements
            HideAllEmojis();
            
            RemoveWelcomeScreenUIListeners();
            LoadWelcomeScreen();
            
            Debug.Log("UI initialization completed");
        }

        public void LoadMainScreen()
        {
            Debug.Log("Calling LoadMainScreen");
            welcomeScreen.SetActive(false);
            mainScreen.SetActive(true);

            MainScreenLoaded.Invoke();

            AddMainScreenUIListeners();  

            RemoveWelcomeScreenUIListeners();
            
            // Ensure mic button is red when main screen loads
            if (uiReferences.Mic != null)
            {
                uiReferences.Mic.color = notRecordingColor;
            }
            
            // Hide all emojis when main screen loads
            HideAllEmojis();

            // Configure WebCamDisplay if needed
            if (webCamDisplay != null && uiReferences.CameraView != null)
            {
                // Set references
                var field = webCamDisplay.GetType().GetField("displayImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field.SetValue(webCamDisplay, uiReferences.CameraView);
                
                field = webCamDisplay.GetType().GetField("aspectRatioFitter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field.SetValue(webCamDisplay, uiReferences.CameraAspectRatioFitter);
            }
        }

        public void LoadWelcomeScreen()
        {
            // Stop any ongoing audio playback
            if (environmentService != null)
            {
                var characterService = FindObjectOfType<CharacterService>();
                if (characterService != null && characterService.GetCurrentAudioSource() != null)
                {
                    characterService.GetCurrentAudioSource().Stop();
                    Debug.Log("Stopped character audio playback");
                }
            }

            welcomeScreen.SetActive(true);
            mainScreen.SetActive(false);

            WelcomeScreenLoaded.Invoke();

            RemoveMainScreenUIListeners();

            AddWelcomeScreenUIListeners();
        }

        private void RemoveMainScreenUIListeners()
        {
            uiReferences.Minimize.GetComponent<Button>().onClick.RemoveAllListeners();
            uiReferences.EndCall.GetComponent<Button>().onClick.RemoveAllListeners();
            uiReferences.StartButton.onClick.RemoveAllListeners();
            uiReferences.VideoCall.GetComponent<Button>().onClick.RemoveAllListeners();
            
            // Also remove mic button listener
            if (uiReferences.Mic != null && uiReferences.Mic.GetComponent<Button>() != null)
            {
                uiReferences.Mic.GetComponent<Button>().onClick.RemoveAllListeners();
            }
            
            // Reset recording state
            isRecording = false;
            if (uiReferences.Mic != null)
            {
                uiReferences.Mic.color = notRecordingColor;
            }
        }
        
        private void RemoveWelcomeScreenUIListeners()
        {
            uiReferences.StartButton.onClick.RemoveAllListeners();
        }
        
        private void AddWelcomeScreenUIListeners()
        {
            Debug.Log("Adding Welcome Screen listeners");
            uiReferences.StartButton.onClick.AddListener(() => {
                LoadMainScreen();
                if (environmentService != null)
                {
                    environmentService.LoadLivingRoom();
                    Debug.Log("Loading living room environment from welcome screen");
                }
            });
        }
        
        private void AddMainScreenUIListeners()
        {
            uiReferences.Minimize.GetComponent<Button>().onClick.AddListener(LoadWelcomeScreen);
            uiReferences.EndCall.GetComponent<Button>().onClick.AddListener(LoadWelcomeScreen);
            uiReferences.StartButton.onClick.AddListener(LoadMainScreen);
            uiReferences.VideoCall.GetComponent<Button>().onClick.AddListener(RaiseShowVideo);
            
            // Add mic button toggle functionality
            if (uiReferences.Mic != null && uiReferences.Mic.GetComponent<Button>() != null)
            {
                uiReferences.Mic.GetComponent<Button>().onClick.AddListener(ToggleRecording);
            }

            // Add VideoCall button functionality to toggle camera
            if (uiReferences.VideoCall != null && uiReferences.VideoCall.GetComponent<Button>() != null)
            {
                uiReferences.VideoCall.GetComponent<Button>().onClick.RemoveAllListeners();
                uiReferences.VideoCall.GetComponent<Button>().onClick.AddListener(ToggleCameraView);
            }
        }
        
        // Toggle recording state and update UI
        private void ToggleRecording()
        {
            if (gptService == null)
            {
                Debug.LogError("Cannot toggle recording: GPTService not found");
                return;
            }
            
            isRecording = !isRecording;
            
            if (isRecording)
            {
                // Start recording
                gptService.StartRecording();
                RaiseStartRecording();
                
                // Change mic button color to green
                if (uiReferences.Mic != null)
                {
                    uiReferences.Mic.color = recordingColor;
                }
                
                Debug.Log("Recording started");
            }
            else
            {
                // Stop recording
                gptService.StopRecording();
                RaiseStopRecording();
                
                // Reset mic button color to red
                if (uiReferences.Mic != null)
                {
                    uiReferences.Mic.color = notRecordingColor;
                }
                
                Debug.Log("Recording stopped");
            }
        }
        
        private void RaiseEndCall() { EndCall.Invoke(); }
        private void RaiseStartRecording() { StartRecording.Invoke(); }
        private void RaiseStopRecording() { StopRecording.Invoke(); }
        private void RaiseShowVideo() { ShowVideo.Invoke(); }

        private void ToggleCameraView()
        {
            if (webCamDisplay != null)
            {
                webCamDisplay.ToggleCamera();
                
                // Toggle visibility of camera container
                if (uiReferences.CameraViewContainer != null)
                {
                    bool isActive = uiReferences.CameraViewContainer.activeSelf;
                    uiReferences.CameraViewContainer.SetActive(!isActive);
                }
                
                // Also raise the ShowVideo event for other components
                RaiseShowVideo();
            }
        }
    }
}