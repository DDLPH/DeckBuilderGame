using System.Collections.Generic;
using UnityEngine;

public enum CosmicCue { Hover, Click, Card, Attack, Guard, Reject, Turn, Victory, Defeat }

// One persistent SFX voice pool: scene changes cannot cut a button click in half.
// All samples are original procedural synthesis, not third-party recordings.
public sealed class CosmicAudio : MonoBehaviour
{
    private static CosmicAudio instance;
    private readonly Dictionary<CosmicCue, AudioClip> clips = new Dictionary<CosmicCue, AudioClip>();
    private readonly List<AudioClip> generated = new List<AudioClip>();
    private AudioSource voice;
    private float lastHover = -10;
    private int clickFrame = -1;
    public static int PlayedCount { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance = null; PlayedCount = 0; }

    public static void Play(CosmicCue cue)
    {
        if (!Application.isPlaying) return;
        if (instance == null)
        {
            var root = new GameObject("Cosmic UI Audio"); DontDestroyOnLoad(root);
            instance = root.AddComponent<CosmicAudio>();
            instance.voice = root.AddComponent<AudioSource>();
            instance.voice.playOnAwake = false; instance.voice.spatialBlend = 0;
            instance.voice.volume = .5f;
        }
        if (cue == CosmicCue.Hover)
        {
            if (Time.unscaledTime - instance.lastHover < .09f) return;
            instance.lastHover = Time.unscaledTime;
        }
        if (cue == CosmicCue.Click)
        {
            if (instance.clickFrame == Time.frameCount) return;
            instance.clickFrame = Time.frameCount;
        }
        if (!instance.clips.TryGetValue(cue, out AudioClip clip))
        {
            clip = Resources.Load<AudioClip>("CosmicAudio/" + cue);
            if (clip == null)
            {
                float[] samples = CosmicAudioSynthesis.Effect(cue);
                clip = AudioClip.Create("Cosmic_" + cue, samples.Length, 1, CosmicAudioSynthesis.Rate, false);
                clip.SetData(samples, 0); instance.generated.Add(clip);
            }
            instance.clips.Add(cue, clip);
        }
        instance.voice.PlayOneShot(clip, cue == CosmicCue.Hover ? .2f : .7f);
        PlayedCount++;
    }

    public static AudioClip BossMusic => Resources.Load<AudioClip>("CosmicAudio/EclipseSiege");

    private void OnDestroy()
    {
        foreach (AudioClip clip in generated) if (clip != null) Destroy(clip);
        if (instance == this) instance = null;
    }
}

public static class CosmicAudioSynthesis
{
    public const int Rate = 22050;
    private static float Tone(float frequency, float t) => Mathf.Sin(2 * Mathf.PI * frequency * t);
    private static float Hz(int midi) => 440 * Mathf.Pow(2, (midi - 69) / 12f);
    private static float Noise(int sample)
    {
        uint value = unchecked((uint)sample * 747796405u + 2891336453u);
        value = ((value >> (int)((value >> 28) + 4)) ^ value) * 277803737u;
        return ((value >> 22) ^ value) / (float)uint.MaxValue * 2 - 1;
    }

    public static float[] Effect(CosmicCue cue)
    {
        float duration = cue == CosmicCue.Victory || cue == CosmicCue.Defeat ? 1.6f :
            cue == CosmicCue.Attack || cue == CosmicCue.Guard ? .32f : .14f;
        var samples = new float[Mathf.CeilToInt(duration * Rate)];
        for (int i = 0; i < samples.Length; i++)
        {
            float t = i / (float)Rate, envelope = Mathf.Min(1, t / .006f) * Mathf.Exp(-t * 24);
            float value;
            switch (cue)
            {
                case CosmicCue.Hover: value = Tone(660, t) * envelope * .2f; break;
                case CosmicCue.Click: value = (Tone(392, t) + .3f * Tone(784, t)) * envelope * .35f; break;
                case CosmicCue.Card: value = Noise(i) * envelope * .16f + Tone(520 - 1100 * t, t) * envelope * .15f; break;
                case CosmicCue.Attack: value = Noise(i) * Mathf.Exp(-t * 22) * .25f + Tone(95 - 100 * t, t) * envelope * .4f; break;
                case CosmicCue.Guard: value = (Tone(294, t) + .4f * Tone(587, t)) * Mathf.Exp(-t * 11) * .28f; break;
                case CosmicCue.Reject: value = (Tone(150, t) + Tone(145, t)) * envelope * .2f; break;
                case CosmicCue.Turn: value = (Tone(196, t) + .5f * Tone(294, t)) * envelope * .3f; break;
                default:
                    int[] notes = cue == CosmicCue.Victory ? new[] { 62, 65, 69, 74 } : new[] { 50, 48, 45, 38 };
                    value = 0;
                    for (int note = 0; note < notes.Length; note++)
                    {
                        float age = t - note * .18f;
                        if (age >= 0) value += Tone(Hz(notes[note]), age) * Mathf.Min(1, age / .01f) * Mathf.Exp(-age * 3) * .2f;
                    }
                    break;
            }
            samples[i] = value * Mathf.Clamp01((duration - t) / .025f);
        }
        return samples;
    }

    // 16 bars at 80 BPM: 48 seconds. Circular note ages preserve reverb tails at the seam.
    public static float[] BossLoop()
    {
        const float beat = .75f, duration = 48;
        int[] roots = { 38, 38, 34, 36 }; // D minor, Bb, C; low pulse with a restrained high choir.
        int[] intervals = { 0, 3, 7, 14 };
        var samples = new float[(int)(duration * Rate)];
        float peak = .001f;
        for (int i = 0; i < samples.Length; i++)
        {
            float t = i / (float)Rate, value = 0;
            int halfBeat = Mathf.FloorToInt(t / (beat * .5f));
            for (int back = 0; back < 3; back++)
            {
                int index = (halfBeat - back + 128) % 128;
                float age = t - (halfBeat - back) * beat * .5f;
                int root = roots[(index / 32) % 4];
                int note = root + (index % 4 == 3 ? 12 : 0);
                float attack = Mathf.Min(1, age / .012f), release = Mathf.Exp(-age * 7);
                value += (Tone(Hz(note), age) + .3f * Tone(Hz(note) * 2, age)) * attack * release * .20f;
            }
            for (int chord = 0; chord < 4; chord++)
            {
                float age = (t - chord * 12 + duration) % duration;
                if (age > 13) continue;
                float shape = Mathf.Clamp01(age / 1.5f) * Mathf.Clamp01((13 - age) / 2);
                int root = roots[chord] + 24;
                foreach (int interval in intervals)
                    value += (Tone(Hz(root + interval), t) + .3f * Tone(Hz(root + interval) * 2, t)) * shape * .028f;
            }
            float kickAge = t % beat;
            value += Tone(48, kickAge) * Mathf.Exp(-kickAge * 13) * .3f;
            float snareAge = (t + beat) % (beat * 2);
            value += Noise(i) * Mathf.Exp(-snareAge * 25) * .10f;
            float bellAge = t % (beat * 4);
            int bellRoot = roots[Mathf.FloorToInt(t / 12) % 4] + 36;
            value += (Tone(Hz(bellRoot + 7), bellAge) + .2f * Tone(Hz(bellRoot + 19), bellAge)) *
                Mathf.Min(1, bellAge / .01f) * Mathf.Exp(-bellAge * 1.6f) * .06f;
            samples[i] = value; peak = Mathf.Max(peak, Mathf.Abs(value));
        }
        // A short equal-power seam avoids clicks while maintaining the downbeat.
        int seam = (int)(Rate * .012f);
        for (int i = 0; i < samples.Length; i++)
        {
            float gain = i < seam ? i / (float)seam : i >= samples.Length - seam ? (samples.Length - 1 - i) / (float)seam : 1;
            samples[i] = samples[i] / peak * .82f * gain;
        }
        return samples;
    }
}
