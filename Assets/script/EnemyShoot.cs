using UnityEngine;

public class EnemyShoot : MonoBehaviour
{
    public ParticleSystem bulletParticle;
    public ParticleSystem tailParticle;
    public Transform player;
    public Transform firePoint;
    public float bulletSpeed = 7f;
    public float shootInterval = 2f;
    public float bulletSize = 3f;
    public float tailSize = 0.9f;
    public float tailInterval = 0.2f;
    public float tailLifetime = 2.5f;
    public float tailScatter = 1.2f;
    public float tailDriftSpeed = 0.35f;
    public float tailBackwardSpeed = 1.2f;

    private float tailTimer;
    private float shootTimer;
    private ParticleSystem.Particle[] particles;

    void Start()
    {
        particles = new ParticleSystem.Particle[1000];  

        if (bulletParticle != null) //파티클 시스템 설정
        {
            ParticleSystem.CollisionModule collision = bulletParticle.collision;
            collision.enabled = true;
           
            collision.sendCollisionMessages = true;

            // 새로운(EnmyAttackCollision.cs) 컴퍼넌트 추가
            EnemyAttackCollision receiver = bulletParticle.GetComponent<EnemyAttackCollision>();
            if (receiver == null)
                receiver = bulletParticle.gameObject.AddComponent<EnemyAttackCollision>();
            receiver.Initialize(bulletParticle);
        }

        if (tailParticle != null)
        {
            ParticleSystem.MainModule tailMain = tailParticle.main;
            
            tailMain.startSpeed = 0f;
            tailMain.gravityModifier = 0f;

            ParticleSystem.EmissionModule emission = tailParticle.emission;
            emission.enabled = false;
            ParticleSystem.VelocityOverLifetimeModule velocity = tailParticle.velocityOverLifetime;
            velocity.enabled = false;
            ParticleSystem.ForceOverLifetimeModule force = tailParticle.forceOverLifetime;
            force.enabled = false;
            ParticleSystem.NoiseModule noise = tailParticle.noise;
            noise.enabled = false;

            Gradient fadeGradient = new Gradient();
            fadeGradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0.75f, 0f),
                    new GradientAlphaKey(0.45f, 0.35f),
                    new GradientAlphaKey(0f, 1f)
                });
            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = tailParticle.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(fadeGradient);

            AnimationCurve shrinkCurve = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.55f, 0.75f),
                new Keyframe(1f, 0.1f));
            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = tailParticle.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, shrinkCurve);
        }
    }

    void Update()
    {
        shootTimer += Time.deltaTime;
        if (shootTimer >= shootInterval)
        {
            Shoot();
            shootTimer = 0f;
        }

        tailTimer += Time.deltaTime;
        if (tailTimer >= tailInterval)
        {
            CreateTail();
            tailTimer = 0f;
        }
    }

    void Shoot()
    {
        if (bulletParticle == null || firePoint == null || player == null)
            return;

        Vector3 startPos = firePoint.position;
        Vector3 targetPos = player.position;
        startPos.z = 0f;
        targetPos.z = 0f;
        Vector3 direction = (targetPos - startPos).normalized;

        ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
        {
            position = startPos,
            velocity = direction * bulletSpeed,
            startSize = bulletSize,
            startLifetime = 5f
        };
        bulletParticle.Emit(emitParams, 1);
    }

    void CreateTail()
    {
        if (bulletParticle == null || tailParticle == null)
            return;

        int count = bulletParticle.GetParticles(particles);
        ParticleSystem.MainModule bulletMain = bulletParticle.main;

        for (int i = 0; i < count; i++)
        {
            Vector3 worldPosition = particles[i].position;
            Vector3 worldVelocity = particles[i].velocity;

            if (bulletMain.simulationSpace == ParticleSystemSimulationSpace.Local)
            {
                worldPosition = bulletParticle.transform.TransformPoint(worldPosition);
                worldVelocity = bulletParticle.transform.TransformVector(worldVelocity);
            }
            else if (bulletMain.simulationSpace == ParticleSystemSimulationSpace.Custom && bulletMain.customSimulationSpace != null)
            {
                worldPosition = bulletMain.customSimulationSpace.TransformPoint(worldPosition);
                worldVelocity = bulletMain.customSimulationSpace.TransformVector(worldVelocity);
            }

            Vector3 perpendicular = new Vector3(-worldVelocity.y, worldVelocity.x, 0f).normalized;
            worldPosition += perpendicular * Random.Range(-tailScatter, tailScatter);
            Vector3 backwardVelocity = worldVelocity.sqrMagnitude > 0.001f
                ? -worldVelocity.normalized * tailBackwardSpeed
                : Vector3.zero;
            Vector3 flutterVelocity = perpendicular * Random.Range(-tailDriftSpeed, tailDriftSpeed);

            ParticleSystem.EmitParams tailParams = new ParticleSystem.EmitParams
            {
                position = worldPosition,
                velocity = backwardVelocity + flutterVelocity,
                startSize = tailSize * Random.Range(0.7f, 1.2f),
                startLifetime = tailLifetime,
                startColor = new Color(0.65f, 0.85f, 1f, 0.65f)
            };
            tailParticle.Emit(tailParams, 1);
        }
    }
}
