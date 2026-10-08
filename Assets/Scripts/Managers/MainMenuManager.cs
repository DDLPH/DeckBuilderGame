using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    private void Start()
    {
        GameObject continueObject = GameObject.Find("ContinueButton");
        Button continueButton = continueObject == null ? null : continueObject.GetComponent<Button>();
        if (continueButton != null)
        {
            continueButton.interactable = RunSaveSystem.TryRead(out _);
            continueButton.onClick.AddListener(ContinueGame);
        }
    }

    public void StartGame()
    {
        RunManager.RequestNewRun();
        // Let the short menu click and pressed-button animation finish first.
        StartCoroutine(LoadMapAfterClick());
    }

    public void ContinueGame()
    {
        if (!RunSaveSystem.HasSave) return;
        RunManager.RequestContinue();
        StartCoroutine(LoadMapAfterClick());
    }

    private IEnumerator LoadMapAfterClick()
    {
        yield return new WaitForSecondsRealtime(0.22f);
        SceneManager.LoadScene("MapScene");
    }
}
