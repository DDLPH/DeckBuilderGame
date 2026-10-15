using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Event remains an overlay on the generated map, not a separate scene.
public sealed class MapEventOverlay : MonoBehaviour
{
    private RunManager runManager;
    private TMP_Text title, description, hint;
    private Button gold, heal, proceed;
    private bool chosen;
    private GameObject oldSelection;
    private readonly Dictionary<Selectable, bool> previous = new Dictionary<Selectable, bool>();
    public Button GoldButton => gold;
    public Button HealButton => heal;
    public Button ContinueButton => proceed;

    public static void Show(RunManager manager)
    {
        if (manager == null || !manager.IsRunActive() || !manager.EventPending || FindAnyObjectByType<MapEventOverlay>() != null) return;
        var canvas = GameObject.Find("MapCanvas")?.GetComponent<Canvas>();
        if (canvas == null) { Debug.LogWarning("Event needs MapCanvas."); return; }
        var root = BattleUIFactory.Image("EventOverlay", canvas.transform, Vector2.zero, Vector2.zero, CosmicUITheme.Scrim, true);
        var rect = root.rectTransform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        var layer = root.gameObject.AddComponent<Canvas>(); layer.overrideSorting = true; layer.sortingOrder = canvas.sortingOrder + 150;
        root.gameObject.AddComponent<GraphicRaycaster>();
        root.gameObject.AddComponent<MapEventOverlay>().Build(manager);
    }
    private void Build(RunManager manager)
    {
        runManager = manager;
        if (EventSystem.current != null) oldSelection = EventSystem.current.currentSelectedGameObject;
        BlockBackground();
        var font = FindAnyObjectByType<MapHudUI>()?.GetFont(CosmicLanguage.IsThai);
        var panel = BattleUIFactory.Image("EventPanel", transform, new Vector2(1100, 680), Vector2.zero, CosmicUITheme.Panel);
        CosmicUITheme.Frame(panel.transform, false);
        var icon = BattleUIFactory.Rect("VoidEmblem", panel.transform, new Vector2(125, 125), new Vector2(0, 228)).gameObject.AddComponent<AstralMapGraphic>();
        icon.motif = AstralMapGraphic.Motif.Eclipse; icon.color = CosmicUITheme.Accent; icon.raycastTarget = false;
        title = BattleUIFactory.Text("Title", panel.transform, new Vector2(960, 70), new Vector2(0, 133), "", 40, font);
        description = BattleUIFactory.Text("Story", panel.transform, new Vector2(930, 110), new Vector2(0, 37), "", 28, font);
        gold = BattleUIFactory.Button("TakeGold", panel.transform, new Vector2(420, 90), new Vector2(-235, -104), "", font, () => Choose(true));
        heal = BattleUIFactory.Button("Heal", panel.transform, new Vector2(420, 90), new Vector2(235, -104), "", font, () => Choose(false));
        proceed = BattleUIFactory.Button("Continue", panel.transform, new Vector2(420, 75), new Vector2(0, -211), "", font, Close);
        hint = BattleUIFactory.Text("Hint", panel.transform, new Vector2(950, 55), new Vector2(0, -222), "", 24, font);
        RefreshLanguage();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(gold.gameObject);
    }
    private void BlockBackground()
    {
        Canvas root = GetComponentInParent<Canvas>().rootCanvas;
        foreach (Selectable item in root.GetComponentsInChildren<Selectable>(true))
        {
            if (item.transform.IsChildOf(transform)) continue;
            if (!previous.ContainsKey(item)) previous[item] = item.interactable;
            item.interactable = false;
        }
    }
    private void OnEnable() { CosmicLanguage.Changed += RefreshLanguage; }
    private void OnDisable() { CosmicLanguage.Changed -= RefreshLanguage; }
    private void OnDestroy()
    {
        foreach (var pair in previous) if (pair.Key != null) pair.Key.interactable = pair.Value;
        if (EventSystem.current != null && oldSelection != null && oldSelection.activeInHierarchy) EventSystem.current.SetSelectedGameObject(oldSelection);
    }
    private void RefreshLanguage()
    {
        if (title == null) return;
        bool th = CosmicLanguage.IsThai;
        TMP_FontAsset font = FindAnyObjectByType<MapHudUI>()?.GetFont(th);
        foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true)) if (font != null) text.font = font;
        title.text = th ? (chosen ? "ของขวัญจากความว่างเปล่า" : "เสียงเรียกจากความว่างเปล่า") : (chosen ? "A GIFT FROM THE VOID" : "A WHISPER IN THE VOID");
        if (!chosen) description.text = th ? "แสงริบหรี่ปรากฏบนแท่นบูชาโบราณ\nรับเหรียญที่ถูกลืม หรือปล่อยให้แสงนั้นเยียวยาคุณ" : "A faint light stirs upon an ancient altar.\nTake its forgotten coins, or let the light mend your wounds.";
        gold.GetComponentInChildren<TMP_Text>().text = th ? "รับทอง 25" : "TAKE 25 GOLD";
        int amount = runManager == null ? 0 : Mathf.Min(12, runManager.CurrentRun.Player.MaxHP - runManager.CurrentRun.Player.CurrentHP);
        heal.GetComponentInChildren<TMP_Text>().text = amount == 0 ? (th ? "พลังชีวิตเต็มแล้ว" : "HEALTH ALREADY FULL") : (th ? "ฟื้นฟู " : "RESTORE ") + amount + " HP";
        heal.interactable = !chosen && amount > 0;
        gold.interactable = !chosen;
        gold.gameObject.SetActive(!chosen); heal.gameObject.SetActive(!chosen); hint.gameObject.SetActive(!chosen); proceed.gameObject.SetActive(chosen);
        proceed.GetComponentInChildren<TMP_Text>().text = th ? "เดินทางต่อ" : "CONTINUE";
        hint.text = th ? "เลือกรับได้หนึ่งอย่าง • บันทึกผลหลังเลือก" : "Choose one gift • Saved after choosing";
    }
    private void Choose(bool chooseGold)
    {
        if (chosen || runManager == null || !runManager.EventPending || !runManager.IsRunActive()) return;
        if (!chooseGold && runManager.CurrentRun.Player.CurrentHP >= runManager.CurrentRun.Player.MaxHP) return;
        chosen = true;
        int before = runManager.CurrentRun.Player.CurrentHP;
        runManager.ResolveEvent(chooseGold);
        BlockBackground(); RefreshLanguage();
        bool th = CosmicLanguage.IsThai;
        description.text = chooseGold ? (th ? "ได้รับทอง 25 แล้ว\nเสียงกระซิบเงียบลง เส้นทางข้างหน้ารออยู่" : "25 gold received.\nThe whisper fades. Your path awaits.") :
            (th ? "ฟื้นฟูพลังชีวิต " : "Restored ") + (runManager.CurrentRun.Player.CurrentHP - before) + " HP\n" + (th ? "ความอบอุ่นเลือนหาย แต่บาดแผลของคุณดีขึ้นแล้ว" : "The warmth fades, but your wounds have eased.");
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(proceed.gameObject);
    }
    public void Close()
    {
        if (!chosen) return;
        // Restore once now; deactivating the scrim must not leave the map locked for a frame.
        foreach (var pair in previous) if (pair.Key != null) pair.Key.interactable = pair.Value;
        previous.Clear(); gameObject.SetActive(false);
        if (Application.isPlaying) Destroy(gameObject); else DestroyImmediate(gameObject);
    }
}
