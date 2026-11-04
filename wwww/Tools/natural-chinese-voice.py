"""Regenerate fixed Mandarin audio with the same worker/profiles used in game."""
import asyncio
import hashlib
import importlib.util
import json
import re
from pathlib import Path
import sys
import wave
import miniaudio
sys.dont_write_bytecode=True

ROOT=Path(__file__).resolve().parents[1]
EVIDENCE=ROOT/'_CodexBackups/chinese_natural_voice_20261104'
ASSETS=ROOT/'Assets/StreamingAssets/ChineseVoice'
spec=importlib.util.spec_from_file_location('neural_worker',ASSETS/'neural-worker.py')
worker=importlib.util.module_from_spec(spec);spec.loader.exec_module(worker)
config=json.loads((ASSETS/'delivery.json').read_text(encoding='utf-8'))

def pcm(source,destination):
    audio=miniaudio.decode(source.read_bytes(),output_format=miniaudio.SampleFormat.SIGNED16,nchannels=1,sample_rate=24000)
    with wave.open(str(destination),'wb') as output:
        output.setnchannels(1);output.setsampwidth(2);output.setframerate(24000);output.writeframes(audio.samples.tobytes())
    return len(audio.samples)/24000

async def main():
    catalog_path=ROOT/'Assets/Resources/ChineseVoice/catalog.json'
    catalog=json.loads(catalog_path.read_text(encoding='utf-8'))
    third=json.loads((ROOT/'Assets/Resources/Chapter3Voice/catalog.json').read_text(encoding='utf-8'))
    third_lines={e['chinese'] for e in third['entries']}
    roles=('bawan','mona','tado','pihu')
    second_roles={}
    scene=(ROOT/'Assets/Scenes/第二章.unity').read_text(encoding='utf-8')
    for match in re.finditer(r'    role: (\d+)\n    chinese: (".*")',scene):
        role={3:'mona',4:'tado',5:'bawan'}.get(int(match[1]))
        if role:
            text=json.loads(re.sub(r'\\x([0-9a-fA-F]{2})',r'\\u00\1',match[2]))
            second_roles.setdefault(text,set()).add(role)
    jobs=[]
    for entry in catalog['entries']:
        jobs.append((entry['chinese'],'default',entry['resource']))
        if entry.get('femaleResource'):jobs.append((entry['chinese'],'female',entry['femaleResource']))
        entry['variants']=[]
        needed_roles=roles if entry['chinese'] in third_lines else second_roles.get(entry['chinese'],())
        if needed_roles:
            # Every Chapter 3 line can be spoken by any of the four actors,
            # including shared notices and branching conversation fragments.
            for role in needed_roles:
                resource=entry['resource'] if role=='bawan' else entry['resource']+'_'+role
                entry['variants'].append({'role':role,'resource':resource,'voice':config['roles'][role]['voice']})
                if role!='bawan':jobs.append((entry['chinese'],role,resource))
        entry['provider']='Neural Mandarin; mature four-character delivery v1'
    catalog['male']='zh-TW-YunJheNeural';catalog['female']='zh-TW-HsiaoChenNeural';catalog['deliveryVersion']=config['version'];catalog.pop('rate',None)
    records={};progress=EVIDENCE/'generation-progress.json'
    if progress.exists():records=json.loads(progress.read_text(encoding='utf-8')).get('clips',{})
    mp3dir=EVIDENCE/'neural-mp3';mp3dir.mkdir(exist_ok=True)
    semaphore=asyncio.Semaphore(4)
    errors=[];done=0
    async def one(text,role,resource):
        nonlocal done
        destination=ROOT/'Assets/Resources'/f'{resource}.wav'
        expected=worker.settings(text,role,config)
        if records.get(resource,{}).get('settings')==expected and destination.exists():done+=1;return
        async with semaphore:
            mp3=mp3dir/(resource.split('/')[-1]+'.mp3')
            for attempt in range(3):
                try:
                    sidecar=mp3.with_suffix('.settings.json')
                    if mp3.exists() and sidecar.exists() and json.loads(sidecar.read_text())==expected:
                        choice=expected
                    else:
                        choice=await worker.synthesize(text,role,mp3,config)
                        sidecar.write_text(json.dumps(choice),encoding='utf-8')
                    seconds=pcm(mp3,destination)
                    if seconds<.15:raise ValueError('Audio too short')
                    records[resource]={'settings':choice,'seconds':seconds,'sha256':hashlib.sha256(destination.read_bytes()).hexdigest()}
                    break
                except Exception as error:
                    if attempt==2:errors.append({'resource':resource,'error':repr(error)});break
                    await asyncio.sleep(3*(attempt+1))
            done+=1
            if done%20==0 or errors:
                progress.write_text(json.dumps({'completed':False,'done':done,'total':len(jobs),'clips':records,'errors':errors},ensure_ascii=False,indent=2),encoding='utf-8')
                print(f'{done}/{len(jobs)} errors={len(errors)}',flush=True)
    await asyncio.gather(*(one(*job) for job in jobs))
    progress.write_text(json.dumps({'completed':not errors,'done':done,'total':len(jobs),'clips':records,'errors':errors},ensure_ascii=False,indent=2),encoding='utf-8')
    if errors:raise RuntimeError(f'{len(errors)} recordings failed; original catalog retained')
    catalog_path.write_text(json.dumps(catalog,ensure_ascii=False,indent=2),encoding='utf-8')
    print(f'Completed {len(jobs)} audio resources',flush=True)

if __name__=='__main__':asyncio.run(main())
