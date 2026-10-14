using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShoot : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform firePoint;
    public AudioClip shotSound;
    [Range(0f, 1f)] public float shotVolume = 0.8f;
    public int maxShot = 5;
    public float reloadTime = 0.5f;

    private int currentShot;
    private bool isReloading;
    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;

        PlayerShoot parentShoot = transform.parent != null
            ? transform.parent.GetComponentInParent<PlayerShoot>()
            : null;
        if (parentShoot != null && parentShoot != this)
            enabled = false;
    }

    void Start()
    {
        currentShot = maxShot;
    }

    void Update()
    {
        bool pressed = Keyboard.current != null && Keyboard.current.xKey.wasPressedThisFrame;

        if (pressed && !isReloading && currentShot > 0)
            Shoot();
    }

    void Shoot()
    {
        if (bulletPrefab == null || firePoint == null)
            return;

        Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

        if (shotSound != null)
            audioSource.PlayOneShot(shotSound, shotVolume);

        currentShot--;

        if (currentShot <= 0)
            StartCoroutine(Reload());
    }

    IEnumerator Reload()
    {
        isReloading = true;
        yield return new WaitForSeconds(reloadTime);
        currentShot = maxShot;
        isReloading = false;
    }
}
