using UnityEngine;
using System.Collections;

public class GazePopupSpawner : MonoBehaviour
{
    public GameObject popupPrefab;
    public float behindOffset = 0.3f;     
    public float riseDistance = 0.25f;    
    public float appearTime = 0.4f;      

    public void SpawnFromBehind(Transform viewer)
    {
        if (!popupPrefab) return;

        Vector3 start = transform.position - viewer.forward * behindOffset;
        var go = Instantiate(popupPrefab, start, Quaternion.identity);

        Vector3 end = transform.position + transform.up * riseDistance;

        go.transform.localScale = Vector3.zero;
        StartCoroutine(AnimateIn(go.transform, start, end, appearTime));
    }

    IEnumerator AnimateIn(Transform t, Vector3 a, Vector3 b, float dur)
    {
        float e = 0f;
        while (e < dur)
        {
            e += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, e / dur);
            t.position = Vector3.Lerp(a, b, k);
            t.localScale = Vector3.one * k;
            yield return null;
        }
    }
}
