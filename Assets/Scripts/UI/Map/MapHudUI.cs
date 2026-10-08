using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MapHudUI : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text statusCaptionText;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private TMP_Text[] legendTexts;
    [SerializeField] private TMP_FontAsset englishFont;
    [SerializeField] private TMP_FontAsset thaiFont;

    private RunManager runManager;
    private string lastStatus;
    private TMP_Text floorValue;
    private TMP_Text hpValue;
    private TMP_Text goldValue;
    private Image hpFill;

    private void Start()
    {
        Transform panel = transform.Find("StatusPanel");
        if (panel != null)
        {
            floorValue = panel.Find("FloorValue")?.GetComponent<TMP_Text>();
            hpValue = panel.Find("HpValue")?.GetComponent<TMP_Text>();
            goldValue = panel.Find("GoldValue")?.GetComponent<TMP_Text>();
            hpFill = panel.Find("HpTrack/HpFill")?.GetComponent<Image>();
        }
        Button back = transform.Find("BackButton")?.GetComponent<Button>();
        if (back != null) back.onClick.AddListener(BackToMenu);
        lastStatus = null;
        RefreshStatus();
    }

    private void BackToMenu()
    {
        if (runManager == null) runManager = FindAnyObjectByType<RunManager>();
        if (runManager != null) runManager.SaveAndReturnToMenu();
    }

    private void OnEnable()
    {
        CosmicLanguage.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        CosmicLanguage.Changed -= Refresh;
    }

    private void LateUpdate()
    {
        // RunManager creates the run in Start, which may be later than this UI.
        if (runManager == null)
        {
            runManager = FindAnyObjectByType<RunManager>();
        }

        RefreshStatus();
    }

    private void Refresh()
    {
        bool thai = CosmicLanguage.IsThai;
        TMP_FontAsset font = thai ? thaiFont : englishFont;

        SetLabel(titleText, thai ? "เลือกเส้นทาง" : "CHOOSE YOUR PATH", font);
        SetLabel(
            subtitleText,
            thai ? "เลือกเส้นทางถัดไป" : "CHOOSE YOUR NEXT DESCENT",
            font
        );
        SetLabel(statusCaptionText, thai ? "สถานะการเดินทาง" : "RUN STATUS", font);
        Transform statusPanel = transform.Find("StatusPanel");
        if (statusPanel != null)
        {
            SetLabel(statusPanel.Find("FloorCaption")?.GetComponent<TMP_Text>(),
                thai ? "ชั้น" : "FLOOR", font);
            SetLabel(statusPanel.Find("HpCaption")?.GetComponent<TMP_Text>(),
                thai ? "พลังชีวิต" : "HP", font);
            SetLabel(statusPanel.Find("GoldCaption")?.GetComponent<TMP_Text>(),
                thai ? "ทอง" : "GOLD", font);
            SetLabel(floorValue, floorValue == null ? "" : floorValue.text, font);
            SetLabel(hpValue, hpValue == null ? "" : hpValue.text, font);
            SetLabel(goldValue, goldValue == null ? "" : goldValue.text, font);
        }
        SetLabel(transform.Find("LegendPanel/LegendTitle")?.GetComponent<TMP_Text>(),
            thai ? "สัญลักษณ์" : "LEGEND", font);
        SetLabel(transform.Find("BackButton/BackLabel")?.GetComponent<TMP_Text>(),
            thai ? "กลับ" : "BACK", font);
        SetLabel(
            hintText,
            thai ? "เลือกด่านที่กดได้เพื่อเดินทางต่อ" : "SELECT AN AVAILABLE NODE TO CONTINUE",
            font
        );

        string[] english = { "BATTLE", "ELITE", "EVENT", "REST", "SHOP", "BOSS" };
        string[] thaiLabels = { "ต่อสู้", "ศัตรูพิเศษ", "เหตุการณ์", "พัก", "ร้านค้า", "บอส" };

        if (legendTexts != null)
        {
            for (int i = 0; i < legendTexts.Length && i < english.Length; i++)
            {
                SetLabel(legendTexts[i], thai ? thaiLabels[i] : english[i], font);
            }
        }

        lastStatus = null;
        RefreshStatus();
    }

    public TMP_FontAsset GetFont(bool thai) => thai ? thaiFont : englishFont;

    private void RefreshStatus()
    {
        if (statusText == null)
        {
            return;
        }

        RunData run = runManager == null ? null : runManager.CurrentRun;
        bool thai = CosmicLanguage.IsThai;
        string value;

        if (run == null || run.Player == null)
        {
            value = thai
                ? "HP --/--   •   ทอง --   •   ชั้น --/--"
                : "HP --/--   •   GOLD --   •   FLOOR --/--";
        }
        else
        {
            int highestLayer = 0;
            if (run.CurrentMap != null)
            {
                foreach (MapNode node in run.CurrentMap.Nodes)
                {
                    highestLayer = Mathf.Max(highestLayer, node.LayerIndex);
                }
            }

            int nextLayer = run.CurrentNode == null
                ? 1
                : Mathf.Min(highestLayer, run.CurrentNode.LayerIndex + 1);

            value = thai
                ? $"HP {run.Player.CurrentHP}/{run.Player.MaxHP}   •   ทอง {run.Gold}   •   ชั้น {nextLayer}/{highestLayer}"
                : $"HP {run.Player.CurrentHP}/{run.Player.MaxHP}   •   GOLD {run.Gold}   •   FLOOR {nextLayer}/{highestLayer}";
        }

        if (value == lastStatus)
        {
            return;
        }

        lastStatus = value;
        statusText.text = value;

        if (floorValue != null)
            floorValue.text = run == null || run.CurrentNode == null
                ? "--" : Mathf.Min(6, run.CurrentNode.LayerIndex + 1).ToString();
        if (hpValue != null)
            hpValue.text = run == null ? "-- / --" :
                $"{run.Player.CurrentHP} / {run.Player.MaxHP}";
        if (goldValue != null)
            goldValue.text = run == null ? "--" : run.Gold.ToString();
        if (hpFill != null)
        {
            hpFill.fillAmount = run == null ? 0f :
                (float)run.Player.CurrentHP / run.Player.MaxHP;
            hpFill.rectTransform.anchorMax = new Vector2(hpFill.fillAmount, 1f);
            hpFill.enabled = hpFill.fillAmount > 0f;
        }

        TMP_FontAsset font = thai ? thaiFont : englishFont;
        if (font != null)
        {
            statusText.font = font;
        }
    }

    private static void SetLabel(TMP_Text label, string value, TMP_FontAsset font)
    {
        if (label == null)
        {
            return;
        }

        label.text = value;
        if (font != null)
        {
            label.font = font;
        }
    }
}
