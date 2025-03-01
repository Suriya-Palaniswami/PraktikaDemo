using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Defines the GPT service interface with events and methods for audio recording,
/// speech-to-text processing, GPT API interaction, and text-to-speech playback.
/// </summary>
/// 
namespace Modules.GPTService.External
{ 
public interface IGPTService
{
    // Events for audio recording lifecycle
    public UnityEvent OnAudioRecordingStarted { get; }
    public UnityEvent OnAudioRecordingStopped { get; }

    // Events for speech-to-text processing
    public UnityEvent OnSpeechToTextStarted { get; }
    public UnityEvent<string> OnSpeechToTextCompleted { get; } // returns the recognized text

    // Events for GPT API processing
    public UnityEvent OnGPTThinking { get; }
    public UnityEvent<string> OnGPTResponseReceived { get; }

    // Events for text-to-speech playback
    public UnityEvent OnTextToSpeechStarted { get; }
    public UnityEvent OnTextToSpeechCompleted { get; }

    // Error event for error handling
    public UnityEvent<string> OnError { get; }

    // Methods to control recording and processing
    void StartRecording();
    void StopRecording();

    // Optionally, allow sending text directly to GPT
    void SendTextToGPT(string text);
}
}