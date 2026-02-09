using UnityEngine;
using Oculus.Interaction; // Grabbable 사용을 위해 필요

public class FloorSnapper : MonoBehaviour
{
    public Transform groupRoot;
    public Transform cubeBody;
    public Transform floor;

    public Grabbable grabbable;

    public float targetGap = 0f;
    public float snapRange = 0.5f;
    public float snapLerpSpeed = 10f; 
    public bool stickyWhileClose = true;

    private bool _snappedOnce = false;
    private Rigidbody _rb;

    void Awake()
    {
        if (groupRoot == null) groupRoot = transform;

        _rb = groupRoot.GetComponent<Rigidbody>();

        if (grabbable == null) grabbable = groupRoot.GetComponentInChildren<Grabbable>();

        if (floor == null)
        {
            GameObject baseObj = GameObject.Find("base");
            if (baseObj != null) floor = baseObj.transform;
        }
    }

    void LateUpdate()
    {
        if (!groupRoot || !cubeBody || !floor) return;

        bool isGrabbed = grabbable != null && grabbable.SelectingPointsCount > 0;
        if (isGrabbed)
        {
            _snappedOnce = false;
            if (_rb != null) _rb.isKinematic = false;
            return;
        }

        if (!TryGetBottomY(cubeBody, out float cubeBottomY)) return;

        float floorY = floor.position.y;
        float wantBottomY = floorY + targetGap;
        float dy = wantBottomY - cubeBottomY;

        if (_snappedOnce)
        {
            float currentBottom = GetBottomY(cubeBody);
            dy = wantBottomY - currentBottom;
        }

        bool inRange = Mathf.Abs(dy) <= snapRange;

        if (inRange || (stickyWhileClose && _snappedOnce))
        {
            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                _rb.isKinematic = true;
            }


            Vector3 visualPosBefore = cubeBody.position;

            groupRoot.rotation = Quaternion.identity;
            if (cubeBody != groupRoot) cubeBody.localRotation = Quaternion.identity;

            Vector3 visualPosAfter = cubeBody.position;

            Vector3 offset = visualPosBefore - visualPosAfter;
            offset.y = 0f; 

            groupRoot.position += offset;


            float finalBottom = GetBottomY(cubeBody);
            float finalDy = wantBottomY - finalBottom;

            if (snapLerpSpeed > 0f && !_snappedOnce)
            {
                float move = Mathf.Lerp(0f, finalDy, Time.deltaTime * snapLerpSpeed);
                groupRoot.position += new Vector3(0f, move, 0f);
            }
            else
            {
                groupRoot.position += new Vector3(0f, finalDy, 0f);
            }

            _snappedOnce = true;
        }
        else
        {
            _snappedOnce = false;
            if (_rb != null) _rb.isKinematic = false;
        }
    }

    float GetBottomY(Transform t)
    {
        var col = t.GetComponentInChildren<Collider>();
        if (col) return col.bounds.min.y;
        var rend = t.GetComponentInChildren<Renderer>();
        if (rend) return rend.bounds.min.y;
        return t.position.y;
    }

    bool TryGetBottomY(Transform t, out float bottomY)
    {
        bottomY = GetBottomY(t);
        return true;
    }
}