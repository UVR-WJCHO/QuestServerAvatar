using UnityEngine;

public class AvatarBoundaryWall : MonoBehaviour
{
    [Header("Avatar References")]
    public GameObject avatar1;
    public GameObject avatar2;
    
    [Header("Boundary Wall")]
    public GameObject boundaryPlane;
    
    [Header("Wall Settings")]
    public bool updateContinuously = true;
    public float wallHeight = 3f; // 벽의 높이
    public float heightOffset = 0f; // Y축 오프셋 (바닥에서 얼마나 위에 배치할지)
    
    [Header("Smoothing")]
    public bool enableSmoothing = true;
    public float positionSmoothSpeed = 5f;
    public float rotationSmoothSpeed = 5f;
    
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    
    void Start()
    {
        // 초기 벽 위치 설정
        if (ValidateReferences())
        {
            UpdateWallTransform();
        }
    }
    
    void Update()
    {
        if (updateContinuously && ValidateReferences())
        {
            UpdateWallTransform();
        }
    }
    
    bool ValidateReferences()
    {
        if (avatar1 == null)
        {
            Debug.LogWarning("Avatar1이 할당되지 않았습니다.");
            return false;
        }
        
        if (avatar2 == null)
        {
            Debug.LogWarning("Avatar2가 할당되지 않았습니다.");
            return false;
        }
        
        if (boundaryPlane == null)
        {
            Debug.LogWarning("Boundary Plane이 할당되지 않았습니다.");
            return false;
        }
        
        return true;
    }
    
    void UpdateWallTransform()
    {
        // 두 아바타의 위치
        Vector3 pos1 = avatar1.transform.position;
        Vector3 pos2 = avatar2.transform.position;
        
        // 중점 계산 (벽의 위치)
        Vector3 midPoint = (pos1 + pos2) * 0.5f;
        midPoint.y += heightOffset; // Y축 오프셋 적용
        
        // 두 아바타를 잇는 방향 벡터 (XZ 평면에서만)
        Vector3 direction = (pos2 - pos1);
        Vector3 directionXZ = new Vector3(direction.x, 0, direction.z).normalized;
        
        // 벽이 두 아바타를 잇는 선에 수직이 되도록 Y축 회전 계산
        float yRotation = Mathf.Atan2(directionXZ.x, directionXZ.z) * Mathf.Rad2Deg;
        
        // 벽을 세로로 세우기 위한 회전: X축으로 90도 회전 + Y축 회전
        targetRotation = Quaternion.Euler(90f, yRotation, 0f);
        targetPosition = midPoint;
        
        // 스무딩 적용 여부에 따라 위치와 회전 설정
        if (enableSmoothing)
        {
            // 부드러운 이동과 회전
            boundaryPlane.transform.position = Vector3.Lerp(
                boundaryPlane.transform.position, 
                targetPosition, 
                positionSmoothSpeed * Time.deltaTime
            );
            
            boundaryPlane.transform.rotation = Quaternion.Slerp(
                boundaryPlane.transform.rotation, 
                targetRotation, 
                rotationSmoothSpeed * Time.deltaTime
            );
        }
        else
        {
            // 즉시 이동과 회전
            boundaryPlane.transform.position = targetPosition;
            boundaryPlane.transform.rotation = targetRotation;
        }
        
        // 벽의 크기를 두 아바타 사이의 거리에 맞게 조정
        AdjustWallSize();
    }
    
    void AdjustWallSize()
    {
        // 두 아바타 사이의 거리 계산
        float distance = Vector3.Distance(avatar1.transform.position, avatar2.transform.position);
        
        // Plane을 세로로 세웠을 때의 스케일 조정
        // 세로로 세운 Plane에서:
        // X축: 벽의 너비 (두 아바타 사이 거리)
        // Y축: 벽의 두께 (얇게 유지)
        // Z축: 벽의 높이
        Vector3 newScale = boundaryPlane.transform.localScale;
        // newScale.x = distance * 0.15f; // 거리에 비례하여 너비 조정
        // newScale.y = 0.1f; // 벽의 두께 (얇게)
        // newScale.z = wallHeight * 0.1f; // 설정된 높이 적용
        
        // boundaryPlane.transform.localScale = newScale;
    }
    
    // 공개 메서드들
    public void ForceUpdateWall()
    {
        if (ValidateReferences())
        {
            UpdateWallTransform();
        }
    }
    
    public void SetWallHeight(float height)
    {
        wallHeight = height;
        ForceUpdateWall();
    }
    
    public void SetHeightOffset(float offset)
    {
        heightOffset = offset;
        ForceUpdateWall();
    }
    
    public float GetDistanceBetweenAvatars()
    {
        if (ValidateReferences())
        {
            return Vector3.Distance(avatar1.transform.position, avatar2.transform.position);
        }
        return 0f;
    }
    
    public Vector3 GetWallCenter()
    {
        if (ValidateReferences())
        {
            return (avatar1.transform.position + avatar2.transform.position) * 0.5f;
        }
        return Vector3.zero;
    }
    
    // 디버그용 기즈모
    void OnDrawGizmos()
    {
        if (!ValidateReferences()) return;
        
        Vector3 pos1 = avatar1.transform.position;
        Vector3 pos2 = avatar2.transform.position;
        Vector3 midPoint = (pos1 + pos2) * 0.5f;
        
        // 아바타들 사이의 선 그리기
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(pos1, pos2);
        
        // 중점 표시
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(midPoint, 0.1f);
        
        // 벽의 방향 표시 (세로 벽이므로 up 방향으로 표시)
        if (boundaryPlane != null)
        {
            Gizmos.color = Color.blue;
            Vector3 wallPos = boundaryPlane.transform.position;
            Vector3 wallUp = boundaryPlane.transform.up;
            Gizmos.DrawRay(wallPos, wallUp * 2f);
            
            Gizmos.color = Color.green;
            Gizmos.DrawRay(wallPos, boundaryPlane.transform.right * 1f);
            
            // 벽의 법선 벡터 표시 (벽이 향하는 방향)
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(wallPos, boundaryPlane.transform.forward * 0.5f);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // 선택되었을 때 더 자세한 정보 표시
        if (!ValidateReferences()) return;
        
        Vector3 pos1 = avatar1.transform.position;
        Vector3 pos2 = avatar2.transform.position;
        float distance = Vector3.Distance(pos1, pos2);
        
        // 거리 정보를 Scene 뷰에 표시
        Vector3 labelPos = (pos1 + pos2) * 0.5f + Vector3.up * 0.5f;
        
#if UNITY_EDITOR
        UnityEditor.Handles.Label(labelPos, $"Distance: {distance:F2}m\nWall Height: {wallHeight:F1}m");
#endif
    }
}