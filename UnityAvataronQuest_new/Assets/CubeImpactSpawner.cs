using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeImpactSpawner : MonoBehaviour
{
    public Transform floor;

    public float triggerDistance = 0.05f;

    public Transform groupRoot;

    public Transform cubeBody;
    public Transform innerContent;
    public ResizeHandle[] handles;

    public bool makeKinematicDuringDrop = true;
    public MonoBehaviour[] extraScriptsToDisable;


    public GameObject effectPrefab;
    public float spawnRate = 24f;
    public int maxSpawnsPerFrame = 8;
    public float orbitRadiusStart = 0.2f;
    public float orbitRadiusEnd = 0.6f;
    public float orbitAngularSpeed = 360f;
    public float spawnHeightOffset = 0f;
    public float effectLifetime = 1.5f;

    private bool _started;          
    private bool _interruptedOnce;  
    private Transform _triggerCube;
    private Collider _triggerCubeCollider; 

    void Update()
    {
        if (_started || floor == null) return;

        if (_triggerCube == null)
        {
            _triggerCube = (cubeBody != null) ? cubeBody : transform;
            _triggerCubeCollider = _triggerCube.GetComponentInChildren<Collider>();
        }
        
        if (_triggerCubeCollider == null)
        {
            if (cubeBody != null)
                Debug.LogWarning("[CubeImpactSpawner] cubeBody 또는 그 자식에 Collider가 없어 밑면 감지가 불가능합니다.", cubeBody);
            else
                Debug.LogWarning("[CubeImpactSpawner] 이 오브젝트 또는 자식에 Collider가 없어 밑면 감지가 불가능합니다.", this);
            
            _started = true;
            return;
        }

        float cubeBottomY = _triggerCubeCollider.bounds.min.y;
        float floorY = floor.position.y;
        float distY = cubeBottomY - floorY;

        if (distY <= triggerDistance)
        {
            _started = true;
            StartCoroutine(SnapAndSpawn(floorY));
        }
    }

    private IEnumerator SnapAndSpawn(float floorY)
    {
        var movers = new List<Transform>();
        if (groupRoot) movers.Add(groupRoot);
        else
        {
            if (cubeBody) movers.Add(cubeBody);
            if (innerContent) movers.Add(innerContent);
            if (handles != null) foreach (var h in handles) if (h) movers.Add(h.transform);
        }
        if (movers.Count == 0)
        {
            Debug.LogWarning("[CubeImpactSpawner] 이동/숨길 대상이 없습니다.");
            yield break;
        }

        if (FloorIsInsideMovers(movers))
        {
            Debug.LogError("[CubeImpactSpawner] floor가 groupRoot(또는 movers) 하위에 있습니다. Floor를 그룹 밖으로 빼주세요.");
            yield break;
        }

        InterruptDuringDrop(movers);

        var starts = new Vector3[movers.Count];
        for (int i = 0; i < movers.Count; i++)
            starts[i] = movers[i].position;

        float currentBottomY = _triggerCubeCollider.bounds.min.y;
        float targetBottomY = floorY; 
        float deltaY = targetBottomY - currentBottomY;

        for (int i = 0; i < movers.Count; i++)
        {
            movers[i].position = new Vector3(
                starts[i].x,
                starts[i].y + deltaY, 
                starts[i].z
            );
        }

        float t0 = Time.realtimeSinceStartup;
        float t1 = t0 + Mathf.Max(0.0001f, effectLifetime);
        float hardEnd = t0 + effectLifetime + 2f; 

        float lastTS = t0;
        float spawnAccumulator = 0f;
        float angleDeg = 0f;

        Vector3 centerNow = new Vector3(
            _triggerCube.position.x, 
            _triggerCube.position.y + spawnHeightOffset, 
            _triggerCube.position.z
        );

        while (Time.realtimeSinceStartup < t1 && Time.realtimeSinceStartup < hardEnd)
        {
            float now = Time.realtimeSinceStartup;
            float dt = Mathf.Max(0f, now - lastTS);
            lastTS = now;

            float k = Mathf.InverseLerp(t0, t1, now);
            k = k * k * (3f - 2f * k); 

            
            if (spawnRate > 0f && effectPrefab != null)
            {
                spawnAccumulator += spawnRate * dt;
                angleDeg += orbitAngularSpeed * dt;

                float radius = Mathf.Lerp(orbitRadiusStart, orbitRadiusEnd, k);
                int spawnedThisFrame = 0;

                while (spawnAccumulator >= 1f && spawnedThisFrame < Mathf.Max(1, maxSpawnsPerFrame))
                {
                    spawnAccumulator -= 1f;
                    spawnedThisFrame++;

                    float a = angleDeg + Random.Range(-10f, 10f);
                    float rad = a * Mathf.Deg2Rad;

                    Vector3 dir = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));
                    Vector3 pos = centerNow + dir * radius; 

                    SpawnEffectAt(pos, Vector3.up);
                }
            }

            yield return null;
        }

    }

    private void SpawnEffectAt(Vector3 pos, Vector3 normal)
    {
        if (effectPrefab != null)
        {
            var go = Instantiate(effectPrefab, pos, Quaternion.LookRotation(normal));
            Destroy(go, Mathf.Max(0.1f, effectLifetime));
        }
    }

    private bool FloorIsInsideMovers(List<Transform> movers)
    {
        if (floor == null) return false;
        foreach (var m in movers)
            if (floor.IsChildOf(m)) return true;
        return false;
    }

    private void InterruptDuringDrop(List<Transform> movers)
    {
        if (_interruptedOnce) return;
        _interruptedOnce = true;

        if (extraScriptsToDisable != null)
            foreach (var s in extraScriptsToDisable) if (s) s.enabled = false;

        if (makeKinematicDuringDrop)
        {
            foreach (var m in movers)
            {
                foreach (var rb in m.GetComponentsInChildren<Rigidbody>(true))
                {
                    rb.isKinematic = true;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
        }

    }
}