using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class CosmicSettingsPresentation : MonoBehaviour
{
    public GameObject scrim;
    public Slider volume;
    public TMP_Text percentage;
    private readonly Dictionary<Selectable, bool> previous = new Dictionary<Selectable, bool>();
    private GameObject selectedBefore;
    private void OnEnable()
    {
        if (!Application.isPlaying) return;
        if (scrim != null) { scrim.SetActive(true); scrim.transform.SetAsLastSibling(); }
        transform.SetAsLastSibling();
        foreach (Selectable item in transform.parent.GetComponentsInChildren<Selectable>(true))
        {
            if (item.transform.IsChildOf(transform)) continue;
            previous[item] = item.interactable; item.interactable = false;
        }
        if (EventSystem.current != null)
        {
            selectedBefore = EventSystem.current.currentSelectedGameObject;
            EventSystem.current.SetSelectedGameObject(volume == null ? null : volume.gameObject);
        }
        if (volume != null) { volume.onValueChanged.AddListener(RefreshValue); RefreshValue(volume.value); }
        StartCoroutine(FadeIn());
    }
    private IEnumerator FadeIn()
    {
        CanvasGroup group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
        float elapsed = 0;
        while (elapsed < .16f)
        { elapsed += Time.unscaledDeltaTime; group.alpha = Mathf.Clamp01(elapsed/.16f); yield return null; }
        group.alpha = 1;
    }
    private void RefreshValue(float value) { if (percentage != null) percentage.text = Mathf.RoundToInt(value * 100) + "%"; }
    private void OnDisable()
    {
        StopAllCoroutines();
        if (volume != null) volume.onValueChanged.RemoveListener(RefreshValue);
        if (scrim != null) scrim.SetActive(false);
        foreach (var pair in previous) if (pair.Key != null) pair.Key.interactable = pair.Value;
        previous.Clear();
        if (EventSystem.current != null && selectedBefore != null) EventSystem.current.SetSelectedGameObject(selectedBefore);
    }
}
