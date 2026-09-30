using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnemyUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI blockText;

    [Header("Enemy Intent")]
    [SerializeField] private TextMeshProUGUI intentTypeText;
    [SerializeField] private TextMeshProUGUI intentValueText;

    [Header("Target")]
    [SerializeField] private Button targetButton;

    private EnemyData enemy;
    private BattleTargetManager battleTargetManager;

    private void Awake()
    {
        enemy = GetComponentInParent<EnemyData>();

        if (enemy == null)
        {
            Debug.LogError(
                "EnemyUI : EnemyData not found"
            );
        }

        if (targetButton == null)
        {
            targetButton = GetComponent<Button>();
        }

        if (targetButton == null)
        {
            Debug.LogError(
                "EnemyUI : Button not found"
            );

            return;
        }

        targetButton.onClick.RemoveAllListeners();
        targetButton.onClick.AddListener(
            SelectTarget
        );

        // เริ่ม Battle ยังเลือก Enemy ไม่ได้
        targetButton.interactable = false;
    }

    private void Start()
    {
        battleTargetManager =
            FindFirstObjectByType<BattleTargetManager>();

        if (battleTargetManager == null)
        {
            Debug.LogError(
                "EnemyUI : BattleTargetManager not found"
            );

            return;
        }

        // รับรู้ว่า Target Selection เปิด/ปิด
        battleTargetManager.OnTargetSelectionChanged +=
            SetTargetSelectionEnabled;

        // ตั้งค่าเริ่มต้นให้ตรงกับสถานะปัจจุบัน
        SetTargetSelectionEnabled(
            battleTargetManager.TargetSelectionActive
        );

        RefreshUI();
    }

    private void OnDestroy()
    {
        if (battleTargetManager != null)
        {
            battleTargetManager.OnTargetSelectionChanged -=
                SetTargetSelectionEnabled;
        }
    }

    private void Update()
    {
        if (enemy == null)
        {
            return;
        }

        RefreshUI();
    }

    private void RefreshUI()
    {
        if (hpText != null)
        {
            hpText.text =
                "HP: " +
                enemy.GetCurrentHP() +
                " / " +
                enemy.GetMaxHP();
        }

        // Enemy ตาย
        if (enemy.IsDead())
        {
            if (blockText != null)
            {
                blockText.text = "";
            }

            if (intentTypeText != null)
            {
                intentTypeText.text = "DEAD";
            }

            if (intentValueText != null)
            {
                intentValueText.text = "";
            }

            return;
        }

        // Enemy ยังมีชีวิต
        if (blockText != null)
        {
            blockText.text =
                "Block: " +
                enemy.GetBlock();
        }

        EnemyAction action =
            enemy.CurrentAction;

        if (action == null)
        {
            if (intentTypeText != null)
            {
                intentTypeText.text = "";
            }

            if (intentValueText != null)
            {
                intentValueText.text = "";
            }

            return;
        }

        if (intentTypeText != null)
        {
            intentTypeText.text =
                action.ActionType.ToString();
        }

        if (intentValueText != null)
        {
            intentValueText.text =
                action.Value.ToString();
        }
    }

    public void SetTargetSelectionEnabled(
        bool enabled)
    {
        if (targetButton == null)
        {
            return;
        }

        if (enemy == null)
        {
            return;
        }

        if (enemy.IsDead())
        {
            targetButton.interactable = false;
            return;
        }

        targetButton.interactable = enabled;

        Debug.Log(
            "EnemyUI : " +
            enemy.name +
            " Target Button = " +
            enabled
        );
    }

    private void SelectTarget()
    {
        if (enemy == null)
        {
            return;
        }

        if (battleTargetManager == null)
        {
            return;
        }

        battleTargetManager.SelectEnemy(enemy);
    }
}