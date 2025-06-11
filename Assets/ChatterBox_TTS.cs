using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using UnityEngine.Networking;
using Newtonsoft.Json; // Used for JSON serialization
using UnityEngine.UI;

public class ChatterBox_TTS : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private InputField inputField;

    // Custom TTS service URL
    private const string customTTSApiUrl = "http://4.213.157.45:28000/generate";

    public AudioSource audioSource;

    void Start()
    {
        // Add a listener to the button to trigger the speech generation
        button.onClick.AddListener(MakeRequest);
    }

    /// <summary>
    /// Initiates the speech generation request when the button is clicked.
    /// Uses async/await for non-blocking network operations.
    /// </summary>
    async void MakeRequest()
    {
        // Call the asynchronous method to generate speech
        await GenerateSpeech(inputField.text);
    }

    /// <summary>
    /// Generates speech from the provided text using the custom TTS service.
    /// It constructs a JSON payload and sends it as a POST request.
    /// </summary>
    /// <param name="text">The text to be converted to speech.</param>
    async Task GenerateSpeech(string text)
    {
        // Use HttpClient for making HTTP requests
        using (HttpClient client = new HttpClient())
        {
            // Clear any default request headers from previous requests
            client.DefaultRequestHeaders.Clear();
            // User-Agent header is recommended for many web APIs
            client.DefaultRequestHeaders.Add("User-Agent", "UnityApp");

            // Create the JSON payload as required by your custom TTS service
            // 'exaggeration' and 'cfg' are hardcoded to 0.5 as per the curl example,
            // since there are no UI elements for them in the provided script.
            var payload = new
            {
                text = text,
                exaggeration = 0.5f,
                cfg = 0.5f
            };

            // Serialize the payload object to a JSON string
            string jsonPayload = JsonConvert.SerializeObject(payload);

            // Create StringContent with the JSON payload, specifying UTF-8 encoding and application/json media type
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            Debug.Log($"Sending request to: {customTTSApiUrl} with payload: {jsonPayload}");

            // Send the POST request to the custom TTS API
            HttpResponseMessage response = await client.PostAsync(customTTSApiUrl, content);

            // Check if the request was successful
            if (response.IsSuccessStatusCode)
            {
                // Read the audio bytes from the response content
                byte[] audioBytes = await response.Content.ReadAsByteArrayAsync();

                // Define the path to save the audio file in persistent data path
                // Using .wav extension as the custom service returns audio/wav
                string path = Path.Combine(Application.persistentDataPath, "generated_speech.wav");

                // Write the audio bytes to the file asynchronously
                await File.WriteAllBytesAsync(path, audioBytes);
                Debug.Log("Audio file saved at: " + path);

                // Start a coroutine to load and play the saved audio file
                StartCoroutine(PlayAudio(path));
            }
            else
            {
                // Log an error if the request was not successful, including status code and response content
                Debug.LogError($"Error from custom TTS service: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            }
        }
    }

    /// <summary>
    /// Loads an audio clip from a local file path and plays it using the AudioSource.
    /// Uses UnityWebRequestMultimedia to handle audio loading.
    /// </summary>
    /// <param name="filePath">The absolute path to the audio file.</param>
    IEnumerator PlayAudio(string filePath)
    {
        // Create a UnityWebRequest to get the audio clip from the local file
        // Ensure the file path is prefixed with "file://" for local file access
        // Specify AudioType.WAV for the .wav file format
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.WAV))
        {
            // Send the web request and wait for it to complete
            yield return www.SendWebRequest();

            // Check if the web request was successful
            if (www.result == UnityWebRequest.Result.Success)
            {
                // Get the downloaded audio clip content
                audioSource.clip = DownloadHandlerAudioClip.GetContent(www);
                // Play the audio clip
                audioSource.Play();
                Debug.Log("Playing audio...");
            }
            else
            {
                // Log an error if loading the audio failed
                Debug.LogError("Failed to load audio: " + www.error);
            }
        }
    }
}
