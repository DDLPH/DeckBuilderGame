using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public sealed class CosmicLocalizedText : MonoBehaviour
{
    [TextArea] public string english;
    [TextArea] public string thai;
    public TMP_FontAsset englishFont;
    public TMP_FontAsset thaiFont;

    private TMP_Text label;

    private void OnEnable()
    {
        label = GetComponent<TMP_Text>();
        CosmicLanguage.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        CosmicLanguage.Changed -= Refresh;
    }

    public void Refresh()
    {
        if (label == null)
        {
            label = GetComponent<TMP_Text>();
        }

        bool useThai = CosmicLanguage.IsThai && !string.IsNullOrEmpty(thai);
        label.text = useThai ? thai : english;
        TMP_FontAsset selectedFont = useThai ? thaiFont : englishFont;
        if (selectedFont != null)
        {
            label.font = selectedFont;
        }
    }
}
