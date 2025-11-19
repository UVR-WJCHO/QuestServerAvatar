using UnityEngine;
using System.Collections;

public class PooledEffect : MonoBehaviour
{
    private System.Action<GameObject> _returnToPool;

    public void Setup(System.Action<GameObject> returnToPool)
    {
        _returnToPool = returnToPool;
    }

    public void PlayAndReturnAfter(float seconds)
    {
        StopAllCoroutines();
        var ps = GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Clear(true);
            ps.Play(true);
        }
        StartCoroutine(ReturnAfter(seconds));
    }

    private IEnumerator ReturnAfter(float sec)
    {
        yield return new WaitForSeconds(sec);
        _returnToPool?.Invoke(gameObject);
    }
}
