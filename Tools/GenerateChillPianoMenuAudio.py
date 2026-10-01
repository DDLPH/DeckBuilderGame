"""Render an original two-minute, gently dark piano loop for the Main Menu.

Requires Python 3 and NumPy to regenerate. Unity only needs the WAV output.
The composition uses no samples or third-party recordings.
"""

from pathlib import Path
import wave

import numpy as np


ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "Assets" / "Audio" / "CosmicMenu" / "HollowBeyond_ChillPiano_2min.wav"
SAMPLE_RATE = 24_000
DURATION = 120.0
COUNT = round(SAMPLE_RATE * DURATION)
BEAT = 60.0 / 64.0
BAR = 4.0 * BEAT

# Each harmony lasts two bars. The final suspended chord leads back to D minor.
CHORDS = [
    (38, [62, 65, 69, 76]),  # D minor add 9
    (34, [58, 62, 65, 69]),  # B-flat major 7
    (41, [65, 69, 72, 76]),  # F major 7
    (36, [60, 64, 67, 74]),  # C add 9
    (43, [67, 70, 74, 77]),  # G minor 7
    (34, [58, 62, 65, 69]),  # B-flat major 7
    (45, [69, 72, 76, 79]),  # A minor 7
    (45, [69, 74, 76, 79]),  # A 7 sus 4
] * 2


def midi_frequency(note):
    return 440.0 * 2.0 ** ((note - 69) / 12.0)


def add_wrapped(target, start_seconds, sound, gain_left, gain_right):
    """Place an event around the circular timeline so the loop seam is natural."""
    start = round(start_seconds * SAMPLE_RATE) % COUNT
    length = len(sound)
    first = min(length, COUNT - start)
    target[start : start + first, 0] += sound[:first] * gain_left
    target[start : start + first, 1] += sound[:first] * gain_right
    if first < length:
        target[: length - first, 0] += sound[first:] * gain_left
        target[: length - first, 1] += sound[first:] * gain_right


def piano_note(midi_note, length=5.5):
    frequency = midi_frequency(midi_note)
    time = np.arange(round(length * SAMPLE_RATE), dtype=np.float64) / SAMPLE_RATE
    attack = 1.0 - np.exp(-time / 0.007)
    tail = np.ones_like(time)
    fade_count = round(0.35 * SAMPLE_RATE)
    tail[-fade_count:] = np.cos(np.linspace(0.0, np.pi / 2.0, fade_count)) ** 2

    # Rounded fundamental plus rapidly fading upper harmonics imitate a soft piano.
    body = np.zeros_like(time)
    for harmonic, amount, decay in (
        (1, 1.00, 2.7),
        (2, 0.32, 1.2),
        (3, 0.12, 0.55),
        (4, 0.05, 0.31),
    ):
        partial_frequency = frequency * harmonic * (1.0 + 0.00012 * harmonic**2)
        body += amount * np.sin(2.0 * np.pi * partial_frequency * time) * np.exp(-time / decay)
    return (attack * tail * body).astype(np.float32)


def soft_pad_note(midi_note, length=9.7):
    frequency = midi_frequency(midi_note)
    time = np.arange(round(length * SAMPLE_RATE), dtype=np.float64) / SAMPLE_RATE
    attack = np.clip(time / 1.6, 0.0, 1.0)
    release = np.clip((length - time) / 1.8, 0.0, 1.0)
    envelope = np.sin(attack * np.pi / 2.0) ** 2 * np.sin(release * np.pi / 2.0) ** 2
    tone = 0.62 * np.sin(2.0 * np.pi * frequency * time)
    tone += 0.38 * np.sin(2.0 * np.pi * frequency * 1.002 * time + 0.8)
    return (envelope * tone).astype(np.float32)


def make_music():
    piano = np.zeros((COUNT, 2), dtype=np.float32)
    pad = np.zeros_like(piano)
    rng = np.random.default_rng(20261001)

    for chord_index, (bass, notes) in enumerate(CHORDS):
        chord_start = chord_index * 2.0 * BAR
        # The pad stays well behind the piano; notes overlap across chord changes.
        for note in (notes[0] - 12, notes[2] - 12, notes[3] - 12):
            sound = soft_pad_note(note)
            add_wrapped(pad, chord_start - 1.0, sound, 0.014, 0.016)

        for bar_in_chord in range(2):
            bar_number = chord_index * 2 + bar_in_chord
            bar_start = bar_number * BAR
            variation = 0.92 + 0.08 * np.sin(2.0 * np.pi * bar_number / 32.0)

            # One quiet bass note per bar and a lightly broken right-hand chord.
            bass_sound = piano_note(bass, 6.0)
            add_wrapped(piano, bar_start, bass_sound, 0.19 * variation, 0.18 * variation)

            order = (0, 2, 1) if bar_in_chord == 0 else (1, 3, 2)
            for step, note_index in enumerate(order):
                time = bar_start + (0.42 + 1.03 * step) * BEAT
                time += float(rng.uniform(-0.035, 0.035))
                velocity = float(rng.uniform(0.065, 0.085)) * variation
                note_sound = piano_note(notes[note_index])
                pan = -0.06 if step % 2 == 0 else 0.06
                add_wrapped(piano, time, note_sound, velocity * (1.0 - pan), velocity * (1.0 + pan))

            # A single airy top note every other bar leaves room to breathe.
            if bar_number % 2 == 0:
                top_note = notes[3] if chord_index % 3 != 1 else notes[2]
                top_sound = piano_note(top_note, 6.2)
                add_wrapped(piano, bar_start + 3.15 * BEAT, top_sound, 0.055, 0.061)

    # Circular room echoes preserve the seamless loop while adding gentle width.
    left = piano[:, 0] + pad[:, 0]
    right = piano[:, 1] + pad[:, 1]
    left += 0.13 * np.roll(piano[:, 1], round(0.27 * SAMPLE_RATE))
    left += 0.07 * np.roll(piano[:, 0], round(0.59 * SAMPLE_RATE))
    right += 0.13 * np.roll(piano[:, 0], round(0.34 * SAMPLE_RATE))
    right += 0.07 * np.roll(piano[:, 1], round(0.68 * SAMPLE_RATE))

    stereo = np.column_stack((left, right))
    stereo = np.tanh(1.2 * stereo)
    stereo *= 0.68 / max(float(np.max(np.abs(stereo))), 1e-9)
    return stereo


def write_wav(samples):
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    pcm = np.round(np.clip(samples, -1.0, 1.0) * 32767.0).astype("<i2")
    with wave.open(str(OUTPUT), "wb") as audio:
        audio.setnchannels(2)
        audio.setsampwidth(2)
        audio.setframerate(SAMPLE_RATE)
        audio.writeframes(pcm.tobytes())


if __name__ == "__main__":
    music = make_music()
    write_wav(music)
    difference = float(np.max(np.abs(music[0] - music[-1])))
    rms = float(np.sqrt(np.mean(music * music)))
    print(OUTPUT)
    print(f"Duration: {DURATION:.0f}s | Peak: {np.max(np.abs(music)):.3f} | RMS: {rms:.3f}")
    print(f"Loop seam difference: {difference:.5f}")
