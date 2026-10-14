using UnityEngine;

public class DestructoDiscProjectile : MonoBehaviour
{
    public static int ActiveCount { get; private set; }

    private Vector2 velocity;
    private float lifeTime;
    private float spinSpeed;
    private bool counted;
    private SpriteRenderer spriteRenderer;

    public void Initialize(Vector2 moveVelocity, float duration, float rotationSpeed)
    {
        velocity = moveVelocity;
        lifeTime = duration;
        spinSpeed = rotationSpeed;
        counted = true;
        ActiveCount++;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        transform.position += (Vector3)(velocity * Time.deltaTime);
        transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
        lifeTime -= Time.deltaTime;

        if (lifeTime <= 0f)
            Destroy(gameObject);

        Camera camera = Camera.main;
        if (camera != null && spriteRenderer != null)
        {
            Bounds bounds = spriteRenderer.bounds;
            Vector3 viewportMin = camera.WorldToViewportPoint(bounds.min);
            Vector3 viewportMax = camera.WorldToViewportPoint(bounds.max);
            if (viewportMax.x < 0f || viewportMin.x > 1f ||
                viewportMax.y < 0f || viewportMin.y > 1f)
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
