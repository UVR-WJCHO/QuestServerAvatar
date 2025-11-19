using System.Collections.Generic;
using UnityEngine;

public class EffectPool : MonoBehaviour
{
    [Header("Pool")]
    public GameObject prefab;           // 풀링할 이펙트 프리팹
    public int prewarmCount = 10;       // 시작 시 미리 생성 개수
    public bool autoExpand = true;      // 부족하면 자동 생성

    private readonly Queue<GameObject> _pool = new Queue<GameObject>();

    void Awake()
    {
        if (prefab == null)
        {
            Debug.LogError("[EffectPool] Prefab is null");
            return;
        }

        for (int i = 0; i < Mathf.Max(0, prewarmCount); i++)
        {
            var go = CreateNew();
            go.SetActive(false);
            _pool.Enqueue(go);
        }
    }

    private GameObject CreateNew()
    {
        var go = Instantiate(prefab, transform);
        return go;
    }

    public GameObject Get()
    {
        if (_pool.Count > 0)
        {
            var go = _pool.Dequeue();
            go.SetActive(true);
            return go;
        }
        if (autoExpand)
        {
            var go = CreateNew();
            go.SetActive(true);
            return go;
        }
        return null;
    }

    public void Return(GameObject go)
    {
        if (go == null) return;
        go.SetActive(false);
        go.transform.SetParent(transform);
        _pool.Enqueue(go);
    }
}
