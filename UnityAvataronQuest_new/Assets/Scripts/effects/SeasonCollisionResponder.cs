using System.Collections.Generic;
using UnityEngine;

public class SeasonCollisionResponder : MonoBehaviour
{
    [Header("Season")]
    [SerializeField] private SeasonType season;

    [Header("Refs")]
    [SerializeField] private ParticleSystem mainPs;

    [Header("Layers")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private LayerMask avatarMask;
    [SerializeField] private LayerMask ceilingMask; // Ceiling_L | Ceiling_R (둘 다 포함)

    [Header("Impact FX (optional)")]
    [SerializeField] private ParticleSystem splashFxPrefab;   // 여름용
    [SerializeField] private ParticleSystem smallHitFxPrefab; // 벚꽃/낙엽/눈이 몸에 닿을 때(선택)
    [SerializeField] public GameObject groundEffect;

    [Header("Accumulation")]
    [SerializeField] private GameObject stampPrefab; // 벚꽃/낙엽용 (Quad/Decal)
    [SerializeField] public GameObject stampParent;
    [SerializeField] private float stampChance = 0.2f;

    [Header("Spawn throttling")]
    [SerializeField] private float minDistanceBetweenSpawns = 0.05f;

    [Header("Ceiling kill (new)")]
    [SerializeField] private float ceilingKillRadius = 0.03f;     // 충돌 지점 근처 파티클 찾는 반경
    [SerializeField] private int ceilingKillMaxMatches = 2;       // 한 이벤트에서 몇 개까지 죽일지(안전장치)
    [SerializeField] private bool killOnlyThisParticleSystem = true;

    private readonly List<ParticleCollisionEvent> events = new();
    private Vector3 lastSpawnPos = new Vector3(999, 999, 999);

    // ceiling kill용 캐시
    private ParticleSystem.Particle[] particleBuffer;
    private readonly Collider[] overlapBuffer = new Collider[16];

    void Reset()
    {
        mainPs = GetComponent<ParticleSystem>();
    }

    void OnParticleCollision(GameObject other)
    {
        if (mainPs == null) return;

        int count = mainPs.GetCollisionEvents(other, events);
        if (count <= 0) return;

        int layerBit = 1 << other.layer;

        bool hitGround = (groundMask.value & layerBit) != 0;
        bool hitAvatar = (avatarMask.value & layerBit) != 0;
        bool hitCeiling = (ceilingMask.value & layerBit) != 0;

        for (int i = 0; i < count; i++)
        {
            Vector3 p = events[i].intersection;

            // 너무 촘촘하면 스폰 생략(과부하/노이즈 방지)
            if (Vector3.Distance(lastSpawnPos, p) < minDistanceBetweenSpawns)
                continue;

            if (hitCeiling)
            {
                //KillParticlesNearPoint(p, ceilingKillRadius, ceilingKillMaxMatches, other);
                continue;
            }

            if (season == SeasonType.Summer)
            {
                // 여름
                if (hitAvatar)
                {
                    // 아바타에 닿으면 항상 splash
                    SpawnImpactFx(splashFxPrefab, p);
                }
                else if (hitGround)
                {
                    // 땅에 닿으면 매우 낮은 확률로 splash
                    if (Random.value <= 1f) // ← 필요에 따라 0.01f ~ 0.1f 조절
                        SpawnImpactFx(splashFxPrefab, p);
                }
            }
            else
            {
                // 봄/가을/겨울
                if (hitGround)
                {
                    if (season == SeasonType.Winter)
                    {
                        // 눈: 여기서 "눈 누적(마스크 페인트)" 호출 추천
                        // SnowAccumulation.Paint(p);
                        if (Random.value <= stampChance)
                            SpawnStamp(p);
                    }
                    else
                    {
                        // 벚꽃/낙엽: 스탬프 누적
                        if (Random.value <= stampChance)
                            SpawnStamp(p);
                    }
                }
                else if (hitAvatar)
                {
                    // 몸에 닿으면 작은 이펙트(선택)
                    SpawnImpactFx(smallHitFxPrefab, p);
                }
            }

            lastSpawnPos = p;
        }
    }

    private void SpawnImpactFx(ParticleSystem fxPrefab, Vector3 pos)
    {
        if (fxPrefab == null) return;
        var fx = Instantiate(fxPrefab, pos, Quaternion.identity);
        fx.Play();
        Destroy(fx.gameObject, fx.main.duration + fx.main.startLifetime.constantMax + 0.5f);
    }

    private void SpawnStamp(Vector3 pos)
    {
        if (stampPrefab == null) return;
        Quaternion rot = Quaternion.Euler(0, Random.Range(0f, 360f), 0f);
        Instantiate(stampPrefab, pos + Vector3.up * 0.001f, rot, stampParent.transform);
    }

    /// <summary>
    /// 충돌 지점 주변의 파티클을 찾아 remainingLifetime=0으로 즉시 제거.
    /// ParticleCollisionEvent로 "그 파티클 1개"에 직접 접근이 안 되므로 위치 근사 매칭 방식.
    /// </summary>
    private void KillParticlesNearPoint(Vector3 worldPoint, float radius, int maxMatches, GameObject collisionOther)
    {
        if (mainPs == null) return;

        // (선택) 이 ParticleSystem이 특정 collider에 닿았을 때만 죽이고 싶다면,
        // mainPs가 실제로 해당 other와 충돌했는지 정도의 체크를 더하고 싶을 수 있음.
        // 지금은 OnParticleCollision이 호출된 other 기준으로만 처리합니다.

        // 캐시/버퍼 준비
        int max = mainPs.main.maxParticles;
        if (particleBuffer == null || particleBuffer.Length < max)
            particleBuffer = new ParticleSystem.Particle[max];

        // 파티클들 가져오기
        int alive = mainPs.GetParticles(particleBuffer);
        if (alive <= 0) return;

        // (옵션) collisionOther 주변 콜라이더들이 실제로 이 파티클 시스템과 연관 있는지 체크할 수 있음
        // 여기서는 단순히 worldPoint 주변 파티클을 죽입니다.

        int killed = 0;
        float r2 = radius * radius;

        // 가장 가까운 파티클부터 죽이도록 1~2개 정도만 처리 (성능/오검출 방지)
        // 단순 루프 + 거리 비교로 충분
        for (int i = 0; i < alive; i++)
        {
            Vector3 particleWorldPos = particleBuffer[i].position;

            // mainPs가 Local simulation이면 position이 local일 수 있으니 변환
            if (mainPs.main.simulationSpace == ParticleSystemSimulationSpace.Local)
                particleWorldPos = mainPs.transform.TransformPoint(particleWorldPos);

            // (선택) custom simulationSpace면 따로 처리 필요하지만 보통 Local/World 사용
            Vector3 d = particleWorldPos - worldPoint;
            if (d.sqrMagnitude <= r2)
            {
                particleBuffer[i].remainingLifetime = 0f;
                killed++;

                if (killed >= maxMatches)
                    break;
            }
        }

        if (killed > 0)
        {
            // 수정한 파티클 반영
            mainPs.SetParticles(particleBuffer, alive);
        }
    }
}
