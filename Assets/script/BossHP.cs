using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BossHP : MonoBehaviour
{
    public Slider hpSlider;
    public Sprite phaseTwoSprite;
    public AudioClip phaseTwoSound;
    [Range(0f, 1f)] public float phaseTwoSoundVolume = 1f;
    public Sprite endingSprite;
    [Min(0f)] public float endingFadeDuration = 1f;
    public AudioClip endingSound;
    [Range(0f, 1f)] public float endingSoundVolume = 1f;

    [Header("Boss Defeat Sequence")]
    [Min(0f)] public float endingDelay = 5f;
    [Min(0f)] public float defeatFallGravity = 0.35f;
    [Min(0.1f)] public float endBossWidthMultiplier = 0.2f;

    public float maxHp = 1000f;
    private float currentHp;
    private RectTransform fillRect;
    private SpriteRenderer bossRenderer;
    private bool phaseTwoActivated;
    private AudioSource audioSource;
    private bool defeatSequenceStarted;

    public bool IsPhaseTwo => phaseTwoActivated;

    private void Start()
    {
        maxHp = Mathf.Max(1f, GameModeSettings.BossMaxHealth);
        currentHp = Mathf.Max(1f, maxHp);
        bossRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        if (hpSlider != null)
        {
            hpSlider.minValue = 0f;
            hpSlider.maxValue = currentHp;
            hpSlider.value = currentHp;
            fillRect = hpSlider.fillRect;
            UpdateHealthUI();
        }
    }

    public void TakeDamage(float damage)
    {
        if (damage <= 0f || currentHp <= 0f || defeatSequenceStarted)
            return;

        currentHp = Mathf.Max(0f, currentHp - damage);
        UpdateHealthUI();
        UpdatePhaseImage();

        if (currentHp <= 0f)
            Die();
    }

    private void UpdatePhaseImage()
    {
        if (phaseTwoActivated || currentHp > maxHp * 0.5f)
            return;

        phaseTwoActivated = true;
        if (bossRenderer != null && phaseTwoSprite != null)
            bossRenderer.sprite = phaseTwoSprite;

        if (audioSource != null && phaseTwoSound != null)
            audioSource.PlayOneShot(phaseTwoSound, phaseTwoSoundVolume);
    }

    private void UpdateHealthUI()
    {
        if (hpSlider == null)
            return;

        hpSlider.SetValueWithoutNotify(currentHp);
        if (fillRect != null)
        {
            float ratio = currentHp / hpSlider.maxValue;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(ratio, 1f);
            fillRect.anchoredPosition = Vector2.zero;
            fillRect.sizeDelta = Vector2.zero;
        }
    }

    private void Die()
    {
        if (defeatSequenceStarted)
            return;

        defeatSequenceStarted = true;
        Debug.Log("Boss Dead");
        StartCoroutine(DefeatSequence());
    }

    private IEnumerator DefeatSequence()
    {
        foreach (BackgroundMusic backgroundMusic in FindObjectsByType<BackgroundMusic>())
            backgroundMusic.StopMusic();

        Boss boss = GetComponent<Boss>();
        if (boss != null)
            boss.enabled = false;

        foreach (EnemyShoot shoot in GetComponentsInChildren<EnemyShoot>())
            shoot.enabled = false;
        foreach (Collider2D bossCollider in GetComponentsInChildren<Collider2D>())
            bossCollider.enabled = false;

        if (hpSlider != null)
            hpSlider.gameObject.SetActive(false);

        Texture2D endBossTexture = Resources.Load<Texture2D>("EndBoss");
        if (bossRenderer != null && endBossTexture != null)
        {
            float originalWidth = Mathf.Max(0.1f, bossRenderer.bounds.size.x);
            bossRenderer.sprite = Sprite.Create(
                endBossTexture,
                new Rect(0f, 0f, endBossTexture.width, endBossTexture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            float newWidth = Mathf.Max(0.1f, bossRenderer.bounds.size.x);
            transform.localScale *= originalWidth * endBossWidthMultiplier / newWidth;
            bossRenderer.sortingOrder = 50;
        }

        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body == null)
            body = gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = defeatFallGravity;
        body.linearVelocity = new Vector2(0f, -0.35f);
        body.angularVelocity = 0f;

        CreateDefeatParticles();
        yield return new WaitForSeconds(Mathf.Max(0f, endingDelay));

        EndingScreen.Show(endingSprite, endingFadeDuration, endingSound, endingSoundVolume, true);
        Destroy(gameObject);
    }

    private void CreateDefeatParticles()
    {
        GameObject effect = new GameObject("Boss Defeat Explosions");
        effect.transform.SetParent(transform, false);

        ParticleSystem particles = effect.AddComponent<ParticleSystem>();
        // Configure duration only after stopping the auto-playing particle system.
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = particles.main;
        main.duration = Mathf.Max(0.1f, endingDelay);
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.3f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 7f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.45f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.08f, 0.02f),
            new Color(1f, 0.85f, 0.1f));
        main.gravityModifier = 0.3f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 180;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 28f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.7f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.9f, 0.2f), 0f),
                new GradientColorKey(new Color(1f, 0.05f, 0f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = fade;

        ParticleSystemRenderer particleRenderer = effect.GetComponent<ParticleSystemRenderer>();
        particleRenderer.sortingOrder = 55;
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader != null)
            particleRenderer.material = new Material(shader);

        particles.Play();
    }
}
