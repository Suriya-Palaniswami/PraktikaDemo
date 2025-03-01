using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Android;

public class RecordingService : MonoBehaviour
{
    private AudioClip recordedClip;
    [SerializeField] private AudioSource audioSource;
    private string filePath = "recording.wav";
    private string directoryPath = "Recordings";
    private float startTime;
    private float recordingLength;
    
    private void Awake()
    {
        directoryPath = Path.Combine(Application.persistentDataPath, "Recordings");
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
    }

    private void Start()
    {
        // Listen to CharacterService for AudioSource
        var characterService = FindObjectOfType<CharacterService>();
        if (characterService != null)
        {
            characterService.OnCharacterAudioSourceReady.AddListener(SetAudioSource);
            Debug.Log("RecordingService: Listening for character AudioSource");
        }
        else
        {
            Debug.LogError("RecordingService: CharacterService not found!");
        }
    }
    
    public void SetAudioSource(AudioSource source)
    {
        audioSource = source;
        Debug.Log("RecordingService: AudioSource set from character");
    }

    public void StartRecording()
    {
        #if PLATFORM_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
            return;
        }
        #endif

        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("No microphone found!");
            return;
        }

        string device = Microphone.devices[0];
        int sampleRate = 44100;
        int lengthSec = 3599;

        recordedClip = Microphone.Start(device, false, lengthSec, sampleRate);
        startTime = Time.realtimeSinceStartup;
    }

    public void StopRecording()
    {
        Microphone.End(null);
        recordingLength = Time.realtimeSinceStartup - startTime;
        recordedClip = TrimClip(recordedClip, recordingLength);
        SaveRecording();
    }

    public void SaveRecording()
    {
        if (recordedClip != null)
        {
            filePath = Path.Combine(directoryPath, filePath);
            WavUtility.Save(filePath, recordedClip);
            Debug.Log("Recording saved as " + filePath);
            //audioSource.clip = recordedClip;
            //audioSource.Play();
        }
        else
        {
            Debug.LogError("No recording found to save.");
        }
    }

    private AudioClip TrimClip(AudioClip clip, float length)
    {
        int samples = (int)(clip.frequency * length);
        float[] data = new float[samples];
        clip.GetData(data, 0);

        AudioClip trimmedClip = AudioClip.Create(clip.name, samples,
            clip.channels, clip.frequency, false);
        trimmedClip.SetData(data, 0);

        return trimmedClip;
    }
    
    public AudioClip GetLastRecordedClip()
    {
        if (recordedClip == null)
        {
            Debug.LogWarning("GetLastRecordedClip: No recording available");
        }
        return recordedClip;
    }
}