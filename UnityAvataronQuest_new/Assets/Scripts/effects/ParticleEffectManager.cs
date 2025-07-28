using UnityEngine;

public class ParticleEffectManager : MonoBehaviour
{
    public ParticleSystem[] particleSystems;  // 다양한 파티클 시스템 프리셋
    public KeyCode[] effectKeys;              // 각각의 효과에 매칭될 키

    private IParticleEffect[] effects;        // 각 파티클에 대응되는 커스텀 효과 클래스
    private int currentEffectIndex = -1;


    void Start()
    {
        // 각 파티클 시스템에 대응되는 효과 컴포넌트 준비
        effects = new IParticleEffect[particleSystems.Length];

        for (int i = 0; i < particleSystems.Length; i++)
        {
            effects[i] = particleSystems[i].GetComponent<IParticleEffect>();
        }

        foreach (var ps in particleSystems)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

    }

    void Update()
    {
        for (int i = 0; i < effectKeys.Length; i++)
        {
            if (Input.GetKeyDown(effectKeys[i]))
            {
                // 이전 시스템은 더 이상 emit 하지 않음 (기존 파티클은 그대로 떨어짐)
                if (currentEffectIndex != -1 && currentEffectIndex != i)
                {
                    particleSystems[currentEffectIndex].Stop(false, ParticleSystemStopBehavior.StopEmitting);
                }

                // 새 시스템 재생
                particleSystems[i].Play();
                currentEffectIndex = i;
            }
        }
        // 현재 동작 중인 시스템에 대해 효과 업데이트
        if (currentEffectIndex != -1 && effects[currentEffectIndex] != null)
        {
            effects[currentEffectIndex].UpdateEffect();
        }

    }
}