using UnityEngine;

public class SilenceDetector : MonoBehaviour
{
    private MicrophoneService microphoneService;
    private SessionManager sessionManager;

    [Header("Настройки громкости")]
    [Tooltip("Порог громкости, ниже которого считаем тишиной")]
    public float volumeThreshold = 0.01f;

    [Header("Тайминги пауз")]
    [Tooltip("Время тишины для мягкой подсказки (в Listening)")]
    public float pauseHelpDelay = 5f;
    [Tooltip("Время тишины для остановки записи (в Recording)")]
    public float recordingSilenceDelay = 7f;

    private float silenceTimer = 0f;       
    private bool wasSpeechDetected = false; 

    void Start()
    {
        microphoneService = FindAnyObjectByType<MicrophoneService>();
        sessionManager = FindAnyObjectByType<SessionManager>();

        if (microphoneService == null)
            Debug.LogError("[SilenceDetector] MicrophoneService не найден!");
        if (sessionManager == null)
            Debug.LogError("[SilenceDetector] SessionManager не найден!");
    }
    
    void Update()
    {
        if (microphoneService == null || !microphoneService.IsRecording)
            return;

        AudioClip clip = microphoneService.GetAudioClip();
        if (clip == null)
            return;

        float currentVolume = CalculateAverageVolume(clip);

        if (Time.frameCount % 30 == 0)
            Debug.Log($"[SilenceDetector] Громкость: {currentVolume:F5} | Тишина: {silenceTimer:F1}с | Речь была: {wasSpeechDetected} | Состояние: {sessionManager?.CurrentState}");

        if (currentVolume > volumeThreshold)
        {
            silenceTimer = 0f;
            if (!wasSpeechDetected)
            {
                wasSpeechDetected = true;
                Debug.Log("[SilenceDetector] Обнаружена речь!");
                if (sessionManager != null && sessionManager.CurrentState == SessionManager.SessionState.Listening)
                {
                    sessionManager.ChangeState(SessionManager.SessionState.Recording);
                }
            }
        }
        else
        {
            silenceTimer += Time.deltaTime;

            if (wasSpeechDetected && silenceTimer >= recordingSilenceDelay)
            {
                Debug.Log($"[SilenceDetector] Тишина {silenceTimer:F1}с достигнута после речи. Останавливаю запись и перехожу в Processing.");
                microphoneService.StopRecording();
                if (sessionManager != null && sessionManager.CurrentState == SessionManager.SessionState.Recording)
                {
                    sessionManager.ChangeState(SessionManager.SessionState.Processing);
                }
                wasSpeechDetected = false;
                silenceTimer = 0f;
            }
            else if (!wasSpeechDetected && silenceTimer >= pauseHelpDelay)
            {
                Debug.Log($"[SilenceDetector] Тишина {silenceTimer:F1}с, вызываю PausedHelp.");
                if (sessionManager != null && sessionManager.CurrentState == SessionManager.SessionState.Listening)
                {
                    sessionManager.ChangeState(SessionManager.SessionState.PausedHelp);
                    silenceTimer = 0f;
                }
            }
        }
    }

    private float CalculateAverageVolume(AudioClip clip)
    {
        if (clip == null || microphoneService == null)
            return 0f;

        int micPosition = Microphone.GetPosition(microphoneService.GetDeviceName());
        if (micPosition <= 0)
            return 0f;

        int channels = clip.channels;
        int neededFrames = 1024;
        int totalSamplesNeeded = neededFrames * channels;

        int startFrame = micPosition - neededFrames;
        if (startFrame < 0)
            startFrame += clip.samples / channels;
        int startSample = startFrame * channels;

        if (startSample + totalSamplesNeeded > clip.samples)
        {
            totalSamplesNeeded = clip.samples - startSample;
            if (totalSamplesNeeded <= 0) return 0f;
        }

        float[] samples = new float[totalSamplesNeeded];
        clip.GetData(samples, startSample);

        float sum = 0f;
        for (int i = 0; i < samples.Length; i++)
            sum += Mathf.Abs(samples[i]);

        return sum / samples.Length;
    }
}