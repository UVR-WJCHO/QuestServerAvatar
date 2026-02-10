using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ModelBaseEffectTrigger : MonoBehaviour
{
    private DecalProjector projector;
    private Material decalInstancedMaterial;
    
    public float targetRadius = 0.45f; // UV 기반일 때 0.5가 최대 원입니다.
    public float spreadSpeed = 1.0f;

    void Start()
    {
        projector = GetComponent<DecalProjector>();
        
        if (projector != null)
        {
            // 마테리얼 복사본 생성 및 프로젝터에 재할당 (중요)
            decalInstancedMaterial = new Material(projector.material);
            projector.material = decalInstancedMaterial; 
            
            decalInstancedMaterial.SetFloat("_Radius", 0f);
            // StartCoroutine(AnimateSpread());
        }
    }
    public void StartEffect()
    {
        StartCoroutine(AnimateSpread());
    }

    System.Collections.IEnumerator AnimateSpread()
    {
        float currentRadius = 0f;
        while (currentRadius < targetRadius)
        {
            currentRadius += Time.deltaTime * spreadSpeed;
            // 셰이더 그래프의 '_Radius' 변수 이름을 정확히 입력하세요.
            decalInstancedMaterial.SetFloat("_Radius", currentRadius);
            // 2. 강제 업데이트: 마테리얼을 다시 할당하여 에디터 갱신 유도
            projector.material = decalInstancedMaterial;
            Debug.Log(currentRadius);
            yield return null;
        }
    }
}