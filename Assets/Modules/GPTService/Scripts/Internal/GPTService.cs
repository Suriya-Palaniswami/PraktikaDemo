using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections;
using UnityEngine.Networking;
using Modules.GPTService.External;
using Zenject;
using Modules.UIService.External;
using System.IO;
using Modules.GPTService.Config;

/// <summary>
/// Implements IGPTService by coordinating recording, STT conversion, GPT API calls,
/// and TTS playback. Adjust the STT/TTS implementations and GPT API payload as needed.
/// </summary>
/// 
namespace Modules.GPTService.Internal
{
    public class GPTService : MonoInstaller, IGPTService
    {
        

        // IGPTService events
        public UnityEvent OnAudioRecordingStarted { get; } = new();
        public UnityEvent OnAudioRecordingStopped { get; } = new();
        public UnityEvent OnSpeechToTextStarted { get; } = new();
        public UnityEvent<string> OnSpeechToTextCompleted { get; } = new();
        public UnityEvent OnGPTThinking { get; } = new();
        public UnityEvent<string> OnGPTResponseReceived { get; } = new();
        public UnityEvent OnTextToSpeechStarted { get; } = new();
        public UnityEvent OnTextToSpeechCompleted { get; } = new();
        public UnityEvent<string> OnError { get; } = new();

        // Reference to your recording service (RecordingService.cs)
        [SerializeField] private RecordingService recordAudio;

        // AudioSource to play the TTS response
        public AudioSource audioSource;

        private APIConfig apiConfig;

        public override void InstallBindings() => Container.Bind<IGPTService>().FromInstance(this).AsSingle();

        private void Awake()
        {
            apiConfig = ConfigLoader.GetConfig();
        }

        private void Start()
        {
            // Find and subscribe to CharacterService
            var characterService = FindObjectOfType<CharacterService>();
            if (characterService != null)
            {
                characterService.OnCharacterAudioSourceReady.AddListener(SetAudioSource);
                Debug.Log("GPTService: Listening for character AudioSource");
            }
            else
            {
                Debug.LogError("GPTService: CharacterService not found!");
            }
        }

        private void SetAudioSource(AudioSource source)
        {
            audioSource = source;
            Debug.Log("GPTService: AudioSource set from character");
        }

        /// <summary>
        /// Starts audio recording and fires the OnAudioRecordingStarted event.
        /// </summary>
        public void StartRecording()
        {
            if (recordAudio != null)
            {
                recordAudio.StartRecording();
                OnAudioRecordingStarted?.Invoke();
            }
            else
            {
                OnError?.Invoke("Recording service not assigned.");
            }
        }

        /// <summary>
        /// Stops recording, fires the OnAudioRecordingStopped event, and begins processing.
        /// </summary>
        public void StopRecording()
        {
            if (recordAudio != null)
            {
                recordAudio.StopRecording();
                OnAudioRecordingStopped?.Invoke();
                // Process the recorded audio (STT → GPT → TTS)
                StartCoroutine(ProcessRecordingCoroutine());
            }
            else
            {
                OnError?.Invoke("Recording service not assigned.");
            }
        }

        /// <summary>
        /// Processes the recorded audio by performing speech-to-text conversion,
        /// sending the recognized text to GPT, and converting the GPT response to speech.
        /// </summary>
        public IEnumerator ProcessRecordingCoroutine()
        {
            // Notify that STT processing is starting.
            OnSpeechToTextStarted?.Invoke();
            Debug.Log("Starting speech-to-text processing...");

            // Instead of starting a new recording, use the one that was already recorded
            // when the user pressed the mic button
            if (recordAudio == null || recordAudio.GetLastRecordedClip() == null)
            {
                Debug.LogError("No recorded audio found. Make sure recording completed successfully.");
                OnError?.Invoke("No recorded audio found.");
                yield break;
            }

            AudioClip recordedClip = recordAudio.GetLastRecordedClip();
            int sampleRate = recordedClip.frequency;
            
            Debug.Log($"Processing recorded clip: {recordedClip.length}s, {sampleRate}Hz");

            // Convert the recorded AudioClip to a WAV byte array.
            byte[] wavBytes = AudioClipToWav(recordedClip);
            Debug.Log($"WAV data size: {wavBytes.Length} bytes");
            
            // Encode WAV data to Base64.
            string base64Audio = Convert.ToBase64String(wavBytes);

            // Build the JSON payload for Google Cloud Speech-to-Text.
            string sttJsonPayload = $@"{{
                ""config"": {{
                    ""encoding"": ""LINEAR16"",
                    ""sampleRateHertz"": {sampleRate},
                    ""languageCode"": ""en-US"",
                    ""model"": ""default"",
                    ""useEnhanced"": true,
                    ""enableAutomaticPunctuation"": true
                }},
                ""audio"": {{
                    ""content"": ""{base64Audio}""
                }}
            }}";

            // Set up the API URL with your valid API key.
            string sttUrl = $"https://speech.googleapis.com/v1/speech:recognize?key={apiConfig.GoogleCloudKey}";

            using (UnityWebRequest sttRequest = new UnityWebRequest(sttUrl, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(sttJsonPayload);
                sttRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                sttRequest.downloadHandler = new DownloadHandlerBuffer();
                sttRequest.SetRequestHeader("Content-Type", "application/json; charset=utf-8");

                Debug.Log("Sending STT request to Google...");
                yield return sttRequest.SendWebRequest();

                if (sttRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("STT API Error: " + sttRequest.error);
                    OnError?.Invoke("Speech recognition failed: " + sttRequest.error);
                    yield break;
                }
                else
                {
                    string responseText = sttRequest.downloadHandler.text;
                    Debug.Log("STT Response: " + responseText);
                    
                    // Check if the response is empty or doesn't contain results
                    if (string.IsNullOrEmpty(responseText) || !responseText.Contains("results"))
                    {
                        Debug.LogError("Empty or invalid STT response");
                        OnError?.Invoke("Speech recognition returned no results. Please try speaking more clearly.");
                        yield break;
                    }
                    
                    // Move the try-catch outside of the yield return
                    string recognizedText = null;
                    bool parseSuccess = false;
                    
                    try
                    {
                        // Parse the response (assumes the response has a "results" array).
                        STTResponse response = JsonUtility.FromJson<STTResponse>(responseText);
                        if (response != null && response.results != null && response.results.Length > 0)
                        {
                            recognizedText = response.results[0].alternatives[0].transcript;
                            parseSuccess = true;
                        }
                        else
                        {
                            // Try a fallback parsing approach
                            recognizedText = ExtractTranscriptFromJson(responseText);
                            parseSuccess = !string.IsNullOrEmpty(recognizedText);
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError("Error parsing STT response: " + e.Message);
                        OnError?.Invoke("Error processing speech recognition results.");
                        yield break;
                    }
                    
                    // Now handle the results outside the try-catch
                    if (parseSuccess)
                    {
                        Debug.Log("Recognized Text: " + recognizedText);
                        OnSpeechToTextCompleted?.Invoke(recognizedText);
                        
                        // Start the GPT coroutine but don't yield on it here
                        StartCoroutine(SendTextToGPTCoroutine(recognizedText));
                    }
                    else
                    {
                        Debug.LogError("No STT results returned and fallback extraction failed.");
                        OnError?.Invoke("Could not recognize speech. Please try again.");
                    }
                }
            }
        }

        // Helper method to extract transcript from JSON when JsonUtility fails
        private string ExtractTranscriptFromJson(string json)
        {
            try
            {
                // Simple string-based extraction for the transcript
                int transcriptIndex = json.IndexOf("\"transcript\":");
                if (transcriptIndex >= 0)
                {
                    int startQuote = json.IndexOf("\"", transcriptIndex + 13) + 1;
                    int endQuote = json.IndexOf("\"", startQuote);
                    if (startQuote > 0 && endQuote > startQuote)
                    {
                        return json.Substring(startQuote, endQuote - startQuote);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error in fallback transcript extraction: " + e.Message);
            }
            return null;
        }

        // Convert an AudioClip to a WAV file byte array.
        byte[] AudioClipToWav(AudioClip clip)
        {
            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            // Convert float samples to 16-bit PCM data.
            short[] intData = new short[samples.Length];
            byte[] bytesData = new byte[samples.Length * 2];

            for (int i = 0; i < samples.Length; i++)
            {
                intData[i] = (short)(samples[i] * short.MaxValue);
                byte[] byteArr = BitConverter.GetBytes(intData[i]);
                Buffer.BlockCopy(byteArr, 0, bytesData, i * 2, 2);
            }

            // Return the WAV bytes with a header.
            return AddWavHeader(bytesData, clip.channels, clip.frequency);
        }

        // Add a WAV header to raw PCM data.
        byte[] AddWavHeader(byte[] pcmData, int channels, int sampleRate)
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            int byteRate = sampleRate * channels * 2;
            int blockAlign = channels * 2;
            int subChunk2Size = pcmData.Length;
            int chunkSize = 36 + subChunk2Size;

            // RIFF header.
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(chunkSize);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

            // fmt subchunk.
            writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);                // Subchunk1Size for PCM.
            writer.Write((short)1);          // AudioFormat: PCM.
            writer.Write((short)channels);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write((short)blockAlign);
            writer.Write((short)16);         // BitsPerSample.

            // data subchunk.
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(subChunk2Size);
            writer.Write(pcmData);

            writer.Flush();
            byte[] wavBytes = stream.ToArray();
            writer.Close();
            stream.Close();

            return wavBytes;
        }

        /// <summary>
        /// Sends text to the GPT API and handles the response.
        /// </summary>
        IEnumerator SendTextToGPTCoroutine(string text)
        {
            OnGPTThinking?.Invoke();

            string gptApiUrl = "https://api.openai.com/v1/chat/completions";

            // Set up a system message to guide the conversation.
            GPTRequest gptRequest = new GPTRequest
            {
                model = "gpt-3.5-turbo",
                messages = new GPTMessage[]
                {
            new GPTMessage { role = "system", content = "You are an English language coach. Engage in a friendly conversation, correct the user's mistakes, and provide constructive feedback to help improve their spoken English." },
            new GPTMessage { role = "user", content = text }
                }
            };

            string jsonBody = JsonUtility.ToJson(gptRequest);

            using (UnityWebRequest www = new UnityWebRequest(gptApiUrl, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                www.SetRequestHeader("Authorization", "Bearer " + apiConfig.OpenAIKey);

                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    OnError?.Invoke("GPT API Error: " + www.error);
                }
                else
                {
                    // Parse the GPT response.
                    GPTResponse gptResponse = JsonUtility.FromJson<GPTResponse>(www.downloadHandler.text);
                    if (gptResponse != null && gptResponse.choices != null && gptResponse.choices.Length > 0)
                    {
                        string responseText = gptResponse.choices[0].message.content;
                        OnGPTResponseReceived?.Invoke(responseText);

                        // Convert GPT text response to speech (TTS)
                        yield return StartCoroutine(TextToSpeechGoogleCoroutine(responseText));
                    }
                    else
                    {
                        OnError?.Invoke("Invalid GPT API response structure.");
                    }
                }
            }
        }

        IEnumerator TextToSpeechGoogleCoroutine(string text)
        {
            OnTextToSpeechStarted?.Invoke();

            // Replace with your actual Google Cloud TTS API key.
            string apiKey = apiConfig.GoogleCloudKey;
            if (string.IsNullOrEmpty(apiKey) || apiKey.StartsWith("YOUR_"))
            {
                Debug.LogError("Invalid API key. Please replace with a valid Google Cloud TTS API key.");
                yield break;
            }
            string ttsUrl = $"https://texttospeech.googleapis.com/v1/text:synthesize?key={apiKey}";

            // Build the request payload.
            TTSRequest requestData = new TTSRequest
            {
                input = new TTSInput { text = text },
                voice = new TTSVoice { languageCode = "en-US", name = "en-US-Wavenet-D" },
                audioConfig = new TTSConfig { audioEncoding = "MP3" }
            };

            string jsonData = JsonUtility.ToJson(requestData);
            Debug.Log("TTS JSON Payload: " + jsonData);

            using (UnityWebRequest www = new UnityWebRequest(ttsUrl, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json; charset=utf-8");
                www.SetRequestHeader("Accept", "application/json");

                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("Google TTS Error: " + www.error);
                }
                else
                {
                    Debug.Log("TTS Response: " + www.downloadHandler.text);
                    TTSResponse response = JsonUtility.FromJson<TTSResponse>(www.downloadHandler.text);
                    if (response == null || string.IsNullOrEmpty(response.audioContent))
                    {
                        Debug.LogError("Failed to decode TTS response.");
                        yield break;
                    }

                    // Decode the base64 audio data.
                    byte[] audioData = Convert.FromBase64String(response.audioContent);
                    // Use Application.persistentDataPath for temporary files on Android
                    string filePath = Path.Combine(Application.persistentDataPath, "tts.mp3");
                    System.IO.File.WriteAllBytes(filePath, audioData);

                    // Use proper URI format for Android
                    string fileUrl = "file://" + filePath;
                    #if PLATFORM_ANDROID
                    fileUrl = "file://" + filePath.Replace(" ", "%20");
                    #endif

                    using (UnityWebRequest audioRequest = UnityWebRequestMultimedia.GetAudioClip(fileUrl, AudioType.MPEG))
                    {
                        yield return audioRequest.SendWebRequest();
                        if (audioRequest.result != UnityWebRequest.Result.Success)
                        {
                            Debug.LogError("Error loading TTS audio: " + audioRequest.error);
                        }
                        else
                        {
                            AudioClip clip = DownloadHandlerAudioClip.GetContent(audioRequest);
                            audioSource.clip = clip;
                            audioSource.Play();
                            yield return new WaitForSeconds(clip.length);
                            OnTextToSpeechCompleted?.Invoke();
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Represents the expected response from Google Cloud TTS.
        /// </summary>
        [Serializable]
        public class TTSResponse
        {
            public string audioContent;
        }


        /// <summary>
        /// Simulates text-to-speech conversion and plays back the generated audio.
        /// Replace this simulation with an actual TTS API or plugin.
        /// </summary>
        IEnumerator TextToSpeechCoroutine(string text)
        {
            OnTextToSpeechStarted?.Invoke();

            // Build the URL for Google Translate TTS.
            // Note: Google TTS might have limitations on text length.
            string ttsUrl = "https://translate.google.com/translate_tts?ie=UTF-8&tl=en&client=tw-ob&q="
                            + UnityWebRequest.EscapeURL(text);

            // Request an audio clip from the TTS service.
            UnityWebRequest ttsRequest = UnityWebRequestMultimedia.GetAudioClip(ttsUrl, AudioType.MPEG);
            yield return ttsRequest.SendWebRequest();

            if (ttsRequest.result != UnityWebRequest.Result.Success)
            {
                OnError?.Invoke("TTS Error: " + ttsRequest.error);
            }
            else
            {
                // Retrieve the audio clip from the response.
                AudioClip ttsClip = DownloadHandlerAudioClip.GetContent(ttsRequest);
                if (ttsClip == null)
                {
                    OnError?.Invoke("Failed to retrieve AudioClip from TTS service.");
                }
                else
                {
                    audioSource.clip = ttsClip;
                    audioSource.Play();

                    // Wait for playback to complete
                    yield return new WaitForSeconds(ttsClip.length);
                    OnTextToSpeechCompleted?.Invoke();
                }
            }
        }


        /// <summary>
        /// Allows sending text directly to GPT (bypassing STT).
        /// </summary>
        public void SendTextToGPT(string text)
        {
            StartCoroutine(SendTextToGPTCoroutine(text));
        }
    }



    [Serializable]
    public class TTSInput
    {
        public string text;
    }

    [Serializable]
    public class TTSVoice
    {
        public string languageCode;
        public string name;
    }

    [Serializable]
    public class TTSConfig
    {
        public string audioEncoding;
    }

    [Serializable]
    public class TTSRequest
    {
        public TTSInput input;
        public TTSVoice voice;
        public TTSConfig audioConfig;
    }

    [Serializable]
    public class TTSResponse
    {
        public string audioContent;
    }

    [Serializable]
    public class STTAlternative
    {
        public string transcript;
        public float confidence;
    }

    [Serializable]
    public class STTResult
    {
        public STTAlternative[] alternatives;
    }

    [Serializable]
    public class STTResponse
    {
        public STTResult[] results;
    }
}
