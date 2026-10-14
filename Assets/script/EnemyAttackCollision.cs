using System.Collections.Generic;
using UnityEngine;

public class EnemyAttackCollision : MonoBehaviour
{
    private ParticleSystem source;
    private readonly List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();

    public void Initialize(ParticleSystem particleSystem)
    {
        source = particleSystem;
    }

    void OnParticleCollision(GameObject other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null || player.IsDead)
            return;

        int collisionCount = source != null
            ? ParticlePhysicsExtensions.GetCollisionEvents(source, other, collisionEvents)
            : 0;

        if (collisionCount > 0)
            player.Die();
    }
}
