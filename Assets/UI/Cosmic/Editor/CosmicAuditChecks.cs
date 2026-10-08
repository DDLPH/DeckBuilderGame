using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class CosmicAuditChecks
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MapScene.unity");
        foreach (string path in new[] { "MainMenu", "MapScene", "BattleScene", "RewardScene", "RestScene", "ShopScene" })
        {
            bool enabled = false;
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
                if (scene.enabled && scene.path == "Assets/Scenes/" + path + ".unity") enabled = true;
            Require(enabled, "Missing enabled build scene: " + path);
        }
        MapGenerator generator = UnityEngine.Object.FindAnyObjectByType<MapGenerator>();
        for (int seed = 0; seed < 100; seed++)
        {
            UnityEngine.Random.InitState(seed); generator.GenerateMap();
            RunSaveSystem.SaveData data = Snapshot(generator.CurrentMap);
            Require(RunSaveSystem.Validate(data), "Invalid generated map at seed " + seed);
            Require(RunSaveSystem.Validate(JsonUtility.FromJson<RunSaveSystem.SaveData>(JsonUtility.ToJson(data))), "JSON round trip failed");
        }
        var valid = Snapshot(generator.CurrentMap);
        valid.version = 99; Require(!RunSaveSystem.Validate(valid), "Unknown save version accepted"); valid.version = 1;
        valid.currentHP = 0; Require(!RunSaveSystem.Validate(valid), "Dead run accepted"); valid.currentHP = 80;
        valid.pendingEvent = true; Require(!RunSaveSystem.Validate(valid), "Non-event pending flag accepted"); valid.pendingEvent = false;
        valid.nodes[valid.startNode].connections.Add(valid.startNode);
        Require(!RunSaveSystem.Validate(valid), "Cyclic save accepted");
        valid = Snapshot(generator.CurrentMap); valid.currentNode = 999;
        Require(!RunSaveSystem.Validate(valid), "Invalid node index accepted");
        valid = Snapshot(generator.CurrentMap); valid.cards.Clear();
        Require(!RunSaveSystem.Validate(valid), "Empty deck accepted");
        RunManager manager = UnityEngine.Object.FindAnyObjectByType<RunManager>();
        MethodInfo find = typeof(RunManager).GetMethod("FindSavedCard", BindingFlags.Instance | BindingFlags.NonPublic);
        foreach (string id in AssetDatabase.FindAssets("t:CardData"))
        {
            CardData card = AssetDatabase.LoadAssetAtPath<CardData>(AssetDatabase.GUIDToAssetPath(id));
            Require((CardData)find.Invoke(manager, new object[] { card.name }) == card, "Unrestorable card: " + card.name);
        }
        GameObject music = GameObject.Find("MapMusic");
        Require(music != null && music.GetComponent<CosmicMapMusic>() != null, "Map music controller missing");
        AudioSource source = music.GetComponent<AudioSource>();
        Require(source.clip != null && source.loop && source.spatialBlend == 0, "Map music configuration invalid");
        var run = new RunData(); run.StartRun(); run.FailRun(); Require(run.State == RunState.Failed, "Failed run still active");
        Debug.Log("Audit checks passed: 100 map/save JSON round trips, invalid-save guards, all card restores, failed state, scene list and map music.");
        CheckAtomicSave(Snapshot(generator.CurrentMap));

        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        Canvas canvas = GameObject.Find("Canvas").GetComponent<Canvas>();
        Button start = GameObject.Find("StartButton").GetComponent<Button>();
        bool interactable = start.interactable;
        int calls = 0;
        TMP_FontAsset english = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/Cosmic/Fonts/Cinzel SDF.asset");
        TMP_FontAsset thai = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/Cosmic/Fonts/NotoSerifThai SDF.asset");
        CosmicModalDialog modal = CosmicModalDialog.Show(canvas, english, "START A NEW RUN?",
            "You already have a saved run. Starting a new run will replace its progress. Do you want to start over?",
            "NEW RUN", () => calls++, "CANCEL");
        Require(!start.interactable, "Menu is clickable behind the modal");
        Render(canvas, "new-run-confirmation-en.png");
        modal.Close(false);
        Require(calls == 0 && start.interactable == interactable, "Cancel did not preserve menu state");
        modal = CosmicModalDialog.Show(canvas, thai, "เริ่มการเดินทางใหม่?",
            "คุณมีเซฟการเดินทางอยู่แล้ว หากเริ่มใหม่ ความคืบหน้าของรอบเดิมจะถูกแทนที่ ต้องการเริ่มใหม่หรือไม่?",
            "เริ่มใหม่", () => calls++, "ยกเลิก");
        Render(canvas, "new-run-confirmation-th.png");
        modal.Close(true); modal.Close(true);
        Require(calls == 1 && start.interactable == interactable, "Confirm callback repeated or state not restored");
        Debug.Log("Modal checks passed: background blocked, cancellation safe, confirmation once only; EN/TH previews rendered. No player saves or preferences changed.");
    }
    private static RunSaveSystem.SaveData Snapshot(MapData map)
    {
        var data = new RunSaveSystem.SaveData { gold = 100, maxHP = 80, currentHP = 80 };
        data.cards.Add("Strike");
        var nodes = new List<MapNode>(map.Nodes);
        data.startNode = data.currentNode = nodes.IndexOf(map.StartNode);
        foreach (MapNode node in nodes)
        {
            var item = new RunSaveSystem.NodeSave { type = (int)node.NodeType, layer = node.LayerIndex };
            foreach (MapNode next in node.Connections) item.connections.Add(nodes.IndexOf(next));
            data.nodes.Add(item);
        }
        return data;
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void CheckAtomicSave(RunSaveSystem.SaveData data)
    {
        string directory = Path.Combine("C:/Users/iiTzDemmy/Desktop/GameProject", "audit-save-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "checkpoint.json");
        MethodInfo write = typeof(RunSaveSystem).GetMethod("WriteSnapshot", BindingFlags.Static | BindingFlags.NonPublic);
        MethodInfo read = typeof(RunSaveSystem).GetMethod("TryReadFile", BindingFlags.Static | BindingFlags.NonPublic);
        try
        {
            write.Invoke(null, new object[] { data, path });
            data.gold = 125; write.Invoke(null, new object[] { data, path });
            object[] args = { path, null };
            Require((bool)read.Invoke(null, args) && ((RunSaveSystem.SaveData)args[1]).gold == 125, "Atomic primary write failed");
            args = new object[] { path + ".bak", null };
            Require((bool)read.Invoke(null, args) && ((RunSaveSystem.SaveData)args[1]).gold == 100, "Backup checkpoint missing");
            File.WriteAllText(path, "broken-json"); args = new object[] { path, null };
            Require(!(bool)read.Invoke(null, args), "Corrupt save was accepted");
            args = new object[] { path + ".bak", null };
            Require((bool)read.Invoke(null, args), "Backup cannot recover after corrupt primary");
            Require(!File.Exists(path + ".tmp"), "Temporary save was not consumed");
            Debug.Log("Atomic disk checks passed: replacement, backup checkpoint and corrupt-primary recovery, using isolated test files.");
        }
        finally
        {
            foreach (string file in new[] { path, path + ".bak", path + ".tmp" }) if (File.Exists(file)) File.Delete(file);
            Directory.Delete(directory);
        }
    }
    public static void Render(Canvas canvas, string filename)
    {
        GameObject cameraObject = new GameObject("AuditCamera", typeof(Camera));
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.transform.position = new Vector3(0, 0, -10); camera.orthographic = true; camera.orthographicSize = 5.4f;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
        var target = new RenderTexture(1920, 1080, 24); target.Create(); camera.targetTexture = target;
        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 10;
        foreach (TMP_Text text in canvas.GetComponentsInChildren<TMP_Text>()) text.ForceMeshUpdate();
        Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
        var image = new Texture2D(1920, 1080, TextureFormat.RGBA32, false);
        image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); image.Apply();
        File.WriteAllBytes(Path.Combine("C:/Users/iiTzDemmy/Desktop/GameProject", filename), image.EncodeToPNG());
        RenderTexture.active = null; camera.targetTexture = null; target.Release();
        UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(cameraObject);
    }
}
