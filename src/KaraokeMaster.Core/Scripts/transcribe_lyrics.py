"""
Transcribes an isolated vocals track into a word-timed "enhanced LRC" file using faster-whisper.
Invoked by KaraokeMaster.Core.Lyrics.LyricsRunner — not meant to be run standalone.

Usage: transcribe_lyrics.py <vocals_wav_path> <output_lrc_path> [model_size]

Output format ("enhanced LRC"): each line gets a leading [mm:ss.xx] line tag (the first word's
start time) followed by one <mm:ss.xx> tag per word, e.g.:

    [00:12.34]<00:12.34>Merhaba <00:12.80>dunya <00:13.20>nasilsin

KaraokeMaster.Core.Lyrics.LrcParser understands this format for real-time word highlighting, and
falls back to plain line-level display for lyrics that don't have it.
"""

import sys

from faster_whisper import WhisperModel


def format_time(seconds: float) -> str:
    minutes = int(seconds // 60)
    remaining = seconds - minutes * 60
    return f"{minutes:02d}:{remaining:05.2f}"


def main() -> None:
    vocals_path = sys.argv[1]
    output_lrc_path = sys.argv[2]
    model_size = sys.argv[3] if len(sys.argv) > 3 else "small"

    print(f"Loading Whisper model '{model_size}'...", flush=True)
    model = WhisperModel(model_size, device="cpu", compute_type="int8")

    # vad_filter suppresses transcription during actual silence, which cuts down on Whisper
    # hallucinating text over instrumental-only stretches of the vocals stem.
    segments, info = model.transcribe(vocals_path, word_timestamps=True, vad_filter=True)
    duration = max(info.duration, 0.001)

    print(f"Detected language: {info.language} (p={info.language_probability:.2f})", flush=True)

    with open(output_lrc_path, "w", encoding="utf-8") as f:
        for segment in segments:
            if not segment.words:
                continue

            line_start = segment.words[0].start
            f.write(f"[{format_time(line_start)}]")
            for word in segment.words:
                text = word.word.strip()
                if not text:
                    continue
                f.write(f"<{format_time(word.start)}>{text} ")
            f.write("\n")

            percent = min(100, int(segment.end / duration * 100))
            print(f"PROGRESS {percent}", flush=True)

    print("PROGRESS 100", flush=True)
    print("Transcription complete.", flush=True)


if __name__ == "__main__":
    main()
