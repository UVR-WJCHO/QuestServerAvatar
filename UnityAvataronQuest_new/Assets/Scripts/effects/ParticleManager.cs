using UnityEngine;



[System.Serializable]
public struct Season
{
    public SeasonType type;
    public string displayName;

    public Season(SeasonType type, string displayName, Color themeColor)
    {
        this.type = type;
        this.displayName = displayName;
    }

    public override string ToString()
    {
        return displayName;
    }
}

[System.Serializable]
public enum SeasonType
{
    Spring,
    Summer,
    Autumn,
    Winter
}


public class ParticleManager : MonoBehaviour
{
    public Transform avatar;
    public GameObject ceiling1;
    public GameObject ceiling2;

    [System.Serializable]
    public struct SeasonParticles
    {
        public Season season;
        public ParticleSystem particleSystem;
        [HideInInspector]
        public LayerMask originalCollisionMask;
    }

    public SeasonParticles[] seasonalParticles;
    public ParticleHandControl particleHandControl;

    void Awake()
    {
        for (int i = 0; i < seasonalParticles.Length; i++)
        {
            var col = seasonalParticles[i].particleSystem.collision;
            seasonalParticles[i].originalCollisionMask = col.collidesWith;
        }
    }

    public void ParticleSystemTriggered(int season_idx)
    {
        // 파티클 선택 및 재생
        ParticleSystem selectedSystem = null;
        SeasonType season_type = (SeasonType)season_idx;

        foreach (var s in seasonalParticles)
        {
            var temp = s.particleSystem.transform.GetComponent<SeasonCollisionResponder>();

            if (s.season.type == season_type)
            {
                particleHandControl.particleSystem = s.particleSystem;
                selectedSystem = s.particleSystem;
                selectedSystem.Play();
                if (temp.stampParent != null)
                    temp.stampParent.SetActive(true);
                if (temp.groundEffect != null)
                    temp.groundEffect.SetActive(true);
            }
            else
            {
                s.particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                if (temp.stampParent != null)
                    temp.stampParent.SetActive(false);
                if (temp.groundEffect != null)
                    temp.groundEffect.SetActive(false);
                var col = s.particleSystem.collision;
                col.collidesWith = s.originalCollisionMask;
            }
        }

        if (selectedSystem == null) return;

        // 거리 비교
        float distToCeiling1 = Vector3.Distance(avatar.position, ceiling1.transform.position);
        float distToCeiling2 = Vector3.Distance(avatar.position, ceiling2.transform.position);

        GameObject fartherCeiling = distToCeiling1 > distToCeiling2 ? ceiling1 : ceiling2;
        int targetLayer = fartherCeiling.layer;

        // 파티클 충돌 설정
        var collision = selectedSystem.collision;
        collision.enabled = true;
        collision.type = ParticleSystemCollisionType.World;

        // 기존 충돌 대상에 천장 레이어만 추가
        int ceilingLayer = fartherCeiling.layer;
        collision.collidesWith |= (1 << ceilingLayer);

        Debug.Log($"{season_type} 효과 재생 (충돌 레이어: {LayerMask.LayerToName(targetLayer)})");
    }
}
