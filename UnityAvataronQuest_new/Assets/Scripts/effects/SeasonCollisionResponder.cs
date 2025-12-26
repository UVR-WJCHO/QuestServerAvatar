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
    [SerializeField] private LayerMask ceilingMask; // Ceiling_L | Ceiling_R (µÑ ´Ù Æ÷ÇÔ)

    [Header("Impact FX (optional)")]
    [SerializeField] private ParticleSystem splashFxPrefab;   // ¿©¸§¿ë
    [SerializeField] private ParticleSystem smallHitFxPrefab; // º¢²É/³«¿±/´«ÀÌ ¸ö¿¡ ´êÀ» ¶§(¼±ÅÃ)
    [SerializeField] public GameObject groundEffect;

    [Header("Accumulation")]
    [SerializeField] private GameObject stampPrefab; // º¢²É/³«¿±¿ë (Quad/Decal)
    [SerializeField] public GameObject stampParent;
    [SerializeField] private float stampChance = 0.2f;

    [Header("Spawn throttling")]
    [SerializeField] private float minDistanceBetweenSpawns = 0.05f;

    private readonly List<ParticleCollisionEvent> events = new();
    private Vector3 lastSpawnPos = new Vector3(999, 999, 999);

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

            // ³Ê¹« ÃÎÃÎÇÏ¸é ½ºÆù »ý·«(°úºÎÇÏ/³ëÀÌÁî ¹æÁö)
            if (Vector3.Distance(lastSpawnPos, p) < minDistanceBetweenSpawns)
                continue;

            if (hitCeiling)
            {
                // º® ³Ê¸Ó Â÷´Ü: º°µµ FX ¾øÀÌ Á¦°Å´Â collision.lifetimeLoss=1·Î Ã³¸®µÊ
                continue;
            }

            if (season == SeasonType.Summer)
            {
                // ºÐ¼ö: ¾îµð¿¡ ´êµç splash
                SpawnImpactFx(splashFxPrefab, p);
            }
            else
            {
                // º½/°¡À»/°Ü¿ï
                if (hitGround)
                {
                    if (season == SeasonType.Winter)
                    {
                        // ´«: ¿©±â¼­ "´« ´©Àû(¸¶½ºÅ© ÆäÀÎÆ®)" È£Ãâ ÃßÃµ
                        // SnowAccumulation.Paint(p);
                        if (Random.value <= stampChance)
                            SpawnStamp(p);
                    }
                    else
                    {
                        // º¢²É/³«¿±: ½ºÅÆÇÁ ´©Àû
                        if (Random.value <= stampChance)
                            SpawnStamp(p);
                    }
                }
                else if (hitAvatar)
                {
                    // ¸ö¿¡ ´êÀ¸¸é ÀÛÀº ÀÌÆåÆ®(¼±ÅÃ)
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
}
