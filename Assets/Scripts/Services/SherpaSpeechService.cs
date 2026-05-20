using System.Collections;
using System.IO;
using UnityEngine;
using SherpaOnnx;

public class SherpaSpeechService : MonoBehaviour
{
    private OfflineTts tts;
    private AudioSource audioSource;
    private SessionManager sessionManager;
    private bool isSpeaking = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        sessionManager = FindAnyObjectByType<SessionManager>();

        // Путь к папке с моделью
        string modelDir = Path.Combine(Application.streamingAssetsPath, "sherpa-tts-models");
        var config = new OfflineTtsConfig
        {
            Model = new OfflineTtsModelConfig
            {
                Vits = new OfflineTtsVitsModelConfig
                {
                    Model = Path.Combine(modelDir, "model.onnx"),
                    Lexicon = Path.Combine(modelDir, "lexicon.txt"),
                    Tokens = Path.Combine(modelDir, "tokens.txt"),
                    NoiseScale = 0.667f,
                    NoiseScaleW = 0.8f,
                    LengthScale = 1.0f
                },
                NumThreads = 2,
                Provider = "cpu"
            },
            MaxNumSentences = 1
        };

        tts = new OfflineTts(config);
        Debug.Log("[SherpaSpeechService] Silero TTS (Ирина) инициализирован");
    }

    public void Speak(string text)
    {
        if (string.IsNullOrEmpty(text) || tts == null || isSpeaking)
        {
            sessionManager?.OnTtsFinished();
            return;
        }

        Debug.Log($"[SherpaSpeechService] Озвучка: \"{text}\"");
        isSpeaking = true;

        // Генерация аудиоклипа (sid = 0 – первый голос, speed = 1.0)
        AudioClip clip = tts.Generate(text, 0, 1.0f);

        if (clip != null)
        {
            audioSource.clip = clip;
            audioSource.Play();
            StartCoroutine(WaitForSpeechEnd(clip.length));
        }
        else
        {
            Debug.LogError("[SherpaSpeechService] Не удалось синтезировать аудио");
            isSpeaking = false;
            sessionManager?.OnTtsFinished();
        }
    }

    private IEnumerator WaitForSpeechEnd(float duration)
    {
        yield return new WaitForSeconds(duration);
        isSpeaking = false;
        sessionManager?.OnTtsFinished();
    }
}