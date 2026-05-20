using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class ChatMessage
{
    public string role;
    public string content;
}

[System.Serializable]
public class ChatCompletionRequest
{
    public string model;
    public ChatMessage[] messages;
    public int max_tokens;
    public double temperature;
}

[System.Serializable]
public class ChatCompletionResponse
{
    public Choice[] choices;

    [System.Serializable]
    public class Choice
    {
        public ChatMessage message;
    }
}

public class LLMService : MonoBehaviour
{
    [Header("Ollama Settings")]
    [SerializeField] private string apiUrl = "http://localhost:11434/v1/chat/completions";
    [SerializeField] private string modelName = "ilyagusev/saiga_llama3:latest"; 

    private SessionManager sessionManager;

    void Start()
    {
        sessionManager = FindAnyObjectByType<SessionManager>();
        if (sessionManager == null)
            Debug.LogError("[LLMService] SessionManager не найден на сцене!");
    }

    public void GenerateResponse(string userText)
    {
        if (string.IsNullOrWhiteSpace(userText))
        {
            Debug.LogWarning("[LLMService] Пустой запрос, использую заглушку.");
            OnResponseReceived("Хорошо, продолжай читать.");
            return;
        }

        StartCoroutine(SendRequest(userText));
    }

    private IEnumerator SendRequest(string userText)
    {
        Debug.Log($"[LLMService] Отправляю запрос к Ollama (модель: {modelName}): \"{userText}\"");

        var requestObj = new ChatCompletionRequest
        {
            model = modelName,
            messages = new ChatMessage[]
            {
                new ChatMessage { role = "system", content = "Ты — дружелюбный книжный персонаж. Отвечай коротко, 1-2 предложениями, поощряй ребёнка читать дальше." },
                new ChatMessage { role = "user", content = userText }
            },
            max_tokens = 500,
            temperature = 0.7
        };

        string jsonBody = JsonUtility.ToJson(requestObj, false);
        Debug.Log($"[LLMService] JSON запроса: {jsonBody}");

        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 120;

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[LLMService] Ошибка запроса: {request.error}\n{request.downloadHandler.text}");
                OnResponseReceived("Извини, я задумался. Давай попробуем ещё раз.");
                yield break;
            }

            string responseJson = request.downloadHandler.text;
            Debug.Log($"[LLMService] Ответ от Ollama: {responseJson}");

            string reply = ExtractReply(responseJson);
            Debug.Log($"[LLMService] Текст ответа: \"{reply}\"");
            OnResponseReceived(reply);
        }
    }

    private void OnResponseReceived(string responseText)
    {
        sessionManager?.OnLLMResponseReady(responseText);
    }

    private string ExtractReply(string json)
    {
        try
        {
            var response = JsonUtility.FromJson<ChatCompletionResponse>(json);
            if (response?.choices != null && response.choices.Length > 0)
            {
                string content = response.choices[0].message?.content;
                if (!string.IsNullOrWhiteSpace(content))
                    return content.Trim();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LLMService] Не удалось разобрать JSON ответа: {e.Message}");
        }

        return "Давай продолжим чтение!";
    }
}