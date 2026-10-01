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
        Application.Quit();
#endif
    }
}
