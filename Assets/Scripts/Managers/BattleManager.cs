using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BattleManager : MonoBehaviour
{
    [Header("Character")]
    [SerializeField] private PlayerData player;

    [Header("Manager")]
    [SerializeField] private DeckManager deckManager;
    [SerializeField] private CardDisplayManager cardDisplayManager;
    [SerializeField] private CardEffectManager cardEffectManager;
    [SerializeField] private EnemyManager enemyManager;
    [SerializeField] private BattleEncounterManager battleEncounterManager;
    [SerializeField] private BattleTargetManager battleTargetManager;
    
    [Header("UI")]
    [SerializeField] private PlayerStatusUI playerStatusUI;

    [Header("Battle State")]
    [SerializeField] private CardData selectedCard;

    private BattleState currentState;
    private bool resolvingCard;
    private bool paused;
    public bool InteractionLocked { get; set; }
    public int TurnNumber { get; private set; }
    public BattleState State => currentState;
    public PlayerData Player => player;
    public bool CanInteract => currentState == BattleState.PlayerTurn && !paused && !resolvingCard && !InteractionLocked && !CosmicSceneLoader.IsLoading;
    public event System.Action<CardData, EnemyData> CardPlayed;
    public event System.Action<EnemyData> EnemyActing;

    public void SetPaused(bool value) { paused = value; }

    public bool CanUseCard(CardData card)
    {
        return CanInteract && card != null && card.EnergyCost >= 0 &&
            deckManager != null && deckManager.CanPlayCard(card) && player != null &&
            !player.IsDead() && player.CurrentEnergy >= card.EnergyCost;
    }

    public bool CanUseCardOn(CardData card, EnemyData enemy)
    {
        if (!CanUseCard(card)) return false;
        if (!RequiresEnemyTarget(card)) return enemy == null;
        if (enemy == null || enemy.IsDead() || enemyManager == null) return false;
        foreach (EnemyData member in enemyManager.Enemies) if (member == enemy) return true;
        return false;
    }

    private void Start()
    {
        StartBattle();
    }

    private void Update()
    {
        if (!CanInteract)
        {
            return;
        }

        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            PlaySelectedCard();
        }

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            EndPlayerTurn();
        }
    }

    private void Awake()
    {
        if (battleTargetManager == null)
        {
            Debug.LogError(
                "BattleManager : BattleTargetManager is null"
            );

            return;
        }

        battleTargetManager.OnEnemySelected += HandleEnemySelected;
    }

    private void StartBattle()
    {
        currentState = BattleState.Start;
        TurnNumber = 0;

        Debug.Log("Battle Started");

        if (!ValidateManagers()) return;
        RunManager activeRun = FindAnyObjectByType<RunManager>();
        if (activeRun == null || !activeRun.IsRunActive())
        {
            CosmicBattleUI screen = FindAnyObjectByType<CosmicBattleUI>();
            if (screen != null) screen.ShowMissingRun();
            return;
        }
        LoadPlayerRunData();

        RefreshBattleUI();

        if (!ValidateManagers())
        {
            return;
        }

        List<EnemyData> spawnedEnemies =
            battleEncounterManager.SpawnEnemies();

        if (spawnedEnemies == null ||
            spawnedEnemies.Count == 0)
        {
            Debug.LogError(
                "BattleManager : Failed to Spawn Enemies"
            );

            return;
        }

        enemyManager.SetEnemies(spawnedEnemies);
        enemyManager.SetPlayer(player);

        Debug.Log(
            "BattleManager : Total Enemies = " +
            enemyManager.Enemies.Count
        );

        deckManager.StartBattleSetup();
        cardDisplayManager.RefreshHand();

        PrepareEnemyActions();

        StartPlayerTurn();
    }

    private bool ValidateManagers()
    {
        if (player == null)
        {
            Debug.LogError(
                "BattleManager : PlayerData is null"
            );

            return false;
        }

        if (deckManager == null)
        {
            Debug.LogError(
                "BattleManager : DeckManager is null"
            );

            return false;
        }

        if (cardDisplayManager == null)
        {
            Debug.LogError(
                "BattleManager : CardDisplayManager is null"
            );

            return false;
        }

        if (cardEffectManager == null)
        {
            Debug.LogError(
                "BattleManager : CardEffectManager is null"
            );

            return false;
        }

        if (enemyManager == null)
        {
            Debug.LogError(
                "BattleManager : EnemyManager is null"
            );

            return false;
        }

        if (battleEncounterManager == null)
        {
            Debug.LogError(
                "BattleManager : BattleEncounterManager is null"
            );

            return false;
        }

        if (battleTargetManager == null)
        {
            Debug.LogError(
                "BattleManager : BattleTargetManager is null"
            );

            return false;
        }

        return true;
    }

    private void RefreshBattleUI()
    {
        if (playerStatusUI == null)
        {
            Debug.LogError(
                "BattleManager : PlayerStatusUI is NULL"
            );

            return;
        }

        if (player == null)
        {
            Debug.LogError(
                "BattleManager : PlayerData is NULL"
            );

            return;
        }
        playerStatusUI.Refresh();
    }

    ///// Player Turn /////

    private void StartPlayerTurn()
    {
        TurnNumber++;
        SetupPlayerTurn();

        currentState = BattleState.PlayerTurn;

        selectedCard = null;

        battleTargetManager.EndTargetSelection();

        battleTargetManager.ClearTarget();

        RefreshBattleUI();

        Debug.Log("Player Turn Started");
    }

    private void SetupPlayerTurn()
    {
        SetupResources();
        SetupCards();
        SetupEffects();
        SetupUI();
    }

    private void SetupResources()
    {
        player.ResetEnergy();
        player.ResetBlock();
    }

    private void SetupCards()
    {
        deckManager.StartPlayerTurnDraw();
        cardDisplayManager.RefreshHand();
    }

    private void SetupEffects()
    {
    }

    private void SetupUI()
    {
    }

    ///// Player Card /////

    public void SelectCard(CardData card)
    {
        if (!CanUseCard(card))
        {
            return;
        }

        if (card == null)
        {
            return;
        }

        selectedCard = card;

        battleTargetManager.EndTargetSelection();

        battleTargetManager.ClearTarget();

        if (RequiresEnemyTarget(card))
        {
            battleTargetManager.BeginTargetSelection();
            
            Debug.Log(
                "BattleManager : Waiting for Enemy Target"
            );

            return;
        }

        battleTargetManager.EndTargetSelection();

        PlaySelectedCard();
    }

    private bool RequiresEnemyTarget(CardData card)
    {
        if (card == null ||
            card.Effects == null)
        {
            return false;
        }

        foreach (CardEffect effect in card.Effects)
        {
            if (effect == null)
            {
                continue;
            }

            if (effect.Target == TargetType.SingleEnemy)
            {
                return true;
            }
        }

        return false;
    }

    public static bool NeedsEnemy(CardData card)
    {
        if (card == null || card.Effects == null) return false;
        foreach (CardEffect effect in card.Effects)
            if (effect != null && effect.Target == TargetType.SingleEnemy) return true;
        return false;
    }

    public static bool TargetsEnemySide(CardData card)
    {
        if (card == null || card.Effects == null) return false;
        foreach (CardEffect effect in card.Effects)
            if (effect != null && effect.Target != TargetType.Player) return true;
        return false;
    }

    public void PlaySelectedCard()
    {
        TryPlayCard(selectedCard, RequiresEnemyTarget(selectedCard) ? battleTargetManager.SelectedEnemy : null);
    }

    public bool TryPlayCard(CardData card, EnemyData targetEnemy)
    {
        if (!CanUseCardOn(card, targetEnemy)) return false;
        // One authority for click and drag: validate before spending or resolving.
        resolvingCard = true;
        try
        {
            if (!player.TryUseEnergy(card.EnergyCost)) return false;
            cardEffectManager.Resolve(card, player, enemyManager.Enemies, targetEnemy);
            deckManager.PlayCard(card);
            selectedCard = null;
            battleTargetManager.EndTargetSelection();
            battleTargetManager.ClearTarget();
            CardPlayed?.Invoke(card, targetEnemy);
            enemyManager.RefreshEnemyUI();
            RefreshBattleUI();
            cardDisplayManager.RefreshHand();
            ProcessBattleEnd();
            return true;
        }
        finally { resolvingCard = false; }
    }

    ///// Player Turn End /////

    public void EndPlayerTurn()
    {
        if (!CanInteract)
        {
            return;
        }

        selectedCard = null;

        battleTargetManager.EndTargetSelection();

        battleTargetManager.ClearTarget();

        deckManager.DiscardHand();

        cardDisplayManager.RefreshHand();

        currentState = BattleState.EnemyTurn;

        Debug.Log("Player Turn Ended");

        StartCoroutine(EnemyTurnSequence());
    }

    private IEnumerator EnemyTurnSequence()
    {
        enemyManager.ResetEnemyBlocks();
        yield return new WaitForSeconds(.35f);
        foreach (EnemyData enemy in enemyManager.Enemies)
        {
            if (enemy == null || enemy.IsDead()) continue;
            EnemyActing?.Invoke(enemy);
            yield return new WaitForSeconds(.15f);
            enemyManager.PerformAction(enemy);
            RefreshBattleUI();
            ProcessBattleEnd();
            if (currentState == BattleState.End) yield break;
            yield return new WaitForSeconds(.45f);
        }
        PrepareEnemyActions();
        EndEnemyTurn();
    }

    ///// Enemy Turn /////

    private void StartEnemyTurn()
    {
        currentState = BattleState.EnemyTurn;

        Debug.Log("Enemy Turn Started");

        enemyManager.ResetEnemyBlocks();

        enemyManager.PerformTurn();

        ProcessBattleEnd();

        if (currentState == BattleState.End)
        {
            return;
        }

        PrepareEnemyActions();
        RefreshBattleUI();

        EndEnemyTurn();
    }

    private void PrepareEnemyActions()
    {
        enemyManager.DecideActions();
    }

    private void EndEnemyTurn()
    {
        Debug.Log("Enemy Turn Ended");

        StartPlayerTurn();
    }

    ///// Enemy /////
  
    private void OnDestroy()
    {
        if (battleTargetManager != null)
        {
            battleTargetManager.OnEnemySelected -= HandleEnemySelected;
        }
    }

    private void HandleEnemySelected(EnemyData enemy)
    {
        if (enemy == null)
        {
            return;
        }

        if (currentState != BattleState.PlayerTurn)
        {
            return;
        }

        if (selectedCard == null)
        {
            return;
        }

        Debug.Log(
            "BattleManager : Enemy Target Received : " +
            enemy.name
        );

        PlaySelectedCard();
    }

    ///// Battle Result /////

    private void ProcessBattleEnd()
    {
        if (currentState == BattleState.End) return;
        if (player.IsDead())
        {
            HandlePlayerLose();
            return;
        }

        if (AreAllEnemiesDead())
        {
            HandlePlayerWin();
            return;
        }
    }

    private bool AreAllEnemiesDead()
    {
        if (enemyManager == null ||
            enemyManager.Enemies == null ||
            enemyManager.Enemies.Count == 0)
        {
            return false;
        }

        foreach (EnemyData enemy in enemyManager.Enemies)
        {
            if (enemy == null)
            {
                continue;
            }

            if (!enemy.IsDead())
            {
                return false;
            }
        }

        return true;
    }

    private void HandlePlayerWin()
    {
        EndBattle();
        Debug.Log("Player Win");

        SavePlayerRunData();

        RunManager runManager =
            FindAnyObjectByType<RunManager>();

        if (runManager == null)
        {
            Debug.LogError("RunManager not found");
            return;
        }

        if (runManager.IsCurrentNodeBoss())
        {
            runManager.ShowRunResult(true);
            return;
        }
        runManager.GoToReward();
    }

    private void HandlePlayerLose()
    {
        EndBattle();
        Debug.Log("Player Loses!");
        RunManager runManager = FindAnyObjectByType<RunManager>();
        if (runManager != null) runManager.ShowRunResult(false);
    }

    private void EndBattle()
    {
        currentState = BattleState.End;

        Debug.Log("Battle Ended");
    }

    ///// Player Run Data /////

    private void LoadPlayerRunData()
    {
        RunManager runManager =
            FindAnyObjectByType<RunManager>();

        if (runManager == null)
        {
            Debug.LogError(
                "BattleManager : RunManager not found"
            );

            return;
        }

        if (runManager.CurrentRun == null)
        {
            Debug.LogError(
                "BattleManager : CurrentRun is null"
            );

            return;
        }

        if (runManager.CurrentRun.Player == null)
        {
            Debug.LogError(
                "BattleManager : PlayerRunData is null"
            );

            return;
        }

        if (player == null)
        {
            Debug.LogError(
                "BattleManager : PlayerData is null"
            );

            return;
        }

        player.SetMaxHP(
            runManager.CurrentRun.Player.MaxHP
        );

        player.SetCurrentHP(
            runManager.CurrentRun.Player.CurrentHP
        );

        Debug.Log(
            "Player HP Loaded : " +
            player.GetCurrentHP() +
            " / " +
            player.GetMaxHP()
        );
    }

    private void SavePlayerRunData()
    {
        RunManager runManager =
            FindAnyObjectByType<RunManager>();

        if (runManager == null)
        {
            Debug.LogError(
                "BattleManager : RunManager not found"
            );

            return;
        }

        if (runManager.CurrentRun == null)
        {
            Debug.LogError(
                "BattleManager : CurrentRun is null"
            );

            return;
        }

        if (runManager.CurrentRun.Player == null)
        {
            Debug.LogError(
                "BattleManager : PlayerRunData is null"
            );

            return;
        }

        if (player == null)
        {
            Debug.LogError(
                "BattleManager : PlayerData is null"
            );

            return;
        }

        int currentHP =
            player.GetCurrentHP();

        runManager.CurrentRun.Player.SetCurrentHP(
            currentHP
        );

        Debug.Log(
            "Player HP Saved : " +
            runManager.CurrentRun.Player.CurrentHP +
            " / " +
            runManager.CurrentRun.Player.MaxHP
        );
    }
}
