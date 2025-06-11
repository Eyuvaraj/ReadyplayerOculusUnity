using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking; // For UnityWebRequest
using System.Text; // For Encoding
using UnityEngine.UI; // For UI elements like Button and InputField

public class GroqTTS : MonoBehaviour
{
    // --- API Configuration ---
    [SerializeField] private string apiKey = "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"; // Your Groq API key (remember to replace for security in production)
    [SerializeField] private string apiUrl = "https://api.groq.com/openai/v1/audio/speech";
    [SerializeField] private string voice = "Fritz-PlayAI"; // Groq voice model
    [SerializeField] private string model = "playai-tts"; // Groq model

    // --- UI References ---
    [SerializeField] private InputField inputField; // Reference to your UI InputField
    [SerializeField] private Button speakButton;    // Reference to your UI Button

    // --- Audio Playback ---
    public AudioSource audioSource; // Reference to your AudioSource component

    void Start()
    {
        // Ensure UI elements are assigned in the Inspector
        if (inputField == null)
        {
            Debug.LogError("InputField not assigned in the Inspector for GroqTTS!");
            return;
        }
        if (speakButton == null)
        {
            Debug.LogError("Speak Button not assigned in the Inspector for GroqTTS!");
            return;
        }

        // Get or add AudioSource component
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // Add a listener to the button to trigger the speech generation
        speakButton.onClick.AddListener(OnSpeakButtonClicked);
    }

    /// <summary>
    /// Called when the speak button is clicked. Initiates the TTS request.
    /// </summary>
    private void OnSpeakButtonClicked()
    {
        string textToSpeak = inputField.text;

        if (string.IsNullOrWhiteSpace(textToSpeak))
        {
            Debug.LogWarning("Input field is empty. Please enter some text to speak.");
            return;
        }

        StartCoroutine(GetSpeechFromGroq(textToSpeak));
    }

    /// <summary>
    /// Sends a request to the Groq API to convert text to speech and attempts to play the audio as it downloads.
    /// </summary>
    /// <param name="text">The text from the InputField to be converted to speech.</param>
    IEnumerator GetSpeechFromGroq(string text)
    {
        string jsonPayload = "{\"model\": \"" + model + "\", \"input\": \"" + text.Replace("\"", "\\\"") + "\", \"voice\": \"" + voice + "\"}";
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        using (UnityWebRequest webRequest = new UnityWebRequest(apiUrl, "POST"))
        {
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);

            // --- Key for "real-time" / progressive playback ---
            // DownloadHandlerAudioClip will try to stream the audio if the format allows
            // and if enough header/initial data is received.
            webRequest.downloadHandler = new DownloadHandlerAudioClip(webRequest.url, AudioType.WAV);
            // --- End Key Change ---

            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.SetRequestHeader("Authorization", "Bearer " + apiKey);

            Debug.Log($"Sending request to Groq API with text: '{text}'");
            yield return webRequest.SendWebRequest(); // Wait for the entire request to complete

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Groq API Error: " + webRequest.error);
                Debug.LogError("Response Code: " + webRequest.responseCode);
                // Important: Check this for API-specific error messages if result is not success
                Debug.LogError("Response Text: " + webRequest.downloadHandler.text);
            }
            else
            {
                Debug.Log("Speech audio received successfully!");

                // Get the AudioClip from the download handler.
                // Play() will then attempt to play as soon as sufficient data is buffered.
                AudioClip audioClip = DownloadHandlerAudioClip.GetContent(webRequest);

                if (audioClip != null)
                {
                    audioSource.clip = audioClip;
                    audioSource.Play();
                    Debug.Log("Playing generated speech.");
                }
                else
                {
                    // This error often indicates that the downloaded data was not a valid audio format
                    // despite the HTTP request succeeding (e.g., malformed WAV, or unexpected content).
                    Debug.LogError("Failed to get AudioClip from response. Verify API's audio output format.");
                }
            }
        }
    }
}