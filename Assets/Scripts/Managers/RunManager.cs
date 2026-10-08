using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class RunManager : MonoBehaviour
{
    public RunData CurrentRun { get; private set; }

    [Header("Map")]
    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private MapManager mapManager;
    [SerializeField] private MapUIManager mapUIManager;

    [Header("Starter Deck")]
    [SerializeField] private CardData strikeCard;
    [SerializeField] private CardData defendCard;
    [SerializeField] private CardData[] saveableCards;
    [SerializeField] private TMP_FontAsset englishFont;
    [SerializeField] private TMP_FontAsset thaiFont;

    private static RunManager instance;
    private static bool continueRequested;
    private bool eventPending;

    public static void RequestContinue() { continueRequested = true; }
    public static void RequestNewRun() { continueRequested = false; }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }
    }
    
    private void Start()
    {
        if (mapGenerator == null)
        {
            Debug.LogError("MapGenerator is not assigned");
            return;
        }
    
        if (mapUIManager == null || strikeCard == null || defendCard == null)
        {
            Debug.LogError("Run cannot start: map UI or starter cards are missing.");
            return;
        }

        if (mapManager == null)
        {
            Debug.LogError("MapManager is not assigned");
            return;
        }
    
        bool loadSave = continueRequested;
        continueRequested = false;
        if (loadSave && TryRestoreRun())
        {
            mapUIManager.SetRunManager(this);
            mapUIManager.RefreshMapUI();
            if (eventPending) MapEventOverlay.Show(this);
            return;
        }

        if (loadSave)
        {
            Debug.LogError("Continue failed. Existing save was kept; no new run was started.");
            SceneManager.LoadScene("MainMenu");
            Destroy(gameObject);
            return;
        }

        mapGenerator.GenerateMap();
        StartNewRun();
    }

    public void StartNewRun()
    {
        eventPending = false;
        CurrentRun = new RunData();

        Debug.Log(
            "Player Run Data Created | HP : " +
            CurrentRun.Player.CurrentHP +
            " / " +
            CurrentRun.Player.MaxHP
        );

        CurrentRun.AddGold(100);

        InitializeStarterDeck();

        Debug.Log("New Run Started");
        Debug.Log("Starting Gold : " + CurrentRun.Gold);

        if (mapGenerator == null)
        {
            Debug.LogError("MapGenerator is not assigned");
            return;
        }

        if (mapGenerator.CurrentMap == null)
        {
            Debug.LogError("Current Map is null");
            return;
        }

        CurrentRun.SetMap(mapGenerator.CurrentMap);

        if (mapGenerator.CurrentMap.StartNode == null)
        {
            Debug.LogError("Start Node is null");
            return;
        }

        CurrentRun.SetCurrentNode(mapGenerator.CurrentMap.StartNode);
        mapManager.SetStartNode(mapGenerator.CurrentMap.StartNode);

        CurrentRun.StartRun();
        SaveCheckpoint();

        /*
        Debug.Log("Run Active : " + IsRunActive());
        Debug.Log("Map Current Node : " + mapManager.CurrentNode);

        Debug.Log("Run Start Node Set");
        Debug.Log("Has Run Started : " + CurrentRun.HasStarted);
        Debug.Log("Current Node Type : " + CurrentRun.CurrentNode.NodeType);
        Debug.Log("Run State : " + CurrentRun.State);
        Debug.Log("Is Run Active : " + IsRunActive());
        Debug.Log("Map and Run Synchronized : " + IsMapAndRunNodeSynchronized());
        */

        mapManager.LogAvailableNodes();

        if (mapUIManager != null)
        {
            mapUIManager.RefreshMapUI();
        }
        else
        {
            Debug.LogError("MapUIManager is not assigned");
        }
    }

    private void InitializeStarterDeck()
    {
        if (strikeCard == null || defendCard == null)
        {
            Debug.LogError("Starter Deck CardData is missing");
            return;
        }

        for (int i = 0; i < 5; i++)
        {
            CurrentRun.Deck.AddCard(strikeCard);
        }

        for (int i = 0; i < 5; i++)
        {
            CurrentRun.Deck.AddCard(defendCard);
        }

        Debug.Log("Starter Deck Initialized : " + CurrentRun.Deck.Count + " cards");
    }
    
    public void AddGold(int amount)
    {
        if (CurrentRun == null)
        {
            Debug.LogError("Cannot add Gold because CurrentRun is null");
            return;
        }

        CurrentRun.AddGold(amount);

        Debug.Log("Current Gold : " + CurrentRun.Gold);
    }

    public void SetCurrentNode(MapNode node)
    {
        CurrentRun.SetCurrentNode(node);
    }

    public MapNode GetCurrentNode()
    {
        if (CurrentRun == null)
        {
            return null;
        }

        return CurrentRun.CurrentNode;
    }

    public RunState GetRunState()
    {
        if (CurrentRun == null)
        {
            return RunState.NotStarted;
        }

        return CurrentRun.State;
    }

    public bool IsRunActive()
    {
        return CurrentRun != null && CurrentRun.State == RunState.Playing;
    }

    public void MoveToNode(MapNode node)
    {
        if (CurrentRun == null)
        {
            Debug.LogError("Cannot move because CurrentRun is null");
            return;
        }

        if (mapManager == null)
        {
            Debug.LogError("Cannot move because MapManager is null");
            return;
        }

        if (!IsRunActive())
        {
            Debug.LogWarning("Cannot move because Run is not active");
            return;
        }

        if (eventPending) return;

        if (!CanMoveToNode(node))
        {
            Debug.LogWarning("Cannot move to this node");
            return;
        }

        bool moveSucceeded = mapManager.MoveToNode(node);

        if (!moveSucceeded)
        {
            Debug.LogWarning("RunManager Move Failed");
            return;
        }

        CurrentRun.SetCurrentNode(node);

        Debug.Log(
            "MOVE NODE : " +
            node.NodeType +
            " | Layer : " +
            node.LayerIndex
        );

        Debug.Log(
            "NEXT NODE COUNT : " +
            node.Connections.Count
        );

        for (int i = 0; i < node.Connections.Count; i++)
        {
            Debug.Log(
                "NEXT NODE " +
                i +
                " : " +
                node.Connections[i].NodeType +
                " | Layer : " +
                node.Connections[i].LayerIndex
            );
        }

        Debug.Log("RunManager Moved To Node");
        Debug.Log("Current Node Type : " + CurrentRun.CurrentNode.NodeType);

        // Battle, shop, and rest are saved only after returning to the map.
        // Quitting mid-encounter therefore restores the previous safe node.
        eventPending = node.NodeType == MapNodeType.Event;
        if (eventPending) SaveCheckpoint();
        HandleNodeSceneTransition();
    }

    public bool CanMoveToNode(MapNode node)
    {
        if (eventPending) return false;
        /*
        Debug.Log(
            "===== CanMoveToNode CHECK ====="
        );

        Debug.Log(
            "CurrentRun : " +
            (CurrentRun != null)
        );

        Debug.Log(
            "MapManager : " +
            (mapManager != null)
        );

        Debug.Log(
            "Run Active : " +
            IsRunActive()
        )
        */

        if (CurrentRun == null)
        {
            Debug.Log(
                "CanMoveToNode FALSE : CurrentRun is null"
            );

            return false;
        }

        if (mapManager == null)
        {
            Debug.Log(
                "CanMoveToNode FALSE : MapManager is null"
            );

            return false;
        }

        if (!IsRunActive())
        {
            Debug.Log(
                "CanMoveToNode FALSE : Run is not active"
            );

            Debug.Log(
                "Current Run State : " +
                CurrentRun.State
            );

            return false;
        }

        bool canMove =
            mapManager.CanMoveToNode(node);

        /*
        Debug.Log(
            "MapManager CanMove : " +
            canMove
        );

        Debug.Log(
            "=============================="
        );
        */

        return canMove;
    }

    private void HandleNodeSceneTransition()
    {
        if (CurrentRun == null)
        {
            return;
        }

        if (CurrentRun.CurrentNode == null)
        {
            return;
        }

        switch (CurrentRun.CurrentNode.NodeType)
        {
            case MapNodeType.NormalBattle:
                SceneManager.LoadScene("BattleScene");
                break;

            case MapNodeType.Elite:
                SceneManager.LoadScene("BattleScene");
                break;

            case MapNodeType.Boss:
                SceneManager.LoadScene("BattleScene");
                break;

            case MapNodeType.Rest:
                SceneManager.LoadScene("RestScene");
                break;

            case MapNodeType.Shop:
                SceneManager.LoadScene("ShopScene");
                break;

            case MapNodeType.Event:
                MapEventOverlay.Show(this);
                break;

            default:
                Debug.LogWarning(
                    "No Scene assigned for Node Type: "
                    + CurrentRun.CurrentNode.NodeType
                );
                break;
        }
    }

    public IReadOnlyList<MapNode> GetAvailableNodes()
    {
        if (mapManager == null)
        {
            Debug.LogError("Cannot get available nodes because MapManager is null");
            return null;
        }

        return mapManager.AvailableNodes;
    }

    public IReadOnlyList<MapNode> GetAllMapNodes()
    {
        if (mapGenerator == null)
        {
            Debug.LogError("Cannot get all map nodes because MapGenerator is null");
            return null;
        }

        if (mapGenerator.CurrentMap == null)
        {
            Debug.LogError("Cannot get all map nodes because CurrentMap is null");
            return null;
        }

        return mapGenerator.CurrentMap.Nodes;
    }

    public bool IsMapAndRunNodeSynchronized()
    {
        if (CurrentRun == null)
        {
            return false;
        }

        if (mapManager == null)
        {
            return false;
        }

        return CurrentRun.CurrentNode == mapManager.CurrentNode;
    }

    public void LogCurrentRunNode()
    {
        if (CurrentRun == null)
        {
            Debug.LogError("CurrentRun is null");
            return;
        }
    
        if (CurrentRun.CurrentNode == null)
        {
            Debug.LogError("CurrentRun CurrentNode is null");
            return;
        }
    
        Debug.Log("Run Current Node Type : " + CurrentRun.CurrentNode.NodeType);
    }

    public bool IsCurrentNodeBoss()
    {
        if (CurrentRun == null)
        {
            return false;
        }

        if (CurrentRun.CurrentNode == null)
        {
            return false;
        }

        return CurrentRun.CurrentNode.NodeType == MapNodeType.Boss;
    }

    public void CompleteRun()
    {
        if (CurrentRun == null)
        {
            return;
        }
    
        CurrentRun.CompleteRun();
        RunSaveSystem.Delete();
    
        Debug.Log("Run Completed");
    
        if (mapUIManager != null)
        {
            mapUIManager.HideMapUI();
        }
    }

    private void OnSceneLoaded(
        Scene scene,
        LoadSceneMode mode)
    {
        if (scene.name == "MainMenu")
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (instance == this) instance = null;
            Destroy(gameObject);
            return;
        }
        /*
        Debug.Log(
            "===== SCENE LOADED ===== " +
            scene.name
        );

        Debug.Log(
            "RunManager Instance : " +
            (instance == this)
        );

        Debug.Log(
            "CurrentRun Exists : " +
            (CurrentRun != null)
        );
        */

        if (CurrentRun != null)
        {
            Debug.Log(
                "CurrentRun State : " +
                CurrentRun.State
            );

            Debug.Log(
                "CurrentRun Node : " +
                (
                    CurrentRun.CurrentNode != null
                    ? CurrentRun.CurrentNode.NodeType.ToString()
                    : "NULL"
                )
            );
        }

        if (scene.name != "MapScene")
        {
            return;
        }

        if (CurrentRun == null)
        {
            Debug.LogWarning(
                "MapScene Loaded But CurrentRun is NULL"
            );

            return;
        }

        mapGenerator =
            FindFirstObjectByType<MapGenerator>();

        mapManager =
            FindFirstObjectByType<MapManager>();

        mapUIManager =
            FindFirstObjectByType<MapUIManager>();

        if (mapGenerator == null)
        {
            Debug.LogError("MapGenerator not found in MapScene");
            return;
        }

        if (mapManager == null)
        {
            Debug.LogError("MapManager not found in MapScene");
            return;
        }

        if (mapUIManager == null)
        {
            Debug.LogError("MapUIManager not found in MapScene");
            return;
        }

        mapUIManager.SetRunManager(this);

        if (CurrentRun.CurrentMap == null)
        {
            Debug.LogError("CurrentRun CurrentMap is null");
            return;
        }

        if (CurrentRun.CurrentNode == null)
        {
            Debug.LogError("CurrentRun CurrentNode is null");
            return;
        }

        mapGenerator.SetCurrentMap(
            CurrentRun.CurrentMap
        );

        mapManager.SetCurrentNode(
            CurrentRun.CurrentNode
        );

        mapUIManager.RefreshMapUI();
        SaveCheckpoint();
        if (eventPending) MapEventOverlay.Show(this);
    }

    public void GoToReward()
    {
        if (!IsRunActive())
        {
            Debug.LogWarning(
                "Cannot go to Reward because Run is not active"
            );
    
            return;
        }
    
        SceneManager.LoadScene("RewardScene");
    }

    public void ReturnToMap()
    {
        if (CurrentRun == null)
        {
            Debug.LogError("Cannot return to Map because CurrentRun is null");
            return;
        }

        if (!IsRunActive())
        {
            Debug.LogWarning("Cannot return to Map because Run is not active");
            return;
        }

        Debug.Log("Returning To MapScene");

        SceneManager.LoadScene("MapScene");
    }

    public void ResolveEvent(bool chooseGold)
    {
        if (!eventPending || !IsRunActive()) return;
        if (chooseGold) CurrentRun.AddGold(25);
        else CurrentRun.Player.Heal(12);
        eventPending = false;
        SaveCheckpoint();
        if (mapUIManager != null) mapUIManager.RefreshMapUI();
    }

    public void SaveAndReturnToMenu()
    {
        if (eventPending) return;
        if (!SaveCheckpoint())
        {
            Debug.LogError("Could not return to menu because the run did not save.");
            MapHudUI hud = FindAnyObjectByType<MapHudUI>();
            bool thai = CosmicLanguage.IsThai;
            CosmicModalDialog.Show(GameObject.Find("MapCanvas").GetComponent<Canvas>(),
                hud == null ? null : hud.GetFont(thai),
                thai ? "บันทึกไม่สำเร็จ" : "SAVE FAILED",
                thai ? "ยังไม่กลับเมนูเพื่อป้องกันความคืบหน้าหาย กรุณาตรวจพื้นที่ว่างและสิทธิ์เขียนไฟล์ แล้วลองอีกครั้ง"
                    : "Your run is still open. Check free disk space and file permissions, then try again.",
                thai ? "ตกลง" : "OK", null);
            return;
        }
        SceneManager.sceneLoaded -= OnSceneLoaded;
        instance = null;
        Destroy(gameObject);
        SceneManager.LoadScene("MainMenu");
    }

    public bool SaveCheckpoint()
    {
        return IsRunActive() && RunSaveSystem.TryWrite(CurrentRun, eventPending);
    }

    public void FailRun()
    {
        if (CurrentRun == null) return;
        CurrentRun.FailRun();
        eventPending = false;
        RunSaveSystem.Delete();
    }

    public void ShowRunResult(bool won)
    {
        if (!IsRunActive()) return;
        if (won) CompleteRun(); else FailRun();
        bool thai = CosmicLanguage.IsThai;
        CosmicModalDialog.Show(FindAnyObjectByType<Canvas>(), thai ? thaiFont : englishFont,
            won ? (thai ? "พ้นจากความว่างเปล่า" : "RUN COMPLETE")
                : (thai ? "การเดินทางสิ้นสุด" : "RUN ENDED"),
            won ? (thai ? "คุณเอาชนะบอสได้แล้ว การเดินทางรอบนี้เสร็จสิ้น" : "You defeated the boss. This run is complete.")
                : (thai ? "คุณพ่ายแพ้ในรอบนี้ กลับไปเริ่มการเดินทางครั้งใหม่ได้ที่เมนูหลัก" : "You fell in battle. Return to the menu to begin a new journey."),
            thai ? "กลับเมนูหลัก" : "MAIN MENU", () => SceneManager.LoadScene("MainMenu"));
    }

    private bool TryRestoreRun()
    {
        if (!RunSaveSystem.TryRead(out RunSaveSystem.SaveData save)) return false;

        List<CardData> cards = new List<CardData>();
        foreach (string cardName in save.cards)
        {
            CardData card = FindSavedCard(cardName);
            if (card == null)
            {
                Debug.LogError("Card missing from save catalogue: " + cardName);
                return false;
            }
            cards.Add(card);
        }

        MapData map = new MapData();
        List<MapNode> nodes = new List<MapNode>();
        foreach (RunSaveSystem.NodeSave stored in save.nodes)
        {
            MapNode node = new MapNode((MapNodeType)stored.type, stored.layer);
            if (stored.visited) node.MarkVisited();
            map.AddNode(node);
            nodes.Add(node);
        }
        for (int i = 0; i < nodes.Count; i++)
            foreach (int next in save.nodes[i].connections)
                nodes[i].AddConnection(nodes[next]);
        map.SetStartNode(nodes[save.startNode]);

        RunData restored = new RunData();
        restored.SetMap(map);
        restored.SetCurrentNode(nodes[save.currentNode]);
        restored.Player.SetMaxHP(save.maxHP);
        restored.Player.SetCurrentHP(save.currentHP);
        restored.AddGold(save.gold);
        foreach (CardData card in cards) restored.Deck.AddCard(card);
        restored.StartRun();

        CurrentRun = restored;
        eventPending = save.pendingEvent;
        mapGenerator.SetCurrentMap(map);
        mapManager.SetCurrentNode(restored.CurrentNode);
        Debug.Log("Run restored from disk.");
        return true;
    }

    private CardData FindSavedCard(string savedName)
    {
        if (strikeCard != null && strikeCard.name == savedName) return strikeCard;
        if (defendCard != null && defendCard.name == savedName) return defendCard;
        if (strikeCard != null && strikeCard.UpgradedCard != null &&
            strikeCard.UpgradedCard.name == savedName) return strikeCard.UpgradedCard;
        if (defendCard != null && defendCard.UpgradedCard != null &&
            defendCard.UpgradedCard.name == savedName) return defendCard.UpgradedCard;
        if (saveableCards != null)
            foreach (CardData card in saveableCards)
                if (card != null && card.name == savedName) return card;
        return null;
    }

}
