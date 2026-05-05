using UnityEngine;
using Vosk;

public class VoskService : MonoBehaviour
{
    [Header("Vosk Settings")]
    public string modelPath = "vosk-model-small-ru-0.22";

    private VoskRecognizer recognizer;
    private SessionManager sessionManager;

    void Start()
    {
        sessionManager = FindAnyObjectByType<SessionManager>();
        if (sessionManager == null)
            Debug.LogError("[VoskService] SessionManager не найден на сцене!");

        Vosk.Vosk.SetLogLevel(0);
        string fullPath = System.IO.Path.Combine(Application.streamingAssetsPath, modelPath);
        Debug.Log($"[VoskService] Загружаю модель из: {fullPath}");

        try
        {
            Model model = new Model(fullPath);
            recognizer = new VoskRecognizer(model, 16000.0f);
            Debug.Log("[VoskService] Модель Vosk успешно загружена.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[VoskService] Ошибка загрузки модели: {e.Message}");
        }
    }

    public void TranscribeAudio(AudioClip clip)
    {
        Debug.Log($"[VoskService] TranscribeAudio вызван. Клип: {(clip != null ? clip.samples + " сэмплов" : "null")}");

        if (recognizer == null)
        {
            Debug.LogError("[VoskService] Распознаватель не инициализирован. Возвращаюсь в Listening.");
            sessionManager?.ChangeState(SessionManager.SessionState.Listening);
            return;
        }

        if (clip == null)
        {
            Debug.LogError("[VoskService] AudioClip is null!");
            sessionManager?.ChangeState(SessionManager.SessionState.Listening);
            return;
        }

        Debug.Log("[VoskService] Начинаю распознавание...");

        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        byte[] byteData = new byte[samples.Length * sizeof(short)];
        int byteIndex = 0;
        foreach (float sample in samples)
        {
            short pcmSample = (short)(sample * 32767);
            byteData[byteIndex++] = (byte)(pcmSample & 0xFF);
            byteData[byteIndex++] = (byte)((pcmSample >> 8) & 0xFF);
        }

        Debug.Log($"[VoskService] Данные отправлены в Vosk. Размер массива: {byteData.Length} байт");

        // Распознавание
        string recognizedText = "Распознавание не удалось.";
        bool accepted = recognizer.AcceptWaveform(byteData, byteData.Length);
        Debug.Log($"[VoskService] AcceptWaveform вернул: {accepted}");

        if (accepted)
        {
            recognizedText = JsonUtility.FromJson<VoskResult>(recognizer.Result()).text;
        }
        else
        {
            recognizedText = JsonUtility.FromJson<VoskResult>(recognizer.PartialResult()).partial;
        }

        Debug.Log($"[VoskService] Распознанный текст: \"{recognizedText}\"");
        sessionManager?.OnVoskResult(recognizedText);
    }

    [System.Serializable]
    private class VoskResult
    {
        public string text;
        public string partial;
    }
}