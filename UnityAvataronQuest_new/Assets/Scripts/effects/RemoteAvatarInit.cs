using System.Collections;
using UnityEngine;


// 디폴트 세팅용 임시 스크립트

public class RemoteAvatarInit : MonoBehaviour
{
    [Header("Delay (seconds)")]
    [SerializeField] private float delayMin = 1f;
    [SerializeField] private float delayMax = 3f;
    [SerializeField] private bool randomDelay = true;

    [Header("Optional: Remote Avatar")]
    [SerializeField] private GameObject remoteAvatarPrefab;
    [SerializeField] private Transform remoteAvatarSpawn;
    [SerializeField] private bool spawnRemoteAvatarOnStart = true;

    [Header("Video Trigger")]
    [SerializeField] private PinchVideoControllerOVR pinchVideoController;
    [SerializeField] private GameObject screen_B;

    [Header("Particle Trigger")]
    [SerializeField] private ParticleManager particleManager;
    [SerializeField] private int particleIndex = 1;

    [Header("Water Trigger")]
    [SerializeField] private PartitionedWaterManager waterManager;
    [SerializeField] private string waterPlayerId = "0"; // ← players[i].id 와 동일해야 함

    [Header("Debug")]
    [SerializeField] private bool log = true;

    private void Start()
    {
        if (spawnRemoteAvatarOnStart && remoteAvatarPrefab != null)
            SpawnRemoteAvatar();

        StartCoroutine(TriggerRoutine());
    }

    private void SpawnRemoteAvatar()
    {
        if (remoteAvatarSpawn != null)
        {
            Instantiate(
                remoteAvatarPrefab,
                remoteAvatarSpawn.position,
                remoteAvatarSpawn.rotation
            );
        }
        else
        {
            Instantiate(remoteAvatarPrefab);
        }

        if (log)
            Debug.Log("[RemoteAvatarInit] Remote avatar spawned.");
    }

    private IEnumerator TriggerRoutine()
    {
        float delay = randomDelay
            ? Random.Range(delayMin, delayMax)
            : delayMin;

        if (log)
            Debug.Log($"[RemoteAvatarInit] Waiting {delay:0.00}s before demo trigger.");

        yield return new WaitForSeconds(delay);

        TriggerVideo();
        TriggerParticle();
        TriggerWater();
    }

    private void TriggerVideo()
    {
        if (pinchVideoController == null || screen_B == null)
        {
            Debug.LogWarning("[RemoteAvatarInit] Video trigger skipped.");
            return;
        }

        pinchVideoController.PlayTargetVideoAndStopOthers(screen_B);

        if (log)
            Debug.Log("[RemoteAvatarInit] Video triggered.");
    }

    private void TriggerParticle()
    {
        if (particleManager == null)
        {
            Debug.LogWarning("[RemoteAvatarInit] Particle trigger skipped.");
            return;
        }

        particleManager.ParticleSystemTriggeredInit(particleIndex);

        if (log)
            Debug.Log($"[RemoteAvatarInit] Particle triggered (index={particleIndex}).");
    }

    private void TriggerWater()
    {
        if (waterManager == null)
        {
            Debug.LogWarning("[RemoteAvatarInit] Water trigger skipped.");
            return;
        }

        waterManager.SetWaterOn(waterPlayerId);

        if (log)
            Debug.Log($"[RemoteAvatarInit] Water ON for playerId={waterPlayerId}");
    }

    /// <summary>
    /// 데모 중에 물 끄고 싶을 때 수동 호출용
    /// </summary>
    public void TurnWaterOff()
    {
        if (waterManager == null) return;

        waterManager.SetWaterOff(waterPlayerId);

        if (log)
            Debug.Log($"[RemoteAvatarInit] Water OFF for playerId={waterPlayerId}");
    }
}
