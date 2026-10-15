using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Adds the same sound vocabulary to older Reward/Shop/Rest buttons without restyling their layout.
[RequireComponent(typeof(Button))]
public sealed class CosmicPlainButtonSound : MonoBehaviour, IPointerEnterHandler
{
    private Button button;
    private void Awake() { button = GetComponent<Button>(); button.onClick.AddListener(Click); }
    private void Click() { CosmicAudio.Play(CosmicCue.Click); }
    public void OnPointerEnter(PointerEventData data)
    { if (button.IsInteractable()) CosmicAudio.Play(CosmicCue.Hover); }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnLoaded; SceneManager.sceneLoaded += OnLoaded;
        AttachRunner();
    }
    private static void OnLoaded(Scene scene, LoadSceneMode mode) { AttachRunner(); }
    private static void AttachRunner()
    {
        new GameObject("Cosmic Scene Audio Binding").AddComponent<CosmicButtonSoundBinder>();
    }
}

public sealed class CosmicButtonSoundBinder : MonoBehaviour
{
    private IEnumerator Start()
    {
        yield return null; // Dynamic reward/shop/map buttons are created in Start().
        foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Include))
        {
            if (button.GetComponent<CosmicButtonEffect>() != null || button.GetComponent<CosmicPlainButtonSound>() != null ||
                button.GetComponent<MapNodeView>() != null || button.GetComponent<CosmicRewardCard>() != null) continue;
            bool legacy = false;
            for (Transform parent = button.transform; parent != null; parent = parent.parent)
                if (parent.name.StartsWith("Legacy", System.StringComparison.Ordinal)) { legacy = true; break; }
            if (!legacy) button.gameObject.AddComponent<CosmicPlainButtonSound>();
        }
        Destroy(gameObject);
    }
}
