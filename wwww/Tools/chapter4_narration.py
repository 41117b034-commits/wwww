"""Master the recorded Chapter 4 ending cards locally. Run after recording with
python Tools/chapter4_narration.py --work <work directory>. For older sped-up masters,
pass --originals <unmastered Chapter4Voice folder>.
Only narrator lines are edited. Matched output hashes make reruns idempotent.
"""
import argparse
import hashlib
import io
import json
import wave
from pathlib import Path

import numpy as np


def digest(data):
    return hashlib.sha256(data).hexdigest()


def shorten_pauses(pcm, rate, min_gap):
    # Mark 10 ms bins only when both energy and peaks are below the noise floor.
    # Keep the beginning and tail of each pause, including breath and word edges.
    block = max(1, rate // 100)
    blocks = np.pad(pcm.astype(float), (0, -len(pcm) % block)).reshape(-1, block)
    energy = np.sqrt(np.mean(blocks ** 2, axis=1))
    floor = np.clip(np.percentile(energy, 85) * .018, 22, 95)
    quiet = (energy < floor) & (abs(blocks).max(axis=1) < floor * 4)
    edges = np.flatnonzero(np.diff(quiet.astype(int), prepend=0, append=0))
    chunks = []
    cursor = 0
    gaps = []
    for start, stop in edges.reshape(-1, 2) * block:
        stop = min(stop, len(pcm))
        length = (stop - start) / rate
        if start == 0:
            keep = .085
            left = 0
        elif stop == len(pcm):
            keep = .16
            left = round(keep * rate)
        elif length > min_gap:
            keep = .40 if length > .95 else .31
            left = round(keep * rate * .5)
            gaps.append(round(length, 2))
        else:
            continue
        cut = stop - start - round(keep * rate)
        if cut <= 0:
            continue
        first = start + left
        chunks.append(pcm[cursor:first])
        cursor = first + cut
    chunks.append(pcm[cursor:])
    return np.concatenate(chunks).astype('<i2', copy=False), gaps


def master(raw):
    with wave.open(io.BytesIO(raw)) as file:
        rate = file.getframerate()
        if file.getsampwidth() != 2 or file.getnchannels() != 1:
            raise ValueError('Expected mono 16-bit recorded narration')
        pcm = np.frombuffer(file.readframes(file.getnframes()), dtype='<i2')
    trimmed, gaps = shorten_pauses(pcm, rate, min_gap=.42)
    # Recheck silence against the new speech-to-silence ratio. Voiced samples
    # keep their exact duration and pitch in both languages.
    final, remaining = shorten_pauses(trimmed, rate, min_gap=.48)
    audio = final.tobytes()
    gaps.extend(remaining)
    out = io.BytesIO()
    with wave.open(out, 'wb') as file:
        file.setparams((1, 2, rate, 0, 'NONE', 'not compressed'))
        file.writeframes(audio)
    return out.getvalue(), (round(len(pcm)/rate,2), round(len(audio)/(2*rate),2), gaps)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--originals', type=Path, help='Original Chapter4Voice folder with Mandarin and Seediq subfolders')
    parser.add_argument('--ffmpeg', help=argparse.SUPPRESS)  # accepted by existing recording jobs
    parser.add_argument('--work', required=True, type=Path)
    parser.add_argument('--resources', type=Path, default=Path(__file__).resolve().parents[1]/'Assets/Resources')
    args = parser.parse_args()
    args.work.mkdir(parents=True, exist_ok=True)
    catalog = args.resources/'Chapter4Voice/catalog.json'
    bank = json.loads(catalog.read_text('utf-8'))
    writes = []
    for entry in bank['entries']:
        if entry['role'] != 'narrator':
            continue
        masters = entry.setdefault('narrationPacing', {})
        for language in ('mandarin', 'seediq'):
            dest = args.resources/(entry[language]+'.wav')
            raw = dest.read_bytes()
            previous = masters.get(language, {})
            if previous.get('masterSha256') == digest(raw):
                if previous.get('tempo') == 1:
                    continue
                if not args.originals:
                    parser.error(f'Pass --originals to restore {dest.name} at its recorded speed')
                original = args.originals / dest.parent.name / dest.name
                raw = original.read_bytes()
                if digest(raw) != previous.get('sourceSha256'):
                    raise ValueError(f'Original recording hash mismatch: {original}')
            sound, (old, new, gaps) = master(raw)
            staged = args.work/(language+'-'+dest.name)
            staged.write_bytes(sound)
            writes.append((dest, staged))
            masters[language] = dict(sourceSha256=digest(raw), masterSha256=digest(sound), tempo=1)
            if language == 'mandarin':
                entry['mandarinSha256'] = digest(sound)
            print(f'{dest.stem} {language}: {old:.2f}s -> {new:.2f}s, {len(gaps)} pauses, before max {max(gaps, default=0):.2f}s')
    for dest, staged in writes:
        dest.write_bytes(staged.read_bytes())
    if writes:
        catalog.write_text(json.dumps(bank,ensure_ascii=False,indent=2), encoding='utf-8')
    print(f'{len(writes)} recordings updated')


if __name__ == '__main__':
    main()
