using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class StartScreenController : MonoBehaviour
{
    private const float ReferenceWidth = 1402f;
    private const float ReferenceHeight = 1122f;
    private AudioSource dragonBallMusic;
    private AudioSource thunderSound;
    private GameObject mainMenuRoot;
    private GameObject collectionRoot;
    private GameObject selectionFrame;
    private GameObject navigationFrame;
    private bool collectionOpen;
    private int mainMenuSelection;
    private int collectionSelection;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateStartScreen()
    {
        ShowStartScreen();
    }

    public static void ShowStartScreen()
    {
        if (GameModeSettings.ConsumeRestartRequest())
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            return;
        }

        if (FindAnyObjectByType<StartScreenController>() != null)
            return;

        Texture2D texture = Resources.Load<Texture2D>("StartScreen");
        if (texture == null)
        {
            Debug.LogWarning("StartScreen image is missing from Resources.");
            return;
        }

        GameObject root = new GameObject("Start Screen");
        StartScreenController controller = root.AddComponent<StartScreenController>();

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2000;

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        root.AddComponent<GraphicRaycaster>();

        controller.mainMenuRoot = new GameObject("Main Menu", typeof(RectTransform));
        controller.mainMenuRoot.transform.SetParent(root.transform, false);
        Stretch(controller.mainMenuRoot.GetComponent<RectTransform>());

        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f), 100f);
        GameObject background = new GameObject("Start Screen Artwork", typeof(RectTransform));
        background.transform.SetParent(controller.mainMenuRoot.transform, false);
        Image image = background.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = false;
        Stretch(background.GetComponent<RectTransform>());

        controller.AddHotspot(controller.mainMenuRoot.transform, "Easy Mode", new Rect(420f, 450f, 530f, 165f), controller.StartEasy);
        controller.AddHotspot(controller.mainMenuRoot.transform, "Normal Mode", new Rect(420f, 625f, 530f, 160f), controller.StartNormal);
        controller.AddHotspot(controller.mainMenuRoot.transform, "Collection", new Rect(510f, 795f, 350f, 105f), controller.ShowCollection);
        controller.CreateNavigationFrame();
        controller.UpdateNavigationFrame();

        EnsureEventSystem();
        Time.timeScale = 0f;
        AudioListener.pause = true;
        controller.PlayStartScreenSounds();
    }

    private void Update()
    {
        if (collectionOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            HideCollection();
            return;
        }

        if (Keyboard.current == null)
            return;

        bool previous = Keyboard.current.upArrowKey.wasPressedThisFrame ||
                        Keyboard.current.leftArrowKey.wasPressedThisFrame;
        bool next = Keyboard.current.downArrowKey.wasPressedThisFrame ||
                    Keyboard.current.rightArrowKey.wasPressedThisFrame;

        if (collectionOpen)
        {
            if (previous || next)
            {
                collectionSelection = collectionSelection == 0 ? 1 : 0;
                UpdateNavigationFrame();
            }
        }
        else
        {
            if (previous)
            {
                mainMenuSelection = (mainMenuSelection + 2) % 3;
                UpdateNavigationFrame();
            }
            else if (next)
            {
                mainMenuSelection = (mainMenuSelection + 1) % 3;
                UpdateNavigationFrame();
            }
        }

        if (Keyboard.current.zKey.wasPressedThisFrame ||
            Keyboard.current.xKey.wasPressedThisFrame ||
            Keyboard.current.enterKey.wasPressedThisFrame ||
            Keyboard.current.numpadEnterKey.wasPressedThisFrame)
            ActivateKeyboardSelection();
    }

    private void ActivateKeyboardSelection()
    {
        if (collectionOpen)
        {
            if (collectionSelection == 0)
                SelectUnlockedCharacter();
            else
                HideCollection();
            return;
        }

        switch (mainMenuSelection)
        {
            case 0:
                StartEasy();
                break;
            case 1:
                StartNormal();
                break;
            case 2:
                ShowCollection();
                break;
        }
    }

    private void ShowCollection()
    {
        Texture2D texture = Resources.Load<Texture2D>("CollectionScreen");
        if (texture == null)
        {
            Debug.LogWarning("CollectionScreen image is missing from Resources.");
            return;
        }

        mainMenuRoot.SetActive(false);
        collectionOpen = true;

        if (collectionRoot == null)
        {
            collectionRoot = new GameObject("Collection Screen", typeof(RectTransform));
            collectionRoot.transform.SetParent(transform, false);
            Stretch(collectionRoot.GetComponent<RectTransform>());

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), 100f);
            GameObject artwork = new GameObject("Collection Artwork", typeof(RectTransform));
            artwork.transform.SetParent(collectionRoot.transform, false);
            Image image = artwork.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = false;
            Stretch(artwork.GetComponent<RectTransform>());

            AddHotspotWithReference(
                collectionRoot.transform,
                "Unlocked Character",
                new Rect(75f, 170f, 335f, 415f),
                1448f,
                1086f,
                SelectUnlockedCharacter);

            AddHotspotWithReference(
                collectionRoot.transform,
                "Back to Main Menu",
                new Rect(550f, 980f, 355f, 100f),
                1448f,
                1086f,
                HideCollection);

            CreateSelectionFrame();
        }

        collectionRoot.SetActive(true);
        collectionSelection = 0;
        UpdateSelectionFrame();
        UpdateNavigationFrame();
    }

    private void HideCollection()
    {
        collectionOpen = false;
        if (collectionRoot != null)
            collectionRoot.SetActive(false);
        mainMenuRoot.SetActive(true);
        UpdateNavigationFrame();
    }

    private void SelectUnlockedCharacter()
    {
        GameModeSettings.SelectCharacter(0);
        UpdateSelectionFrame();
    }

    private void CreateSelectionFrame()
    {
        selectionFrame = new GameObject("Selected Character Frame", typeof(RectTransform));
        selectionFrame.transform.SetParent(collectionRoot.transform, false);
        Image frame = selectionFrame.AddComponent<Image>();
        frame.color = new Color(1f, 0.85f, 0.15f, 0.2f);
        frame.raycastTarget = false;
        Outline outline = selectionFrame.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.9f, 0.2f, 1f);
        outline.effectDistance = new Vector2(8f, -8f);

        RectTransform rect = selectionFrame.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(75f / 1448f, 1f - 585f / 1086f);
        rect.anchorMax = new Vector2(410f / 1448f, 1f - 170f / 1086f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void UpdateSelectionFrame()
    {
        if (selectionFrame != null)
            selectionFrame.SetActive(GameModeSettings.SelectedCharacter == 0);
    }

    private void CreateNavigationFrame()
    {
        navigationFrame = new GameObject("Keyboard Navigation Frame", typeof(RectTransform));
        navigationFrame.transform.SetParent(transform, false);
        Image image = navigationFrame.AddComponent<Image>();
        image.color = new Color(1f, 0.85f, 0.1f, 0.08f);
        image.raycastTarget = false;
        Outline outline = navigationFrame.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.9f, 0.1f, 1f);
        outline.effectDistance = new Vector2(6f, -6f);
    }

    private void UpdateNavigationFrame()
    {
        if (navigationFrame == null)
            return;

        Rect pixelRect;
        float width;
        float height;
        if (collectionOpen)
        {
            pixelRect = collectionSelection == 0
                ? new Rect(75f, 170f, 335f, 415f)
                : new Rect(550f, 980f, 355f, 100f);
            width = 1448f;
            height = 1086f;
        }
        else
        {
            Rect[] menuRects =
            {
                new Rect(420f, 450f, 530f, 165f),
                new Rect(420f, 625f, 530f, 160f),
                new Rect(510f, 795f, 350f, 105f)
            };
            pixelRect = menuRects[mainMenuSelection];
            width = ReferenceWidth;
            height = ReferenceHeight;
        }

        RectTransform rect = navigationFrame.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(pixelRect.xMin / width, 1f - pixelRect.yMax / height);
        rect.anchorMax = new Vector2(pixelRect.xMax / width, 1f - pixelRect.yMin / height);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        navigationFrame.transform.SetAsLastSibling();
    }

    private void PlayStartScreenSounds()
    {
        AudioClip musicClip = Resources.Load<AudioClip>("DragonBallZ_StartMusic");
        AudioClip thunderClip = Resources.Load<AudioClip>("Thunder_StartSound");

        if (musicClip != null)
        {
            dragonBallMusic = gameObject.AddComponent<AudioSource>();
            dragonBallMusic.clip = musicClip;
            dragonBallMusic.loop = true;
            dragonBallMusic.playOnAwake = false;
            dragonBallMusic.spatialBlend = 0f;
            dragonBallMusic.volume = 0.9f;
            dragonBallMusic.ignoreListenerPause = true;
            dragonBallMusic.Play();
        }

        if (thunderClip != null)
        {
            thunderSound = gameObject.AddComponent<AudioSource>();
            thunderSound.clip = thunderClip;
            thunderSound.loop = false;
            thunderSound.playOnAwake = false;
            thunderSound.spatialBlend = 0f;
            thunderSound.volume = 1f;
            thunderSound.ignoreListenerPause = true;
            thunderSound.Play();
        }
    }

    private void AddHotspot(Transform parent, string objectName, Rect pixelRect, UnityEngine.Events.UnityAction action)
    {
        AddHotspotWithReference(parent, objectName, pixelRect, ReferenceWidth, ReferenceHeight, action);
    }

    private void AddHotspotWithReference(
        Transform parent,
        string objectName,
        Rect pixelRect,
        float referenceWidth,
        float referenceHeight,
        UnityEngine.Events.UnityAction action)
    {
        GameObject hotspot = new GameObject(objectName + " Button", typeof(RectTransform));
        hotspot.transform.SetParent(parent, false);
        Image hitArea = hotspot.AddComponent<Image>();
        hitArea.color = new Color(1f, 1f, 1f, 0.001f);

        RectTransform rect = hotspot.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(pixelRect.xMin / referenceWidth, 1f - pixelRect.yMax / referenceHeight);
        rect.anchorMax = new Vector2(pixelRect.xMax / referenceWidth, 1f - pixelRect.yMin / referenceHeight);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Button button = hotspot.AddComponent<Button>();
        button.targetGraphic = hitArea;
        button.onClick.AddListener(action);
    }

    private void StartEasy()
    {
        GameModeSettings.SelectEasyMode();
        BeginGame();
    }

    private void StartNormal()
    {
        GameModeSettings.SelectNormalMode();
        BeginGame();
    }

    private void BeginGame()
    {
        PauseMenuController.EnsureExists();
        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player != null)
            player.SetMaxHealth(GameModeSettings.PlayerMaxHealth);

        if (dragonBallMusic != null)
            dragonBallMusic.Stop();
        if (thunderSound != null)
            thunderSound.Stop();

        AudioListener.pause = false;
        Time.timeScale = 1f;
        Destroy(gameObject);
    }

    private void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
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
