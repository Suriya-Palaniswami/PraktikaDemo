using UnityEngine;
using Modules.GPTService.Internal;
namespace Modules.GPTService.Internal
{
    public class GPTServiceTest : MonoBehaviour
    {
        [SerializeField] private GPTService gptService;

        private void Start()
        {
            if (gptService == null)
            {
                Debug.LogError("GPTService reference not assigned in GPTServiceTest.");
                return;
            }

            gptService.OnAudioRecordingStarted.AddListener(() => Debug.Log("Recording started."));
            gptService.OnAudioRecordingStopped.AddListener(() => Debug.Log("Recording stopped."));
            gptService.OnSpeechToTextStarted.AddListener(() => Debug.Log("STT processing started."));
            gptService.OnSpeechToTextCompleted.AddListener((text) => Debug.Log("STT completed. Recognized text: " + text));
            gptService.OnGPTThinking.AddListener(() => Debug.Log("GPT thinking..."));
            gptService.OnGPTResponseReceived.AddListener((response) => Debug.Log("GPT response: " + response));
            gptService.OnTextToSpeechStarted.AddListener(() => Debug.Log("TTS conversion started."));
            gptService.OnTextToSpeechCompleted.AddListener(() => Debug.Log("TTS playback completed."));
            gptService.OnError.AddListener((error) => Debug.LogError("Error: " + error));
        }
    }
}