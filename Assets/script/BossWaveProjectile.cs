using UnityEngine;

public class BossWaveProjectile : MonoBehaviour
{
    public static int ActiveCount { get; private set; }

    private Vector2 velocity;
    private float lifeTime;
    private bool counted;

    public void Initialize(Vector2 moveVelocity, float duration)
    {
        velocity = moveVelocity;
        lifeTime = duration;
        counted = true;
        ActiveCount++;
    }

    private void Update()
    {
        transform.position += (Vector3)(velocity * Time.deltaTime);
        lifeTime -= Time.deltaTime;

        if (lifeTime <= 0f)
            Destroy(gameObject);

        Camera camera = Camera.main;
        if (camera != null)
        {
            Vector3 viewport = camera.WorldToViewportPoint(transform.position);
            if (viewport.x < -0.15f || viewport.x > 1.15f ||
                viewport.y < -0.15f || viewport.y > 1.15f)
                Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null || player.IsDead)
            return;

        player.Die();
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (!counted)
            return;

        counted = false;
        ActiveCount = Mathf.Max(0, ActiveCount - 1);
    }
}
