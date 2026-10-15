using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One presentation per combatant; health/intent are always read from CharacterData.
public sealed class CosmicBattleActor : MonoBehaviour, IPointerClickHandler
{
    public CharacterData Character { get; private set; }
    public EnemyData Enemy => Character as EnemyData;
    public Image Portrait { get; private set; }
    private CosmicBattleUI screen;
    private TMP_Text hp, block, intent, nameLabel, feedback;
    private Image hpFill;
    private AstralMapGraphic ring;
    private int previousHP, previousBlock;
    private float flash, lunge, deathAlpha = 1, feedbackTime;
    private Vector2 portraitOrigin;
    private string enName, thName;

    public void Build(CosmicBattleUI owner, CharacterData data, Sprite sprite, string english, string thai, bool boss)
    {
        screen = owner; Character = data; enName = english; thName = thai;
        var rect = (RectTransform)transform;
        var hit = gameObject.AddComponent<Image>(); hit.color = new Color(0, 0, 0, .001f);
        var rim = BattleUIFactory.Rect("TargetRing", rect, new Vector2(boss ? 470 : 340, boss ? 450 : 380), new Vector2(0, boss ? 80 : 40));
        ring = rim.gameObject.AddComponent<AstralMapGraphic>();
        ring.motif = AstralMapGraphic.Motif.ButtonFrame; ring.raycastTarget = false; ring.enabled = false;
        Portrait = BattleUIFactory.Image("Portrait", rect, new Vector2(boss ? 470 : 360, 440),
            new Vector2(0, boss ? 80 : 40), Color.white);
        Portrait.sprite = sprite; Portrait.preserveAspect = true; portraitOrigin = Portrait.rectTransform.anchoredPosition;
        nameLabel = BattleUIFactory.Text("Name", rect, new Vector2(310, 36), new Vector2(0, -162), english, 24, owner.Font);
        BattleUIFactory.Image("HealthTrack", rect, new Vector2(250, 23), new Vector2(0, -194), CosmicUITheme.Panel);
        hpFill = BattleUIFactory.Image("HealthFill", rect, new Vector2(246, 19), new Vector2(0, -194), new Color(.56f, .18f, .28f));
        hpFill.rectTransform.pivot = new Vector2(0, .5f);
        hpFill.rectTransform.anchoredPosition = new Vector2(-123, -194);
        hp = BattleUIFactory.Text("HP", rect, new Vector2(250, 28), new Vector2(0, -194), "", 22, owner.Font);
        block = BattleUIFactory.Text("Block", rect, new Vector2(290, 34), new Vector2(0, -230), "", 22, owner.Font);
        block.color = new Color(.64f, .76f, .95f);
        intent = BattleUIFactory.Text("Intent", rect, new Vector2(310, 45), new Vector2(boss ? -220 : 0, 280), "", 25, owner.Font);
        intent.color = new Color(.95f, .62f, .58f);
        feedback = BattleUIFactory.Text("Feedback", rect, new Vector2(220, 70), new Vector2(0, 100), "", 45, owner.Font);
        if (data != null) { previousHP = data.GetCurrentHP(); previousBlock = data.GetBlock(); }
        Refresh();
    }

    public void Refresh()
    {
        if (Character == null || screen == null) return;
        bool thai = CosmicLanguage.IsThai;
        nameLabel.font = block.font = intent.font = screen.Font;
        hp.font = feedback.font = screen.englishFont;
        nameLabel.text = thai ? thName : enName;
        int value = Character.GetCurrentHP();
        if (value < previousHP) { flash = .32f; ShowFeedback("-" + (previousHP - value), new Color(1, .45f, .45f)); }
        if (Character.GetBlock() > previousBlock) ShowFeedback("+" + (Character.GetBlock() - previousBlock), new Color(.65f, .8f, 1));
        previousHP = value; previousBlock = Character.GetBlock();
        hp.text = value + " / " + Character.GetMaxHP();
        hpFill.rectTransform.sizeDelta = new Vector2(246f * value / Mathf.Max(1, Character.GetMaxHP()), 19);
        block.text = (thai ? "เกราะ " : "BLOCK ") + Character.GetBlock();
        EnemyAction action = Enemy == null ? null : Enemy.CurrentAction;
        intent.text = action == null || Character.IsDead() ? "" :
            (action.ActionType == EnemyActionType.Attack ? (thai ? "โจมตี " : "ATTACK ") : (thai ? "ป้องกัน " : "DEFEND ")) + action.Value;
        if (Character.IsDead()) { nameLabel.text += thai ? " — พ่ายแพ้" : " — DEFEATED"; block.text = ""; }
    }

    private void ShowFeedback(string value, Color color)
    { feedback.text = value; feedback.color = color; feedbackTime = .8f; }

    private void Update()
    {
        if (Character == null) return;
        Refresh();
        float dt = Time.deltaTime;
        flash = Mathf.Max(0, flash - dt); lunge = Mathf.Max(0, lunge - dt);
        float breathe = Character.IsDead() ? 0 : Mathf.Sin(Time.time * 1.6f + transform.GetSiblingIndex()) * 3;
        float kick = lunge > 0 ? Mathf.Sin(lunge / .3f * Mathf.PI) * (Enemy == null ? 26 : -26) : 0;
        float shake = flash > 0 ? Mathf.Sin(Time.time * 100) * 5 : 0;
        Portrait.rectTransform.anchoredPosition = portraitOrigin + new Vector2(kick + shake, breathe);
        Portrait.rectTransform.localScale = Vector3.one * (1 + breathe * .0015f);
        deathAlpha = Mathf.MoveTowards(deathAlpha, Character.IsDead() ? .16f : 1, dt * 2);
        Portrait.color = new Color(1, flash > 0 ? .6f : 1, flash > 0 ? .65f : 1, deathAlpha);
        bool target = !Character.IsDead() && screen.IsValidHighlight(this);
        ring.enabled = target;
        ring.color = target ? CosmicUITheme.Accent : new Color(.3f, .2f, .4f, .15f);
        ring.emphasis = target ? .8f : 0;
        if (feedbackTime > 0)
        {
            feedbackTime -= dt;
            feedback.rectTransform.anchoredPosition = new Vector2(0, 100 + (1 - feedbackTime / .8f) * 70);
            Color c = feedback.color; c.a = Mathf.Clamp01(feedbackTime * 3); feedback.color = c;
        }
        else feedback.text = "";
    }

    public void Act() { lunge = .3f; }
    public void ResetFeedback()
    {
        previousHP = Character.GetCurrentHP(); previousBlock = Character.GetBlock();
        flash = feedbackTime = 0; feedback.text = ""; Refresh();
    }
    public void OnPointerClick(PointerEventData eventData)
    { if (eventData.button == PointerEventData.InputButton.Left) screen.ClickActor(this); }
}
