using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Runtime modal: blocks both pointer and keyboard interaction with the screen below.
public sealed class CosmicModalDialog : MonoBehaviour
{
    private readonly Dictionary<Selectable, bool> previous = new Dictionary<Selectable, bool>();
    private GameObject previousSelection;
    private Action confirmAction;
    private bool closed;

    public static CosmicModalDialog Show(Canvas canvas, TMP_FontAsset font,
        string title, string message, string accept, Action confirmed, string cancel = null)
    {
        if (canvas == null) throw new InvalidOperationException("A canvas is required for a modal dialog.");
        GameObject root = new GameObject("CosmicConfirmation", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        root.transform.SetParent(canvas.transform, false);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        root.GetComponent<Image>().color = CosmicUITheme.Scrim;
        CosmicModalDialog modal = root.AddComponent<CosmicModalDialog>();
        modal.confirmAction = confirmed;
        if (EventSystem.current != null) modal.previousSelection = EventSystem.current.currentSelectedGameObject;
        foreach (Selectable selectable in canvas.GetComponentsInChildren<Selectable>(true))
        {
            modal.previous[selectable] = selectable.interactable;
            selectable.interactable = false;
        }
        RectTransform panel = Child("ConfirmationPanel", root.transform, new Vector2(760, 440), Vector2.zero);
        panel.gameObject.AddComponent<Image>();
        CosmicUITheme.StylePanel(panel);
        Text(panel, font, title, 36, new Vector2(680, 70), new Vector2(0, 130));
        Text(panel, font, message, 26, new Vector2(660, 145), new Vector2(0, 10));
        Button acceptButton = modal.AddButton(panel, font, accept, new Vector2(cancel == null ? 0 : 160, -145), true);
        Button cancelButton = cancel == null ? null : modal.AddButton(panel, font, cancel, new Vector2(-160, -145), false);
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject((cancelButton != null ? cancelButton : acceptButton).gameObject);
        return modal;
    }

    private Button AddButton(RectTransform parent, TMP_FontAsset font, string caption, Vector2 position, bool accept)
    {
        RectTransform rect = Child(accept ? "Confirm" : "Cancel", parent, new Vector2(275, 62), position);
        Image image = rect.gameObject.AddComponent<Image>(); image.color = new Color(.12f, .075f, .20f, 1);
        Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        Text(rect, font, caption, 25, new Vector2(260, 58), Vector2.zero);
        CosmicUITheme.StyleButton(button);
        button.onClick.AddListener(() => Close(accept));
        rect.gameObject.AddComponent<CosmicButtonEffect>().primaryAction = accept;
        return button;
    }

    public void Close(bool accepted)
    {
        if (closed) return;
        closed = true;
        Restore();
        gameObject.SetActive(false);
        if (Application.isPlaying) Destroy(gameObject);
        else DestroyImmediate(gameObject);
        if (accepted) confirmAction?.Invoke();
    }

    private void OnDestroy() { Restore(); }
    private void Restore()
    {
        foreach (var pair in previous) if (pair.Key != null) pair.Key.interactable = pair.Value;
        previous.Clear();
        if (EventSystem.current != null && previousSelection != null)
            EventSystem.current.SetSelectedGameObject(previousSelection);
    }

    private static RectTransform Child(string name, Transform parent, Vector2 size, Vector2 position)
    {
        GameObject item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        item.layer = 5; item.transform.SetParent(parent, false);
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.sizeDelta = size; rect.anchoredPosition = position;
        return rect;
    }
    private static void Text(Transform parent, TMP_FontAsset font, string value, float size, Vector2 bounds, Vector2 position)
    {
        TMP_Text text = Child("Label", parent, bounds, position).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font; text.text = value; text.fontSize = size;
        text.enableAutoSizing = true; text.fontSizeMin = 19; text.fontSizeMax = size;
        text.color = CosmicUITheme.Ivory; text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
    }
}
