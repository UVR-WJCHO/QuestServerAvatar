using UnityEngine;
using System.Collections.Generic;


public class effect_cclock : MonoBehaviour, IParticleEffect
{
    public ParticleSystem particleSystem;
    float spiralSpeed = 2f;
    float fallSpeed = 0.1f;
    float spiralRadius = 0.1f;

    private ParticleSystem.Particle[] particles;
    private Vector3[] spiralOffsets;

    // 그룹별 랜덤 오프셋 저장
    private Dictionary<int, float> groupAngleOffsets = new Dictionary<int, float>();

    void Start()
    {
        if (particleSystem == null)
            particleSystem = GetComponent<ParticleSystem>();

        particles = new ParticleSystem.Particle[particleSystem.main.maxParticles];
        spiralOffsets = new Vector3[particles.Length];
    }

    public void UpdateEffect()
    {
        int count = particleSystem.GetParticles(particles);

        for (int i = 0; i < count; i++)
        {
            float lifePercent = 1f - (particles[i].remainingLifetime / particles[i].startLifetime);
            
            // 그룹 번호 계산 (50개 단위)
            int groupIndex = i / 50;

            // 그룹별 랜덤 값 생성 (없으면 추가)
            if (!groupAngleOffsets.ContainsKey(groupIndex))
            {
                groupAngleOffsets[groupIndex] = Random.Range(0f, 60f);
            }

            float angleOffset = groupAngleOffsets[groupIndex];
            float angle = lifePercent * 360f * spiralSpeed + angleOffset;
            float radius = spiralRadius * (1f - lifePercent);

            Vector3 offset = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                -fallSpeed * Time.deltaTime,
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius
            );

            particles[i].position += offset;
        }

        particleSystem.SetParticles(particles, count);
    }
}
