using UnityEngine;

public class AudioInputManager : MonoBehaviour
{
    private AudioSource audioSource;
    
    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        if(Microphone.devices.Length > 0)
        {
            string mic = Microphone.devices[0];
            Debug.Log("[AudioInputManager] Microphone: " + mic);
            audioSource.clip = Microphone.Start(mic, true, 10, 44100);
            audioSource.loop = true;

            while (!(Microphone.GetPosition(mic) > 0)) { }

            audioSource.Play();
            Debug.Log("[AudioInputManager] Audio Play");
        }
        else
        {
            Debug.Log("[AudioInputManager] No microphone detected on Quest 3");
        }
    }

}
