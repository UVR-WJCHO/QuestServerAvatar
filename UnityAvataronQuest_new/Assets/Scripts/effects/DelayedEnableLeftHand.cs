using UnityEngine;
using System.Collections;

public class DelayedEnableLeftHand : MonoBehaviour
{
    [Header("Target Objects")]
    public GameObject RightHandVisualObject;
    public GameObject LeftHandVisualObject;

    private void Start()
    {
        if (RightHandVisualObject == null || LeftHandVisualObject == null)
        {
            Debug.LogError("[ActivateBAfterA] Object A or B is not assigned.");
            return;
        }

        // 필요하다면 시작 시 B 비활성화
        LeftHandVisualObject.SetActive(false);

        StartCoroutine(WaitForAAndActivateB());
    }

    private IEnumerator WaitForAAndActivateB()
    {
        // A가 활성화될 때까지 대기
        yield return new WaitUntil(() => RightHandVisualObject.activeInHierarchy);

        // A가 활성화되면 B 활성화
        LeftHandVisualObject.SetActive(true);
    }
}
