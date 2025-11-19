using UnityEngine;
using UnityEngine.Pool;
using System.Collections;

public class PopupSpawner : MonoBehaviour
{
    [Header("Popup Object")]
    [SerializeField] private GameObject popupPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float riseHeight = 2f;
    [SerializeField] private float riseTime = 0.6f;
    [SerializeField] private AnimationCurve riseCurve = AnimationCurve.EaseInOut(0,0,1,1);

    [Header("Ring Effects (pooled)")]
    [SerializeField] private GameObject effectPrefab;   
    [SerializeField] private int effectsPerRing = 8;
    [SerializeField] private float ringRadius = 1.2f;
    [SerializeField] private float effectLifetime = 1.0f;
    [SerializeField] private int poolDefaultSize = 16;
    [SerializeField] private int poolMaxSize = 64;

    private GameObject popupInstance;
    private ObjectPool<GameObject> effectPool;
    private bool isPopping = false;

    void Awake()
    {
        effectPool = new ObjectPool<GameObject>(
            createFunc: () => {
                var go = Instantiate(effectPrefab);
                go.SetActive(false);
                var pe = go.GetComponent<PooledEffect>();
                if (pe == null) pe = go.AddComponent<PooledEffect>();
                pe.Setup(ReturnEffectToPool);
                return go;
            },
            actionOnGet: go => go.SetActive(true),
            actionOnRelease: go => go.SetActive(false),
            actionOnDestroy: go => Destroy(go),
            collectionCheck: false,
            defaultCapacity: poolDefaultSize,
            maxSize: poolMaxSize
        );
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !isPopping)
            StartCoroutine(PopRoutine());
    }

    private IEnumerator PopRoutine()
    {
        isPopping = true;

        if (popupInstance == null)
            popupInstance = Instantiate(popupPrefab);

        Vector3 startPos = spawnPoint.position;
        Vector3 endPos   = startPos + Vector3.up * riseHeight;

        popupInstance.transform.position = startPos;
        popupInstance.SetActive(true);

        float t = 0f;
        while (t < riseTime)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / riseTime);
            float k = riseCurve.Evaluate(u);
            popupInstance.transform.position = Vector3.LerpUnclamped(startPos, endPos, k);
            yield return null;
        }
        popupInstance.transform.position = endPos;

        SpawnRingEffects(endPos);

        t = 0f;
        while (t < riseTime * 0.7f)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / (riseTime * 0.7f));
            float k = riseCurve.Evaluate(u);
            popupInstance.transform.position = Vector3.LerpUnclamped(endPos, startPos, k);
            yield return null;
        }
        popupInstance.transform.position = startPos;

        isPopping = false;
    }

    private void SpawnRingEffects(Vector3 center)
    {
        float step = 360f / effectsPerRing;
        for (int i = 0; i < effectsPerRing; i++)
        {
            float angle = step * i * Mathf.Deg2Rad;
            Vector3 pos = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * ringRadius;

            var fx = effectPool.Get();
            fx.transform.position = pos;
            fx.transform.rotation = Quaternion.LookRotation((pos - center).normalized + Vector3.up * 0.1f);

            var pe = fx.GetComponent<PooledEffect>();
            pe.PlayAndReturnAfter(effectLifetime);
        }
    }

    private void ReturnEffectToPool(GameObject go)
    {
        if (go != null)
            effectPool.Release(go);
    }
}
