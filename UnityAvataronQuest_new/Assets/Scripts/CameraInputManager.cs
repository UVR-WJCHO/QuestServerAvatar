using UnityEngine;
using Meta.XR.Samples;
using PassthroughCameraSamples;
using UnityEngine.UI;
using System.Collections;

public class CameraInputManager : MonoBehaviour
{
    [SerializeField] private WebCamTextureManager m_webCamTextureManager;
    [SerializeField] private RawImage m_image;
   
    private IEnumerator Start()
    {
        while(m_webCamTextureManager.WebCamTexture == null)
        {
            yield return null;
        }

        m_image.texture = m_webCamTextureManager.WebCamTexture;
    }
}
