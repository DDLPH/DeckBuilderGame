using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class CosmicMenuActions : MonoBehaviour
{
    public GameObject settingsPanel;
    public Slider volumeSlider;
    public Button settingsButton;
    public Button quitButton;
    public Button languageButton;
    public Button settingsLanguageButton;
    public Button closeSettingsButton;

    private const string VolumeKey = "CosmicUI.Volume";

    private void Start()
    {
        AudioSource clickSound = GameObject.Find("MainMenuSFX")?.GetComponent<AudioSource>();

        // Visual feedback is added at runtime so existing scene button callbacks and
        // the user's click sounds are left untouched.
        AddButtonEffect(transform.Find("StartButton"), clickSound);
        AddButtonEffect(transform.Find("ContinueButton"));
        AddButtonEffect(settingsButton != null ? settingsButton.transform : null);
        AddButtonEffect(quitButton != null ? quitButton.transform : null);
        // These two buttons have no working click-sound callback in the scene.
        AddButtonEffect(languageButton != null ? languageButton.transform : null, clickSound);
        AddButtonEffect(settingsLanguageButton != null ? settingsLanguageButton.transform : null);
        AddButtonEffect(closeSettingsButton != null ? closeSettingsButton.transform : null);

        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
        if (languageButton != null) languageButton.onClick.AddListener(ToggleLanguage);
        if (settingsLanguageButton != null) settingsLanguageButton.onClick.AddListener(ToggleLanguage);
        if (closeSettingsButton != null) closeSettingsButton.onClick.AddListener(CloseSettings);

        float volume = PlayerPrefs.GetFloat(VolumeKey, 1f);
        AudioListener.volume = volume;
        if (volumeSlider != null)
        {
            volumeSlider.SetValueWithoutNotify(volume);
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    public void ToggleLanguage()
    {
        CosmicLanguage.Toggle();
    }

    public void SetVolume(float value)
    {
        float volume = Mathf.Clamp01(value);
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat(VolumeKey, volume);
        PlayerPrefs.Save();
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        Debug.Log("Quit is available in a built game.");
#else
        // The scene's AudioSource.Play callback runs before this listener.
        StartCoroutine(QuitAfterClick());
#endif
    }

#if !UNITY_EDITOR
    private IEnumerator QuitAfterClick()
    {
        yield return new WaitForSecondsRealtime(0.22f);
        Application.Quit();
    }
#endif

    private static void AddButtonEffect(Transform buttonTransform, AudioSource clickSound = null)
    {
        if (buttonTransform == null || buttonTransform.GetComponent<Button>() == null) return;
        CosmicButtonEffect effect = buttonTransform.GetComponent<CosmicButtonEffect>();
        if (effect == null)
        {
            effect = buttonTransform.gameObject.AddComponent<CosmicButtonEffect>();
        }
        effect.clickSound = clickSound;
    }
}
