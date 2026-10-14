using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EndingScreen : MonoBehaviour
{
    private static bool shown;

    public static void Show(
        Sprite endingSprite,
        float fadeDuration,
        AudioClip endingSound,
        float soundVolume,
        bool playEndingSound = true)
    {
        if (shown || endingSprite == null)
            return;

        shown = true;

        // Stop the Infinite Castle background music before the ending song begins.
        BackgroundMusic[] backgroundMusics = FindObjectsByType<BackgroundMusic>();
        foreach (BackgroundMusic backgroundMusic in backgroundMusics)
            backgroundMusic.StopMusic();

        GameObject root = new GameObject("Ending Screen");
        EndingScreen screen = root.AddComponent<EndingScreen>();

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        root.AddComponent<CanvasScaler>();
        root.AddComponent<GraphicRaycaster>();
        CanvasGroup group = root.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        if (playEndingSound)
        {
            AudioClip configuredEndingSound = endingSound;
            if (configuredEndingSound == null)
                configuredEndingSound = Resources.Load<AudioClip>("EndingSound");
            EndingAudioPlayer endingAudio = root.AddComponent<EndingAudioPlayer>();
            endingAudio.Play(configuredEndingSound, soundVolume);
        }

        GameObject background = new GameObject("Black Background", typeof(RectTransform));
        background.transform.SetParent(root.transform, false);
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = Color.black;
        StretchToScreen(background.GetComponent<RectTransform>());

        GameObject picture = new GameObject("Ending Image", typeof(RectTransform));
        picture.transform.SetParent(root.transform, false);
        Image image = picture.AddComponent<Image>();
        image.sprite = endingSprite;
        image.color = Color.white;
        image.preserveAspect = true;
        image.raycastTarget = false;
        StretchToScreen(picture.GetComponent<RectTransform>());

        Time.timeScale = 0f;
        screen.StartCoroutine(screen.FadeIn(group, fadeDuration));
    }

    private static void StretchToScreen(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private IEnumerator FadeIn(CanvasGroup group, float duration)
    {
        float elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);
        while (elapsed < duration)
        {
            group.alpha = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        group.alpha = 1f;
    }
}
