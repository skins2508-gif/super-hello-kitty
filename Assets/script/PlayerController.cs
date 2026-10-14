using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 6f;
    public float jumpForce = 8f;
    public int maxJump = 2;

    private int jumpCount;
    private Rigidbody2D rb;
    private float moveInput;
    private bool isDead;
    private int currentHealth;
    private Transform healthDisplay;
    private float nextDamageTime;
    private float fallDeathY = float.NegativeInfinity;

    [Min(0f)] public float damageInvincibilityDuration = 1f;
    [Min(0f)] public float bottomDeathOffset = 1f;

    public bool IsDead => isDead;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        SetMaxHealth(GameModeSettings.PlayerMaxHealth);

        GameObject bottom = GameObject.Find("Bottom");
        if (bottom != null)
        {
            Collider2D bottomCollider = bottom.GetComponent<Collider2D>();
            fallDeathY = bottomCollider != null
                ? bottomCollider.bounds.min.y - bottomDeathOffset
                : bottom.transform.position.y - bottomDeathOffset;
        }
    }

    void Update()
    {
        if (isDead)
            return;

        // Only the lower boundary is lethal. Going above the top of the camera is allowed.
        if (transform.position.y < fallDeathY)
        {
            currentHealth = 1;
            Die();
            return;
        }

        if (Keyboard.current == null)
            return;

        moveInput = 0f;
        if (Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed)
            moveInput = -1f;
        if (Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed)
            moveInput = 1f;

        if (Keyboard.current.zKey.wasPressedThisFrame && jumpCount < maxJump)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpCount++;
        }
    }

    void FixedUpdate()
    {
        if (!isDead)
            rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
            jumpCount = 0;
    }

    public void Die()
    {
        if (isDead)
            return;

        if (Time.time < nextDamageTime)
            return;

        currentHealth--;
        nextDamageTime = Time.time + damageInvincibilityDuration;
        UpdateHealthDisplay();
        if (currentHealth > 0)
        {
            Debug.Log($"Player Hit - {currentHealth} HP left");
            return;
        }

        isDead = true;
        moveInput = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;

        foreach (PlayerShoot shootScript in GetComponentsInChildren<PlayerShoot>())
            shootScript.enabled = false;

        Debug.Log("Player Dead");
        CreateRedDeathBurst(transform.position);
        gameObject.SetActive(false);
        GameOverScreen.Show();
    }

    private static void CreateRedDeathBurst(Vector3 position)
    {
        GameObject effect = new GameObject("Player Red Death Burst");
        effect.transform.position = position;

        ParticleSystem particles = effect.AddComponent<ParticleSystem>();
        // A newly added ParticleSystem can already be playing. Duration cannot be
        // changed while it is playing, so configure it from a stopped state.
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.35f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 11f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.32f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.65f, 0f, 0f), new Color(1f, 0.12f, 0.12f));
        main.gravityModifier = 1.4f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 50;
        main.stopAction = ParticleSystemStopAction.Destroy;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 42) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.28f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.08f, 0.08f), 0f),
                new GradientColorKey(new Color(0.45f, 0f, 0f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 0.65f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = fade;

        ParticleSystemRenderer particleRenderer = effect.GetComponent<ParticleSystemRenderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            Material material = new Material(shader);
            particleRenderer.material = material;
            Destroy(material, 3f);
        }

        particles.Play();
        Destroy(effect, 3f);
    }

    public void SetMaxHealth(int maxHealth)
    {
        currentHealth = Mathf.Max(1, maxHealth);
        CreateHealthDisplay();
        UpdateHealthDisplay();
    }

    private void CreateHealthDisplay()
    {
        if (healthDisplay != null)
            return;

        GameObject canvasObject = new GameObject("Player Health UI");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1400f, 1152f);

        GameObject row = new GameObject("Hearts", typeof(RectTransform));
        row.transform.SetParent(canvasObject.transform, false);
        RectTransform rect = row.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(35f, -30f);
        rect.sizeDelta = new Vector2(300f, 70f);

        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        healthDisplay = row.transform;
    }

    private void UpdateHealthDisplay()
    {
        if (healthDisplay == null)
            return;

        for (int i = healthDisplay.childCount - 1; i >= 0; i--)
            Destroy(healthDisplay.GetChild(i).gameObject);

        for (int i = 0; i < currentHealth; i++)
        {
            GameObject heart = new GameObject("Heart " + (i + 1), typeof(RectTransform));
            heart.transform.SetParent(healthDisplay, false);
            Image image = heart.AddComponent<Image>();
            Texture2D heartTexture = Resources.Load<Texture2D>("PlayerHeart");
            if (heartTexture != null)
            {
                image.sprite = Sprite.Create(
                    heartTexture,
                    new Rect(0f, 0f, heartTexture.width, heartTexture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
            }
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;
            RectTransform rect = heart.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(62f, 62f);
        }
    }
}
