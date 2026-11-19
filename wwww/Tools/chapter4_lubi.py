"""Shape the five existing Mandarin performances locally (Python + NumPy)."""
import argparse
import hashlib
import json
import wave
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[1]
MASTERS = ROOT / 'Tools/VoiceMasters/Chapter4Lubi'
REVISION = 'trail_partner_local_v1'
# Boundaries are inside observed rests in the original takes. Each beat has
# its own pace, level, opening/closing pitch shading (cents) and following rest.
LINES = {
    '5a321b690406ebd6a426': ([.89, 3.04, 5.02], [
        (1.03, .4, 8, 16, 135), (1.06, .1, 8, -4, 215), (1.02, -.2, 4, -8, 115), (.97, -.7, -4, -18, 0)]),
    '83a2f6c1b205f93bcfdc': ([.89, 3.16, 5.96, 8.20], [
        (1.02, .5, 9, 18, 95), (1.07, .3, 12, -4, 185), (1.10, .5, 16, 2, 125), (1.04, -.1, 6, -5, 95), (.97, -.6, 0, -17, 0)]),
    '65b3318ec69c35ba575f': ([2.80, 3.96, 5.04, 6.50], [
        (1.05, .1, 16, -7, 230), (1.03, .4, 4, 13, 90), (1.08, .5, 12, -4, 105), (1.07, .2, 8, -6, 160), (.98, -.6, 2, -20, 0)]),
    'ba43bff57d24c6cdb6b1': ([.89, 3.40, 5.29, 6.52], [
        (1.01, .6, 12, 22, 95), (1.09, .4, 16, -3, 230), (1.05, .1, 8, -8, 90), (1.10, .35, 14, -4, 150), (.96, -.6, 2, -20, 0)]),
    '129d4a3a43d6b14529f8': ([.85, 2.18, 3.89], [
        (1.02, .4, 8, 16, 110), (1.04, .15, 8, -5, 200), (.96, -.6, 1, -10, 140), (1.01, -.3, 3, -17, 0)]),
}


def phrase_audio(source, rate, beat):
    pace, level, initial, final, _ = beat
    live = np.flatnonzero(np.abs(source) > .0035)
    if len(live) < rate // 15:
        raise ValueError('Missing voice in aligned phrase')
    sound = source[max(0, live[0]-int(.040*rate)):live[-1]+int(.060*rate)]
    half = round(rate*.018)
    overlap = (1-np.cos(np.pi * np.arange(half)/half))*.5
    radius = round(rate*.006)
    lead = np.pad(sound, (0, half*3))
    count = round(len(sound)/pace)
    stretched = np.zeros(count+half*2)
    stretched[:2*half] = lead[:2*half]
    for start in range(half, count, half):
        expected = round(start*pace)
        lo, hi = max(0,expected-radius), min(len(lead)-2*half,expected+radius)
        prior = stretched[start:start+half]
        search = lead[lo:hi+half]
        energy = np.r_[0., np.cumsum(search*search)]
        similarity = np.correlate(search, prior) / np.sqrt(np.maximum(1.e-8, energy[half:]-energy[:-half]))
        best = lo + int(np.argmax(similarity)) if np.max(np.abs(prior)) > .002 else expected
        frame = lead[best:best+2*half]
        stretched[start:start+half] = prior*(1-overlap)+frame[:half]*overlap
        stretched[start+half:start+2*half] = frame[half:]
    phase = np.linspace(0, 1, count)
    cents = np.interp(phase, [0,.30,.60,1], [initial,initial,initial*.40+final*.60,final])
    positions = np.cumsum(2**(cents/1200.))
    positions -= positions[0]
    performed = np.interp(positions, np.arange(count), stretched[:count], right=0)
    performed *= 10**((level-.6*phase**2)/20)
    fade = min(int(.006*rate),count//2)
    performed[:fade] *= np.linspace(0,1,fade)
    performed[-fade:] *= np.linspace(1,0,fade)
    return performed


def render_entry(entry, dest, work):
    key = Path(entry['mandarin']).name
    master = MASTERS / (key+'.wav')
    source_hash = hashlib.sha256(master.read_bytes()).hexdigest()
    expected = entry.get('mandarinSourceSha256', entry['mandarinSha256'])
    if source_hash != expected:
        raise ValueError('Source recording does not match its alignment: '+key)
    with wave.open(str(master),'rb') as handle:
        rate=handle.getframerate()
        if (handle.getnchannels(),handle.getsampwidth(),rate)!=(1,2,24000):
            raise ValueError('Unexpected master format')
        samples=np.frombuffer(handle.readframes(handle.getnframes()),dtype='<i2').astype(float)/32768
    boundaries, beats = LINES[key]
    cuts=[0]+[round(mark*rate) for mark in boundaries]+[len(samples)]
    chunks=[np.zeros(int(rate*.035))]
    for start,end,beat in zip(cuts,cuts[1:],beats):
        chunks.extend([phrase_audio(samples[start:end],rate,beat), np.zeros(round(rate*beat[-1]/1000))])
    chunks.append(np.zeros(int(rate*.08)))
    out=np.concatenate(chunks)
    high = out - np.convolve(out,[.12,.23,.30,.23,.12],mode='same')
    out -= .22*high
    out *= min(1, .94/max(.001,np.max(np.abs(out))))
    work.mkdir(parents=True,exist_ok=True)
    staged=work/(key+'.wav')
    with wave.open(str(staged),'wb') as result:
        result.setparams((1,2,rate,0,'NONE','not compressed'))
        result.writeframes(np.rint(out*32767).astype('<i2').tobytes())
    entry.update(mandarinSourceSha256=source_hash,mandarinMastering=REVISION,preserveMandarinPauses=True,
                 mandarinSha256=hashlib.sha256(staged.read_bytes()).hexdigest())
    return staged, dest


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--work',required=True,type=Path)
    work=parser.parse_args().work
    bank_path=ROOT/'Assets/Resources/Chapter4Voice/catalog.json'
    bank=json.loads(bank_path.read_text('utf-8'))
    lines=[entry for entry in bank['entries'] if entry['role']=='lubi']
    if {Path(e['mandarin']).name for e in lines}!=set(LINES): raise ValueError('Unexpected Lubi line inventory')
    takes=[render_entry(entry,ROOT/'Assets/Resources'/(entry['mandarin']+'.wav'),work) for entry in lines]
    for src,dest in takes: dest.write_bytes(src.read_bytes())
    bank_path.write_text(json.dumps(bank,ensure_ascii=False,indent=2),'utf-8')
    print('Updated five Mandarin recordings from local masters.')


if __name__=='__main__': main()
