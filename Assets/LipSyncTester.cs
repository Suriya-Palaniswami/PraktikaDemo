using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class LipSyncTester : MonoBehaviour
{
    public AudioSource audioSource;
    public SkinnedMeshRenderer faceRenderer;

    // Mouth Blend Shapes
    public int mouthOpenBlendShapeIndex = 0;
    public int midMouthLeftBlendShapeIndex = 1;
    public int midMouthRightBlendShapeIndex = 2;
    public int mouthNarrowLeftBlendShapeIndex = 3;
    public int mouthNarrowRightBlendShapeIndex = 4;
    public int mouthUpBlendShapeIndex = 5;

    // Brow Blend Shapes
    public int browsUpLeftBlendShapeIndex = 6;
    public int browsUpRightBlendShapeIndex = 7;

    // Eye Blinking Blend Shapes
    public int eyesClosedLeftBlendShapeIndex = 8;
    public int eyesClosedRightBlendShapeIndex = 9;

    // Advanced lip-sync settings
    [Header("Advanced Settings")]
    [Range(0.1f, 10f)]
    public float amplitudeMultiplier = 5f;
    [Range(0.01f, 0.1f)]
    public float updateFrequency = 0.02f;
    [Range(1, 10)]
    public int smoothingFactor = 3;
    
    // Frequency analysis
    [Header("Frequency Analysis")]
    public bool useFrequencyAnalysis = true;
    public float lowFrequencyInfluence = 1.0f;
    public float midFrequencyInfluence = 1.5f;
    public float highFrequencyInfluence = 0.8f;
    
    // Vowel simulation
    private enum VowelShape { A, E, I, O, U, Rest }
    private VowelShape currentVowel = VowelShape.Rest;
    private float vowelChangeTime = 0f;
    private float vowelHoldTime = 0.1f;
    
    // Smoothing
    private Queue<float> amplitudeHistory;
    private float currentAmplitude = 0f;
    
    // Spectrum data
    private float[] spectrumData = new float[1024];

    void Start()
    {
        if (audioSource == null)
        {
            Debug.LogError("AudioSource not assigned!");
            return;
        }

        if (faceRenderer == null)
        {
            Debug.LogError("SkinnedMeshRenderer not assigned!");
            return;
        }

        // Initialize amplitude history for smoothing
        amplitudeHistory = new Queue<float>();
        for (int i = 0; i < smoothingFactor; i++)
        {
            amplitudeHistory.Enqueue(0f);
        }

        // Start automatic blinking
        StartCoroutine(BlinkRoutine());
    }

    void Update()
    {
        if (audioSource.isPlaying)
        {
            UpdateLipSync();
        }
        else
        {
            // Gradually return to rest position when not speaking
            currentAmplitude = Mathf.Lerp(currentAmplitude, 0, Time.deltaTime * 5f);
            if (currentAmplitude < 0.01f)
            {
                ResetBlendShapes();
            }
            else
            {
                ApplyBlendShapes(currentAmplitude);
            }
        }
    }

    void UpdateLipSync()
    {
        // Get amplitude from audio
        float[] samples = new float[512];
        audioSource.GetOutputData(samples, 0);
        float rawAmplitude = samples.Select(Mathf.Abs).Average() * amplitudeMultiplier;
        
        // Update amplitude history and calculate smoothed amplitude
        amplitudeHistory.Dequeue();
        amplitudeHistory.Enqueue(rawAmplitude);
        currentAmplitude = amplitudeHistory.Average();
        
        // Get frequency data if enabled
        if (useFrequencyAnalysis)
        {
            audioSource.GetSpectrumData(spectrumData, 0, FFTWindow.BlackmanHarris);
            
            // Analyze different frequency bands
            float lowFreq = AnalyzeFrequencyRange(0, 5) * lowFrequencyInfluence;
            float midFreq = AnalyzeFrequencyRange(5, 50) * midFrequencyInfluence;
            float highFreq = AnalyzeFrequencyRange(50, 100) * highFrequencyInfluence;
            
            // Use frequency data to determine vowel shape
            DetermineVowelShape(lowFreq, midFreq, highFreq);
        }
        else
        {
            // Simple amplitude-based vowel changes
            if (Time.time > vowelChangeTime && currentAmplitude > 0.1f)
            {
                vowelChangeTime = Time.time + vowelHoldTime;
                currentVowel = (VowelShape)Random.Range(0, 5); // Random vowel
            }
        }
        
        // Apply the blend shapes based on current amplitude and vowel
        ApplyBlendShapes(currentAmplitude);
    }
    
    float AnalyzeFrequencyRange(int startIdx, int endIdx)
    {
        float sum = 0f;
        for (int i = startIdx; i < endIdx && i < spectrumData.Length; i++)
        {
            sum += spectrumData[i];
        }
        return sum / (endIdx - startIdx);
    }
    
    void DetermineVowelShape(float lowFreq, float midFreq, float highFreq)
    {
        // Only change vowel if we have significant sound and enough time has passed
        if (Time.time > vowelChangeTime && (lowFreq + midFreq + highFreq) > 0.01f)
        {
            vowelChangeTime = Time.time + vowelHoldTime;
            
            // Simple frequency-based vowel determination
            if (lowFreq > midFreq && lowFreq > highFreq)
            {
                currentVowel = VowelShape.O; // Low frequencies - rounded mouth
            }
            else if (midFreq > lowFreq && midFreq > highFreq)
            {
                if (Random.value > 0.5f)
                    currentVowel = VowelShape.A; // Mid frequencies - open mouth
                else
                    currentVowel = VowelShape.E; // Mid frequencies - wide mouth
            }
            else
            {
                if (Random.value > 0.5f)
                    currentVowel = VowelShape.I; // High frequencies - narrow mouth
                else
                    currentVowel = VowelShape.U; // High frequencies - small rounded mouth
            }
        }
        
        // If sound is very quiet, return to rest
        if ((lowFreq + midFreq + highFreq) < 0.005f)
        {
            currentVowel = VowelShape.Rest;
        }
    }
    
    void ApplyBlendShapes(float amplitude)
    {
        // Base values for all blend shapes
        float mouthOpenValue = 0f;
        float mouthUpValue = 0f;
        float midMouthValue = 0f;
        float narrowMouthValue = 0f;
        
        // Scale amplitude to a reasonable range
        float scaledAmplitude = Mathf.Clamp(amplitude * 100f, 0, 100);
        
        // Apply different blend shape combinations based on vowel
        switch (currentVowel)
        {
            case VowelShape.A: // "Ah" sound
                mouthOpenValue = scaledAmplitude;
                mouthUpValue = scaledAmplitude * 0.1f;
                midMouthValue = scaledAmplitude * 0.3f;
                break;
                
            case VowelShape.E: // "Eh" sound
                mouthOpenValue = scaledAmplitude * 0.5f;
                midMouthValue = scaledAmplitude * 0.8f;
                mouthUpValue = scaledAmplitude * 0.3f;
                break;
                
            case VowelShape.I: // "Ee" sound
                mouthOpenValue = scaledAmplitude * 0.3f;
                midMouthValue = scaledAmplitude * 0.9f;
                narrowMouthValue = scaledAmplitude * 0.5f;
                break;
                
            case VowelShape.O: // "Oh" sound
                mouthOpenValue = scaledAmplitude * 0.7f;
                mouthUpValue = scaledAmplitude * 0.2f;
                narrowMouthValue = scaledAmplitude * 0.3f;
                break;
                
            case VowelShape.U: // "Oo" sound
                mouthOpenValue = scaledAmplitude * 0.4f;
                narrowMouthValue = scaledAmplitude * 0.8f;
                break;
                
            case VowelShape.Rest: // Neutral/rest position
                mouthOpenValue = scaledAmplitude * 0.1f;
                break;
        }
        
        // Add subtle random variations for more natural movement
        float randomVariation = Mathf.Sin(Time.time * 15f) * 5f;
        
        // Apply the blend shape weights
        faceRenderer.SetBlendShapeWeight(mouthOpenBlendShapeIndex, mouthOpenValue + randomVariation * 0.2f);
        faceRenderer.SetBlendShapeWeight(mouthUpBlendShapeIndex, mouthUpValue + randomVariation * 0.1f);
        faceRenderer.SetBlendShapeWeight(midMouthLeftBlendShapeIndex, midMouthValue);
        faceRenderer.SetBlendShapeWeight(midMouthRightBlendShapeIndex, midMouthValue);
        faceRenderer.SetBlendShapeWeight(mouthNarrowLeftBlendShapeIndex, narrowMouthValue);
        faceRenderer.SetBlendShapeWeight(mouthNarrowRightBlendShapeIndex, narrowMouthValue);
        
        // Add subtle eyebrow movement for emphasis on louder sounds
        if (scaledAmplitude > 50f)
        {
            float browLift = Mathf.Lerp(0, 30, (scaledAmplitude - 50f) / 50f);
            faceRenderer.SetBlendShapeWeight(browsUpLeftBlendShapeIndex, browLift);
            faceRenderer.SetBlendShapeWeight(browsUpRightBlendShapeIndex, browLift);
        }
        else
        {
            faceRenderer.SetBlendShapeWeight(browsUpLeftBlendShapeIndex, 0);
            faceRenderer.SetBlendShapeWeight(browsUpRightBlendShapeIndex, 0);
        }
    }

    IEnumerator BlinkRoutine()
    {
        while (true)
        {
            // Random time between blinks
            yield return new WaitForSeconds(Random.Range(2f, 5f));

            // Blink animation (eyes close)
            faceRenderer.SetBlendShapeWeight(eyesClosedLeftBlendShapeIndex, 100);
            faceRenderer.SetBlendShapeWeight(eyesClosedRightBlendShapeIndex, 100);

            yield return new WaitForSeconds(0.1f); // Blink duration

            // Reset blink (eyes open)
            faceRenderer.SetBlendShapeWeight(eyesClosedLeftBlendShapeIndex, 0);
            faceRenderer.SetBlendShapeWeight(eyesClosedRightBlendShapeIndex, 0);
        }
    }

    void ResetBlendShapes()
    {
        faceRenderer.SetBlendShapeWeight(mouthOpenBlendShapeIndex, 0);
        faceRenderer.SetBlendShapeWeight(midMouthLeftBlendShapeIndex, 0);
        faceRenderer.SetBlendShapeWeight(midMouthRightBlendShapeIndex, 0);
        faceRenderer.SetBlendShapeWeight(mouthNarrowLeftBlendShapeIndex, 0);
        faceRenderer.SetBlendShapeWeight(mouthNarrowRightBlendShapeIndex, 0);
        faceRenderer.SetBlendShapeWeight(mouthUpBlendShapeIndex, 0);
        faceRenderer.SetBlendShapeWeight(browsUpLeftBlendShapeIndex, 0);
        faceRenderer.SetBlendShapeWeight(browsUpRightBlendShapeIndex, 0);
    }

    [ContextMenu("Play Voiceover")]
    public void PlayVoiceover()
    {
        if (audioSource.clip != null)
        {
            audioSource.Play();
        }
        else
        {
            Debug.LogError("No audio clip assigned!");
        }
    }
}
