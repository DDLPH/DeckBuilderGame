using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CosmicAuditSetup
{
    public static void Apply()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        MainMenuManager menu = UnityEngine.Object.FindAnyObjectByType<MainMenuManager>();
        if (menu == null) throw new InvalidOperationException("Main menu manager is missing.");
        SerializedObject settings = new SerializedObject(menu);
        settings.FindProperty("englishFont").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/Cosmic/Fonts/Cinzel SDF.asset");
        settings.FindProperty("thaiFont").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/Cosmic/Fonts/NotoSerifThai SDF.asset");
        settings.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);

        scene = EditorSceneManager.OpenScene("Assets/Scenes/MapScene.unity");
        GameObject music = GameObject.Find("MapMusic");
        if (music == null) music = new GameObject("MapMusic", typeof(AudioSource));
        AudioSource source = music.GetComponent<AudioSource>();
        source.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Waking_at_the_Gates.mp3");
        if (source.clip == null) throw new InvalidOperationException("Existing music track is missing.");
        source.playOnAwake = false; source.loop = true; source.spatialBlend = 0; source.volume = .2f;
        if (music.GetComponent<CosmicMapMusic>() == null) music.AddComponent<CosmicMapMusic>();

        RunManager manager = UnityEngine.Object.FindAnyObjectByType<RunManager>();
        SerializedObject run = new SerializedObject(manager);
        run.FindProperty("englishFont").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/Cosmic/Fonts/Cinzel SDF.asset");
        run.FindProperty("thaiFont").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/Cosmic/Fonts/NotoSerifThai SDF.asset");
        SerializedProperty catalogue = run.FindProperty("saveableCards");
        string[] ids = AssetDatabase.FindAssets("t:CardData");
        Array.Sort(ids, StringComparer.Ordinal);
        var names = new HashSet<string>(StringComparer.Ordinal);
        catalogue.arraySize = ids.Length;
        for (int i = 0; i < ids.Length; i++)
        {
            CardData card = AssetDatabase.LoadAssetAtPath<CardData>(AssetDatabase.GUIDToAssetPath(ids[i]));
            if (!names.Add(card.name)) throw new InvalidOperationException("Duplicate save card name: " + card.name);
            catalogue.GetArrayElementAtIndex(i).objectReferenceValue = card;
        }
        run.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Audit setup saved: menu confirmation fonts, looping map music and " + ids.Length + " saveable cards.");
    }
}
