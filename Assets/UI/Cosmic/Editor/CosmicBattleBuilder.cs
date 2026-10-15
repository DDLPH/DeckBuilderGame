using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class CosmicBattleBuilder
{
    [MenuItem("Tools/Cosmic UI/Build Battle Screen")]
    public static void Apply()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/BattleScene.unity");
        var canvas = GameObject.Find("Canvas").GetComponent<Canvas>();
        if (canvas.GetComponent<CosmicBattleUI>() != null)
        { Debug.Log("Battle already configured; no scene changes made."); return; }

        var legacy = new GameObject("LegacyBattleUI", typeof(RectTransform), typeof(CanvasGroup));
        legacy.transform.SetParent(canvas.transform, false);
        var previous = new System.Collections.Generic.List<Transform>();
        foreach (Transform child in canvas.transform) if (child != legacy.transform) previous.Add(child);
        foreach (Transform child in previous) child.SetParent(legacy.transform, false);
        var legacyGroup = legacy.GetComponent<CanvasGroup>();
        legacyGroup.alpha = 0; legacyGroup.interactable = false; legacyGroup.blocksRaycasts = false;
        // Keep old spawn/data references alive, but old visuals and input cannot compete.
        foreach (BattleUIManager old in Object.FindObjectsByType<BattleUIManager>()) old.enabled = false;

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        var ui = canvas.gameObject.AddComponent<CosmicBattleUI>();
        ui.englishFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/Cosmic/Fonts/Cinzel SDF.asset");
        ui.thaiFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/Cosmic/Fonts/NotoSerifThai SDF.asset");
        ui.background = Sprite("BattleLandscape"); ui.wanderer = Sprite("BattleWanderer");
        ui.pilgrim = Sprite("BattlePilgrim"); ui.sovereign = Sprite("BattleSovereign");
        ui.musicClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Waking_at_the_Gates.mp3");
        ui.clickClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/01_soft_tap.wav");
        ui.BuildLayout();
        var camera = Object.FindAnyObjectByType<Camera>();
        if (camera != null) camera.backgroundColor = CosmicUITheme.Panel;
        EditorUtility.SetDirty(ui);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        EditorSceneManager.SaveScene(canvas.gameObject.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Cosmic Battle screen installed; original gameplay references retained in LegacyBattleUI.");
    }

    private static Sprite Sprite(string name)
    {
        string path = "Assets/UI/Cosmic/Art/" + name + ".png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
        importer.maxTextureSize = 2048; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
