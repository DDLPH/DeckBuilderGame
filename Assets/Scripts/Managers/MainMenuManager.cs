using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset englishFont;
    [SerializeField] private TMP_FontAsset thaiFont;
    private bool loading;
    private CosmicModalDialog confirmation;
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
        if (loading || confirmation != null) return;
        if (RunSaveSystem.HasSave)
        {
            bool thai = CosmicLanguage.IsThai;
            confirmation = CosmicModalDialog.Show(FindAnyObjectByType<Canvas>(),
                thai ? thaiFont : englishFont,
                thai ? "เริ่มการเดินทางใหม่?" : "START A NEW RUN?",
                thai ? "คุณมีเซฟการเดินทางอยู่แล้ว หากเริ่มใหม่ ความคืบหน้าของรอบเดิมจะถูกแทนที่ ต้องการเริ่มใหม่หรือไม่?"
                    : "You already have a saved run. Starting a new run will replace its progress. Do you want to start over?",
                thai ? "เริ่มใหม่" : "NEW RUN", BeginNewRun,
                thai ? "ยกเลิก" : "CANCEL");
            return;
        }
        BeginNewRun();
    }

    private void BeginNewRun()
    {
        if (loading) return;
        loading = true;
        RunManager.RequestNewRun();
        // Let the short menu click and pressed-button animation finish first.
        StartCoroutine(LoadMapAfterClick());
    }

    public void ContinueGame()
    {
        if (loading || confirmation != null || !RunSaveSystem.TryRead(out _)) return;
        loading = true;
        RunManager.RequestContinue();
        StartCoroutine(LoadMapAfterClick());
    }

    private IEnumerator LoadMapAfterClick()
    {
        yield return new WaitForSecondsRealtime(0.22f);
        SceneManager.LoadScene("MapScene");
    }
}
