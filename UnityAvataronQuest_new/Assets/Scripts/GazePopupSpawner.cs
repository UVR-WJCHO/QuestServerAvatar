using UnityEngine;
using System.Collections;

public class GazePopupSpawner : MonoBehaviour
{
    public GameObject popupPrefab;
    public float behindOffset = 0.3f;
    public float riseDistance = 0.25f;
    public float appearTime = 0.4f;
    public float finalScale = 0.3f;
    private bool hasSpawned = false;

    public void SpawnFromBehind(Transform viewer)
    {
        if (hasSpawned || !popupPrefab) return;
        hasSpawned = true;

        Vector3 forwardDir = transform.forward;


        Vector3 start = transform.position + (forwardDir * behindOffset);

        Vector3 end = transform.position - (forwardDir * riseDistance);

        var go = Instantiate(popupPrefab, start, transform.rotation);
        go.transform.localScale = Vector3.zero;

        StartCoroutine(AnimateIn(go.transform, start, end, appearTime));
    }

    IEnumerator AnimateIn(Transform t, Vector3 a, Vector3 b, float dur)
    {
        float e = 0f;
        while (e < dur)
        {
            if (t == null) yield break;

            e += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, e / dur);
            t.position = Vector3.Lerp(a, b, k);
            t.localScale = Vector3.one * (k*finalScale);
            yield return null;
        }
    }
}