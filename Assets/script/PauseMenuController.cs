using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    private const float ReferenceWidth = 1448f;
    private const float ReferenceHeight = 1086f;
    private static PauseMenuController instance;

    private GameObject menuRoot;
    private GameObject artwork;
    private GameObject navigationFrame;
    private Text countdownText;
    private Button[] buttons;
    private int selectedIndex;
    private bool paused;
    private bool countingDown;
    private bool showMainMenuAfterLoad;
    private int openedFrame = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateController()
    {
        EnsureExists();
    }

    public static void EnsureExists()
    {
        if (instance != null)
            return;

        PauseMenuController existing = FindAnyObjectByType<PauseMenuController>();
        if (existing != null)
        {
            instance = existing;
            return;
        }

        GameObject controller = new GameObject("Pause Menu Controller");
        instance = controller.AddComponent<PauseMenuController>();
        DontDestroyOnLoad(controller);
    }

    public static void OpenFromGame()
    {
        EnsureExists();
        if (instance != null && !instance.paused && !instance.countingDown && instance.CanPauseGame())
            instance.OpenPauseMenu();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        BuildUI();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!showMainMenuAfterLoad)
            return;

        showMainMenuAfterLoad = false;
        StartScreenController.ShowStartScreen();
    }

    private void Update()
    {
        if (Keyboard.current == null || countingDown)
            return;

        // Listen for ESC here instead of relying on PlayerController. The pause menu
        // must remain available even when the player component is disabled or inactive.
        if (!paused)
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame && CanPauseGame())
                OpenPauseMenu();
            return;
        }

        // Do not treat the ESC that opened the menu as an immediate close command.
        if (Time.frameCount == openedFrame)
            return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            StartResumeCountdown();
            return;
        }

        if (Keyboard.current.upArrowKey.wasPressedThisFrame ||
            Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            selectedIndex = (selectedIndex + buttons.Length - 1) % buttons.Length;
            UpdateSelectionFrame();
        }
        else if (Keyboard.current.downArrowKey.wasPressedThisFrame ||
                 Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            selectedIndex = (selectedIndex + 1) % buttons.Length;
            UpdateSelectionFrame();
        }

        if (Keyboard.current.zKey.wasPressedThisFrame ||
            Keyboard.current.xKey.wasPressedThisFrame ||
            Keyboard.current.enterKey.wasPressedThisFrame ||
            Keyboard.current.numpadEnterKey.wasPressedThisFrame)
        {
            buttons[selectedIndex].onClick.Invoke();
        }
    }

    private bool CanPauseGame()
    {
        return FindAnyObjectByType<StartScreenController>() == null &&
               FindAnyObjectByType<GameOverScreen>() == null &&
               FindAnyObjectByType<EndingScreen>() == null;
    }

    private void BuildUI()
    {
        menuRoot = new GameObject("Hello Kitty Pause Menu");
        menuRoot.transform.SetParent(transform, false);

        Canvas canvas = menuRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;
        canvas.overrideSorting = true;

        CanvasScaler scaler = menuRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        menuRoot.AddComponent<GraphicRaycaster>();

        Texture2D pauseTexture = Resources.Load<Texture2D>("HelloKittyPause");
        artwork = CreateUIObject("Pause Artwork", menuRoot.transform);
        Image artworkImage = artwork.AddComponent<Image>();
        if (pauseTexture != null)
        {
            artworkImage.sprite = Sprite.Create(
                pauseTexture,
                new Rect(0f, 0f, pauseTexture.width, pauseTexture.height),
                new Vector2(0.5f, 0.5f),
                100f);
        }
        artworkImage.color = Color.white;
        artworkImage.preserveAspect = false;
        artworkImage.raycastTarget = false;
        Stretch(artwork.GetComponent<RectTransform>());

        buttons = new[]
        {
            CreateHotspot("Main Screen", new Rect(375f, 360f, 700f, 165f), ReturnToMainMenu),
            CreateHotspot("Resume In 3 Seconds", new Rect(375f, 545f, 700f, 170f), StartResumeCountdown),
            CreateHotspot("Restart Game", new Rect(375f, 730f, 700f, 170f), RestartGame)
        };

        navigationFrame = CreateUIObject("Menu Selection Frame", menuRoot.transform);
        Image frameImage = navigationFrame.AddComponent<Image>();
        frameImage.color = new Color(1f, 0.85f, 0.15f, 0.12f);
        frameImage.raycastTarget = false;
        Outline frameOutline = navigationFrame.AddComponent<Outline>();
        frameOutline.effectColor = new Color(1f, 0.95f, 0.25f, 1f);
        frameOutline.effectDistance = new Vector2(7f, -7f);

        GameObject countdownObject = CreateUIObject("Resume Countdown", menuRoot.transform);
        countdownText = countdownObject.AddComponent<Text>();
        countdownText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        countdownText.fontSize = 240;
        countdownText.fontStyle = FontStyle.Bold;
        countdownText.alignment = TextAnchor.MiddleCenter;
        countdownText.color = new Color(1f, 0.2f, 0.55f, 1f);
        countdownText.raycastTarget = false;
        Outline countdownOutline = countdownObject.AddComponent<Outline>();
        countdownOutline.effectColor = Color.white;
        countdownOutline.effectDistance = new Vector2(9f, -9f);
        RectTransform countdownRect = countdownObject.GetComponent<RectTransform>();
        countdownRect.anchorMin = countdownRect.anchorMax = new Vector2(0.5f, 0.5f);
        countdownRect.sizeDelta = new Vector2(700f, 320f);
        countdownObject.SetActive(false);

        UpdateSelectionFrame();
        menuRoot.SetActive(false);
        EnsureEventSystem();
    }

    private Button CreateHotspot(string objectName, Rect pixelRect,
        UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = CreateUIObject(objectName, menuRoot.transform);
        Image hitArea = buttonObject.AddComponent<Image>();
        hitArea.color = new Color(1f, 1f, 1f, 0.001f);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        SetPixelRect(rect, pixelRect);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = hitArea;
        button.onClick.AddListener(action);
        return button;
    }

    private void OpenPauseMenu()
    {
        StopAllCoroutines();
        paused = true;
        countingDown = false;
        openedFrame = Time.frameCount;
        selectedIndex = 1;
        Time.timeScale = 0f;
        AudioListener.pause = true;

        menuRoot.SetActive(true);
        artwork.SetActive(true);
        navigationFrame.SetActive(true);
        countdownText.gameObject.SetActive(false);
        foreach (Button button in buttons)
            button.gameObject.SetActive(true);
        UpdateSelectionFrame();
    }

    private void StartResumeCountdown()
    {
        if (!countingDown)
            StartCoroutine(ResumeCountdown());
    }

    private IEnumerator ResumeCountdown()
    {
        countingDown = true;
        artwork.SetActive(false);
        navigationFrame.SetActive(false);
        foreach (Button button in buttons)
            button.gameObject.SetActive(false);
        countdownText.gameObject.SetActive(true);

        for (int number = 3; number >= 1; number--)
        {
            countdownText.text = number.ToString();
            yield return new WaitForSecondsRealtime(1f);
        }

        countdownText.text = "START!";
        yield return new WaitForSecondsRealtime(0.45f);

        countdownText.gameObject.SetActive(false);
        menuRoot.SetActive(false);
        Time.timeScale = 1f;
        AudioListener.pause = false;
        paused = false;
        countingDown = false;
    }

    private void ReturnToMainMenu()
    {
        GameModeSettings.ReturnToMainMenu();
        showMainMenuAfterLoad = true;
        PrepareForSceneLoad();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void RestartGame()
    {
        GameModeSettings.RequestRestart();
        showMainMenuAfterLoad = false;
        PrepareForSceneLoad();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void PrepareForSceneLoad()
    {
        StopAllCoroutines();
        paused = false;
        countingDown = false;
        countdownText.gameObject.SetActive(false);
        menuRoot.SetActive(false);
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    private void UpdateSelectionFrame()
    {
        if (navigationFrame == null)
            return;

        Rect[] rects =
        {
            new Rect(375f, 360f, 700f, 165f),
            new Rect(375f, 545f, 700f, 170f),
            new Rect(375f, 730f, 700f, 170f)
        };
        SetPixelRect(navigationFrame.GetComponent<RectTransform>(), rects[selectedIndex]);
        navigationFrame.transform.SetAsLastSibling();
        countdownText.transform.SetAsLastSibling();
    }

    private static void SetPixelRect(RectTransform rect, Rect pixelRect)
    {
        rect.anchorMin = new Vector2(
            pixelRect.xMin / ReferenceWidth,
            1f - pixelRect.yMax / ReferenceHeight);
        rect.anchorMax = new Vector2(
            pixelRect.xMax / ReferenceWidth,
            1f - pixelRect.yMin / ReferenceHeight);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject created = new GameObject(name, typeof(RectTransform));
        created.transform.SetParent(parent, false);
        return created;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
    }
}
