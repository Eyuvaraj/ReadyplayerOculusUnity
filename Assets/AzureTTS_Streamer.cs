using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Text;

public class AzureTTS_Streamer : MonoBehaviour
{
    [Header("Azure TTS Settings")]
    [SerializeField] private string subscriptionKey = "2aMXT5pyJYKZSxwUTgsO3ziEQjUA7rKtjnHbGEIkVdk7D4zCUkPnJQQJ99BAACGhslBXJ3w3AAAYACOG6IKl";
    [SerializeField] private string region = "centralindia";
    [SerializeField] private string voiceName = "en-US-JennyNeural";

    [Header("UI")]
    [SerializeField] private InputField inputField;
    [SerializeField] private Button speakButton;
    [SerializeField] private AudioSource audioSource;

    void Start()
    {
        speakButton.onClick.AddListener(() =>
        {
            string text = inputField.text;
            if (!string.IsNullOrWhiteSpace(text))
                StartCoroutine(StreamTTS(text));
        });
    }

    IEnumerator StreamTTS(string text)
    {
        string uri = $"https://{region}.tts.speech.microsoft.com/cognitiveservices/v1";

        string ssml = @$"
<speak version='1.0' xml:lang='en-US'>
    <voice xml:lang='en-US' xml:gender='Female' name='{voiceName}'>
        {text}
    </voice>
</speak>";

        byte[] postData = Encoding.UTF8.GetBytes(ssml);

        UnityWebRequest www = new UnityWebRequest(uri, "POST");
        www.uploadHandler = new UploadHandlerRaw(postData);
        www.downloadHandler = new DownloadHandlerBuffer(); // We'll convert to audio
        www.SetRequestHeader("Content-Type", "application/ssml+xml");
        www.SetRequestHeader("X-Microsoft-OutputFormat", "riff-16khz-16bit-mono-pcm"); // WAV/PCM, Unity compatible
        www.SetRequestHeader("Ocp-Apim-Subscription-Key", subscriptionKey);
        www.SetRequestHeader("User-Agent", "Unity-AzureTTS");

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("TTS failed: " + www.error);
        }
        else
        {
            Debug.Log("TTS stream received. Decoding...");

            byte[] wavData = www.downloadHandler.data;

            // Skip WAV header to get PCM data
            int headerOffset = 44; // Standard WAV header size
            if (wavData.Length <= headerOffset)
            {
                Debug.LogError("Invalid WAV file received.");
                yield break;
            }

            byte[] pcmData = new byte[wavData.Length - headerOffset];
            System.Buffer.BlockCopy(wavData, headerOffset, pcmData, 0, pcmData.Length);

            float[] samples = Convert16BitPCMToFloats(pcmData);

            AudioClip clip = AudioClip.Create("AzureTTS", samples.Length, 1, 16000, false);
            clip.SetData(samples, 0);
            audioSource.clip = clip;
            audioSource.Play();
        }
    }

    // Converts 16-bit PCM data to float samples (-1 to 1)
    private float[] Convert16BitPCMToFloats(byte[] pcmData)
    {
        int sampleCount = pcmData.Length / 2;
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            short sample = (short)(pcmData[i * 2] | (pcmData[i * 2 + 1] << 8));
            samples[i] = sample / 32768f;
        }

        return samples;
    }
}
