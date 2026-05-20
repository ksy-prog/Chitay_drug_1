using SpeechLib; // Подключаем новую библиотеку
using UnityEngine;

public class TTSService : MonoBehaviour
{
    private SpVoice voice;

    void Start()
    {
        voice = new SpVoice();
        // Установите русский голос, если он есть в системе
        try
        {
            // Попытка найти голос с русской культурой
            var voices = voice.GetVoices(string.Empty, string.Empty);
            for (int i = 0; i < voices.Count; i++)
            {
                if (voices.Item(i).GetAttribute("Language") == "419") // Код русского языка
                {
                    voice.Voice = voices.Item(i);
                    Debug.Log($"[TTSService] Выбран русский голос: {voice.Voice.GetAttribute("Name")}");
                    break;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[TTSService] Не удалось установить русский голос: {e.Message}. Будет использован стандартный.");
        }
    }

    public void Speak(string text)
    {
        if (string.IsNullOrEmpty(text) || voice == null) return;
        Debug.Log($"[TTSService] Озвучка: \"{text}\"");
        voice.Speak(text, SpeechVoiceSpeakFlags.SVSFlagsAsync);
    }
}