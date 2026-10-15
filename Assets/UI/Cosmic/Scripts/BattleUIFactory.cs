using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Small shared building blocks; gameplay data never lives in these graphics.
public static class BattleUIFactory
{
    public static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
    {
        var item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        item.layer = 5;
        item.transform.SetParent(parent, false);
        var rect = item.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    public static Image Image(string name, Transform parent, Vector2 size, Vector2 position, Color color, bool hit = false)
    {
        var image = Rect(name, parent, size, position).gameObject.AddComponent<Image>();
        image.color = color; image.raycastTarget = hit;
        return image;
    }

    public static TMP_Text Text(string name, Transform parent, Vector2 size, Vector2 position, string value,
        float fontSize, TMP_FontAsset font, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        var text = Rect(name, parent, size, position).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font; text.text = value; text.fontSize = fontSize;
        text.enableAutoSizing = true; text.fontSizeMin = fontSize * .8f; text.fontSizeMax = fontSize;
        text.color = CosmicUITheme.Ivory; text.alignment = alignment;
        text.raycastTarget = false; text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }

    public static Button Button(string name, Transform parent, Vector2 size, Vector2 position,
        string caption, TMP_FontAsset font, UnityEngine.Events.UnityAction action)
    {
        var image = Image(name, parent, size, position, CosmicUITheme.ButtonFill, true);
        var button = image.gameObject.AddComponent<Button>();
        Text("Label", image.transform, size - new Vector2(20, 8), Vector2.zero, caption, 25, font);
        CosmicUITheme.StyleButton(button);
        if (action != null) button.onClick.AddListener(action);
        image.gameObject.AddComponent<CosmicButtonEffect>();
        return button;
    }

    public static void Clear(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            GameObject child = parent.GetChild(i).gameObject;
            child.SetActive(false); child.transform.SetParent(null, false);
            if (Application.isPlaying) Object.Destroy(child); else Object.DestroyImmediate(child);
        }
    }
}
