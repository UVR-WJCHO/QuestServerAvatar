using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class VideoTimeResponse
{
    public float t;
    public float updated_at;
    public string screenId;
    public bool hasData;
}

public class VideoAudioSync : MonoBehaviour
{
    [SerializeField] private string centralServerBase = "http://CENTRAL_SERVER_IP:8000";
    [SerializeField] private string screenId = "A";
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float extraOffsetSeconds = 0.0f;

    private Coroutine _co;

    public void TriggerSyncAudio()
    {
        // 새로 트리거된 스크린이 우선권을 가지도록 요청
        if (ScreenAudioManager.Instance != null)
            ScreenAudioManager.Instance.RequestPlay(this);

        // 기존 코루틴이 있으면 중단(연타 대응)
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(CoSyncAndPlay());
    }

    public void StopAudio()
    {
        if (_co != null)
        {
            StopCoroutine(_co);
            _co = null;
        }

        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    private IEnumerator CoSyncAndPlay()
    {
        string url = $"{centralServerBase}/video/time?screenId={UnityWebRequest.EscapeURL(screenId)}";

        using var req = UnityWebRequest.Get(url);
        req.timeout = 2;
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"Time fetch failed ({screenId}): {req.error}");
            yield break;
        }

        VideoTimeResponse data;
        try { data = JsonUtility.FromJson<VideoTimeResponse>(req.downloadHandler.text); }
        catch (Exception e)
        {
            Debug.LogWarning($"JSON parse failed ({screenId}): {e}");
            yield break;
        }

        if (!data.hasData)
        {
            Debug.LogWarning($"No time data yet for screenId={screenId}");
            yield break;
        }

        float t = Mathf.Max(0f, data.t + extraOffsetSeconds);

        if (audioSource.clip != null)
            t = Mathf.Min(t, Mathf.Max(0f, audioSource.clip.length - 0.02f));

        audioSource.time = t;
        audioSource.Play();
    }
}
