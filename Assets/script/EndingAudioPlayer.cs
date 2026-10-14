using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class EndingAudioPlayer : MonoBehaviour
{
    private AudioSource audioSource;

    public void Play(AudioClip clip, float volume)
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
        audioSource.ignoreListenerPause = true;
        audioSource.clip = clip;
        audioSource.volume = Mathf.Clamp01(volume);

        if (clip != null)
            audioSource.Play();
    }
}
