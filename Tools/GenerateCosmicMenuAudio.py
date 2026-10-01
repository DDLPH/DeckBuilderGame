"""Create original, loopable Main Menu music and one soft UI click.

Run with Python 3 and NumPy. The WAV files are ordinary Unity AudioClips;
this generator is not needed at game runtime.
"""

from pathlib import Path
import wave

import numpy as np


ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "Assets" / "Audio" / "CosmicMenu"
SAMPLE_RATE = 24_000
LOOP_SECONDS = 32
SAMPLES = SAMPLE_RATE * LOOP_SECONDS
TIME = np.arange(SAMPLES, dtype=np.float64) / SAMPLE_RATE
SEED = 4711


def loop_frequency(hertz):
    """Snap to whole cycles per loop, preventing a pop at the loop seam."""
    return round(hertz * LOOP_SECONDS) / LOOP_SECONDS


def sine(hertz, phase=0.0):
    return np.sin(2.0 * np.pi * loop_frequency(hertz) * TIME + phase)


def pad(hertz, color=0.0):
    fundamental = sine(hertz, color)
    slow_beat = sine(hertz + 0.0625, color + 0.7)
    second = sine(hertz * 2.0, color + 0.2)
    fifth = sine(hertz * 3.0, color + 1.1)
    motion = 0.78 + 0.22 * np.sin(2.0 * np.pi * TIME / LOOP_SECONDS + color)
    return motion * (0.48 * fundamental + 0.38 * slow_beat + 0.11 * second + 0.03 * fifth)


def bell(hertz, onset, pan):
    age = (TIME - onset) % LOOP_SECONDS
    attack = 1.0 - np.exp(-age / 0.018)
    decay = np.exp(-age / 1.55)
    tone = (
        0.70 * sine(hertz)
        + 0.22 * sine(hertz * 2.01, 0.4)
        + 0.08 * sine(hertz * 3.93, 1.3)
    )
    signal = attack * decay * tone
    # Circular, quiet echoes keep the reverb tail continuous across the loop.
    signal = signal + 0.23 * np.roll(signal, int(0.29 * SAMPLE_RATE))
    signal = signal + 0.11 * np.roll(signal, int(0.61 * SAMPLE_RATE))
    return signal * (0.5 - 0.35 * pan), signal * (0.5 + 0.35 * pan)


def write_wav(path, samples):
    path.parent.mkdir(parents=True, exist_ok=True)
    pcm = np.round(np.clip(samples, -1.0, 1.0) * 32767.0).astype("<i2")
    with wave.open(str(path), "wb") as audio:
        audio.setnchannels(1 if pcm.ndim == 1 else pcm.shape[1])
        audio.setsampwidth(2)
        audio.setframerate(SAMPLE_RATE)
        audio.writeframes(pcm.tobytes())


def make_music():
    # D minor(add9): a low, unresolved drone rather than a busy melody.
    left = 0.20 * pad(73.416, 0.1) + 0.12 * pad(110.0, 0.8)
    right = 0.20 * pad(73.416, 0.6) + 0.12 * pad(110.0, 1.2)
    left += 0.10 * pad(174.614, 0.2) + 0.055 * pad(164.814, 1.0)
    right += 0.10 * pad(174.614, 1.0) + 0.055 * pad(164.814, 0.3)

    # Slow pulse under the atmosphere, with no drums or sharp transient.
    pulse = (0.5 + 0.5 * np.cos(2.0 * np.pi * TIME / 8.0)) ** 8
    sub = 0.10 * pulse * sine(36.708)
    left += sub
    right += sub

    # Deterministic, circular filtered noise gives the pad some "air".
    rng = np.random.default_rng(SEED)
    spectrum = np.fft.rfft(rng.standard_normal(SAMPLES))
    frequencies = np.fft.rfftfreq(SAMPLES, 1.0 / SAMPLE_RATE)
    band = np.exp(-((frequencies - 950.0) / 720.0) ** 2)
    air = np.fft.irfft(spectrum * band, n=SAMPLES)
    air /= max(np.std(air), 1e-9)
    air *= 0.008 * (0.65 + 0.35 * np.sin(2.0 * np.pi * TIME / 16.0))
    left += air
    right += np.roll(air, int(0.12 * SAMPLE_RATE))

    # Four sparse notes over 32 seconds; the return to the first note loops.
    for note, onset, pan in (
        (293.665, 3.0, -0.6),   # D4
        (261.626, 11.0, 0.5),   # C4
        (349.228, 19.0, -0.3),  # F4
        (329.628, 27.0, 0.6),   # E4
    ):
        bell_left, bell_right = bell(note, onset, pan)
        left += 0.14 * bell_left
        right += 0.14 * bell_right

    stereo = np.column_stack((left, right))
    stereo = np.tanh(1.4 * stereo)
    stereo *= 0.72 / np.max(np.abs(stereo))
    return stereo


def make_click():
    duration = 0.17
    time = np.arange(round(duration * SAMPLE_RATE), dtype=np.float64) / SAMPLE_RATE
    phase = 2.0 * np.pi * (750.0 * time - 1_250.0 * time * time)
    attack = 1.0 - np.exp(-time / 0.0015)
    decay = np.exp(-time / 0.026)
    rng = np.random.default_rng(SEED + 1)
    texture = rng.standard_normal(len(time))
    texture = (texture + np.roll(texture, 1) + np.roll(texture, 2)) / 3.0
    click = attack * decay * (
        0.75 * np.sin(phase) + 0.18 * np.sin(2.0 * np.pi * 215.0 * time)
        + 0.07 * texture
    )
    click *= 0.43 / max(np.max(np.abs(click)), 1e-9)
    return click


if __name__ == "__main__":
    music = make_music()
    click = make_click()
    write_wav(OUTPUT / "HollowBeyond_MenuLoop.wav", music)
    write_wav(OUTPUT / "HollowBeyond_UIClick.wav", click)
    print("Created:", OUTPUT)
    print("Music: 32-second stereo loop, 24 kHz PCM16")
    print("Music peak:", round(float(np.max(np.abs(music))), 3))
    print("Music seam jump:", round(float(np.max(np.abs(music[0] - music[-1]))), 5))
    print("Click: 0.17-second mono, 24 kHz PCM16")
