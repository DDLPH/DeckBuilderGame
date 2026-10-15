using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class CosmicPolishChecks
{
    public static void Run()
    {
        CosmicBattleChecks.RunFinal();
        EditorSceneManager.OpenScene("Assets/Scenes/BattleScene.unity");
        Canvas visible = GameObject.Find("Canvas").GetComponent<Canvas>();
        var hidden = new GameObject("HiddenEnemyCanvas", typeof(RectTransform), typeof(Canvas));
        hidden.transform.SetParent(visible.transform.Find("LegacyBattleUI"), false);
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/Cosmic/Fonts/Cinzel SDF.asset");
        var modal = CosmicModalDialog.Show(hidden.GetComponent<Canvas>(), font, "RUN COMPLETE", "The result must never inherit a hidden enemy canvas.", "MAIN MENU", null);
        Require(modal.transform.parent == visible.transform, "Modal was attached below a hidden legacy canvas.");
        Require(modal.GetComponent<Canvas>().overrideSorting && modal.GetComponent<GraphicRaycaster>() != null, "Modal is not above the game or cannot receive clicks.");
        foreach (CanvasGroup group in modal.GetComponentsInParent<CanvasGroup>()) Require(group.alpha > .99f, "Modal inherits transparency.");
        modal.Close(false);
        Require(CosmicAudio.BossMusic != null && Math.Abs(CosmicAudio.BossMusic.length - 48) < .1f, "Boss loop missing or wrong duration.");
        foreach (CosmicCue cue in Enum.GetValues(typeof(CosmicCue)))
        {
            Require(Resources.Load<AudioClip>("CosmicAudio/" + cue) != null, "Missing authored audio cue: " + cue);
            foreach (float value in CosmicAudioSynthesis.Effect(cue))
                Require(!float.IsNaN(value) && !float.IsInfinity(value) && Mathf.Abs(value) < 1, "Clipped/non-finite SFX sample: " + cue);
        }
        var preview = new GameObject("Loading Preview", typeof(RectTransform)).AddComponent<CosmicSceneLoader>();
        typeof(CosmicSceneLoader).GetMethod("Build", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(preview, null);
        typeof(CosmicSceneLoader).GetMethod("SetProgress", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(preview, new object[] { .62f });
        preview.GetComponent<CanvasGroup>().alpha = 1;
        CosmicAuditChecks.Render(preview.GetComponent<Canvas>(), "cosmic-loading.png");
        UnityEngine.Object.DestroyImmediate(preview.gameObject);
        Debug.Log("Polish regression checks passed: all Battle/Map/save/modal tests, nested hidden-canvas recovery and original boss/UI audio assets.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
