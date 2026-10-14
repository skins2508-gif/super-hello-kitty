using UnityEngine;

public class PlayerBullet : MonoBehaviour
{
    public float speed = 10f;
    public float damage = 10f;
    public float lifeTime = 3f;

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = transform.right * speed;

        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        if (rb == null)
            transform.position += transform.right * speed * Time.deltaTime;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        BossHP boss = other.GetComponentInParent<BossHP>();
        if (boss == null)
            return;

        boss.TakeDamage(damage);
        Destroy(gameObject);
    }
}
