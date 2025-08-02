using Oculus.Interaction.Input;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using Oculus.Interaction;

public class ParticleHandControl : MonoBehaviour
{
    [SerializeField, Interface(typeof(IHmd))]
    private UnityEngine.Object _hmd;
    private IHmd Hmd { get; set; }

    [SerializeField]
    private ActiveStateSelector[] _poses;

    [SerializeField]
    private ParticleSystem particleSystem;

    [SerializeField]
    private float forceMultiplier = 2f;
    [SerializeField]
    private float smoothing = 0.1f; // 갑작스러운 손 떨림 방지
    [SerializeField]
    private bool isRockPoseActive = false;

    private Vector3 startPos;
    private Vector3 endPos;
    private Vector3 smoothedVelocity;

    protected virtual void Awake()
    {
        Hmd = _hmd as IHmd;
    }

    protected virtual void Start()
    {
        this.AssertField(Hmd, nameof(Hmd));

        for (int i = 0; i < _poses.Length; i++)
        {
            int poseNumber = i;
            _poses[i].WhenSelected += () => PoseSelected(poseNumber);
            _poses[i].WhenUnselected += () => PoseUnselected(poseNumber);
        }

        startPos = Vector3.zero;
        endPos = Vector3.zero;
        smoothedVelocity = Vector3.zero;
    }

    void ApplyForceToParticles(Vector3 force)
    {
        var velocityModule = particleSystem.velocityOverLifetime;
        velocityModule.enabled = true;
        velocityModule.space = ParticleSystemSimulationSpace.World;

        // 초기: 아래로 떨어지다가 → 후반: 손 방향으로
        AnimationCurve curveX = new AnimationCurve();
        curveX.AddKey(0f, 0f);                      // 시작 속도 없음
        curveX.AddKey(1f, force.x);                 // 마지막에 손 방향

        AnimationCurve curveY = new AnimationCurve();
        curveY.AddKey(0f, -2f);                     // 초기에 아래로 낙하
        curveY.AddKey(1f, force.y);                 // 이후 손 방향으로 점점 이동

        AnimationCurve curveZ = new AnimationCurve();
        curveZ.AddKey(0f, 0f);
        curveZ.AddKey(1f, force.z);

        velocityModule.x = new ParticleSystem.MinMaxCurve(1f, curveX);
        velocityModule.y = new ParticleSystem.MinMaxCurve(1f, curveY);
        velocityModule.z = new ParticleSystem.MinMaxCurve(1f, curveZ);
    }


    private void PoseSelected(int poseNumber)
    {
        isRockPoseActive = true;

        var hands = _poses[poseNumber].GetComponents<HandRef>();
        Vector3 handPos = Vector3.zero;
        foreach (var hand in hands)
        {
            hand.GetRootPose(out Pose wristPose);
            Vector3 forward = hand.Handedness == Handedness.Left ? wristPose.right : -wristPose.right;
            handPos += wristPose.position + forward;
        }
        startPos = handPos;
        Debug.Log("starting point");
        Debug.Log(startPos);

    }

    private void PoseUnselected(int poseNumber)
    {
        isRockPoseActive = false;

        var hands = _poses[poseNumber].GetComponents<HandRef>();
        Vector3 handPos = Vector3.zero;
        foreach (var hand in hands)
        {
            hand.GetRootPose(out Pose wristPose);
            Vector3 forward = hand.Handedness == Handedness.Left ? wristPose.right : -wristPose.right;
            handPos += wristPose.position + forward;
        }
        endPos = handPos;
        Debug.Log("ending point");
        Debug.Log(endPos);

        Vector3 rawVelocity = (endPos - startPos) / Time.deltaTime;

        // 스무딩 처리 (움찔 방지)
        smoothedVelocity = Vector3.Lerp(smoothedVelocity, rawVelocity, smoothing);

        ApplyForceToParticles(smoothedVelocity * forceMultiplier);
    }

}
