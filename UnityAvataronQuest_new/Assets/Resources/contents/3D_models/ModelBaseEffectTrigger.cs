using UnityEngine;
using System.Collections;

public class ModelBaseEffectTrigger : MonoBehaviour
{
    private Material targetMaterial;
    public string floorObjectName = "base"; // 씬에 있는 바닥 오브젝트 이름
    public float targetRadius = 2.0f;       // 최종 반지름
    public float spreadSpeed = 1.5f;        // 퍼지는 속도

    void Start()
    {
        // 1. 씬에서 이름으로 바닥 오브젝트를 찾습니다.
        GameObject floor = GameObject.Find(floorObjectName);
        
        if (floor != null)
        {
            // 2. 바닥의 마테리얼을 가져옵니다.
            targetMaterial = floor.GetComponent<Renderer>().material;
            
            // 3. 효과 시작 (코루틴으로 부드럽게 확산)
            StartCoroutine(SpreadAlpha());
        }
        else
        {
            Debug.LogError($"{floorObjectName} 오브젝트를 씬에서 찾을 수 없습니다!");
        }
    }

    void Update()
    {
        // 모델이 이동할 수도 있으므로 실시간으로 위치 업데이트
        if (targetMaterial != null)
        {
            targetMaterial.SetVector("_Center", transform.position);
        }
    }

    IEnumerator SpreadAlpha()
    {
        float currentRadius = 0f;
        while (currentRadius < targetRadius)
        {
            currentRadius += Time.deltaTime * spreadSpeed;
            targetMaterial.SetFloat("_Radius", currentRadius);
            yield return null;
        }
    }
}