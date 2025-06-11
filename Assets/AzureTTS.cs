using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using UnityEngine.Networking;
using Newtonsoft.Json;
using UnityEngine.UI;
using UnityEditor; // This might not be needed in a final build, only for editor functionality

public class AzureTTS : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private InputField inputField;

    // Azure TTS credentials
    private const string azureTTSKey = "2aMXT5pyJYKZSxwUTgsO3ziEQjUA7rKtjnHbGEIkVdk7D4zCUkPnJQQJ99BAACGhslBXJ3w3AAAYACOG6IKl";
    private const string azureRegion = "centralindia";
    private static readonly string azureAuthTokenUrl = $"https://{azureRegion}.api.cognitive.microsoft.com/sts/v1.0/issueToken";
    private static readonly string azureTTSApiUrl = $"https://{azureRegion}.tts.speech.microsoft.com/cognitiveservices/v1";

    public AudioSource audioSource;

    void Start()
    {
        button.onClick.AddListener(MakeRequest);
    }

    async void MakeRequest()
    {
        await GenerateSpeech(inputField.text);
    }

    async Task GenerateSpeech(string text)
    {
        using (HttpClient client = new HttpClient())
        {
            // 1. Get Azure TTS Access Token
            client.DefaultRequestHeaders.Clear(); // Clear any previous headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", azureTTSKey);
            HttpResponseMessage tokenResponse = await client.PostAsync(azureAuthTokenUrl, null);

            string authToken;
            if (tokenResponse.IsSuccessStatusCode)
            {
                authToken = await tokenResponse.Content.ReadAsStringAsync();
                Debug.Log("Azure TTS Token obtained successfully.");
            }
            else
            {
                Debug.LogError($"Error obtaining Azure TTS token: {tokenResponse.StatusCode} - {await tokenResponse.Content.ReadAsStringAsync()}");
                return;
            }

            // 2. Make the TTS request
            client.DefaultRequestHeaders.Clear(); // Clear headers for the next request
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");
            client.DefaultRequestHeaders.Add("X-Microsoft-OutputFormat", "audio-16khz-128kbitrate-mono-mp3"); // Desired audio format
            client.DefaultRequestHeaders.Add("User-Agent", "UnityApp"); // Recommended for Azure APIs

            // SSML (Speech Synthesis Markup Language) for more control over speech.
            // You can customize voice, style, etc.
            // Example for a cheerful tone with a female voice (e.g., Aria or Jenny)
            // You'll need to choose an appropriate voice name for your region.
            // You can find available voices here: https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/speech-synthesis-markup#supported-voices
            string ssml = $@"
                <speak version='1.0' xml:lang='en-US'>
                    <voice name='en-US-JennyNeural'>
                        <prosody rate='+10%' pitch='+10%'>
                            {text}
                        </prosody>
                    </voice>
                </speak>";

            var content = new StringContent(ssml, Encoding.UTF8, "application/ssml+xml");

            HttpResponseMessage response = await client.PostAsync(azureTTSApiUrl, content);

            if (response.IsSuccessStatusCode)
            {
                byte[] audioBytes = await response.Content.ReadAsByteArrayAsync();
                string path = Path.Combine(Application.persistentDataPath, "audio.mp3");
                await File.WriteAllBytesAsync(path, audioBytes);
                Debug.Log("Audio file saved at: " + path);
                StartCoroutine(PlayAudio(path));
            }
            else
            {
                Debug.LogError($"Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            }
        }
    }

    IEnumerator PlayAudio(string filePath)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                audioSource.clip = DownloadHandlerAudioClip.GetContent(www);
                audioSource.Play();
                Debug.Log("Playing audio...");
            }
            else
            {
                Debug.LogError("Failed to load audio: " + www.error);
            }
        }
    }
}