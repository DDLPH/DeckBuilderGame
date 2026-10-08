using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public sealed class CosmicMapMusic : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float musicVolume = .20f;
    private IEnumerator Start()
    {
        AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat("CosmicUI.Volume", 1f));
        AudioSource source = GetComponent<AudioSource>();
        source.playOnAwake = false; source.loop = true; source.spatialBlend = 0;
        source.volume = 0;
        if (source.clip == null) { Debug.LogWarning("Map music clip is not assigned."); yield break; }
        source.Play();
        float elapsed = 0;
        while (elapsed < 1.25f)
        {
            elapsed += Time.unscaledDeltaTime;
            source.volume = musicVolume * Mathf.Clamp01(elapsed / 1.25f);
            yield return null;
        }
    }
    // Scene-owned: it stops automatically when leaving MapScene; no duplicate persistent music.
}
