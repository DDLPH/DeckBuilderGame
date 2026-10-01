using System;
using UnityEngine;

public static class CosmicLanguage
{
    private const string PreferenceKey = "CosmicUI.Language";

    public static event Action Changed;

    public static bool IsThai => PlayerPrefs.GetInt(PreferenceKey, 0) == 1;

    public static void Toggle()
    {
        PlayerPrefs.SetInt(PreferenceKey, IsThai ? 0 : 1);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
