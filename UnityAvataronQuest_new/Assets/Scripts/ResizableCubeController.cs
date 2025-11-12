using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;

public class ResizableCubeController_Oculus : MonoBehaviour
{
    [Header("References")]
    public Transform cubeBody;                  
    public Grabbable cubeGrabbable;             
    public HandGrabInteractable cubeHandGrab;   
    public Transform innerContent;              
    public ResizeHandle[] handles;              
    public Grabbable[] handleGrabbables;        

    public Vector3 innerPadding = new Vector3(0.02f, 0.02f, 0.02f);
    public Vector3 minSize = new Vector3(0.1f, 0.1f, 0.1f);
    public Vector3 maxSize = new Vector3(5f, 5f, 5f);
    public BoxCollider cubeBoxCollider;
    public Transform handlesParent;
    public bool zeroHandleVelocityOnSnap = true;
    public bool disableScaleTransformersDuringGrab = true;

    private Transform _root;
    private Renderer _cubeRenderer; 
    private int _activeHandleIdx = -1;      
    private Vector3 _lastCenterW, _lastSizeW; 
    private Quaternion _lastRotW;

    private bool _wasCubeGrabbed = false;
    private Vector3 _lockedLocalScale;     

    private readonly List<Behaviour> _disabledScaleBehaviours = new();

    void Awake()
    {
        _root = transform;

        if (!cubeBody) Debug.LogError("[Resizable] cubeBody not set");
        _cubeRenderer = cubeBody ? cubeBody.GetComponentInChildren<Renderer>() : null;

        if (handles == null || handles.Length != 8)
            Debug.LogWarning("[Resizable] handles should contain exactly 8 items (cornerIndex 0..7).");
        if (handleGrabbables == null || handleGrabbables.Length != handles?.Length)
            Debug.LogWarning("[Resizable] handleGrabbables length must match handles length.");

        if (handlesParent == null) handlesParent = _root;
        ReparentHandlesIfUnderCube(); 
    }

    void OnEnable()
    {
        Application.onBeforeRender += OnBeforeRenderScaleLock;
    }

    void OnDisable()
    {
        Application.onBeforeRender -= OnBeforeRenderScaleLock;
    }

    void Start()
    {
        RepositionAllHandlesFromCube(-1);
        FitInnerContent();
        CacheCubeState();

        _lockedLocalScale = cubeBody.localScale;
    }

    void Update()
    {
        int grabbedHandleIdx = GetGrabbedHandleIndex();
        if (grabbedHandleIdx >= 0)
        {
            _activeHandleIdx = grabbedHandleIdx;
            ResizeWithActiveHandle(_activeHandleIdx); 
            CacheCubeState();
            _lockedLocalScale = cubeBody.localScale;
            return;
        }
        else
        {
            _activeHandleIdx = -1;
        }

        bool cubeGrabbed = IsGrabbed(cubeGrabbable);

        Vector3 center = GetCubeWorldCenter();
        Vector3 size = GetCubeWorldSize();
        Quaternion rot = cubeBody.rotation;

        bool moved = (center - _lastCenterW).sqrMagnitude > 1e-10f;
        bool sized = (size - _lastSizeW).sqrMagnitude > 1e-10f;
        bool rotated = Quaternion.Angle(rot, _lastRotW) > 0.001f;

        if (!_wasCubeGrabbed && cubeGrabbed)
        {
            if (disableScaleTransformersDuringGrab)
                DisableScaleTransformers(true);
        }
        else if (_wasCubeGrabbed && !cubeGrabbed)
        {
            if (disableScaleTransformersDuringGrab)
                DisableScaleTransformers(false);
        }

        if (cubeGrabbed && _activeHandleIdx < 0)
        {
            if ((cubeBody.localScale - _lockedLocalScale).sqrMagnitude > 1e-10f)
                cubeBody.localScale = _lockedLocalScale;
        }

        if (cubeGrabbed || moved || sized || rotated)
        {
            _lastCenterW = center;
            _lastSizeW = size;
            _lastRotW = rot;
        }

        _wasCubeGrabbed = cubeGrabbed;
    }

    void LateUpdate()
    {
        int grabbedIdx = GetGrabbedHandleIndex();

        if (_wasCubeGrabbed && _activeHandleIdx < 0)
        {
            if ((cubeBody.localScale - _lockedLocalScale).sqrMagnitude > 1e-10f)
                cubeBody.localScale = _lockedLocalScale;
        }

        if (grabbedIdx >= 0)
        {
            RepositionAllHandlesFromCube(grabbedIdx);
            FitInnerContent();
            if (zeroHandleVelocityOnSnap) ZeroHandleVelocitiesIfAny(grabbedIdx);
        }
        else
        {
            RepositionAllHandlesFromCube(-1);
            FitInnerContent();
            if (zeroHandleVelocityOnSnap) ZeroHandleVelocitiesIfAny(-1);
        }
    }

    private void OnBeforeRenderScaleLock()
    {
        if (_wasCubeGrabbed && _activeHandleIdx < 0)
        {
            if ((cubeBody.localScale - _lockedLocalScale).sqrMagnitude > 1e-12f)
                cubeBody.localScale = _lockedLocalScale;
        }
    }

    private void ResizeWithActiveHandle(int handleIdx)
    {
        if (handleIdx < 0 || handleIdx >= handles.Length) return;
        var active = handles[handleIdx];
        if (!active || !cubeBody) return;

        GetCubeLocalCenterSize(out Vector3 centerL, out Vector3 sizeL);

        Vector3 pW = active.transform.position;
        Vector3 pL = cubeBody.InverseTransformPoint(pW);
        Vector3 vL = pL - centerL;

        Vector3 sign = active.GetSignVector();
        float hx = Mathf.Max(0.0001f, sign.x * vL.x);
        float hy = Mathf.Max(0.0001f, sign.y * vL.y);
        float hz = Mathf.Max(0.0001f, sign.z * vL.z);

        Vector3 axisWorldScale = AbsVec(cubeBody.lossyScale);
        float minHX = (minSize.x * 0.5f) / Mathf.Max(1e-6f, axisWorldScale.x);
        float minHY = (minSize.y * 0.5f) / Mathf.Max(1e-6f, axisWorldScale.y);
        float minHZ = (minSize.z * 0.5f) / Mathf.Max(1e-6f, axisWorldScale.z);
        float maxHX = (maxSize.x * 0.5f) / Mathf.Max(1e-6f, axisWorldScale.x);
        float maxHY = (maxSize.y * 0.5f) / Mathf.Max(1e-6f, axisWorldScale.y);
        float maxHZ = (maxSize.z * 0.5f) / Mathf.Max(1e-6f, axisWorldScale.z);

        hx = Mathf.Clamp(hx, minHX, maxHX);
        hy = Mathf.Clamp(hy, minHY, maxHY);
        hz = Mathf.Clamp(hz, minHZ, maxHZ);

        Vector3 targetLocalSize = new Vector3(hx * 2f, hy * 2f, hz * 2f);

        Vector3 ratio = new Vector3(
            SafeDiv(targetLocalSize.x, sizeL.x),
            SafeDiv(targetLocalSize.y, sizeL.y),
            SafeDiv(targetLocalSize.z, sizeL.z)
        );

        cubeBody.localScale = new Vector3(
            cubeBody.localScale.x * ratio.x,
            cubeBody.localScale.y * ratio.y,
            cubeBody.localScale.z * ratio.z
        );

        RepositionAllHandlesFromCube(handleIdx);
        FitInnerContent();

        _lockedLocalScale = cubeBody.localScale;
    }

    private void RepositionAllHandlesFromCube(int skipIdx = -1)
    {
        if (handles == null || handles.Length == 0 || !cubeBody) return;

        GetCubeLocalCenterSize(out Vector3 centerL, out Vector3 sizeL);
        Vector3 halfL = sizeL * 0.5f;

        for (int i = 0; i < handles.Length; i++)
        {
            if (i == skipIdx) continue;
            var h = handles[i];
            if (!h) continue;

            Vector3 s = h.GetSignVector();  
            Vector3 cornerLocal = centerL + Vector3.Scale(halfL, s);
            Vector3 cornerWorld = cubeBody.TransformPoint(cornerLocal);
            h.transform.position = cornerWorld;
        }
    }

    private int GetGrabbedHandleIndex()
    {
        if (handleGrabbables == null) return -1;
        for (int i = 0; i < handleGrabbables.Length; i++)
        {
            var g = handleGrabbables[i];
            if (IsGrabbed(g)) return i;
        }
        return -1;
    }

    private bool IsGrabbed(Grabbable g) => g != null && g.SelectingPointsCount > 0;

    private void GetCubeLocalCenterSize(out Vector3 centerL, out Vector3 sizeL)
    {
        if (cubeBoxCollider != null)
        {
            centerL = cubeBoxCollider.center;
            sizeL = cubeBoxCollider.size;
            return;
        }

        var mf = cubeBody.GetComponentInChildren<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            var b = mf.sharedMesh.bounds; 
            centerL = b.center;
            sizeL = b.size;
            return;
        }

        centerL = Vector3.zero;
        sizeL = cubeBody.localScale;
    }

    private Vector3 GetCubeWorldCenter()
    {
        GetCubeLocalCenterSize(out Vector3 centerL, out _);
        return cubeBody.TransformPoint(centerL);
    }

    private Vector3 GetCubeWorldSize()
    {
        GetCubeLocalCenterSize(out _, out Vector3 sizeL);
        Vector3 axisWorldScale = AbsVec(cubeBody.lossyScale);
        return new Vector3(
            Mathf.Abs(sizeL.x * axisWorldScale.x),
            Mathf.Abs(sizeL.y * axisWorldScale.y),
            Mathf.Abs(sizeL.z * axisWorldScale.z)
        );
    }

    private float SafeDiv(float a, float b) => (Mathf.Abs(b) < 1e-6f) ? 0f : a / b;

    private static Vector3 AbsVec(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

    private void FitInnerContent()
    {
        if (!innerContent) return;

        Vector3 sizeW = GetCubeWorldSize();
        Vector3 fitW = new Vector3(
            Mathf.Max(0f, sizeW.x - innerPadding.x * 2f),
            Mathf.Max(0f, sizeW.y - innerPadding.y * 2f),
            Mathf.Max(0f, sizeW.z - innerPadding.z * 2f)
        );

        innerContent.position = GetCubeWorldCenter();

        Vector3 parentScale = innerContent.parent ? innerContent.parent.lossyScale : Vector3.one;
        innerContent.localScale = new Vector3(
            SafeDiv(fitW.x, Mathf.Abs(parentScale.x)),
            SafeDiv(fitW.y, Mathf.Abs(parentScale.y)),
            SafeDiv(fitW.z, Mathf.Abs(parentScale.z))
        );
    }

    private void CacheCubeState()
    {
        _lastCenterW = GetCubeWorldCenter();
        _lastSizeW = GetCubeWorldSize();
        _lastRotW = cubeBody ? cubeBody.rotation : Quaternion.identity;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            if (cubeBody) _cubeRenderer = cubeBody.GetComponentInChildren<Renderer>();
            RepositionAllHandlesFromCube(-1);
            FitInnerContent();
        }
    }
#endif

    private void ReparentHandlesIfUnderCube()
    {
        if (handles == null) return;
        foreach (var h in handles)
        {
            if (!h) continue;
            if (cubeBody != null && h.transform.IsChildOf(cubeBody))
                h.transform.SetParent(handlesParent, true); 
        }
    }

    private void ZeroHandleVelocitiesIfAny(int skipIdx)
    {
        if (handles == null) return;
        for (int i = 0; i < handles.Length; i++)
        {
            if (i == skipIdx) continue;
            var h = handles[i];
            if (!h) continue;
            var rb = h.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    private void DisableScaleTransformers(bool disable)
    {
        _disabledScaleBehaviours.Clear();

        if (!cubeGrabbable) return;
        var go = cubeGrabbable.gameObject;

        var behaviours = go.GetComponentsInChildren<Behaviour>(true);
        foreach (var b in behaviours)
        {
            if (b == null) continue;
            var t = b.GetType();
            var name = t.Name; 
            var ns = t.Namespace ?? "";

            bool looksLikeScaler =
                name.Contains("ScaleTransformer") ||
                (ns.Contains("Oculus.Interaction") && name.Contains("Scale"));

            if (looksLikeScaler && b.enabled != !disable)
            {
                b.enabled = !disable;
                _disabledScaleBehaviours.Add(b);
            }
        }

        if (!disable)
        {
            foreach (var b in _disabledScaleBehaviours) { if (b) b.enabled = true; }
            _disabledScaleBehaviours.Clear();
        }
    }
}
