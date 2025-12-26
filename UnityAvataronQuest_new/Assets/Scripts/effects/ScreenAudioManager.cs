using UnityEngine;

public class ScreenAudioManager : MonoBehaviour
{
    public static ScreenAudioManager Instance { get; private set; }

    private VideoAudioSync _current;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // 필요하면 DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 새 스크린이 재생을 시작하려고 할 때 호출.
    /// 이전에 재생 중이던 스크린이 있으면 멈춘다.
    /// </summary>
    public void RequestPlay(VideoAudioSync requester)
    {
        if (requester == null) return;

        // 이미 같은 스크린이면 그대로
        if (_current == requester) return;

        // 이전 스크린 stop
        if (_current != null)
            _current.StopAudio();

        // 새 스크린을 current로
        _current = requester;
    }

    public void StopCurrent()
    {
        if (_current != null)
        {
            _current.StopAudio();
            _current = null;
        }
    }
}
