using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public void StartGame()
    {
        // Let the short menu click and pressed-button animation finish first.
        StartCoroutine(LoadMapAfterClick());
    }

    private IEnumerator LoadMapAfterClick()
    {
        yield return new WaitForSecondsRealtime(0.22f);
        SceneManager.LoadScene("MapScene");
    }
}
