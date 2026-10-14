using UnityEngine;

public class InfiniteBackgroundScroll : MonoBehaviour
{
    [Header("Infinite Background")]
    public Sprite backgroundSprite;
    [Min(0f)] public float scrollSpeed = 9f;
    public int sortingOrder = -100;

    private Transform[] panels;
    private float panelHeight;

    private void Start()
    {
        if (backgroundSprite == null || Camera.main == null)
        {
            enabled = false;
            return;
        }

        float cameraHeight = Camera.main.orthographicSize * 2f;
        float cameraWidth = cameraHeight * Camera.main.aspect;
        Vector2 spriteSize = backgroundSprite.bounds.size;
        float scale = Mathf.Max(cameraWidth / spriteSize.x, cameraHeight / spriteSize.y);
        panelHeight = spriteSize.y * scale;

        GameObject root = new GameObject("Infinite Background");
        panels = new Transform[2];

        for (int i = 0; i < panels.Length; i++)
        {
            GameObject panel = new GameObject("Background " + (i + 1));
            panel.transform.SetParent(root.transform);
            panel.transform.localScale = Vector3.one * scale;
            panel.transform.position = new Vector3(
                Camera.main.transform.position.x,
                Camera.main.transform.position.y + panelHeight * i,
                0f);

            SpriteRenderer renderer = panel.AddComponent<SpriteRenderer>();
            renderer.sprite = backgroundSprite;
            renderer.sortingOrder = sortingOrder;
            panels[i] = panel.transform;
        }
    }

    private void Update()
    {
        if (panels == null || Camera.main == null)
            return;

        float bottomLimit = Camera.main.transform.position.y - panelHeight;
        foreach (Transform panel in panels)
        {
            panel.position += Vector3.up * (scrollSpeed * Time.deltaTime);

            if (panel.position.y >= Camera.main.transform.position.y + panelHeight)
                panel.position -= Vector3.up * (panelHeight * panels.Length);

            if (panel.position.y < bottomLimit)
                panel.position += Vector3.up * (panelHeight * panels.Length);
        }
    }
}
