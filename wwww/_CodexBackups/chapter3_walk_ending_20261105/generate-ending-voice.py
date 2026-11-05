import asyncio, hashlib, importlib.util, json, shutil, sys, wave
from pathlib import Path
import miniaudio
sys.dont_write_bytecode=True
ROOT=Path(__file__).resolve().parents[2]
TASK=Path(__file__).resolve().parent
ASSETS=ROOT/'Assets/StreamingAssets/ChineseVoice'
spec=importlib.util.spec_from_file_location('worker',ASSETS/'neural-worker.py')
worker=importlib.util.module_from_spec(spec);spec.loader.exec_module(worker)
config=json.loads((ASSETS/'delivery.json').read_text(encoding='utf-8'))
lines=[
 ('bawan','人帶回來了。先讓比胡休息，等莫那過來。'),
 ('mona','左側、右側和進入會場的人，各有幾個？把人數和情況告訴我。'),
 ('bawan','左側通路2人，他和我一起打通側門，都回來了。'),
 ('bawan','左側通路1人，由我打通側門，我已回來。'),
 ('tado','進入會場的有12人，包括他、我和一起進入會場的同伴。'),
 ('tado','進入會場的有11人，是我和一起進入會場的同伴。'),
 ('tado','右側通路原有2人，他和比胡都回來了，比胡受傷，由他扶回接應處。'),
 ('tado','右側通路原有1人，比胡受傷，由他扶回接應處，已經回來了。'),
 ('tado','右側通路原有2人，他回來了，比胡留在林路。我回去確認，比胡已經死了。'),
 ('tado','右側通路原有1人，比胡留在林路。我回去確認，他已經死了，右側沒有人回來。'),
 ('bawan','他的獵刀也帶回來了。'),
 ('bawan','他的村田十八年式步槍也帶回來了。'),
]
async def main():
    path=ROOT/'Assets/Resources/ChineseVoice/catalog.json'
    backup=TASK/'before/Assets/Resources/ChineseVoice/catalog.json';backup.parent.mkdir(parents=True,exist_ok=True)
    if not backup.exists():shutil.copy2(path,backup)
    catalog=json.loads(path.read_text(encoding='utf-8'));byline={e['chinese']:e for e in catalog['entries']}
    records=[];mp3dir=TASK/'neural-mp3';mp3dir.mkdir(exist_ok=True)
    sem=asyncio.Semaphore(3)
    async def one(role,text):
        key=hashlib.sha256((role+'|'+text).encode()).hexdigest()[:20]
        resource='ChineseVoice/Audio/'+key+'_ending_'+role
        destination=ROOT/'Assets/Resources'/f'{resource}.wav';mp3=mp3dir/(key+'.mp3')
        async with sem:
            if not mp3.exists() or mp3.stat().st_size<128:await worker.synthesize(text,role,mp3,config)
            audio=miniaudio.decode(mp3.read_bytes(),output_format=miniaudio.SampleFormat.SIGNED16,nchannels=1,sample_rate=24000)
            with wave.open(str(destination),'wb') as wav:
                wav.setnchannels(1);wav.setsampwidth(2);wav.setframerate(24000);wav.writeframes(audio.samples.tobytes())
            duration=len(audio.samples)/24000
            assert duration>.4
            records.append({'role':role,'text':text,'resource':resource,'seconds':duration})
            byline[text]={'chinese':text,'source':'Chapter3:Chapter3Ending.cs','resource':resource, 'femaleResource':'','provider':'Neural Mandarin; mature four-character delivery v1','variants':[{'role':role,'resource':resource,'voice':config['roles'][role]['voice']}]}
            print(role,text,duration,flush=True)
    await asyncio.gather(*(one(*line) for line in lines))
    catalog['entries']=list(byline.values());path.write_text(json.dumps(catalog,ensure_ascii=False,indent=2),encoding='utf-8')
    (TASK/'ending-voice.json').write_text(json.dumps({'completed':True,'clips':records},ensure_ascii=False,indent=2),encoding='utf-8')
asyncio.run(main())
