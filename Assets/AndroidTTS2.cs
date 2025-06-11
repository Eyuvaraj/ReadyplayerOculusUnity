using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;

public class AndroidTTS2 : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private InputField inputField;
    // No longer need the AudioSource
    // [SerializeField] private AudioSource audioSource;

    private AndroidJavaObject ttsObject;
    private AndroidJavaObject unityActivity;
    private UtteranceProgressListener progressListener;

    private const string utteranceId = "ttsDirectPlayback";

    // Constant for queue mode
    private const int QUEUE_FLUSH = 0;

    void Start()
    {
        button.onClick.AddListener(() =>
        {
            string text = inputField.text;
            if (!string.IsNullOrEmpty(text))
            {
                SpeakText(text);
            }
        });

        if (Application.platform == RuntimePlatform.Android)
        {
            InitTTS();
        }
    }

    void InitTTS()
    {
        Debug.Log("[InitTTS] Initializing Android TTS...");

        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

        // Instantiate the listener
        progressListener = new UtteranceProgressListener();

        ttsObject = new AndroidJavaObject("android.speech.tts.TextToSpeech", unityActivity, new TTSInitListener((status) =>
        {
            if (status == 0) // SUCCESS
            {
                AndroidJavaObject localeIndia = new AndroidJavaObject("java.util.Locale", "en", "IN");
                int result = ttsObject.Call<int>("setLanguage", localeIndia);

                if (result == -1 || result == -2) // LANG_MISSING_DATA or LANG_NOT_SUPPORTED
                {
                    Debug.LogError("[InitTTS] Language not supported: en-IN");
                    return;
                }

                // Set the progress listener to receive callbacks
                ttsObject.Call("setOnUtteranceProgressListener", progressListener);

                Debug.Log("[TTSInitListener] TTS initialized successfully.");
            }
            else
            {
                Debug.LogError("[TTSInitListener] TTS initialization failed.");
            }
        }));
    }

    public void SpeakText(string text)
    {
        Debug.Log("[SpeakText] Speaking text: " + text);

        if (Application.platform != RuntimePlatform.Android || ttsObject == null) return;

        // Use the 'speak' method for direct playback
        // Method signature: speak(CharSequence text, int queueMode, Bundle params, String utteranceId)
        int result = ttsObject.Call<int>("speak", text, QUEUE_FLUSH, null, utteranceId);

        if (result != 0) // SUCCESS is 0
        {
            Debug.LogError("[SpeakText] TTS speak call failed.");
        }
    }

    void OnDestroy()
    {
        if (ttsObject != null)
        {
            ttsObject.Call("stop");
            ttsObject.Call("shutdown");
            Debug.Log("[OnDestroy] TTS shutdown.");
        }
    }

    // --- Inner proxy class for Initialization ---
    private class TTSInitListener : AndroidJavaProxy
    {
        private readonly Action<int> onInitCallback;

        public TTSInitListener(Action<int> onInitCallback) : base("android.speech.tts.TextToSpeech$OnInitListener")
        {
            this.onInitCallback = onInitCallback;
        }

        void onInit(int status)
        {
            Debug.Log("[TTSInitListener.onInit] Status: " + status);
            onInitCallback?.Invoke(status);
        }
    }

    // --- Inner proxy class for Playback Callbacks ---
    private class UtteranceProgressListener : AndroidJavaProxy
    {
        public UtteranceProgressListener() : base("android.speech.tts.UtteranceProgressListener") { }

        // Called when the TTS engine starts speaking the utterance.
        void onStart(string utteranceId)
        {
            Debug.Log($"[UtteranceProgressListener] Speech started for utterance: {utteranceId}");
        }

        // Called when the utterance has been successfully spoken.
        void onDone(string utteranceId)
        {
            Debug.Log($"[UtteranceProgressListener] Speech finished for utterance: {utteranceId}");
        }

        // Called when an error occurs during processing.
        void onError(string utteranceId)
        {
            Debug.LogError($"[UtteranceProgressListener] Speech error for utterance: {utteranceId}");
        }
    }
}