using UnityEngine;
using PassthroughCameraSamples;

public class PassthroughCameraDisplay : MonoBehaviour
{
    public WebCamTextureManager webcamManager;
    public Renderer quadRenderer;
    public float quadDistance = 1.0f;
    private Texture2D pictureTexture;
    public string textureName;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        quadRenderer.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (webcamManager.WebCamTexture != null)
        {
            if (OVRInput.GetDown(OVRInput.Button.One))
            {
                TakePicture();
                PlaceQuad();
            }
        }
    }

    public void TakePicture()
    {
        quadRenderer.gameObject.SetActive(true);

        int width = webcamManager.WebCamTexture.width;
        int height = webcamManager.WebCamTexture.height;

        if (pictureTexture == null)
        {
            pictureTexture = new Texture2D(width, height);
        }

        Color32[] pixels = new Color32[width * height];
        webcamManager.WebCamTexture.GetPixels32(pixels);

        pictureTexture.SetPixels32(pixels);
        pictureTexture.Apply();

        quadRenderer.material.SetTexture(textureName, pictureTexture);
    }

    public void PlaceQuad()
    {
        Transform quadTransform = quadRenderer.transform;

        Pose cameraPose = PassthroughCameraUtils.GetCameraPoseInWorld(PassthroughCameraEye.Left);

        Vector2Int resolution = PassthroughCameraUtils.GetCameraIntrinsics(PassthroughCameraEye.Left).Resolution;

        quadTransform.position = cameraPose.position + cameraPose.forward * quadDistance;
        quadTransform.rotation = cameraPose.rotation;

        Ray leftside = PassthroughCameraUtils.ScreenPointToRayInCamera(PassthroughCameraEye.Left, new Vector2Int(0, resolution.y / 2));
        Ray rightside = PassthroughCameraUtils.ScreenPointToRayInCamera(PassthroughCameraEye.Left, new Vector2Int(resolution.x, resolution.y / 2));

        float horizontalFoV = Vector3.Angle(leftside.direction, rightside.direction);

        float quadScale = 2 * quadDistance * Mathf.Tan(horizontalFoV / 2 * Mathf.Deg2Rad);

        float ratio = (float)resolution.x / (float)resolution.y;
        quadTransform.localScale = new Vector3(quadScale, quadScale * ratio, 1);
    }
}
