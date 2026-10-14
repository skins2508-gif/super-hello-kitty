using System.Collections.Generic;
using UnityEngine;

public class StraightHeartProjectile : MonoBehaviour
{
    private static readonly HashSet<StraightHeartProjectile> Active =
        new HashSet<StraightHeartProjectile>();

    private Vector2 velocity;
    private float lifeTime;

    public void Initialize(Vector2 movementVelocity, float maxLifeTime)
    {
        velocity = movementVelocity;
        lifeTime = Mathf.Max(0.1f, maxLifeTime);
    }

    private void OnEnable()
    {
        Active.Add(this);
    }

    private void Update()
    {
        transform.position += (Vector3)(velocity * Time.deltaTime);
        lifeTime -= Time.deltaTime;
        if (lifeTime <= 0f)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null || player.IsDead)
            return;

        player.Die();
        Destroy(gameObject);
    }

    private void OnDisable()
    {
        Active.Remove(this);
    }

    public static void DestroyAllActive()
    {
        StraightHeartProjectile[] snapshot = new StraightHeartProjectile[Active.Count];
        Active.CopyTo(snapshot);
        foreach (StraightHeartProjectile projectile in snapshot)
        {
            if (projectile != null)
                Destroy(projectile.gameObject);
        }

        Active.Clear();
    }
}
