using UnityEngine;
using System.Collections;

public class MicrophoneService : MonoBehaviour
{
    private string selectedDevice;         
    private AudioClip recordedClip;        
    private bool isRecording = false;     

    private const int RECORDING_LENGTH_SEC = 10;   
    private const int SAMPLE_RATE = 16000;         

    public bool IsRecording => isRecording;

    public void InitializeMicrophone()
    {
        if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            Debug.Log("[MicrophoneService] Запрашиваю разрешение на микрофон...");
            StartCoroutine(RequestMicrophonePermission());
            return;
        }

        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("[MicrophoneService] Микрофоны не найдены!");
            return;
        }

        selectedDevice = Microphone.devices[0];
        Debug.Log($"[MicrophoneService] Выбран микрофон: {selectedDevice}");
    }
    
    public string GetDeviceName()
    {
        return selectedDevice;
    }

    private IEnumerator RequestMicrophonePermission()
    {
        yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);

        if (Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            Debug.Log("[MicrophoneService] Разрешение получено, инициализирую...");
            if (Microphone.devices.Length > 0)
            {
                selectedDevice = Microphone.devices[0];
                Debug.Log($"[MicrophoneService] Выбран микрофон: {selectedDevice}");
            }
            else
            {
                Debug.LogError("[MicrophoneService] Микрофоны не найдены!");
            }
        }
        else
        {
            Debug.LogError("[MicrophoneService] Пользователь отклонил запрос на доступ к микрофону!");
        }
    }

    public void StartRecording()
    {
        if (selectedDevice == null)
        {
            Debug.LogError("[MicrophoneService] Микрофон не инициализирован!");
            return;
        }

        if (isRecording)
        {
            Debug.LogWarning("[MicrophoneService] Запись уже идёт.");
            return;
        }

        recordedClip = Microphone.Start(selectedDevice, false, RECORDING_LENGTH_SEC, SAMPLE_RATE);
        isRecording = true;
        Debug.Log("[MicrophoneService] Запись начата.");
    }

    public void StopRecording()
    {
        if (!isRecording)
            return;

        Microphone.End(selectedDevice);
        isRecording = false;
        Debug.Log("[MicrophoneService] Запись остановлена.");
    }

    public AudioClip GetAudioClip()
    {
        return recordedClip;
    }

    public void ClearClip()
    {
        recordedClip = null;
    }
}