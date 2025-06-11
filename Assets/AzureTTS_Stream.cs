using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class AzureTTS_Stream1 : MonoBehaviour
{
    [Header("Azure TTS Settings")]
    [Tooltip("Your Azure Cognitive Services subscription key.")]
    [SerializeField] private string azureTTSKey = "2aMXT5pyJYKZSxwUTgsO3ziEQjUA7rKtjnHbGEIkVdk7D4zCUkPnJQQJ99BAACGhslBXJ3w3AAAYACOG6IKl";
    [Tooltip("The region for your Azure resource (e.g., 'centralindia', 'eastus').")]
    [SerializeField] private string azureRegion = "centralindia";

    [Header("Voice Settings")]
    [Tooltip("The voice to use for synthesis. E.g., 'en-US-JennyNeural'.")]
    [SerializeField] private string voiceName = "en-US-JennyNeural";
    [Tooltip("The output audio format.")]
    [SerializeField] private string outputFormat = "audio-24khz-160kbitrate-mono-mp3";

    [Header("UI")]
    [SerializeField] private Button speakButton;
    [SerializeField] private InputField inputField;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    private string ttsEndpoint;

    void Start()
    {
        // Construct the endpoint URL from the region
        ttsEndpoint = $"https://{azureRegion}.tts.speech.microsoft.com/cognitiveservices/v1";

        if (speakButton != null && inputField != null)
        {
            speakButton.onClick.AddListener(() =>
            {
                StartCoroutine(GenerateAndPlayAzureTTS_Rest(inputField.text));
            });
        }
    }

    private IEnumerator GenerateAndPlayAzureTTS_Rest(string text)
    {
        if (string.IsNullOrWhiteSpace(azureTTSKey) || string.IsNullOrWhiteSpace(azureRegion))
        {
            Debug.LogError("Azure TTS Key or Region is not set.");
            yield break;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            Debug.LogWarning("No text provided for TTS.");
            yield break;
        }

        speakButton.interactable = false;

        // Create the request body using SSML (Speech Synthesis Markup Language)
        // This gives you more control over the speech output
        string ssml = $"<speak version='1.0' xml:lang='en-US'><voice xml:lang='en-US' name='{voiceName}'>{EscapeXml(text)}</voice></speak>";
        byte[] ssmlData = Encoding.UTF8.GetBytes(ssml);

        // Create the UnityWebRequest
        using (UnityWebRequest www = new UnityWebRequest(ttsEndpoint, "POST"))
        {
            // Set Headers
            www.SetRequestHeader("Ocp-Apim-Subscription-Key", azureTTSKey);
            www.SetRequestHeader("Content-Type", "application/ssml+xml");
            www.SetRequestHeader("X-Microsoft-OutputFormat", outputFormat);
            www.SetRequestHeader("User-Agent", "Unity");

            // Set Body
            www.uploadHandler = new UploadHandlerRaw(ssmlData);

            // Set the DownloadHandler to process audio directly
            // Note: The AudioType must match the format requested in the header.
            // For "audio-24khz-160kbitrate-mono-mp3", use AudioType.MPEG.
            // For WAV formats, use AudioType.WAV.
            www.downloadHandler = new DownloadHandlerAudioClip(ttsEndpoint, AudioType.MPEG);

            Debug.Log("Sending TTS request to Azure...");
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("TTS audio successfully received.");
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                audioSource.clip = clip;
                audioSource.Play();
            }
            else
            {
                Debug.LogError("Azure TTS Error: " + www.error);
                // The error details are often in the response body
                Debug.LogError("Error Details: " + www.downloadHandler.text);
            }
        }

        speakButton.interactable = true;
    }

    // Helper function to escape special XML characters
    private string EscapeXml(string s)
    {
        return s.Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
    }
}