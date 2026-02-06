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
    public Transform localAvatar;
    public Transform remoteAvatar;
    private Transform targetAvatar;
    public GameObject ceiling1;
    public GameObject ceiling2;
    public GameObject wall;

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

    // 디폴트 세팅 용 임시 함수
    public void ParticleSystemTriggeredInit(int season_idx)
    {
        targetAvatar = remoteAvatar;

        ParticleSystemTriggered(season_idx);

        targetAvatar = localAvatar;
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

                selectedSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); // 방출 중지 + 전부 삭제
                selectedSystem.Clear(true);                                                 // (중복이지만 안전)
                selectedSystem.Play(true);                                                  // 0에서 시작
                selectedSystem.Play();
                if (temp.stampParent != null)
                    temp.stampParent.SetActive(true);
                if (temp.groundEffect != null)
                    temp.groundEffect.SetActive(true);
            }
            else
            {
                //s.particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
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
        float distToCeiling1 = Vector3.Distance(targetAvatar.position, ceiling1.transform.position);
        float distToCeiling2 = Vector3.Distance(targetAvatar.position, ceiling2.transform.position);

        GameObject fartherCeiling = distToCeiling1 > distToCeiling2 ? ceiling1 : ceiling2;
        int targetCeilLayer = fartherCeiling.layer;
        int targetWallLayer = wall.layer;

        // 파티클 충돌 설정
        var collision = selectedSystem.collision;
        collision.enabled = true;
        collision.type = ParticleSystemCollisionType.World;

        // 기존 충돌 대상에 천장 레이어만 추가
        collision.collidesWith |= (1 << targetCeilLayer);
        collision.collidesWith |= (1 << targetWallLayer);

        Debug.Log($"{season_type} 효과 재생 (충돌 레이어: {LayerMask.LayerToName(targetCeilLayer)})");
    }
}
