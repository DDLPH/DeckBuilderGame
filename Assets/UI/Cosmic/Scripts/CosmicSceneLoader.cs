using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// A persistent overlay, not a seventh game scene. Async loading never depends on timeScale.
public sealed class CosmicSceneLoader : MonoBehaviour
{
    private static CosmicSceneLoader instance;
    private CanvasGroup group;
    private Image fill;
    private TMP_Text percentage;
    private readonly System.Collections.Generic.Dictionary<UnityEngine.EventSystems.EventSystem, bool> navigation =
        new System.Collections.Generic.Dictionary<UnityEngine.EventSystems.EventSystem, bool>();
    public static bool IsLoading => instance != null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance = null; }

    public static bool Load(string scene)
    {
        if (IsLoading) return false;
        if (!Application.CanStreamedLevelBeLoaded(scene))
        { Debug.LogError("Scene is missing from Build Profiles: " + scene); return false; }
        if (!Application.isPlaying) { SceneManager.LoadScene(scene); return true; }
        var root = new GameObject("Cosmic Loading", typeof(RectTransform));
        DontDestroyOnLoad(root);
        instance = root.AddComponent<CosmicSceneLoader>();
        instance.Build(); instance.StartCoroutine(instance.Transition(scene));
        return true;
    }

    private void Build()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 32000;
        var scaler = gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        gameObject.AddComponent<GraphicRaycaster>(); group = gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0;
        var background = BattleUIFactory.Image("Screen", transform, new Vector2(1920, 1080), Vector2.zero, CosmicUITheme.Panel, true);
        background.rectTransform.anchorMin = Vector2.zero; background.rectTransform.anchorMax = Vector2.one;
        background.rectTransform.offsetMin = background.rectTransform.offsetMax = Vector2.zero;
        TMP_FontAsset font = FindFont();
        var emblem = BattleUIFactory.Rect("Eclipse", transform, new Vector2(120, 120), new Vector2(0, 100)).gameObject.AddComponent<AstralMapGraphic>();
        emblem.motif = AstralMapGraphic.Motif.Eclipse; emblem.color = CosmicUITheme.Accent; emblem.raycastTarget = false;
        BattleUIFactory.Text("Title", transform, new Vector2(1000, 80), new Vector2(0, -15),
            CosmicLanguage.IsThai ? "กำลังเดินทาง..." : "BEYOND THE VEIL", 35, font);
        BattleUIFactory.Image("ProgressTrack", transform, new Vector2(540, 4), new Vector2(0, -105), CosmicUITheme.Muted);
        fill = BattleUIFactory.Image("Progress", transform, new Vector2(540, 4), new Vector2(0, -105), CosmicUITheme.Accent);
        fill.rectTransform.pivot = new Vector2(0, .5f); fill.rectTransform.anchoredPosition = new Vector2(-270, -105);
        percentage = BattleUIFactory.Text("ProgressLabel", transform, new Vector2(540, 45), new Vector2(0, -158), "0%", 22, font);
    }

    private static TMP_FontAsset FindFont()
    {
        var battle = FindAnyObjectByType<CosmicBattleUI>(); if (battle != null) return battle.Font;
        // Read the configured font, not only the font currently rendered on a label.
        // This also works when a language switch and a transition happen in one frame.
        foreach (CosmicLocalizedText localized in FindObjectsByType<CosmicLocalizedText>(FindObjectsInactive.Include))
        {
            TMP_FontAsset candidate = CosmicLanguage.IsThai ? localized.thaiFont : localized.englishFont;
            if (candidate != null) return candidate;
        }
        MapHudUI hud = FindAnyObjectByType<MapHudUI>();
        if (hud != null && hud.GetFont(CosmicLanguage.IsThai) != null) return hud.GetFont(CosmicLanguage.IsThai);
        foreach (TMP_Text text in FindObjectsByType<TMP_Text>())
            if (text.font != null && text.font.name.Contains(CosmicLanguage.IsThai ? "Noto" : "Cinzel")) return text.font;
        return TMP_Settings.defaultFontAsset;
    }

    private IEnumerator Transition(string scene)
    {
        // Pointer input is blocked immediately. Do not leave a paused clock in the next scene.
        float previousClock = Time.timeScale; Time.timeScale = 0;
        if (EventSystemAvailable()) UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        for (float t = 0; t < .14f; t += Time.unscaledDeltaTime)
        { group.alpha = t / .14f; yield return null; }
        group.alpha = 1;
        AsyncOperation operation = SceneManager.LoadSceneAsync(scene);
        if (operation == null) { Time.timeScale = previousClock; Debug.LogError("Could not load " + scene); Destroy(gameObject); yield break; }
        operation.allowSceneActivation = false;
        while (operation.progress < .9f)
        {
            SetProgress(operation.progress / .9f); yield return null;
        }
        SetProgress(1); Time.timeScale = 1; operation.allowSceneActivation = true;
        while (!operation.isDone) yield return null;
        yield return null; // Allow Start() to initialise the new UI before revealing it.
        for (float t = 0; t < .22f; t += Time.unscaledDeltaTime)
        { group.alpha = 1 - t / .22f; yield return null; }
        Destroy(gameObject);
    }
    private void SetProgress(float value)
    { fill.rectTransform.sizeDelta = new Vector2(540 * Mathf.Clamp01(value), 4); percentage.text = Mathf.RoundToInt(value * 100) + "%"; }
    private static bool EventSystemAvailable() => UnityEngine.EventSystems.EventSystem.current != null;
    private void Update()
    {
        var events = UnityEngine.EventSystems.EventSystem.current;
        if (events == null) return;
        if (!navigation.ContainsKey(events)) navigation.Add(events, events.sendNavigationEvents);
        events.sendNavigationEvents = false; events.SetSelectedGameObject(null);
    }
    private void OnDestroy()
    {
        foreach (var pair in navigation) if (pair.Key != null) pair.Key.sendNavigationEvents = pair.Value;
        if (instance == this) instance = null;
    }
}
