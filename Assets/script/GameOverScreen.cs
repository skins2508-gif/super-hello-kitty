using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverScreen : MonoBehaviour
{
    private bool restarting;

    public static void Show()
    {
        if (FindAnyObjectByType<GameOverScreen>() != null)
            return;

        GameObject root = new GameObject("Game Over Screen");
        root.AddComponent<GameOverScreen>();

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 3000;

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1400f, 1152f);
        root.AddComponent<GraphicRaycaster>();

        CreateText(root.transform, "GAME OVER", 92, new Vector2(0f, 55f),
            new Color(1f, 0.12f, 0.12f), FontStyle.Bold);
        CreateText(root.transform, "Press 'R' to restart", 38, new Vector2(0f, -55f),
            Color.white, FontStyle.Bold);

    }

    private void Update()
    {
        if (restarting || Keyboard.current == null)
            return;

        bool restartPressed = Keyboard.current.rKey.wasPressedThisFrame;
        if (!restartPressed)
            return;

        restarting = true;
        GameModeSettings.RequestRestart();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private static void CreateText(Transform parent, string value, int fontSize,
        Vector2 position, Color color, FontStyle style)
    {
        GameObject textObject = new GameObject(value);
        textObject.transform.SetParent(parent, false);

        Text text = textObject.AddComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;

        Outline outline = textObject.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(4f, -4f);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(900f, 120f);
    }

}
