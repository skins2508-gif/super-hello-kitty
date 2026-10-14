using UnityEngine;

public class BouncingHeartProjectile : MonoBehaviour
{
    public static int ActiveCount { get; private set; }

    private float lifeTime;
    private bool counted;

    public void Initialize(float duration)
    {
        lifeTime = duration;
        counted = true;
        ActiveCount++;
    }

    private void Update()
    {
        lifeTime -= Time.deltaTime;
        if (lifeTime <= 0f)
            Destroy(gameObject);

        Camera camera = Camera.main;
        if (camera != null)
        {
            Vector3 viewport = camera.WorldToViewportPoint(transform.position);
            if (viewport.y < -0.15f)
                Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player != null && !player.IsDead)
            player.Die();
    }

    private void OnDestroy()
    {
        if (!counted)
            return;

        counted = false;
        ActiveCount = Mathf.Max(0, ActiveCount - 1);
    }
}
