using System;
using UnityEngine;

public class BattleTargetManager : MonoBehaviour
{
    private EnemyData selectedEnemy;
    private bool targetSelectionActive;

    public EnemyData SelectedEnemy =>
        selectedEnemy;

    public bool TargetSelectionActive =>
        targetSelectionActive;

    // Event เดิม
    public event Action<EnemyData> OnEnemySelected;

    // Event ใหม่
    public event Action<bool> OnTargetSelectionChanged;

    public void BeginTargetSelection()
    {
        ClearTarget();

        targetSelectionActive = true;

        OnTargetSelectionChanged?.Invoke(true);

        Debug.Log(
            "BattleTargetManager : Enemy Target Selection Started"
        );
    }

    public void EndTargetSelection()
    {
        targetSelectionActive = false;

        OnTargetSelectionChanged?.Invoke(false);

        Debug.Log(
            "BattleTargetManager : Enemy Target Selection Ended"
        );
    }

    public void SelectEnemy(EnemyData enemy)
    {
        if (!targetSelectionActive)
        {
            Debug.LogWarning(
                "BattleTargetManager : Target selection is not active"
            );
    
            return;
        }
    
        if (enemy == null)
        {
            Debug.LogWarning(
                "BattleTargetManager : Cannot select null enemy"
            );
    
            return;
        }
    
        if (enemy.IsDead())
        {
            Debug.LogWarning(
                "BattleTargetManager : Cannot select dead enemy"
            );
    
            return;
        }
    
        selectedEnemy = enemy;
    
        Debug.Log(
            "Target Selected : " +
            enemy.name
        );
    
        // แจ้ง BattleManager ให้ทำการ์ด
        OnEnemySelected?.Invoke(enemy);
    
        // เลือกเสร็จแล้ว ปิดการเลือก Target
        EndTargetSelection();
    }

    public void ClearTarget()
    {
        selectedEnemy = null;
    }

    public bool HasTarget()
    {
        return selectedEnemy != null &&
               !selectedEnemy.IsDead();
    }
}