using System.Collections.Generic;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using UnityEngine;
using UnityEngine.Assertions;

public class ParticleHandControl : MonoBehaviour
{
    [Header("Pose Selectors")]
    [SerializeField] private ActiveStateSelector[] _poses;

    [Header("Particle System")]
    public ParticleSystem particleSystem;

    [Header("Gesture Settings")]
    [Tooltip("흔들림 노이즈로 간주할 최소 이동 거리 (xz 평면)")]
    [SerializeField] private float pointThreshold = 0.02f;

    [Tooltip("원/반원 제스처 인식 최소 포인트 개수")]
    [SerializeField] private int minGesturePoints = 10;

    [Tooltip("반원(π 라디안) 이상을 원형으로 간주하는 각도 임계치")]
    [SerializeField] private float circleAngleThreshold = 5;

    [Tooltip("파티클 궤도 회전 속도 크기")]
    [SerializeField] private float orbitalSpeed = 0.3f;

    // 내부 상태
    private bool isRockPoseActive;
    private int currentPoseIndex = -1;
    private Vector2 startPos2D;
    private readonly List<Vector2> gesturePositions = new List<Vector2>();

    void Start()
    {
        // ActiveStateSelector 이벤트 연결
        for (int i = 0; i < _poses.Length; i++)
        {
            int idx = i;
            _poses[i].WhenSelected += () => PoseSelected(idx);
            _poses[i].WhenUnselected += () => PoseUnselected(idx);
        }
    }

    void Update()
    {
        if (!isRockPoseActive || currentPoseIndex < 0)
            return;

        // Rock 포즈 유지 중일 때: 손 위치 수집 (xz 평면)
        var handRefs = _poses[currentPoseIndex].GetComponents<HandRef>();
        if (handRefs.Length == 0) return;

        handRefs[0].GetRootPose(out Pose w);
        Vector2 pos2D = new Vector2(w.position.x, w.position.z);

        // 거리 기준으로 노이즈 필터링 후 추가
        if (gesturePositions.Count == 0 ||
            Vector2.Distance(gesturePositions[^1], pos2D) > pointThreshold)
        {
            gesturePositions.Add(pos2D);
            if (gesturePositions.Count == 1)
                startPos2D = pos2D;
        }
    }

    private void PoseSelected(int poseNumber)
    {
        currentPoseIndex = poseNumber;
        isRockPoseActive = true;
        gesturePositions.Clear();
    }

    private void PoseUnselected(int poseNumber)
    {
        isRockPoseActive = false;

        bool isCircle = false;
        float signedAngle = 0f;
        if (gesturePositions.Count >= minGesturePoints)
        {
            signedAngle = -CalculateSignedTotalAngle(gesturePositions);
            isCircle = Mathf.Abs(signedAngle) >= circleAngleThreshold;
        }

        if (isCircle)
        {
            // 원형 제스처: linear velocity 초기화 후 orbital만 설정
            ClearDirectionalForce();
            ApplyOrbital(Mathf.Sign(signedAngle) * orbitalSpeed);
        }
        else
        {
            // 방향 벡터 계산
            Vector2 endPos2D = gesturePositions.Count > 0
                ? gesturePositions[^1]
                : startPos2D;
            Vector2 dir = (endPos2D - startPos2D).normalized;

            // orbital 초기화
            ClearOrbital();

            float speedMagMin = 1f;
            float speedMagMax = 1.5f;

            Vector3 forceMin = new Vector3(
                dir.x * speedMagMin,
                -1.5f,
                dir.y * speedMagMin
            );
            Vector3 forceMax = new Vector3(
                dir.x * speedMagMax,
                -1f,
                dir.y * speedMagMax
            );

            ApplyDirectionalForce(forceMin, forceMax);
        }

        gesturePositions.Clear();
        currentPoseIndex = -1;
    }

    /// <summary>
    /// RandomBetweenTwoConstants 모드로 x/y/z 속도 설정
    /// </summary>
    private void ApplyDirectionalForce(Vector3 forceMin, Vector3 forceMax)
    //    float minX, float maxX,
    //   float minY, float maxY,
    //    float minZ, float maxZ)
    {
        if (particleSystem == null) return;

        var vel = particleSystem.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;

        // 랜덤 범위 설정 (MinMaxCurve(min, max) → RandomBetweenConstants)
        vel.x = new ParticleSystem.MinMaxCurve(forceMin.x, forceMax.x);
        vel.y = new ParticleSystem.MinMaxCurve(forceMin.y, forceMax.y);
        vel.z = new ParticleSystem.MinMaxCurve(forceMin.z, forceMax.z);
    }

    /// <summary>
    /// linear velocity (x/z) 모두 0으로 초기화
    /// </summary>
    private void ClearDirectionalForce()
    {
        if (particleSystem == null) return;

        var vel = particleSystem.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;

        vel.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
    }

    /// <summary>
    /// orbitalY 속도 적용
    /// </summary>
    private void ApplyOrbital(float speed)
    {
        if (particleSystem == null) return;

        var vel = particleSystem.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;

        vel.orbitalY = new ParticleSystem.MinMaxCurve(speed);
    }

    /// <summary>
    /// orbital 설정 초기화 (0으로)
    /// </summary>
    private void ClearOrbital()
    {
        if (particleSystem == null) return;

        var vel = particleSystem.velocityOverLifetime;
        vel.enabled = true;
        vel.orbitalY = new ParticleSystem.MinMaxCurve(0f);
    }

    /// <summary>
    /// gesturePositions를 중심으로 한 signed 총 회전 각도 (라디안) 계산
    /// </summary>
    private float CalculateSignedTotalAngle(List<Vector2> pts)
    {
        int n = pts.Count;
        Vector2 center = Vector2.zero;
        foreach (var p in pts) center += p;
        center /= n;

        float total = 0f;
        for (int i = 1; i < n; i++)
        {
            Vector2 v1 = (pts[i - 1] - center).normalized;
            Vector2 v2 = (pts[i] - center).normalized;
            float deg = Vector2.SignedAngle(v1, v2);
            total += deg * Mathf.Deg2Rad;
        }
        return total;
    }
}
