using UnityEngine;
public class PlaySoundOnTrigger : MonoBehaviour
{
    // Referenz zum AudioSource-Komponente
    public AudioSource audioSource;

    private void Start()
    {
        // Stellen Sie sicher, dass der AudioSource nicht automatisch beim Start spielt.
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Prüfen, ob das andere GameObject den Tag 'Player' hat
        if (other.CompareTag("Player"))
        {
            // Audioclip abspielen, wenn nicht bereits gespielt wird
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }
    }
}
