using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Small, non-interactive motion behind the Main Menu controls.
/// The background art stays still; only the stars and eclipse halo breathe.
/// </summary>
public sealed class CosmicMenuAmbience : MonoBehaviour
{
    private static readonly Vector2[] StarPositions =
    {
        // Keep the left side clear for the title and menu buttons.
        new Vector2(0.39f, 0.83f), new Vector2(0.43f, 0.95f),
        new Vector2(0.48f, 0.72f), new Vector2(0.52f, 0.88f),
        new Vector2(0.56f, 0.97f), new Vector2(0.59f, 0.62f),
        new Vector2(0.62f, 0.78f), new Vector2(0.66f, 0.91f),
        new Vector2(0.71f, 0.67f), new Vector2(0.75f, 0.96f),
        new Vector2(0.79f, 0.57f), new Vector2(0.82f, 0.87f),
        new Vector2(0.86f, 0.73f), new Vector2(0.89f, 0.98f),
        new Vector2(0.92f, 0.62f), new Vector2(0.95f, 0.85f),
        new Vector2(0.97f, 0.95f), new Vector2(0.99f, 0.71f)
    };

    private RawImage halo;
    private RawImage[] stars;
    private Texture2D haloTexture;
    private Texture2D starTexture;

    private void Awake()
    {
        RectTransform canvasRect = transform as RectTransform;
        if (canvasRect == null) return;

        GameObject layer = new GameObject("CosmicAmbience", typeof(RectTransform));
        layer.layer = gameObject.layer;
        RectTransform layerRect = (RectTransform)layer.transform;
        layerRect.SetParent(canvasRect, false);
        layerRect.anchorMin = Vector2.zero;
        layerRect.anchorMax = Vector2.one;
        layerRect.offsetMin = Vector2.zero;
        layerRect.offsetMax = Vector2.zero;
        layerRect.SetSiblingIndex(1); // Above the background, below the menu buttons.

        haloTexture = CreateSoftTexture(true);
        starTexture = CreateSoftTexture(false);
        halo = CreateImage(layerRect, "EclipseHalo", haloTexture,
            new Vector2(0.84f, 0.81f), new Vector2(370f, 370f));

        stars = new RawImage[StarPositions.Length];
        for (int i = 0; i < stars.Length; i++)
        {
            float size = 4f + (i % 4) * 1.2f;
            stars[i] = CreateImage(layerRect, "TwinklingStar" + (i + 1), starTexture,
                StarPositions[i], new Vector2(size, size));
        }
    }

    private void Update()
    {
        if (halo == null) return;

        float time = Time.unscaledTime;
        float breath = 0.5f + 0.5f * Mathf.Sin(time * .65f);
        halo.color = new Color(0.65f, 0.38f, 0.95f, Mathf.Lerp(.045f, .11f, breath));

        for (int i = 0; i < stars.Length; i++)
        {
            float twinkle = 0.5f + 0.5f * Mathf.Sin(time * (.45f + i * .035f) + i * 1.7f);
            stars[i].color = new Color(0.88f, 0.80f, 1f, Mathf.Lerp(.08f, .40f, twinkle));
        }
    }

    private void OnDestroy()
    {
        if (haloTexture != null) Destroy(haloTexture);
        if (starTexture != null) Destroy(starTexture);
    }

    private static RawImage CreateImage(RectTransform parent, string name, Texture2D texture,
        Vector2 anchor, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        go.layer = parent.gameObject.layer;
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;

        RawImage image = go.GetComponent<RawImage>();
        image.texture = texture;
        image.raycastTarget = false;
        return image;
    }

    private static Texture2D CreateSoftTexture(bool ring)
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = ring ? "Runtime Eclipse Halo" : "Runtime Soft Star";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size - 0.5f;
                float dy = (y + 0.5f) / size - 0.5f;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = ring
                    ? Mathf.Exp(-Mathf.Pow((distance - 0.38f) / 0.055f, 2f))
                    : Mathf.Exp(-Mathf.Pow(distance / 0.18f, 2f));
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return texture;
    }
}
