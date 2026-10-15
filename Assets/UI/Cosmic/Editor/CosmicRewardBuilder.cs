using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class CosmicRewardBuilder
{
    [MenuItem("Tools/Cosmic UI/Build Rewards Screen")]
    public static void Apply()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene("Assets/Scenes/RewardScene.unity");
        Canvas canvas = GameObject.Find("Canvas").GetComponent<Canvas>();
        if (canvas.GetComponent<CosmicRewardUI>() != null) { Debug.Log("Rewards already configured; no scene changes made."); return; }
        var legacy = new GameObject("LegacyRewardUI", typeof(RectTransform));
        legacy.transform.SetParent(canvas.transform, false);
        var children = new System.Collections.Generic.List<Transform>();
        foreach (Transform child in canvas.transform) if (child != legacy.transform) children.Add(child);
        foreach (Transform child in children) child.SetParent(legacy.transform, false);
        legacy.SetActive(false); // References are retained, but old controls cannot accept input.
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        var ui = canvas.gameObject.AddComponent<CosmicRewardUI>();
        ui.englishFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/Cosmic/Fonts/Cinzel SDF.asset");
        ui.thaiFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/Cosmic/Fonts/NotoSerifThai SDF.asset");
        ui.background = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Cosmic/Art/BattleLandscape.png");
        ui.musicClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Waking_at_the_Gates.mp3");
        ui.BuildLayout(); EditorUtility.SetDirty(ui);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene); EditorSceneManager.SaveScene(canvas.gameObject.scene);
        AssetDatabase.SaveAssets(); Debug.Log("Cosmic Rewards screen installed; legacy UI and reward pool preserved.");
    }
}
