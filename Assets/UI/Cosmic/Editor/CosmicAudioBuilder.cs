using System.IO;
using UnityEditor;
using UnityEngine;

public static class CosmicAudioBuilder
{
    [MenuItem("Tools/Cosmic UI/Bake Original Boss Music")]
    public static void Apply()
    {
        const string folder = "Assets/UI/Cosmic/Resources/CosmicAudio";
        Directory.CreateDirectory(folder);
        WriteWav(folder + "/EclipseSiege.wav", CosmicAudioSynthesis.BossLoop());
        // Keep previews editable/replaceable in the Inspector; no existing user music is overwritten.
        foreach (CosmicCue cue in System.Enum.GetValues(typeof(CosmicCue)))
            WriteWav(folder + "/" + cue + ".wav", CosmicAudioSynthesis.Effect(cue));
        AssetDatabase.Refresh();
        var importer = (AudioImporter)AssetImporter.GetAtPath(folder + "/EclipseSiege.wav");
        var settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.Streaming;
        settings.compressionFormat = AudioCompressionFormat.Vorbis; settings.quality = .7f;
        importer.defaultSampleSettings = settings; importer.SaveAndReimport();
        Debug.Log("Original cosmic audio baked: 48-second boss loop and nine UI/battle cues.");
    }

    private static void WriteWav(string path, float[] samples)
    {
        using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + samples.Length * 2);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
            writer.Write((short)1); writer.Write((short)1); writer.Write(CosmicAudioSynthesis.Rate);
            writer.Write(CosmicAudioSynthesis.Rate * 2); writer.Write((short)2); writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(samples.Length * 2);
            foreach (float value in samples) writer.Write((short)(Mathf.Clamp(value, -1, 1) * 32767));
        }
    }
}
