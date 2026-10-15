using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Explicit one-shot authoring operation; never runs at game startup.
public static class CosmicBattleBalance
{
    public static void Apply()
    {
        SetInt(Load<DamageEffect>("Effects/StrikeDamage"), "damageAmount", 6);
        SetInt(Load<DamageEffect>("Effects/Strike+Damage"), "damageAmount", 9);
        SetInt(Load<BlockEffect>("Effects/DefendBlock"), "blockAmount", 5);
        SetInt(Load<BlockEffect>("Effects/Defend+Block"), "blockAmount", 8);
        var rewards = new List<CardData>();
        rewards.Add(Card("AstralCut", "Astral Cut", "Deal 9 damage.", 1, Effect<DamageEffect>("AstralCutDamage", "damageAmount", 9, TargetType.SingleEnemy), false, CardType.Attack));
        rewards.Add(Card("MoonWard", "Moon Ward", "Gain 8 Block.", 1, Effect<BlockEffect>("MoonWardBlock", "blockAmount", 8, TargetType.Player), false, CardType.Skill));
        rewards.Add(Card("Starfall", "Starfall", "Deal 7 damage to ALL enemies.", 2, Effect<DamageEffect>("StarfallDamage", "damageAmount", 7, TargetType.AllEnemies), false, CardType.Attack));
        rewards.Add(Card("Eclipse", "Eclipse", "Deal 16 damage.", 2, Effect<DamageEffect>("EclipseDamage", "damageAmount", 16, TargetType.SingleEnemy), false, CardType.Attack));
        rewards.Add(Card("Insight", "Insight", "Draw 2 cards.", 1, Effect<DrawCardsEffect>("InsightDraw", "amount", 2, TargetType.Player), false, CardType.Skill));
        rewards.Add(Card("Focus", "Focus", "Gain 1 Energy. Exhaust.", 0, Effect<GainEnergyEffect>("FocusEnergy", "amount", 1, TargetType.Player), true, CardType.Skill));
        rewards.Add(Load<CardData>("Cards/Strike+")); rewards.Add(Load<CardData>("Cards/Defend +"));
        var catalogue = new List<CardData>(rewards) { Load<CardData>("Cards/Strike"), Load<CardData>("Cards/Defend") };

        var normal = Pattern("CosmicNormalPattern", new[] { 5, 3, 6 }, new[] { 0, 1, 0 });
        var elite = Pattern("CosmicElitePattern", new[] { 8, 5, 10 }, new[] { 0, 1, 0 });
        var boss = Pattern("CosmicBossPattern", new[] { 9, 7, 12, 6 }, new[] { 0, 1, 0, 0 });
        Enemy("NormalEnemy", 22, normal); Enemy("Elite", 52, elite); Enemy("Boss", 100, boss);
        Encounter("NormalEncounter_2", "Pilgrim pair", 2, Load<EnemyPool>("EnemyPools/NormalEnemyPool"));
        Encounter("NormalEncounter_3", "Lone pilgrim", 1, Load<EnemyPool>("EnemyPools/NormalEnemyPool"));
        Encounter("EliteEncounter_1", "Astral pilgrim", 1, Load<EnemyPool>("EnemyPools/EliteEnemyPool"));
        Encounter("BossEncounter_1", "Eclipse sovereign", 1, Load<EnemyPool>("EnemyPools/BossEnemyPool"));
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();

        EditorSceneManager.OpenScene("Assets/Scenes/MapScene.unity");
        var run = UnityEngine.Object.FindAnyObjectByType<RunManager>();
        catalogue = RewardCards(); catalogue.Add(Load<CardData>("Cards/Strike")); catalogue.Add(Load<CardData>("Cards/Defend"));
        ArrayProperty(run, "saveableCards", catalogue);
        EditorSceneManager.MarkSceneDirty(run.gameObject.scene); EditorSceneManager.SaveScene(run.gameObject.scene);
        EditorSceneManager.OpenScene("Assets/Scenes/RewardScene.unity");
        var reward = UnityEngine.Object.FindAnyObjectByType<RewardManager>();
        ArrayProperty(reward, "rewardCardPool", RewardCards()); SetInt(reward, "goldReward", 25);
        EditorSceneManager.MarkSceneDirty(reward.gameObject.scene); EditorSceneManager.SaveScene(reward.gameObject.scene);
        EditorSceneManager.OpenScene("Assets/Scenes/ShopScene.unity");
        var shop = UnityEngine.Object.FindAnyObjectByType<ShopManager>();
        ArrayProperty(shop, "shopCards", RewardCards()); SetInt(shop, "cardPrice", 45);
        EditorSceneManager.MarkSceneDirty(shop.gameObject.scene); EditorSceneManager.SaveScene(shop.gameObject.scene);
        EditorSceneManager.OpenScene("Assets/Scenes/BattleScene.unity");
        // Layout only: do not replace serialized gameplay references.
        Transform turn = GameObject.Find("Canvas").transform.Find("BattleBoard/Turn");
        var rect = (RectTransform)turn; rect.anchoredPosition = new Vector2(-135, 270); rect.sizeDelta = new Vector2(320, 80);
        EditorSceneManager.MarkSceneDirty(turn.gameObject.scene); EditorSceneManager.SaveScene(turn.gameObject.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Battle balance applied: eight base card designs, two starter upgrades, normal 22HP / elite 52HP / boss 100HP, 1-2 normal enemies, 25 gold rewards, 45 gold shop; all ten card assets are saveable.");
    }

    private static T Load<T>(string part) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>("Assets/ScriptableObjects/" + part + ".asset");
    private static List<CardData> RewardCards()
    {
        var cards = new List<CardData>();
        foreach (string id in new[] { "AstralCut", "MoonWard", "Starfall", "Eclipse", "Insight", "Focus", "Strike+", "Defend +" })
        {
            CardData card = Load<CardData>("Cards/" + id);
            if (card == null) throw new InvalidOperationException("Missing reward card: " + id);
            cards.Add(card);
        }
        return cards;
    }
    private static T Asset<T>(string part) where T : ScriptableObject
    {
        T asset = Load<T>(part);
        if (asset == null) { asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, "Assets/ScriptableObjects/" + part + ".asset"); }
        return asset;
    }
    private static T Effect<T>(string name, string field, int value, TargetType target) where T : CardEffect
    {
        T effect = Asset<T>("Effects/" + name); var so = new SerializedObject(effect);
        so.FindProperty(field).intValue = value; so.FindProperty("targetType").enumValueIndex = (int)target;
        so.ApplyModifiedPropertiesWithoutUndo(); return effect;
    }
    private static CardData Card(string id, string title, string description, int cost, CardEffect effect, bool exhaust, CardType type)
    {
        CardData card = Asset<CardData>("Cards/" + id); var so = new SerializedObject(card);
        so.FindProperty("cardName").stringValue = title; so.FindProperty("description").stringValue = description;
        so.FindProperty("energyCost").intValue = cost; so.FindProperty("exhausts").boolValue = exhaust;
        so.FindProperty("cardType").enumValueIndex = (int)type;
        var list = so.FindProperty("effects"); list.arraySize = 1; list.GetArrayElementAtIndex(0).objectReferenceValue = effect;
        so.ApplyModifiedPropertiesWithoutUndo(); return card;
    }
    private static EnemyActionPattern Pattern(string id, int[] values, int[] kinds)
    {
        var pattern = Asset<EnemyActionPattern>("EnemyPattern/" + id); var so = new SerializedObject(pattern);
        var actions = so.FindProperty("actions"); actions.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            var action = actions.GetArrayElementAtIndex(i);
            action.FindPropertyRelative("actionType").enumValueIndex = kinds[i];
            action.FindPropertyRelative("value").intValue = values[i];
        }
        so.ApplyModifiedPropertiesWithoutUndo(); return pattern;
    }
    private static void Enemy(string name, int hp, EnemyActionPattern pattern)
    {
        string path = "Assets/Prefabs/Enemy/" + name + ".prefab";
        GameObject prefab = PrefabUtility.LoadPrefabContents(path);
        var enemy = prefab.GetComponent<EnemyData>(); var so = new SerializedObject(enemy);
        so.FindProperty("maxHP").intValue = hp; so.FindProperty("currentHP").intValue = hp;
        so.FindProperty("block").intValue = 0; so.FindProperty("actionPattern").objectReferenceValue = pattern;
        so.ApplyModifiedPropertiesWithoutUndo();
        // Old prefabs contain an unused second EnemyData on a child. Remove only
        // that duplicate component, keeping child objects/visual references intact.
        foreach (EnemyData duplicate in prefab.GetComponentsInChildren<EnemyData>(true))
            if (duplicate != enemy) UnityEngine.Object.DestroyImmediate(duplicate);
        PrefabUtility.SaveAsPrefabAsset(prefab, path); PrefabUtility.UnloadPrefabContents(prefab);
    }
    private static void Encounter(string name, string title, int count, EnemyPool pool)
    {
        var so = new SerializedObject(Load<EncounterDefinition>("Encounters/" + name));
        so.FindProperty("encounterName").stringValue = title;
        var groups = so.FindProperty("enemyGroups"); groups.arraySize = 1;
        var group = groups.GetArrayElementAtIndex(0);
        group.FindPropertyRelative("enemyPool").objectReferenceValue = pool;
        group.FindPropertyRelative("minimumCount").intValue = count; group.FindPropertyRelative("maximumCount").intValue = count;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void SetInt(UnityEngine.Object target, string field, int value)
    { var so = new SerializedObject(target); so.FindProperty(field).intValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
    private static void ArrayProperty(UnityEngine.Object target, string field, List<CardData> cards)
    {
        var so = new SerializedObject(target); var list = so.FindProperty(field); list.arraySize = cards.Count;
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] == null) throw new InvalidOperationException("Cannot save a null card reference.");
            list.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
