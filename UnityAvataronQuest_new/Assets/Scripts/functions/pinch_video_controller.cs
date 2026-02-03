using UnityEngine;
using UnityEngine.Video;
using System.Collections.Generic;

// OVR Hand Tracking을 사용하는 Pinch Video Controller (Plane Object 버전)
public class PinchVideoControllerOVR : MonoBehaviour
{
    [Header("OVR Hand Tracking")]
    public OVRHand leftOVRHand;
    public OVRHand rightOVRHand;
    
    [Header("Eye Tracking")]
    public Camera eyeCamera;
    
    [Header("Video Plane Management")]
    public List<GameObject> videoPlanes = new List<GameObject>();
    public bool autoFindVideoPlanes = true;
    
    [Header("Distance-based Speed Control")]
    public bool enableDistanceSpeedControl = true;
    public Transform userAvatar;
    public float normalSpeedDistance = 3f;
    public float minPlaybackSpeed = 0.1f;
    public float maxPlaybackSpeed = 1f;
    public AnimationCurve speedCurve = AnimationCurve.EaseInOut(0f, 0.1f, 1f, 1f);
    public bool updateSpeedContinuously = true;
    
    [Header("Detection Settings")]
    public float raycastDistance = 10f;
    public LayerMask planeLayerMask = -1;
    
    private bool leftPinchTriggered = false;
    private bool rightPinchTriggered = false;
    private Dictionary<GameObject, VideoPlayer> planeVideoPlayers = new Dictionary<GameObject, VideoPlayer>();
    private GameObject currentPlayingPlane = null;
    
    void Start()
    {
        // OVRHand 컴포넌트 자동 찾기
        if (leftOVRHand == null)
        {
            GameObject leftHandObj = GameObject.Find("OVRHandPrefab_Left");
            if (leftHandObj != null)
                leftOVRHand = leftHandObj.GetComponent<OVRHand>();
        }
        
        if (rightOVRHand == null)
        {
            GameObject rightHandObj = GameObject.Find("OVRHandPrefab_Right");
            if (rightHandObj != null)
                rightOVRHand = rightHandObj.GetComponent<OVRHand>();
        }
        
        if (eyeCamera == null)
            eyeCamera = Camera.main ?? FindObjectOfType<Camera>();
            
        if (userAvatar == null)
        {
            userAvatar = eyeCamera.transform;
            Debug.Log("User avatar not assigned, using camera as reference point");
        }
            
        InitializeVideoPlanes();
        
        Debug.Log($"Video controller initialized with {planeVideoPlayers.Count} video planes");
    }
    
    void InitializeVideoPlanes()
    {
        if (autoFindVideoPlanes)
        {
            // VideoPlayer 컴포넌트를 가진 모든 GameObject 찾기
            VideoPlayer[] allVideoPlayers = FindObjectsOfType<VideoPlayer>();
            foreach (VideoPlayer videoPlayer in allVideoPlayers)
            {
                GameObject planeObj = videoPlayer.gameObject;
                
                // Plane 또는 Quad 메시를 가진 오브젝트인지 확인
                MeshFilter meshFilter = planeObj.GetComponent<MeshFilter>();
                if (meshFilter != null && 
                    (meshFilter.sharedMesh.name.Contains("Plane") || 
                     meshFilter.sharedMesh.name.Contains("Quad")) &&
                    !videoPlanes.Contains(planeObj))
                {
                    videoPlanes.Add(planeObj);
                }
            }
        }
        
        planeVideoPlayers.Clear();
        foreach (GameObject plane in videoPlanes)
        {
            VideoPlayer videoPlayer = plane.GetComponent<VideoPlayer>();
            if (videoPlayer != null)
            {
                planeVideoPlayers[plane] = videoPlayer;
                
                Debug.Log($"Video plane registered: {plane.name}");
                
                // Video Material 확인
                Renderer renderer = plane.GetComponent<Renderer>();
                if (renderer != null && renderer.material != null)
                {
                    Debug.Log($"Video material found on {plane.name}: {renderer.material.name}");
                }
                else
                {
                    Debug.LogWarning($"Plane {plane.name} has no renderer or material!");
                }
            }
            else
            {
                Debug.LogWarning($"Plane {plane.name} has no VideoPlayer component!");
            }
        }
    }
    
    void Update()
    {
        // CheckOVRPinchGesture();
        
        // 거리 기반 속도 조절이 활성화되고 지속 업데이트가 켜져있으면
        if (enableDistanceSpeedControl && updateSpeedContinuously && currentPlayingPlane != null)
        {
            UpdatePlaybackSpeedBasedOnDistance(currentPlayingPlane);
        }
    }
    
    void CheckOVRPinchGesture()
    {
        bool leftPinching = IsOVRPinching(leftOVRHand);
        bool rightPinching = IsOVRPinching(rightOVRHand);
        
        // 왼손 pinch 감지
        if (leftPinching && !leftPinchTriggered)
        {
            leftPinchTriggered = true;
            OnPinchTriggered();
        }
        else if (!leftPinching && leftPinchTriggered)
        {
            leftPinchTriggered = false;
        }
        
        // 오른손 pinch 감지
        if (rightPinching && !rightPinchTriggered)
        {
            rightPinchTriggered = true;
            OnPinchTriggered();
        }
        else if (!rightPinching && rightPinchTriggered)
        {
            rightPinchTriggered = false;
        }
    }
    
    bool IsOVRPinching(OVRHand hand)
    {
        if (hand == null || !hand.IsTracked)
            return false;
            
        return hand.GetFingerPinchStrength(OVRHand.HandFinger.Index) > 0.8f;
    }
    
    void OnPinchTriggered()
    {
        Debug.Log("OVR Pinch gesture detected!");
        
        Vector3 gazeDirection = eyeCamera.transform.forward;
        Vector3 gazeOrigin = eyeCamera.transform.position;
        
        RaycastHit hit;
        GameObject targetPlane = null;
        
        if (Physics.Raycast(gazeOrigin, gazeDirection, out hit, raycastDistance, planeLayerMask))
        {
            Debug.Log($"Gaze hit: {hit.collider.name}");
            
            targetPlane = hit.collider.gameObject;
            
            if (targetPlane != null && planeVideoPlayers.ContainsKey(targetPlane))
            {
                Debug.Log($"Target video plane found: {targetPlane.name}");
                PlayTargetVideoAndStopOthers(targetPlane);
            }
            else
            {
                Debug.Log("Hit object is not in the registered video plane list");
                StopAllVideos();
            }
        }
        else
        {
            Debug.Log("No object detected in gaze direction - stopping all videos");
            StopAllVideos();
        }
        
        Debug.DrawRay(gazeOrigin, gazeDirection * raycastDistance, Color.green, 1f);
    }
    
    public void PlayTargetVideoAndStopOthers(GameObject targetPlane)
    {
        foreach (var kvp in planeVideoPlayers)
        {
            GameObject plane = kvp.Key;
            VideoPlayer videoPlayer = kvp.Value;
            
            if (plane == targetPlane)
            {
                if (!videoPlayer.isPlaying)
                {
                    videoPlayer.Play();
                    Debug.Log($"Video started: {videoPlayer.name} on plane {plane.name}");
                }
                else
                {
                    Debug.Log($"Video already playing: {videoPlayer.name} on plane {plane.name}");
                }
                
                currentPlayingPlane = targetPlane;
                
                if (enableDistanceSpeedControl)
                {
                    UpdatePlaybackSpeedBasedOnDistance(targetPlane);
                }
            }
            else
            {
                if (videoPlayer.isPlaying)
                {
                    videoPlayer.Pause();
                    Debug.Log($"Video stopped: {videoPlayer.name} on plane {plane.name}");
                }
            }
        }
    }
    
    void StopAllVideos()
    {
        foreach (var kvp in planeVideoPlayers)
        {
            VideoPlayer videoPlayer = kvp.Value;
            if (videoPlayer.isPlaying)
            {
                videoPlayer.Pause();
                Debug.Log($"Video paused: {videoPlayer.name}");
            }
        }
        currentPlayingPlane = null;
        Debug.Log("All videos stopped");
    }
    
    void UpdatePlaybackSpeedBasedOnDistance(GameObject targetPlane)
    {
        if (targetPlane == null || userAvatar == null || !planeVideoPlayers.ContainsKey(targetPlane))
            return;
            
        VideoPlayer videoPlayer = planeVideoPlayers[targetPlane];
        if (!videoPlayer.isPlaying)
            return;
        
        // 사용자 아바타와 비디오 플레인 간의 거리 계산
        float distance = Vector3.Distance(userAvatar.position, targetPlane.transform.position);
        
        // 거리를 0-1 범위로 정규화 (normalSpeedDistance 이상이면 1)
        float normalizedDistance = Mathf.Clamp01(distance / normalSpeedDistance);
        
        // 커브를 사용하여 재생 속도 계산
        float targetSpeed = Mathf.Lerp(minPlaybackSpeed, maxPlaybackSpeed, speedCurve.Evaluate(normalizedDistance));
        
        // 비디오 재생 속도 적용
        videoPlayer.playbackSpeed = targetSpeed;
        
        // 디버그 정보
        if (Application.isEditor)
        {
            Debug.Log($"Distance: {distance:F2}m, Normalized: {normalizedDistance:F2}, Speed: {targetSpeed:F2}x");
        }
    }
    
    // 거리 기반 속도 조절 관련 공개 메서드들
    public void SetDistanceSpeedControl(bool enabled)
    {
        enableDistanceSpeedControl = enabled;
        
        // 비활성화시 모든 비디오를 정상 속도로 복원
        if (!enabled)
        {
            foreach (var kvp in planeVideoPlayers)
            {
                VideoPlayer videoPlayer = kvp.Value;
                videoPlayer.playbackSpeed = 1f;
            }
        }
    }
    
    public void SetNormalSpeedDistance(float distance)
    {
        normalSpeedDistance = Mathf.Max(0.1f, distance);
    }
    
    public void SetSpeedRange(float minSpeed, float maxSpeed)
    {
        minPlaybackSpeed = Mathf.Clamp(minSpeed, 0.01f, 10f);
        maxPlaybackSpeed = Mathf.Clamp(maxSpeed, minPlaybackSpeed, 10f);
    }
    
    public float GetCurrentDistance()
    {
        if (currentPlayingPlane != null && userAvatar != null)
        {
            return Vector3.Distance(userAvatar.position, currentPlayingPlane.transform.position);
        }
        return -1f;
    }
    
    public float GetCurrentPlaybackSpeed()
    {
        if (currentPlayingPlane != null && planeVideoPlayers.ContainsKey(currentPlayingPlane))
        {
            return planeVideoPlayers[currentPlayingPlane].playbackSpeed;
        }
        return 1f;
    }
    
    // 공개 메서드들
    public void RefreshPlanesList()
    {
        InitializeVideoPlanes();
    }
    
    public void AddVideoPlane(GameObject plane)
    {
        if (!videoPlanes.Contains(plane))
        {
            VideoPlayer videoPlayer = plane.GetComponent<VideoPlayer>();
            if (videoPlayer != null)
            {
                videoPlanes.Add(plane);
                planeVideoPlayers[plane] = videoPlayer;
                
                Debug.Log($"Video plane added: {plane.name}");
            }
            else
            {
                Debug.LogWarning($"Plane {plane.name} has no VideoPlayer component!");
            }
        }
    }
    
    public void RemoveVideoPlane(GameObject plane)
    {
        if (videoPlanes.Contains(plane))
        {
            videoPlanes.Remove(plane);
            if (planeVideoPlayers.ContainsKey(plane))
            {
                planeVideoPlayers.Remove(plane);
            }
            Debug.Log($"Video plane removed: {plane.name}");
        }
    }
    
    public int GetRegisteredPlaneCount()
    {
        return planeVideoPlayers.Count;
    }
    
    public bool IsVideoPlaying(GameObject plane)
    {
        if (planeVideoPlayers.ContainsKey(plane))
        {
            return planeVideoPlayers[plane].isPlaying;
        }
        return false;
    }
    
    public GameObject GetCurrentPlayingPlane()
    {
        return currentPlayingPlane;
    }
    
    public List<GameObject> GetAllVideoPlanes()
    {
        return new List<GameObject>(videoPlanes);
    }
    
    // 특정 비디오 플레인의 Material 정보 가져오기
    public Material GetVideoMaterial(GameObject plane)
    {
        Renderer renderer = plane.GetComponent<Renderer>();
        return renderer != null ? renderer.material : null;
    }
    
    // 시각적 디버그를 위한 Gizmo
    void OnDrawGizmos()
    {
        if (eyeCamera != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(eyeCamera.transform.position, eyeCamera.transform.forward * raycastDistance);
        }
        
        // 등록된 비디오 플레인들 표시
        foreach (GameObject plane in videoPlanes)
        {
            if (plane != null)
            {
                Gizmos.color = plane == currentPlayingPlane ? Color.green : Color.gray;
                Gizmos.DrawWireCube(plane.transform.position, plane.transform.localScale);
            }
        }
        
        // 거리 기반 속도 조절 관련 기즈모
        if (enableDistanceSpeedControl && userAvatar != null && currentPlayingPlane != null)
        {
            // 사용자 아바타 위치
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(userAvatar.position, 0.1f);
            
            // 현재 재생 중인 플레인과의 연결선
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(userAvatar.position, currentPlayingPlane.transform.position);
            
            // 정상 속도 거리 범위 표시
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(userAvatar.position, normalSpeedDistance);
        }
    }
}