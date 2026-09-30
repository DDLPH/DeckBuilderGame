using UnityEngine;
using TMPro;

public class EnemyIntentDisplayManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI intentTypeText;
    [SerializeField] private TextMeshProUGUI intentValueText;

    public void ShowIntent(EnemyAction action)
    {
        if (action == null)
        {
            ClearIntent();
            
            Debug.LogWarning("Enemy has no intent to show.");
            return;
        }

        intentTypeText.text = action.ActionType.ToString();
        intentValueText.text = action.Value.ToString();
    }

    private void ClearIntent()
    {
        intentTypeText.text = "";
        intentValueText.text = "";
    }
}