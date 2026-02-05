using UnityEngine;

public class FloorSnapper : MonoBehaviour
{
    public Transform groupRoot;
    public Transform cubeBody;
    public Transform floor; 

    public float targetGap = 0f;
    public float snapRange = 0.5f;
    public float snapLerpSpeed = 0f;
    public bool stickyWhileClose = true;

    bool _snappedOnce = false;

    void Awake()
    {
        if (floor == null)
        {
            GameObject baseObj = GameObject.Find("base");
            if (baseObj != null)
            {
                floor = baseObj.transform;
                Debug.Log($"{gameObject.name}: 'base' 객체를 자동으로 찾아 할당했습니다.");
            }
            else
            {
                Debug.LogWarning($"{gameObject.name}: 씬에서 'base'라는 이름의 객체를 찾을 수 없습니다!");
            }
        }
    }

    void Reset()
    {
        groupRoot = transform;
    }

    void Update()
    {
        if (!groupRoot || !cubeBody || !floor) return;

        bool ok = TryGetBottomY(cubeBody, out float cubeBottomY);
        if (!ok) return;

        float floorY = floor.position.y;
        float wantBottomY = floorY + targetGap;
        float dy = wantBottomY - cubeBottomY;
        float absDy = Mathf.Abs(dy);

        bool inRange = absDy <= snapRange;

        if (inRange || (stickyWhileClose && _snappedOnce))
        {
            if (snapLerpSpeed > 0f)
            {
                float move = Mathf.Lerp(0f, dy, Time.deltaTime * snapLerpSpeed);
                groupRoot.position += new Vector3(0f, move, 0f);
            }
            else
            {
                groupRoot.position += new Vector3(0f, dy, 0f);
            }
            _snappedOnce = true;
        }
        else
        {
            _snappedOnce = false;
        }
    }

    bool TryGetBottomY(Transform t, out float bottomY)
    {
        bottomY = 0f;
        if (t == null) return false;

        var col = t.GetComponentInChildren<Collider>();
        if (col)
        {
            bottomY = col.bounds.min.y;
            return true;
        }

        var rend = t.GetComponentInChildren<Renderer>();
        if (rend)
        {
            bottomY = rend.bounds.min.y;
            return true;
        }

        return false;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!cubeBody || !floor) return;
        if (TryGetBottomY(cubeBody, out float btm))
        {
            float want = floor.position.y + targetGap;
            Gizmos.color = Color.green;
            Gizmos.DrawLine(new Vector3(cubeBody.position.x - 0.2f, want, cubeBody.position.z),
                            new Vector3(cubeBody.position.x + 0.2f, want, cubeBody.position.z));
        }
    }
#endif
}