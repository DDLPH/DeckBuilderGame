using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// One rest action per map node, owned by RunData rather than the scene instance.
public class RestManager : MonoBehaviour
{
    [Header("Legacy Rest UI (preserved)")]
    [SerializeField] private Button healButton;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private GameObject restPanel;
    [Header("Legacy Upgrade UI (preserved)")]
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private Transform cardContainer;
    [SerializeField] private GameObject restCardPrefab;
    [SerializeField] private Button upgradeButton;
    private RunManager runManager;
    private bool leaving;
    public RunData Run => runManager == null ? null : runManager.CurrentRun;
    public bool Ready => Run != null && runManager.IsRunActive() && Run.CurrentNode != null && Run.CurrentNode.NodeType == MapNodeType.Rest;
    public bool ActionUsed => Ready && Run.RestActionUsed;
    public bool CanAct => Ready && Run.CanUseRest && !leaving && !CosmicSceneLoader.IsLoading;
    public int HealAmount => Ready ? Mathf.Min(Mathf.Max(1, Run.Player.MaxHP / 3), Run.Player.MaxHP - Run.Player.CurrentHP) : 0;
    public bool CanHeal => CanAct && HealAmount > 0;
    public event Action Changed;
    private void Start()
    {
        runManager = FindAnyObjectByType<RunManager>();
        var view = FindAnyObjectByType<CosmicCampUI>();
        if (view != null) { view.Bind(this); if (!Ready) view.ShowUnavailable(); }
        Refresh();
    }
    public bool TryHeal()
    {
        if (!CanHeal || !Run.TryRestHeal()) return false;
        Refresh(); return true;
    }
    public void HealPlayer() { TryHeal(); }
    public bool TryUpgrade(int index)
    {
        if (!CanAct || !Run.TryRestUpgrade(index)) return false;
        if (upgradePanel != null) upgradePanel.SetActive(false);
        if (restPanel != null) restPanel.SetActive(true);
        Refresh(); return true;
    }
    public void SelectCardForUpgrade(int index) { TryUpgrade(index); }
    private void Refresh()
    {
        if (hpText != null && Ready) hpText.text = "HP " + Run.Player.CurrentHP + " / " + Run.Player.MaxHP;
        if (healButton != null) healButton.interactable = CanHeal;
        if (upgradeButton != null) upgradeButton.interactable = CanAct && HasUpgradeableCard();
        Changed?.Invoke();
    }
    public bool HasUpgradeableCard()
    {
        if (!Ready) return false;
        foreach (CardData card in Run.Deck.Cards) if (card != null && card.UpgradedCard != null) return true;
        return false;
    }
    public void OpenUpgradePanel()
    {
        if (!CanAct) return;
        var view = FindAnyObjectByType<CosmicCampUI>();
        if (view != null) { view.OpenUpgrades(); return; }
        if (cardContainer == null || restCardPrefab == null) return;
        BattleUIFactory.Clear(cardContainer);
        for (int i = 0; i < Run.Deck.Count; i++)
        {
            CardData card = Run.Deck.GetCard(i); if (card == null || card.UpgradedCard == null) continue;
            var item = Instantiate(restCardPrefab, cardContainer).GetComponent<RestCardUI>();
            if (item != null) item.Setup(card, i, this);
        }
        if (restPanel != null) restPanel.SetActive(false);
        if (upgradePanel != null) upgradePanel.SetActive(true);
    }
    public void LeaveRest()
    {
        if (!Ready || leaving || CosmicSceneLoader.IsLoading) return;
        leaving = true; runManager.ReturnToMap();
        if (!CosmicSceneLoader.IsLoading) leaving = false;
    }
}
