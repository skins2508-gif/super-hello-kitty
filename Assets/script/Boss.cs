using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss : MonoBehaviour
{
    [Header("Laser Attack")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private float firstAttackExtraDelay = 0.2f;
    [SerializeField] private float attackInterval = 5f;
    [SerializeField] private int lasersPerCycle = 3;
    [SerializeField] private float firstAlignmentDuration = 5f;
    [SerializeField] private float nextAlignmentDuration = 1f;
    [SerializeField] private float warningDuration = 1f;
    [SerializeField] private float firstBeamChargeTime = 3f;
    [SerializeField] private float laserDuration = 0.5f;
    [SerializeField] private float chainedShotInterval = 3f;
    [SerializeField] private float alignmentSpeed = 7f;
    [SerializeField] private float laserLength = 30f;
    [Tooltip("Thickness of the white warning line.")]
    [Min(0.01f)] public float warningThickness = 0.15f;
    [Tooltip("Maximum thickness of the damaging laser beam.")]
    [Min(0.05f)] public float laserThickness = 1.3f;
    [SerializeField] private float laserGrowDuration = 0.12f;
    [SerializeField, Range(0f, 1f)] private float laserAlpha = 0.55f;

    [Header("Energy Wave Sound")]
    [SerializeField] private AudioClip firstEnergyWaveSound;
    [SerializeField] private AudioClip chainedEnergyWaveSound;
    [SerializeField, Range(0f, 1f)] private float energyWaveVolume = 1f;

    [Header("Energy Wave Camera Shake")]
    [SerializeField] private float firstShakeDuration = 0.8f;
    [SerializeField] private float firstShakeStrength = 0.8f;
    [SerializeField] private float chainedShakeDuration = 0.5f;
    [SerializeField] private float chainedShakeStrength = 0.45f;

    [Header("First Shot Recoil")]
    [SerializeField] private float middleHeight = 0f;
    [SerializeField] private float middleRange = 1f;
    [SerializeField] private float recoilDistance = 1.2f;
    [SerializeField] private float recoilSpeed = 10f;

    [Header("Wave Bullet Attack")]
    [SerializeField] private Sprite waveBulletSprite;
    [SerializeField] private int waveCount = 8;
    [SerializeField] private int bulletsPerWave = 5;
    [SerializeField] private float waveInterval = 0.65f;
    [SerializeField] private float waveBulletSpeed = 3.5f;
    [SerializeField] private float waveBulletSize = 0.22f;
    [SerializeField] private float waveSpreadAngle = 65f;
    [SerializeField] private float waveCurveAngle = 12f;
    [SerializeField] private float waveBulletLifeTime = 8f;

    [Header("Destructo Disc Attack")]
    [SerializeField] private Sprite destructoDiscSprite;
    [SerializeField] private AudioClip destructoDiscSound;
    [SerializeField] private float destructoChargeTime = 4f;
    [SerializeField] private float destructoHeightAboveBoss = 2.2f;
    [SerializeField] private float destructoChargeSize = 8f;
    [SerializeField] private float destructoProjectileSize = 8f;
    [SerializeField] private float destructoProjectileSpeed = 4f;
    [SerializeField] private float destructoLifeTime = 6f;
    [SerializeField] private float destructoSpinSpeed = 0f;
    [SerializeField] private float destructosound = 5f;

    [Header("Bouncing Heart Rain")]
    [SerializeField] private int bouncingHeartCount = 12;
    [SerializeField] private float bouncingHeartSpawnInterval = 0.65f;
    [SerializeField] private float bouncingHeartSize = 0.3f;
    [SerializeField] private float bouncingHeartGravity = 0.35f;
    [SerializeField] private float bouncingHeartSideSpeed = 1.2f;
    [SerializeField] private float bouncingHeartLifeTime = 15f;

    [Header("Straight Heart Lane Attack")]
    [SerializeField] private float straightHeartInterval = 2f;
    [SerializeField] private float straightHeartSpeed = 7f;
    [SerializeField] private float straightHeartSize = 0.45f;
    [SerializeField] private float straightHeartLaneOffsetY = 1f;
    [SerializeField] private float straightHeartLifeTime = 8f;

    [Header("Spirit Bomb Phase Attack")]
    [SerializeField] private float spiritBombChargeTime = 5f;
    [SerializeField] private float spiritBombHeightAboveBoss = 2.5f;
    [SerializeField] private float spiritBombSize = 12f;
    [SerializeField] private float spiritBombSpeed = 6f;
    [SerializeField] private float spiritBombLifeTime = 8f;

    [Header("Camera Bounds")]
    [SerializeField] private float cameraEdgePadding = 0.15f;

    private PlayerController player;
    private LineRenderer laserLine;
    private Animator bossAnimator;
    private AudioSource audioSource;
    private CameraShake cameraShake;
    private BossHP bossHealth;
    private bool attacking;
    private readonly List<EnemyShoot> pausedEnemyShoot = new List<EnemyShoot>();
    private SpriteRenderer bossRenderer;

    private void Awake()
    {
        bossAnimator = GetComponent<Animator>();
        bossRenderer = GetComponent<SpriteRenderer>();
        bossHealth = GetComponent<BossHP>();
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        if (Camera.main != null)
            cameraShake = Camera.main.GetComponent<CameraShake>();

        if (firePoint == null)
        {
            Transform child = transform.Find("FirePoint");
            firePoint = child != null ? child : transform;
        }

        GameObject laserObject = new GameObject("BossLaser");
        laserObject.transform.SetParent(transform, false);
        laserLine = laserObject.AddComponent<LineRenderer>();
        laserLine.useWorldSpace = true;
        laserLine.positionCount = 2;
        laserLine.textureMode = LineTextureMode.Stretch;
        laserLine.alignment = LineAlignment.View;
        laserLine.numCapVertices = 4;
        laserLine.sortingOrder = 20;
        laserLine.material = new Material(Shader.Find("Sprites/Default"));
        laserLine.enabled = false;

        IgnoreBottomCollision();
    }

    private void LateUpdate()
    {
        KeepBossInsideCamera();
    }

    private void KeepBossInsideCamera()
    {
        Camera camera = Camera.main;
        if (camera == null || !camera.orthographic)
            return;

        float verticalExtent = camera.orthographicSize;
        float horizontalExtent = verticalExtent * camera.aspect;
        Vector3 cameraPosition = camera.transform.position;
        Vector3 bossPosition = transform.position;
        Vector3 spriteExtent = bossRenderer != null
            ? bossRenderer.bounds.extents
            : Vector3.zero;

        float minX = cameraPosition.x - horizontalExtent + spriteExtent.x + cameraEdgePadding;
        float maxX = cameraPosition.x + horizontalExtent - spriteExtent.x - cameraEdgePadding;
        float minY = cameraPosition.y - verticalExtent + spriteExtent.y + cameraEdgePadding;
        float maxY = cameraPosition.y + verticalExtent - spriteExtent.y - cameraEdgePadding;

        bossPosition.x = minX <= maxX ? Mathf.Clamp(bossPosition.x, minX, maxX) : cameraPosition.x;
        bossPosition.y = minY <= maxY ? Mathf.Clamp(bossPosition.y, minY, maxY) : cameraPosition.y;
        transform.position = bossPosition;
    }

    private void IgnoreBottomCollision()
    {
        GameObject bottom = GameObject.Find("Bottom");
        if (bottom == null)
            return;

        Collider2D[] bossColliders = GetComponentsInChildren<Collider2D>();
        Collider2D[] bottomColliders = bottom.GetComponentsInChildren<Collider2D>();

        foreach (Collider2D bossCollider in bossColliders)
        {
            foreach (Collider2D bottomCollider in bottomColliders)
            {
                if (bossCollider != null && bottomCollider != null)
                    Physics2D.IgnoreCollision(bossCollider, bottomCollider, true);
            }
        }
    }

    private IEnumerator Start()
    {
        // Finish the full intro chain before any attack can disable or move the Animator.
        if (bossAnimator != null)
        {
            while (!bossAnimator.GetCurrentAnimatorStateInfo(0).IsName("BossBigMovie") ||
                   bossAnimator.IsInTransition(0))
            {
                yield return null;
            }
        }

        if (firstAttackExtraDelay > 0f)
            yield return new WaitForSeconds(firstAttackExtraDelay);

        bool spiritBombUsed = false;
        while (enabled)
        {
            yield return LaserAttack();
            yield return WaveBulletAttack();
            yield return DestructoDiscAttack();

            if (!spiritBombUsed && bossHealth != null && bossHealth.IsPhaseTwo)
            {
                yield return SpiritBombAttack();
                spiritBombUsed = true;
            }
            else
            {
                yield return BouncingHeartRainAttack();

                if (!spiritBombUsed && bossHealth != null)
                {
                    if (!bossHealth.IsPhaseTwo)
                        yield return StraightHeartLaneAttackOnce();

                    if (bossHealth.IsPhaseTwo)
                    {
                        yield return SpiritBombAttack();
                        spiritBombUsed = true;
                    }
                }
            }

            yield return new WaitForSeconds(attackInterval);
        }
    }

    private IEnumerator LaserAttack()
    {
        if (attacking)
            yield break;

        attacking = true;
        FindPlayer();

        if (player == null || player.IsDead)
        {
            attacking = false;
            yield break;
        }

        if (bossAnimator != null)
            bossAnimator.enabled = false;

        for (int shot = 0; shot < lasersPerCycle; shot++)
        {
            FindPlayer();
            if (player == null || player.IsDead)
                break;

            float alignmentTime = shot == 0 ? firstAlignmentDuration : nextAlignmentDuration;
            float targetY = player.transform.position.y;
            float elapsed = 0f;

            // Like the original fight, Kitty follows the player's elevation first.
            while (elapsed < alignmentTime)
            {
                if (player == null || player.IsDead)
                    break;

                targetY = player.transform.position.y;
                Vector3 bossPosition = transform.position;
                bossPosition.y = Mathf.MoveTowards(
                    bossPosition.y,
                    targetY,
                    alignmentSpeed * Time.deltaTime);
                transform.position = bossPosition;
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (player == null || player.IsDead)
                break;

            Vector2 origin = firePoint.position;
            Vector2 direction = player.transform.position.x < origin.x
                ? Vector2.left
                : Vector2.right;

            // The first shot charges for the full original sound using a harmless white guide line.
            float currentWarningDuration = warningDuration;
            if (shot == 0)
            {
                PlaySound(firstEnergyWaveSound);
                currentWarningDuration = firstBeamChargeTime;
            }

            // The white guide line is visual only. Damage starts after this wait.
            SetLaser(origin, direction, warningThickness, new Color(1f, 1f, 1f, 0.9f));
            yield return new WaitForSeconds(currentWarningDuration);

            // The first full sound continues into the beam. Chained shots use the cut sound.
            if (shot > 0)
                PlaySound(chainedEnergyWaveSound);
            ShakeCamera(shot);

            float currentLaserDuration;
            if (shot == 0 && firstEnergyWaveSound != null)
                currentLaserDuration = Mathf.Max(0.1f, firstEnergyWaveSound.length - firstBeamChargeTime);
            else
                currentLaserDuration = chainedEnergyWaveSound != null
                    ? chainedEnergyWaveSound.length
                    : laserDuration;

            float recoilDirection = 0f;
            float recoilTargetY = transform.position.y;
            if (shot == 0)
            {
                recoilDirection = GetRecoilDirection(transform.position.y);
                recoilTargetY += recoilDirection * recoilDistance;
            }

            elapsed = 0f;
            while (elapsed < currentLaserDuration)
            {
                if (shot == 0)
                {
                    Vector3 recoilPosition = transform.position;
                    recoilPosition.y = Mathf.MoveTowards(
                        recoilPosition.y,
                        recoilTargetY,
                        recoilSpeed * Time.deltaTime);
                    transform.position = recoilPosition;
                }

                origin = firePoint.position;
                float grow = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, laserGrowDuration));
                grow = Mathf.SmoothStep(0f, 1f, grow);
                float fade = Mathf.Clamp01((currentLaserDuration - elapsed) / Mathf.Max(0.01f, laserGrowDuration));
                fade = Mathf.SmoothStep(0f, 1f, fade);
                float sizeEnvelope = Mathf.Min(grow, fade);
                float pulse = 1f + Mathf.Sin(elapsed * 24f) * 0.06f;
                float animatedWidth = Mathf.Lerp(warningThickness, laserThickness, sizeEnvelope) * pulse;
                float animatedAlpha = (laserAlpha + Mathf.Sin(elapsed * 18f) * 0.08f) * fade;

                SetLaser(
                    origin,
                    direction,
                    animatedWidth,
                    new Color(1f, 1f, 1f, Mathf.Clamp01(animatedAlpha)));
                CheckPlayerHit(origin, direction, animatedWidth);
                elapsed += Time.deltaTime;
                yield return null;
            }

            laserLine.enabled = false;
            if (shot < lasersPerCycle - 1)
            {
                // Alignment and warning are part of the requested time between actual beams.
                float remainingDelay = Mathf.Max(
                    0f,
                    chainedShotInterval - nextAlignmentDuration - warningDuration);
                yield return new WaitForSeconds(remainingDelay);
            }
        }

        laserLine.enabled = false;
        if (bossAnimator != null)
            bossAnimator.enabled = true;
        attacking = false;
    }

    private IEnumerator WaveBulletAttack()
    {
        if (waveBulletSprite == null || player == null || player.IsDead)
            yield break;

        EnemyShoot[] enemyShootScripts = FindObjectsByType<EnemyShoot>();
        pausedEnemyShoot.Clear();
        foreach (EnemyShoot enemyShoot in enemyShootScripts)
        {
            if (enemyShoot != null && enemyShoot.enabled)
            {
                pausedEnemyShoot.Add(enemyShoot);
                enemyShoot.enabled = false;
            }
        }

        for (int wave = 0; wave < waveCount; wave++)
        {
            Vector2 origin = firePoint.position;
            bool fireLeft = player.transform.position.x < origin.x;
            float forwardAngle = fireLeft ? 180f : 0f;
            float curveOffset = Mathf.Sin(wave * 0.9f) * waveCurveAngle;

            for (int bullet = 0; bullet < bulletsPerWave; bullet++)
            {
                float t = bulletsPerWave <= 1 ? 0.5f : bullet / (float)(bulletsPerWave - 1);
                float spread = Mathf.Lerp(-waveSpreadAngle, waveSpreadAngle, t);
                float angle = (forwardAngle + spread + curveOffset) * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                SpawnWaveBullet(origin, direction);
            }

            yield return new WaitForSeconds(waveInterval);
        }

        // Keep normal enemy shooting paused until every wave bullet leaves the screen.
        while (BossWaveProjectile.ActiveCount > 0)
            yield return null;

        ResumeEnemyShoot();
    }

    private void ResumeEnemyShoot()
    {
        foreach (EnemyShoot enemyShoot in pausedEnemyShoot)
        {
            if (enemyShoot != null)
                enemyShoot.enabled = true;
        }

        pausedEnemyShoot.Clear();
    }

    private void SpawnWaveBullet(Vector2 origin, Vector2 direction)
    {
        GameObject bullet = new GameObject("Boss Wave Bullet");
        bullet.transform.position = origin;
        bullet.transform.localScale = Vector3.one * waveBulletSize;

        SpriteRenderer renderer = bullet.AddComponent<SpriteRenderer>();
        renderer.sprite = waveBulletSprite;
        renderer.sortingOrder = 15;

        CircleCollider2D collider = bullet.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.45f;

        Rigidbody2D body = bullet.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;

        BossWaveProjectile projectile = bullet.AddComponent<BossWaveProjectile>();
        projectile.Initialize(direction * waveBulletSpeed, waveBulletLifeTime);
    }

    private IEnumerator DestructoDiscAttack()
    {
        if (destructoDiscSprite == null)
            yield break;

        FindPlayer();
        if (player == null || player.IsDead || Camera.main == null)
            yield break;

        if (bossAnimator != null)
            bossAnimator.enabled = false;

        GameObject chargeDisc = CreateDestructoDisc("Charging Destructo Disc");
        chargeDisc.transform.position = transform.position + Vector3.up * destructoHeightAboveBoss;
        Vector3 finalChargeScale = GetDiscScale(destructoChargeSize);
        PlaySound(destructoDiscSound);

        float elapsed = 0f;
        while (elapsed < destructoChargeTime)
        {
            float grow = Mathf.SmoothStep(0f, 1f, elapsed / Mathf.Max(0.01f, destructoChargeTime));
            chargeDisc.transform.position = transform.position + Vector3.up * destructoHeightAboveBoss;
            chargeDisc.transform.localScale = finalChargeScale * grow;
            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(chargeDisc);

        for (int volley = 0; volley < 1; volley++)
        {
            FindPlayer();
            if (player == null || player.IsDead)
                break;

            Vector2 origin = transform.position + Vector3.up * destructoHeightAboveBoss;
            Vector2 baseDirection = ((Vector2)player.transform.position - origin).normalized;
            SpawnDestructoProjectile(origin, baseDirection);
        }

        // Do not start the next pattern until the disc is completely outside the camera.
        while (DestructoDiscProjectile.ActiveCount > 0)
            yield return null;

        if (bossAnimator != null)
            bossAnimator.enabled = true;
    }

    private GameObject CreateDestructoDisc(string objectName)
    {
        GameObject disc = new GameObject(objectName);
        SpriteRenderer renderer = disc.AddComponent<SpriteRenderer>();
        renderer.sprite = destructoDiscSprite;
        renderer.sortingOrder = 22;
        return disc;
    }

    private Vector3 GetDiscScale(float targetWidth)
    {
        float spriteWidth = Mathf.Max(0.01f, destructoDiscSprite.bounds.size.x);
        float scale = targetWidth / spriteWidth;
        return Vector3.one * scale;
    }

    private void SpawnDestructoProjectile(Vector2 origin, Vector2 direction)
    {
        GameObject disc = CreateDestructoDisc("Destructo Disc");
        disc.transform.position = origin;
        disc.transform.localScale = GetDiscScale(destructoProjectileSize);

        CircleCollider2D collider = disc.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.42f;

        Rigidbody2D body = disc.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;

        DestructoDiscProjectile projectile = disc.AddComponent<DestructoDiscProjectile>();
        projectile.Initialize(
            direction * destructoProjectileSpeed,
            destructoLifeTime,
            destructoSpinSpeed);
    }

    private IEnumerator BouncingHeartRainAttack()
    {
        if (waveBulletSprite == null || Camera.main == null)
            yield break;

        PauseEnemyShoot();

        GameObject bottom = GameObject.Find("Bottom");
        Collider2D bottomCollider = bottom != null ? bottom.GetComponent<Collider2D>() : null;

        float cameraHalfWidth = Camera.main.orthographicSize * Camera.main.aspect;
        float cameraMinX = Camera.main.transform.position.x - cameraHalfWidth + 0.5f;
        float cameraMaxX = Camera.main.transform.position.x + cameraHalfWidth - 0.5f;
        float minX = bottomCollider != null
            ? Mathf.Max(cameraMinX, bottomCollider.bounds.min.x)
            : cameraMinX;
        float maxX = bottomCollider != null
            ? Mathf.Min(cameraMaxX, bottomCollider.bounds.max.x)
            : cameraMaxX;
        float spawnY = Camera.main.transform.position.y + Camera.main.orthographicSize + 1f;

        for (int i = 0; i < bouncingHeartCount; i++)
        {
            float x = Random.Range(minX, maxX);
            SpawnBouncingHeart(new Vector2(x, spawnY));
            yield return new WaitForSeconds(bouncingHeartSpawnInterval);
        }

        // Resume all other attacks only after the final heart is gone.
        while (BouncingHeartProjectile.ActiveCount > 0)
            yield return null;

        ResumeEnemyShoot();
    }

    private void PauseEnemyShoot()
    {
        pausedEnemyShoot.Clear();
        EnemyShoot[] enemyShootScripts = FindObjectsByType<EnemyShoot>();
        foreach (EnemyShoot enemyShoot in enemyShootScripts)
        {
            if (enemyShoot != null && enemyShoot.enabled)
            {
                pausedEnemyShoot.Add(enemyShoot);
                enemyShoot.enabled = false;
            }
        }
    }

    private void SpawnBouncingHeart(Vector2 position)
    {
        GameObject heart = new GameObject("Bouncing Heart");
        heart.transform.position = position;
        heart.transform.localScale = Vector3.one * bouncingHeartSize;

        SpriteRenderer renderer = heart.AddComponent<SpriteRenderer>();
        renderer.sprite = waveBulletSprite;
        renderer.sortingOrder = 16;

        CircleCollider2D collider = heart.AddComponent<CircleCollider2D>();
        collider.radius = 0.42f;
        collider.isTrigger = true;

        Rigidbody2D body = heart.AddComponent<Rigidbody2D>();
        body.gravityScale = bouncingHeartGravity;
        body.linearDamping = 0.15f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.linearVelocity = new Vector2(
            Random.Range(-bouncingHeartSideSpeed, bouncingHeartSideSpeed),
            0f);

        BouncingHeartProjectile projectile = heart.AddComponent<BouncingHeartProjectile>();
        projectile.Initialize(bouncingHeartLifeTime);
    }

    private IEnumerator StraightHeartLaneAttackOnce()
    {
        if (waveBulletSprite == null || bossHealth == null)
            yield break;

        PauseEnemyShoot();

        Transform[] lanes =
        {
            FindTransformByName("Square (3)"),
            FindTransformByName("Square (2)"),
            FindTransformByName("Square (1)")
        };

        foreach (Transform lane in lanes)
        {
            if (bossHealth == null || bossHealth.IsPhaseTwo)
                break;

            if (lane != null)
            {
                Vector2 origin = new Vector2(
                    firePoint != null ? firePoint.position.x : transform.position.x,
                    lane.position.y + straightHeartLaneOffsetY);
                SpawnStraightHeart(origin, Vector2.left);
            }

            float elapsed = 0f;
            while (elapsed < straightHeartInterval)
            {
                if (bossHealth == null || bossHealth.IsPhaseTwo)
                    break;

                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        // The lane volley is one cycle only. If phase two has not started, the main
        // boss loop continues from LaserAttack (energy wave) instead of repeating here.
        StraightHeartProjectile.DestroyAllActive();
        ResumeEnemyShoot();
    }

    private void SpawnStraightHeart(Vector2 position, Vector2 direction)
    {
        GameObject heart = new GameObject("Straight Lane Heart");
        heart.transform.position = position;
        heart.transform.localScale = Vector3.one * straightHeartSize;

        SpriteRenderer renderer = heart.AddComponent<SpriteRenderer>();
        renderer.sprite = waveBulletSprite;
        renderer.sortingOrder = 18;

        CircleCollider2D collider = heart.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.42f;

        Rigidbody2D body = heart.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;

        StraightHeartProjectile projectile = heart.AddComponent<StraightHeartProjectile>();
        projectile.Initialize(direction * straightHeartSpeed, straightHeartLifeTime);
    }

    private static Transform FindTransformByName(string objectName)
    {
        GameObject found = GameObject.Find(objectName);
        return found != null ? found.transform : null;
    }

    private IEnumerator SpiritBombAttack()
    {
        if (waveBulletSprite == null)
            yield break;

        FindPlayer();
        if (player == null || player.IsDead)
            yield break;

        PauseEnemyShoot();

        if (bossAnimator != null)
            bossAnimator.enabled = false;

        GameObject spiritBomb = new GameObject("Spirit Bomb");
        SpriteRenderer renderer = spiritBomb.AddComponent<SpriteRenderer>();
        renderer.sprite = waveBulletSprite;
        renderer.sortingOrder = 24;

        Vector3 chargePosition = transform.position + Vector3.up * spiritBombHeightAboveBoss;
        float spriteWidth = Mathf.Max(0.01f, waveBulletSprite.bounds.size.x);
        Vector3 fullScale = Vector3.one * (spiritBombSize / spriteWidth);
        spiritBomb.transform.position = chargePosition;
        spiritBomb.transform.localScale = Vector3.zero;

        float elapsed = 0f;
        while (elapsed < spiritBombChargeTime)
        {
            float grow = Mathf.SmoothStep(0f, 1f, elapsed / Mathf.Max(0.01f, spiritBombChargeTime));
            spiritBomb.transform.position = transform.position + Vector3.up * spiritBombHeightAboveBoss;
            spiritBomb.transform.localScale = fullScale * grow;
            elapsed += Time.deltaTime;
            yield return null;
        }

        Vector2 origin = spiritBomb.transform.position;
        Vector2 direction = ((Vector2)player.transform.position - origin).normalized;

        CircleCollider2D collider = spiritBomb.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.42f;

        Rigidbody2D body = spiritBomb.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;

        DestructoDiscProjectile projectile = spiritBomb.AddComponent<DestructoDiscProjectile>();
        projectile.Initialize(direction * spiritBombSpeed, spiritBombLifeTime, 0f);

        while (DestructoDiscProjectile.ActiveCount > 0)
            yield return null;

        ResumeEnemyShoot();

        if (bossAnimator != null)
            bossAnimator.enabled = true;
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip, energyWaveVolume);
    }

    private void ShakeCamera(int shot)
    {
        if (cameraShake != null)
        {
            float duration = shot == 0 ? firstShakeDuration : chainedShakeDuration;
            float strength = shot == 0 ? firstShakeStrength : chainedShakeStrength;
            cameraShake.Shake(duration, strength);
        }
    }

    private float GetRecoilDirection(float currentY)
    {
        if (currentY > middleHeight + middleRange)
            return -1f;

        if (currentY < middleHeight - middleRange)
            return 1f;

        return Random.value < 0.5f ? -1f : 1f;
    }

    private void SetLaser(Vector2 origin, Vector2 direction, float width, Color color)
    {
        laserLine.enabled = true;
        laserLine.startWidth = width;
        laserLine.endWidth = width;
        laserLine.startColor = color;
        laserLine.endColor = color;
        laserLine.SetPosition(0, origin);
        laserLine.SetPosition(1, origin + direction * laserLength);
    }

    private void CheckPlayerHit(Vector2 origin, Vector2 direction, float currentWidth)
    {
        RaycastHit2D[] hits = Physics2D.CircleCastAll(
            origin,
            currentWidth * 0.5f,
            direction,
            laserLength);

        foreach (RaycastHit2D hit in hits)
        {
            PlayerController hitPlayer = hit.collider.GetComponentInParent<PlayerController>();
            if (hitPlayer != null && !hitPlayer.IsDead)
            {
                hitPlayer.Die();
                return;
            }
        }
    }

    private void FindPlayer()
    {
        if (player == null || player.IsDead)
            player = FindAnyObjectByType<PlayerController>();
    }

    private void OnDisable()
    {
        ResumeEnemyShoot();
        StraightHeartProjectile.DestroyAllActive();
        attacking = false;
        if (laserLine != null)
            laserLine.enabled = false;
        if (bossAnimator != null)
            bossAnimator.enabled = true;
    }
}
