"""Rebuild Chapter 4's isolated offline voice bank. Intermediate files require --work."""
import argparse, asyncio, hashlib, importlib.util, json, re, sys, wave
from pathlib import Path
import miniaudio
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT/'Assets/Resources/Chapter4Voice'
parser = argparse.ArgumentParser()
parser.add_argument('mode', choices=['inventory', 'mandarin', 'verify'])
parser.add_argument('--work', required=True)
args = parser.parse_args()
work = Path(args.work); work.mkdir(parents=True, exist_ok=True)
catalog_path = OUT/'catalog.json'

def inventory():
    previous = json.loads(catalog_path.read_text('utf-8'))['entries'] if catalog_path.exists() else []
    existing = {(e['role'],e['chinese']):e for e in previous}
    entries = {}
    def add(role,text):
        key=(role,text)
        entries[key]=existing.get(key,dict(role=role,chinese=text,mandarin='',seediq='',translation='',reviewed=False))
        ident=hashlib.sha256((role+'|'+text).encode()).hexdigest()[:20]
        resource='Chapter4Voice/Mandarin/'+ident
        if (ROOT/'Assets/Resources'/(resource+'.wav')).exists(): entries[key]['mandarin']=resource
    for name in ['Chapter4Evacuation.cs','Chapter4GasSequence.cs','Chapter4Contact.cs']:
        source=(ROOT/'Assets/Chapter4'/name).read_text('utf-8-sig')
        for m in re.finditer(r'(?:Say|flow\.Announce)\("(\w+)",([\s\S]*?);',source):
            for literal in re.findall(r'"(?:[^"\\]|\\.)*"',m[2]):
                text=json.loads(literal)
                if re.search('[\u4e00-\u9fff]',text): add(m[1],text)
    source=(ROOT/'Assets/Chapter4/Chapter4Ending.cs').read_text('utf-8-sig')
    for literal in re.findall(r'const string History\d+ = ("(?:[^"\\]|\\.)*")',source): add('narrator',json.loads(literal))
    for outcome in ['歐冰循著你告訴她的通路，實際走回隊伍。你們在集合地再次會合。','兩處聯絡點都沒有傳出你的呼喊。主隊完成撤離，歐冰未歸。她的去向仍不明。']:
        for bihu in ['比胡獲救的結果，承接自第三章。','比胡已於第三章死亡；這份記憶也隨你留下。','缺少有效的第三章紀錄，比胡的狀態仍未確認。']:
            add('narrator',outcome+'\n\n露比扶著古慕，兩人都已抵達。\n\n'+bihu)
    OUT.mkdir(parents=True,exist_ok=True)
    catalog={'version':1,'seediqAttribution':'ILRDF AI Tgdaya draft, teacher review pending','entries':list(entries.values())}
    catalog_path.write_text(json.dumps(catalog,ensure_ascii=False,indent=2),'utf-8')
    print('Inventoried',len(entries),'role/line pairs')

async def mandarin():
    spec=importlib.util.spec_from_file_location('worker',ROOT/'Assets/StreamingAssets/ChineseVoice/neural-worker.py')
    worker=importlib.util.module_from_spec(spec);spec.loader.exec_module(worker)
    config=json.loads((ROOT/'Assets/StreamingAssets/ChineseVoice/delivery.json').read_text('utf-8'))
    config['roles'].update({
        'gumu':dict(voice='zh-TW-HsiaoChenNeural',rate=-8,pitch=-8),
        'lubi':dict(voice='zh-TW-HsiaoYuNeural',rate=-5,pitch=-6),
        'obing':dict(voice='zh-CN-XiaoxiaoNeural',rate=-5,pitch=-8),
        'narrator':dict(voice='zh-TW-YunJheNeural',rate=-8,pitch=-14)})
    (OUT/'delivery.json').write_text(json.dumps(config,ensure_ascii=False,indent=2),'utf-8')
    bank=json.loads(catalog_path.read_text('utf-8')); sem=asyncio.Semaphore(3)
    (OUT/'Mandarin').mkdir(exist_ok=True)
    async def one(entry):
        async with sem:
            key=hashlib.sha256((entry['role']+'|'+entry['chinese']).encode()).hexdigest()[:20]
            resource='Chapter4Voice/Mandarin/'+key; dest=ROOT/'Assets/Resources'/(resource+'.wav')
            text=entry['chinese'].replace('1930','一九三零').replace('1931','一九三一')
            mp3=work/(key+'.mp3')
            choice=worker.settings(text,entry['role'],config)
            if dest.exists() and entry.get('mandarinVoice') == choice and entry.get('mandarinSha256') == hashlib.sha256(dest.read_bytes()).hexdigest():
                entry['mandarin']=resource
                return
            if entry['role']=='narrator' and '\n\n' in text:
                # Bounded paragraph requests retain every word of long cards.
                # Keep a short pause between complete paragraph recordings.
                frames=[]
                for index,part in enumerate(text.split('\n\n')):
                    segment=work/(key+'_'+str(index)+'.mp3')
                    marker=work/(key+'_'+str(index)+'.complete.json')
                    expected=worker.settings(part,entry['role'],config)
                    if not segment.exists() or not marker.exists() or json.loads(marker.read_text('utf-8'))!=expected:
                        for attempt in range(4):
                            try:
                                await worker.synthesize(part,entry['role'],segment,config)
                                marker.write_text(json.dumps(expected),'utf-8');break
                            except Exception:
                                if attempt==3:raise
                                await asyncio.sleep(2+attempt*2)
                    sound=miniaudio.decode(segment.read_bytes(),output_format=miniaudio.SampleFormat.SIGNED16,nchannels=1,sample_rate=24000)
                    if len(sound.samples)<2400 or max(abs(x) for x in sound.samples)<164:raise ValueError('Empty paragraph')
                    if frames:frames.append(b'\x00\x00'*6000)
                    frames.append(sound.samples.tobytes())
                with wave.open(str(dest),'wb') as w:
                    w.setnchannels(1);w.setsampwidth(2);w.setframerate(24000);w.writeframes(b''.join(frames))
                entry.update(mandarin=resource,mandarinVoice=choice,mandarinSha256=hashlib.sha256(dest.read_bytes()).hexdigest(),mandarinSegments=text.split('\n\n'))
                print('Mandarin paragraphs',key,flush=True)
                return
            sidecar=work/(key+'.complete.json')
            ready=mp3.exists() and sidecar.exists() and json.loads(sidecar.read_text('utf-8'))==choice
            if not ready:
                for attempt in range(4):
                    try:
                        await worker.synthesize(text,entry['role'],mp3,config)
                        sidecar.write_text(json.dumps(choice),'utf-8')
                        break
                    except Exception:
                        if attempt == 3: raise
                        await asyncio.sleep(2 + attempt * 2)
            if not dest.exists() or entry.get('mandarinVoice') != choice:
                sound=miniaudio.decode(mp3.read_bytes(),output_format=miniaudio.SampleFormat.SIGNED16,nchannels=1,sample_rate=24000)
                if len(sound.samples) < 2400 or max(abs(x) for x in sound.samples) < 164: raise ValueError('Empty recording')
                with wave.open(str(dest),'wb') as w:
                    w.setnchannels(1);w.setsampwidth(2);w.setframerate(24000);w.writeframes(sound.samples.tobytes())
            entry['mandarin']=resource; entry['mandarinVoice']=choice
            entry['mandarinSha256']=hashlib.sha256(dest.read_bytes()).hexdigest()
            print('Mandarin',entry['role'],key,flush=True)
    results=await asyncio.gather(*(one(e) for e in bank['entries']),return_exceptions=True)
    catalog_path.write_text(json.dumps(bank,ensure_ascii=False,indent=2),'utf-8')
    errors=[str(r) for r in results if isinstance(r,Exception)]
    if errors: raise RuntimeError(errors)

def verify():
    bank=json.loads(catalog_path.read_text('utf-8')); records=[]
    for e in bank['entries']:
        for language in ['mandarin','seediq']:
            resource=e[language]
            if not resource: raise ValueError('Missing '+language+': '+e['chinese'])
            p=ROOT/'Assets/Resources'/(resource+'.wav')
            sound=miniaudio.decode(p.read_bytes(),output_format=miniaudio.SampleFormat.SIGNED16,nchannels=1,sample_rate=24000)
            peak=max(abs(x) for x in sound.samples)/32768
            if peak<.005: raise ValueError('Silent clip '+resource)
            records.append(dict(resource=resource,seconds=len(sound.samples)/24000,peak=peak,sha256=hashlib.sha256(p.read_bytes()).hexdigest()))
    (work/'report-audio.json').write_text(json.dumps(dict(completed=True,entries=len(bank['entries']),clips=records),ensure_ascii=False,indent=2),'utf-8')
    print('Validated',len(records),'non-silent audio files')

if args.mode=='inventory': inventory()
elif args.mode=='mandarin': asyncio.run(mandarin())
else: verify()
