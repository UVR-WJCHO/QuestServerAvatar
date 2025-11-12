using UnityEngine;

public class HeadGazeDwell : MonoBehaviour
{
    [Header("Raycast")]
    public Camera vrCam;                 
    public float maxDistance = 200f;
    public LayerMask hitMask;

    [Header("Dwell")]
    public float dwellTime = 0.2f;        
    private float timer = 0f;
    private Transform current;
    private Transform last;

    void Update()
    {
        var ray = new Ray(vrCam.transform.position, vrCam.transform.forward);
        Debug.DrawRay(vrCam.transform.position, vrCam.transform.forward * maxDistance, Color.cyan, 0f);

        if (Physics.Raycast(ray, out var hit, maxDistance, hitMask))
        {
            current = hit.transform;

            if (current != last) { timer = 0f; last = current; }

            timer += Time.deltaTime;
            if (timer >= dwellTime)
            {
              
                var spawner = current.GetComponent<GazePopupSpawner>();
                if (spawner) spawner.SpawnFromBehind(vrCam.transform);
                timer = 0f; 
            }
        }
        else
        {
            current = null; last = null; timer = 0f;
        }
    }
}