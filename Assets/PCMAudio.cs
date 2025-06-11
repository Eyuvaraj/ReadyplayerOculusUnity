using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Required for Button and InputField
using UnityEngine.Networking; // Required for UnityWebRequest and DownloadHandlerAudioClip
using System.IO; // Required for Path

public class PCMAudio : MonoBehaviour
{
    // --- UI References ---
    [SerializeField] private InputField nameInputField; // Input field for "name sake"
    [SerializeField] private Button playButton;         // Button to trigger audio playback

    // --- Audio Playback ---
    public AudioSource audioSource; // Assign this in the Inspector

    // --- Audio File Path ---
    // The path to your pre-existing audio file
    // IMPORTANT: Make sure this file exists at this exact path on your system!
    // Unity's file loading generally expects paths to be converted to URI format (e.g., file:///C:/...)
    private const string audioFilePath = "C:/Users/admin/RPM-LipSync/Assets/output_audio.wav";


    void Start()
    {
        // --- Input Validation ---
        if (nameInputField == null)
        {
            Debug.LogError("Name InputField not assigned in the Inspector for LocalAudioPlayer!");
            return;
        }
        if (playButton == null)
        {
            Debug.LogError("Play Button not assigned in the Inspector for LocalAudioPlayer!");
            return;
        }

        // --- AudioSource Setup ---
        // Get or add AudioSource component to this GameObject if not already present.
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // --- Button Listener Setup ---
        // Add a listener to the button to call the OnPlayButtonClicked method when clicked.
        playButton.onClick.AddListener(OnPlayButtonClicked);

        Debug.Log("LocalAudioPlayer initialized. Ready to play audio from: " + audioFilePath);
    }

    /// <summary>
    /// This method is called when the play button is clicked.
    /// It starts the coroutine to load and play the audio file.
    /// </summary>
    private void OnPlayButtonClicked()
    {
        // You can use the text from nameInputField here if you need it for logging or other purposes,
        // but it's not used for playing the audio file itself as per your request.
        Debug.Log($"Playing audio triggered. Input name: '{nameInputField.text}'");

        // Start the coroutine to load and play the audio.
        StartCoroutine(LoadAndPlayAudio());
    }

    /// <summary>
    /// Coroutine to load the audio clip from the local file system and play it.
    /// </summary>
    IEnumerator LoadAndPlayAudio()
    {
        // Construct the URI for the local file.
        // On Windows, a path like "C:/path/to/file.wav" needs to become "file:///C:/path/to/file.wav"
        string uriPath = "file:///" + audioFilePath.Replace("\\", "/"); // Normalize backslashes for URI

        Debug.Log("Attempting to load audio from URI: " + uriPath);

        // Use UnityWebRequestMultimedia to load the audio clip.
        // AudioType.WAV is specified as the expected format.
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uriPath, AudioType.WAV))
        {
            // Send the request and wait for it to complete.
            yield return www.SendWebRequest();

            // Check if there were any errors during the web request.
            if (www.result == UnityWebRequest.Result.Success)
            {
                // Get the downloaded AudioClip.
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);

                if (clip != null)
                {
                    // Assign the loaded clip to the AudioSource and play it.
                    audioSource.clip = clip;
                    audioSource.Play();
                    Debug.Log("Audio successfully loaded and playing: " + audioFilePath);
                }
                else
                {
                    // This can happen if the file is not a valid audio clip, even if download succeeded.
                    Debug.LogError("Failed to get AudioClip from the loaded file. Is it a valid WAV? " + uriPath);
                }
            }
            else
            {
                // Log any network or file access errors.
                Debug.LogError($"Error loading audio file: {www.error} at path: {uriPath}");
            }
        }
    }
}