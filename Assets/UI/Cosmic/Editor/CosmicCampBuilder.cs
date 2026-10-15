using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class CosmicCampBuilder
{
    [MenuItem("Tools/Cosmic UI/Build Shop and Rest Screens")]
    public static void Apply()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Install("ShopScene", false); Install("RestScene", true);
        AssetDatabase.SaveAssets(); Debug.Log("Cosmic Shop and Rest screens installed; original pools and scene references preserved.");
    }
    private static void Install(string sceneName, bool rest)
    {
        EditorSceneManager.OpenScene("Assets/Scenes/" + sceneName + ".unity");
        Canvas canvas = GameObject.Find("Canvas").GetComponent<Canvas>();
        if (canvas.GetComponent<CosmicCampUI>() != null) return;
        var legacy = new GameObject("LegacyCampUI", typeof(RectTransform)); legacy.transform.SetParent(canvas.transform, false);
        var children = new List<Transform>(); foreach (Transform child in canvas.transform) if (child != legacy.transform) children.Add(child);
        foreach (Transform child in children) child.SetParent(legacy.transform, false); legacy.SetActive(false);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        var ui = canvas.gameObject.AddComponent<CosmicCampUI>(); ui.restSite = rest;
        ui.englishFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/Cosmic/Fonts/Cinzel SDF.asset");
        ui.thaiFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/Cosmic/Fonts/NotoSerifThai SDF.asset");
        ui.background = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Cosmic/Art/CosmicMapLandscape.png");
        ui.musicClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Waking_at_the_Gates.mp3");
        ui.BuildLayout(); EditorUtility.SetDirty(ui); EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene); EditorSceneManager.SaveScene(canvas.gameObject.scene);
    }
}
