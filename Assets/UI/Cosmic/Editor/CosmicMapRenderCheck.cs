using System;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CosmicMapRenderCheck
{
    public static void CheckNewRunMap()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        var start = GameObject.Find("StartButton").GetComponent<UnityEngine.UI.Button>();
        bool wired = false;
        for (int i = 0; i < start.onClick.GetPersistentEventCount(); i++)
            wired |= start.onClick.GetPersistentTarget(i) is MainMenuManager &&
                start.onClick.GetPersistentMethodName(i) == "StartGame";
        if (!wired) throw new InvalidOperationException("New Run button is not wired to StartGame.");

        EditorSceneManager.OpenScene("Assets/Scenes/MapScene.unity");
        var generator = UnityEngine.Object.FindAnyObjectByType<MapGenerator>();
        var manager = UnityEngine.Object.FindAnyObjectByType<RunManager>();
        var navigation = UnityEngine.Object.FindAnyObjectByType<MapManager>();
        var ui = UnityEngine.Object.FindAnyObjectByType<MapUIManager>();
        ui.SetRunManager(manager);
        ui.RefreshMapUI(); // A premature refresh must recover after data becomes available.
        MapData previous = null;
        for (int seed = 0; seed < 3; seed++)
        {
            UnityEngine.Random.InitState(seed);
            generator.GenerateMap();
            if (generator.CurrentMap == null || generator.CurrentMap == previous)
                throw new InvalidOperationException("New graph was not generated.");
            var run = new RunData();
            run.SetMap(generator.CurrentMap);
            run.SetCurrentNode(generator.CurrentMap.StartNode);
            run.StartRun();
            typeof(RunManager).GetProperty("CurrentRun").SetValue(manager, run);
            navigation.SetCurrentNode(run.CurrentNode);
            ui.RefreshMapUI();
            ui.RefreshMapUI(); // Same graph: no duplicates or reroll.
            if (ui.GetDisplayedNodeCount() != generator.CurrentMap.Nodes.Count)
                throw new InvalidOperationException("Map UI missing nodes or retaining old nodes.");
            foreach (var art in UnityEngine.Object.FindObjectsByType<CosmicMapNodeArt>())
            {
                bool belongs = false;
                foreach (MapNode node in generator.CurrentMap.Nodes) belongs |= art.Node == node;
                if (!belongs) throw new InvalidOperationException("Old map button remains active.");
            }
            ValidateNodeStates(manager, ui, run);
            previous = generator.CurrentMap;
        }
        Debug.Log("New Run map checks passed: menu binding, early refresh recovery, 3 fresh graphs, no stale/duplicate nodes and valid navigation.");
        // Tests use in-memory runs only; never overwrite the player's save.
        CosmicAuditChecks.Run();
        Render();
    }

    public static void Render()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MapScene.unity");
        UnityEngine.Random.InitState(4317);
        MapGenerator generator=UnityEngine.Object.FindAnyObjectByType<MapGenerator>();
        RunManager manager=UnityEngine.Object.FindAnyObjectByType<RunManager>();
        MapManager navigation=UnityEngine.Object.FindAnyObjectByType<MapManager>();
        MapUIManager ui=UnityEngine.Object.FindAnyObjectByType<MapUIManager>();
        generator.GenerateMap();
        RunData run=new RunData();run.SetMap(generator.CurrentMap);run.SetCurrentNode(generator.CurrentMap.StartNode);
        run.AddGold(100);run.StartRun();
        CardData strike=AssetDatabase.LoadAssetAtPath<CardData>("Assets/ScriptableObjects/Cards/Strike.asset");
        CardData defend=AssetDatabase.LoadAssetAtPath<CardData>("Assets/ScriptableObjects/Cards/Defend.asset");
        for(int i=0;i<5;i++){run.Deck.AddCard(strike);run.Deck.AddCard(defend);}
        typeof(RunManager).GetProperty("CurrentRun").SetValue(manager,run);
        navigation.SetCurrentNode(run.CurrentNode);
        ui.SetRunManager(manager);ui.RefreshMapUI();
        ValidateNodeStates(manager, ui, run);

        GameObject cameraObject=new GameObject("PreviewRenderCamera",typeof(Camera));
        Camera camera=cameraObject.GetComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;
        camera.backgroundColor=Color.black;camera.orthographic=true;camera.orthographicSize=5.4f;
        camera.transform.position=new Vector3(0f,0f,-10f);camera.nearClipPlane=.1f;camera.farClipPlane=100f;
        RenderTexture target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32);
        target.Create();camera.targetTexture=target;
        Canvas canvas=GameObject.Find("MapCanvas").GetComponent<Canvas>();
        canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=10f;
        Canvas.ForceUpdateCanvases();
        MapHudUI hud=UnityEngine.Object.FindAnyObjectByType<MapHudUI>();
        Invoke(hud,"Start");Invoke(hud,"LateUpdate");Invoke(hud,"Refresh");
        foreach(TMP_Text text in UnityEngine.Object.FindObjectsByType<TMP_Text>())text.ForceMeshUpdate();
        Canvas.ForceUpdateCanvases();camera.Render();
        RenderTexture.active=target;
        Texture2D image=new Texture2D(1920,1080,TextureFormat.RGBA32,false);
        image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();
        string path="C:/Users/iiTzDemmy/Desktop/GameProject/map-illustrated-preview.png";
        File.WriteAllBytes(path,image.EncodeToPNG());
        RenderTexture.active=null;camera.targetTexture=null;
        target.Release();UnityEngine.Object.DestroyImmediate(target);
        UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(cameraObject);
        Debug.Log("Map preview rendered: "+path+" | Nodes: "+ui.GetDisplayedNodeCount());
        // This check neither starts Play mode nor writes a run save or scene.
    }
    private static void Invoke(object target,string name)
    {target.GetType().GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(target,null);}

    private static void ValidateNodeStates(RunManager manager, MapUIManager ui, RunData run)
    {
        CosmicMapNodeArt[] nodes = UnityEngine.Object.FindObjectsByType<CosmicMapNodeArt>();
        MapNode next = null;
        foreach (CosmicMapNodeArt art in nodes)
        {
            if (art.IsAvailable != manager.CanMoveToNode(art.Node))
                throw new InvalidOperationException("Map button availability does not match navigation.");
            if (art.IsAvailable) next = art.Node;
        }
        if (next == null) throw new InvalidOperationException("No starting path is available.");
        MapNode start = run.CurrentNode;
        run.SetCurrentNode(next);
        ui.RefreshMapUI();
        foreach (CosmicMapNodeArt art in nodes)
        {
            if (art.IsAvailable != manager.CanMoveToNode(art.Node))
                throw new InvalidOperationException("Map button state was not refreshed after movement.");
        }
        foreach (MapConnectionUI route in UnityEngine.Object.FindObjectsByType<MapConnectionUI>())
            Invoke(route, "Update");
        run.SetCurrentNode(start);
        ui.RefreshMapUI();
        foreach (MapConnectionUI route in UnityEngine.Object.FindObjectsByType<MapConnectionUI>())
            Invoke(route, "Update");
        Debug.Log("Map state checks passed: available paths and current-node refresh.");
    }
}
