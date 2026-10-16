# Measuring SoundPreview renders

Plain Python 3 (no packages). Each reads a 16-bit stereo WAV from SoundPreview and prints dBFS.

- `standing.py out.wav SECONDS name1 name2 ...`: RMS of the standing (second) half of each scene, every scene SECONDS long. Scenes walk for the first half, so this is the bed without footsteps.
- `steps.py out.wav SECONDS name1 ...`: walking-half RMS, standing-half RMS and the loudest 50 ms window while walking (how far steps rise over the bed).
- `windows.py out.wav S1 S2 ...`: per scene (lengths S1, S2...), RMS, loudest 50 ms and peak, skipping the first 2 s and the last 1.2 s (the work ending).

Compare scenes rendered standalone (one scene per run, same length): wind gusts carry over from one scene to the next in a single run and skew levels by a few dB.

Example: `dotnet run -c Release -- /tmp/s.wav --no-music winter:40 && python3 measure/standing.py /tmp/s.wav 40 winter`
