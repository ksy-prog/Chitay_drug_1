using UnityEngine;
using System.Collections;
public class SessionManager : MonoBehaviour
{
    public enum SessionState
    {
        Listening,
        Recording,
        Processing,
        Speaking,
        PausedHelp
    }

    [SerializeField] private SessionState currentState = SessionState.Listening;

    public SessionState CurrentState => currentState;

    [Header("Services & Systems")]
    public MicrophoneService microphoneService;   
    public SilenceDetector silenceDetector;
    //public CharacterVisualController visualController;
    
    [Header("LLM & TTS")]
    public LLMService llmService;
    public TTSService ttsService; 

    public void ChangeState(SessionState newState)
    {
        if (!IsTransitionAllowed(currentState, newState))
        {
            Debug.LogWarning($"[SessionManager] Переход {currentState} -> {newState} запрещён.");
            return;
        }

        SessionState oldState = currentState;
        currentState = newState;
        Debug.Log($"[SessionManager] Состояние: {oldState} -> {newState}");

        OnStateEnter(newState);
    }

    private bool IsTransitionAllowed(SessionState from, SessionState to)
    {
        switch (from)
        {
            case SessionState.Listening:
                return to == SessionState.Recording || to == SessionState.PausedHelp;
            case SessionState.Recording:
                return to == SessionState.Processing;
            case SessionState.Processing:
                return to == SessionState.Speaking || to == SessionState.Listening; 
            case SessionState.Speaking:
                return to == SessionState.Listening;
            case SessionState.PausedHelp:
                return to == SessionState.Listening;
            default:
                return false;
        }
    }

    private void OnStateEnter(SessionState state)
    {
        switch (state)
        {
            case SessionState.Listening:
                Debug.Log("-> Слушаю пользователя...");
                if (microphoneService != null && !microphoneService.IsRecording)
                {
                    microphoneService.InitializeMicrophone();
                    microphoneService.StartRecording();
                }
                break;

            case SessionState.Recording:
                Debug.Log("-> Запись речи...");
                break;

            case SessionState.Processing:
                Debug.Log("-> Обработка аудио...");
                AudioClip clip = microphoneService?.GetAudioClip();
                Debug.Log($"[SessionManager] Размер клипа: {clip?.samples ?? 0} сэмплов");
                GetComponent<VoskService>()?.TranscribeAudio(clip);
                break;

            case SessionState.Speaking:
                Debug.Log("-> Озвучка ответа...");
                break;

            case SessionState.PausedHelp:
                Debug.Log("-> Пауза, будет подсказка.");
                pausedHelpTimer = 0f;   
                break;


        }
    }

    private float pausedHelpTimer = 0f;
    private const float pausedHelpDuration = 2f; 

    void Update()
    {
        if (currentState == SessionState.PausedHelp)
        {
            pausedHelpTimer += Time.deltaTime;
            if (pausedHelpTimer >= pausedHelpDuration)
            {
                pausedHelpTimer = 0f;
                ChangeState(SessionState.Listening);
            }
        }
    }

    void Start()
    {
        currentState = SessionState.Listening;
        Debug.Log("[SessionManager] Старт. Состояние: Listening");

        OnStateEnter(SessionState.Listening);
    }
    
    public void OnVoskResult(string recognizedText)
    {
        Debug.Log($"[SessionManager] Получен текст от Vosk: \"{recognizedText}\"");
        if (llmService != null)
        {
            llmService.GenerateResponse(recognizedText);
        }
        else
        {
            Debug.LogError("[SessionManager] LLMService не подключён! Возврат в Listening.");
            ChangeState(SessionState.Listening);
        }
    }
    
    public void OnLLMResponseReady(string responseText)
    {
        Debug.Log($"[SessionManager] Ответ LLM готов: \"{responseText}\"");
        ChangeState(SessionState.Speaking);
        if (ttsService != null)
        {
            ttsService.Speak(responseText);
        }
        else
        {
            Debug.LogWarning("[SessionManager] TTSService не подключён, озвучки не будет.");
        }
        StartCoroutine(ReturnToListeningAfterSpeech(responseText));
    }

    private IEnumerator ReturnToListeningAfterSpeech(string text)
    {
        float estimatedDuration = Mathf.Max(2f, text.Length * 0.1f);
        yield return new WaitForSeconds(estimatedDuration);
        ChangeState(SessionState.Listening);
    }
}
