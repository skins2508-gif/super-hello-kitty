using UnityEngine;

[DisallowMultipleComponent]
public class BackgroundMusic : MonoBehaviour
{
    [Header("Background Music")]
    public AudioClip musicClip;
    [Range(0f, 1f)] public float volume = 0.5f;
    public bool playOnStart = true;
    public bool loop = true;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        ApplySettings();
    }

    private void Start()
    {
        if (playOnStart && musicClip != null)
            audioSource.Play();
    }

    private void OnValidate()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource != null)
            ApplySettings();
    }

    private void ApplySettings()
    {
        audioSource.clip = musicClip;
        audioSource.volume = volume;
        audioSource.loop = loop;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    public void StopMusic()
    {
        if (audioSource != null)
            audioSource.Stop();
    }
}
