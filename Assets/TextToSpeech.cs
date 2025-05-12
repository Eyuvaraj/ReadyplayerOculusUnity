using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class TextToSpeech : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private InputField inputField;
    [SerializeField] private AudioSource audioSource;

    private AndroidJavaObject ttsObject;
    private AndroidJavaObject unityActivity;
    private string filePath;
    private const string utteranceId = "ttsOutput";
    private const string fileName = "audio.wav";

    void Start()
    {
        button.onClick.AddListener(() =>
        {
            string text = inputField.text;
            if (!string.IsNullOrEmpty(text))
                SpeakText(text);
        });

        if (Application.platform == RuntimePlatform.Android)
        {
            InitTTS();
        }

        filePath = Path.Combine(Application.persistentDataPath, fileName);

        Debug.Log("\n\n[Start] Expected audio path: " + filePath + "\n\n");
    }

    void InitTTS()
    {
        Debug.Log("\n\n[InitTTS] Initializing Android TTS...\n\n");

        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

        ttsObject = new AndroidJavaObject("android.speech.tts.TextToSpeech", unityActivity, new TTSInitListener((status) =>
        {
            if (status == 0) // SUCCESS
            {
                AndroidJavaObject localeIndia = new AndroidJavaObject("java.util.Locale", "en", "IN");
                int result = ttsObject.Call<int>("setLanguage", localeIndia);

                if (result == -1 || result == -2)
                {
                    Debug.LogError("\n\n[InitTTS] Language not supported: en-IN\n\n");
                    return;
                }

                // Just log available voices for info
                try
                {
                    AndroidJavaObject voices = ttsObject.Call<AndroidJavaObject>("getVoices");
                    AndroidJavaObject iterator = voices.Call<AndroidJavaObject>("iterator");

                    while (iterator.Call<bool>("hasNext"))
                    {
                        AndroidJavaObject voice = iterator.Call<AndroidJavaObject>("next");
                        string voiceName = voice.Call<string>("getName");
                        Debug.Log($"[InitTTS] Found voice: {voiceName}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[InitTTS] Error while listing voices: {e.Message}");
                }

                Debug.Log("\n\n[TTSInitListener] TTS initialized successfully.\n\n");
            }
            else
            {
                Debug.LogError("\n\n[TTSInitListener] TTS initialization failed.\n\n");
            }
        }));
    }

    public void SpeakText(string text)
    {
        Debug.Log("\n\n[SpeakText] Synthesizing text: " + text + "\n\n");

        if (Application.platform != RuntimePlatform.Android || ttsObject == null) return;

        AndroidJavaObject file = new AndroidJavaObject("java.io.File", filePath);
        AndroidJavaObject bundle = new AndroidJavaObject("android.os.Bundle");
        bundle.Call("putString", "utteranceId", utteranceId);
        bundle.Call("putString", "voiceName", "en-in-x-end-local"); // <-- Force the desired voice

        int result = ttsObject.Call<int>("synthesizeToFile", text, bundle, file, utteranceId);

        if (result != 0)
        {
            Debug.LogError("\n\n[SpeakText] TTS synthesis failed\n\n");
        }
        else
        {
            Debug.Log("\n\n[SpeakText] TTS synthesis started: " + filePath + "\n\n");
            StartCoroutine(WaitAndPlay(2.0f));
        }
    }

    IEnumerator WaitAndPlay(float delay)
    {
        Debug.Log("\n\n[WaitAndPlay] Waiting " + delay + " seconds for file generation...\n\n");

        yield return new WaitForSeconds(delay);

        if (File.Exists(filePath))
        {
            Debug.Log("\n\n[WaitAndPlay] Audio file exists, playing...\n\n");
            PlayAudioManually(filePath);
        }
        else
        {
            Debug.LogError("\n\n[WaitAndPlay] Audio file not found at: " + filePath + "\n\n");
        }
    }

    void PlayAudioManually(string path)
    {
        Debug.Log("\n\n[PlayAudioManually] Attempting to play audio from: " + path + "\n\n");

        try
        {
            byte[] fileData = File.ReadAllBytes(path);
            AudioClip clip = ToAudioClip(fileData, 0, "TTS_Audio");

            if (clip != null)
            {
                audioSource.clip = clip;
                audioSource.Play();
                Debug.Log("\n\n[PlayAudioManually] Audio playback started.\n\n");
            }
            else
            {
                Debug.LogError("\n\n[PlayAudioManually] AudioClip creation failed.\n\n");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("\n\n[PlayAudioManually] Error: " + ex.Message + "\n\n");
        }
    }

    AudioClip ToAudioClip(byte[] wavFile, int offsetSamples = 0, string name = "wav")
    {
        Debug.Log("\n\n[ToAudioClip] Parsing WAV file to AudioClip...\n\n");

        try
        {
            int channels = wavFile[22];
            int sampleRate = BitConverter.ToInt32(wavFile, 24);
            int subchunk2 = BitConverter.ToInt32(wavFile, 40);
            int samples = subchunk2 / 2;

            float[] data = new float[samples];
            int offset = 44;

            for (int i = 0; i < samples; i++)
            {
                short sample = BitConverter.ToInt16(wavFile, offset + i * 2);
                data[i] = sample / 32768.0f;
            }

            AudioClip audioClip = AudioClip.Create(name, samples, channels, sampleRate, false);
            audioClip.SetData(data, offsetSamples);

            Debug.Log("\n\n[ToAudioClip] AudioClip created successfully.\n\n");

            return audioClip;
        }
        catch (Exception ex)
        {
            Debug.LogError("\n\n[ToAudioClip] WAV parsing error: " + ex.Message + "\n\n");
            return null;
        }
    }

    void OnDestroy()
    {
        if (ttsObject != null)
        {
            ttsObject.Call("shutdown");
            Debug.Log("\n\n[OnDestroy] TTS shutdown.\n\n");
        }
    }

    // --- Inner proxy class (used only during init) ---

    private class TTSInitListener : AndroidJavaProxy
    {
        private readonly Action<int> onInitCallback;

        public TTSInitListener(Action<int> onInitCallback) : base("android.speech.tts.TextToSpeech$OnInitListener")
        {
            this.onInitCallback = onInitCallback;
        }

        void onInit(int status)
        {
            Debug.Log("\n\n[TTSInitListener.onInit] Status: " + status + "\n\n");
            onInitCallback?.Invoke(status);
        }
    }
}