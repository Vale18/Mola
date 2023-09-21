using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class DepthAudioTrigger : MonoBehaviour
{
    public Transform playerTransform;    // Reference to the player's transform
    public float triggerDepth1 = -1f;    // Depth at which first audio is played (Epipelagial Zone)
    public float triggerDepth2 = -200f;  // Mesopelagial Zone
    public float triggerDepth3 = -1000f; // Bathypelagial Zone
    public float triggerDepth4 = -4000f; // Abyssopelagial Zone
    public float triggerDepth5 = -6000f; // Hadopelagial Zone
    public float triggerDepth6 = -11000f;// End

    private bool hasPlayedAudio1 = false;  // To ensure audio plays only once
    private bool hasPlayedAudio2 = false;
    private bool hasPlayedAudio3 = false;
    private bool hasPlayedAudio4 = false;
    private bool hasPlayedAudio5 = false;
    private bool hasPlayedAudio6 = false;

    private AudioSource audioSource;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        // Check if first audio hasn't played and player's depth has reached the first trigger point
        if (!hasPlayedAudio1 && playerTransform.position.y <= triggerDepth1)
        {
            audioSource.clip = Resources.Load<AudioClip>("Audio/Audio_1");
            audioSource.Play();
            hasPlayedAudio1 = true;  // Set flag to ensure audio plays only once
        }
        
        // Check if second audio hasn't played and player's depth has reached the second trigger point
        if (!hasPlayedAudio2 && playerTransform.position.y <= triggerDepth2)
        {
            audioSource.clip = Resources.Load<AudioClip>("Audio/Audio_2");
            audioSource.Play();
            hasPlayedAudio2 = true;  // Set flag to ensure audio plays only once
        }


        if (!hasPlayedAudio3 && playerTransform.position.y <= triggerDepth1)
        {
            audioSource.clip = Resources.Load<AudioClip>("Audio/Audio_3");
            audioSource.Play();
            hasPlayedAudio1 = true;  // Set flag to ensure audio plays only once
        }


        if (!hasPlayedAudio4 && playerTransform.position.y <= triggerDepth1)
        {
            audioSource.clip = Resources.Load<AudioClip>("Audio/Audio_4");
            audioSource.Play();
            hasPlayedAudio1 = true;  // Set flag to ensure audio plays only once
        }


        if (!hasPlayedAudio5 && playerTransform.position.y <= triggerDepth1)
        {
            audioSource.clip = Resources.Load<AudioClip>("Audio/Audio_5");
            audioSource.Play();
            hasPlayedAudio1 = true;  // Set flag to ensure audio plays only once
        }


        if (!hasPlayedAudio6 && playerTransform.position.y <= triggerDepth1)
        {
            audioSource.clip = Resources.Load<AudioClip>("Audio/Audio_6");
            audioSource.Play();
            hasPlayedAudio1 = true;  // Set flag to ensure audio plays only once
        }
    }
}
