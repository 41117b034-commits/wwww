import fs from 'node:fs/promises';
import crypto from 'node:crypto';

export const translationBase = 'https://ai-labs.ilrdf.org.tw/kari-seejiq-tnpusu-ai-hmjil';
export const speechBase = 'https://ai-labs.ilrdf.org.tw/hnang-kari-ai-asi-sluhay';
const session = crypto.randomUUID().replaceAll('-','');
const configs=new Map();
export async function call(base, name, data) {
  if(!configs.has(base))configs.set(base,await (await fetch(base+'/config',{signal:AbortSignal.timeout(15000)})).json());
  const config=configs.get(base);
  const fn=config.dependencies.find(d=>d.api_name===name)?.id;
  if(fn===undefined)throw Error('API missing: '+name);
  const r = await fetch(`${base}/gradio_api/queue/join`, {method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({data,session_hash:session,fn_index:fn}),signal:AbortSignal.timeout(180000)});
  if (!r.ok) throw Error(`${name}: HTTP ${r.status}`);
  const {event_id} = await r.json();
  if (!event_id) throw Error(`${name}: missing event id`);
  const stream = await fetch(`${base}/gradio_api/queue/data?session_hash=${session}`,{signal:AbortSignal.timeout(180000)});
  let buffer='';const decoder=new TextDecoder();
  for await(const chunk of stream.body){
    buffer+=decoder.decode(chunk,{stream:true});
    let boundary;
    while((boundary=buffer.indexOf('\n\n'))>=0){
      const block=buffer.slice(0,boundary);buffer=buffer.slice(boundary+2);
      if(!block.startsWith('data:'))continue;
      const event=JSON.parse(block.slice(5).trim());
      if(event.msg==='process_completed'){
        if(!event.success)throw Error(`${name}: ${JSON.stringify(event.output)}`);
        return event.output.data;
      }
    }
  }
  throw Error(`${name}: stream ended without a result`);
}
if(process.argv[2]==='inspect') {
  console.log('languages',JSON.stringify(await call(translationBase,'to_formosan_languages',['賽德克'])));
  console.log('voices',JSON.stringify(await call(speechBase,'lambda',['賽德克'])));
}
if(process.argv[2]==='sample') {
  await call(translationBase,'to_formosan_languages',['賽德克']);
  await call(speechBase,'lambda',['賽德克']);
  const text='前面有巡警。靠著岩石走，別站在路中央。';
  const translated=await call(translationBase,'translate_1',[text,'zho_Hant','trv_Tegu']);
  console.log('translation',JSON.stringify(translated));
  console.log('audio',JSON.stringify(await call(speechBase,'default_speaker_tts',['賽德克_德固達雅_男聲',translated[0]])));
}
const out='Assets/Resources/Chapter3Voice';
const evidence='_CodexBackups/chapter3_voice_five_fixes_20261104';
const literal=/"(?:[^"\\]|\\.)*"/g;
function chineseNumber(value){
  const n=Number(value);if(!Number.isSafeInteger(n)||n<0||n>9999)throw Error('Unsupported numeral '+value);
  if(n===0)return '零';let result='',zero=false;const digits='零一二三四五六七八九',units=['千','百','十',''];
  for(let i=3;i>=0;i--){const d=Math.floor(n/10**i)%10;if(d){if(zero)result+='零';result+=(d===1&&i===1&&!result?'':digits[d])+units[3-i];zero=false;}else if(result)zero=true;}
  return result;
}
const spokenNumbers=new Map();
async function speechText(text){
  let normalized=text.replace(/\.{2,}|…+/g,'.').replace(/[:;：；]/g,',').replace(/["“”「」『』()（）]/g,'').replace(/^[.,\s]+/,'');
  for(const digits of new Set(normalized.match(/\d+/g)||[])){
    if(!spokenNumbers.has(digits)){
      const translated=(await call(translationBase,'translate_1',[chineseNumber(digits),'zho_Hant','trv_Tegu']))[0];
      if(!translated||/\d|[\u4e00-\u9fff]/.test(translated))throw Error('Number translation unavailable: '+digits);
      spokenNumbers.set(digits,translated.replace(/[.!?,]+$/,''));
    }
    normalized=normalized.replace(new RegExp('\\b'+digits+'\\b','g'),spokenNumbers.get(digits));
  }
  return normalized;
}
function decode(s){try{return JSON.parse(s);}catch{return '';}}
async function inventory(){
  const entries=new Map();
  const add=(chinese,source)=>{if(!entries.has(chinese))entries.set(chinese,{chinese,source,seediq:'',resource:'',reviewed:false,status:'pending'});};
  for(const file of await fs.readdir('Assets/Chapter3')){
    if(!file.endsWith('.cs')||/Controller.cs/.test(file)&&!/(Station|School)Controller/.test(file))continue;
    if(!/Combat.cs|StationController|SchoolApproach|SchoolBattle|Rescue.cs|Ending.cs|Conversation.cs|DialogueGrounding|CompanionReaction/.test(file))continue;
    const source=await fs.readFile('Assets/Chapter3/'+file,'utf8');
    for(const match of source.matchAll(literal)){
      const s=decode(match[0]);
      if(s.length<5||s.length>180||!/[。？！!…]$/.test(s)||!/[\u4e00-\u9fff]/.test(s)||/只回|繁體中文|玩家|輸入|模型|備援|最多|JSON|READY|按 |點選|請選擇/.test(s))continue;
      if(s.startsWith('你是巴萬')||s.startsWith('今天你不准跟來。把')||s.startsWith('還在。先收好')||s.startsWith('留下，回家')||s.startsWith('都拿了'))continue;
      add(s,`Assets/Chapter3/${file}:${source.slice(0,match.index).split('\n').length}`);
    }
  }
  for(const weapon of ['刀','槍']){
    add(weapon+'都拿了，現在才說沒準備好？抬起頭，看著我！','Assets/Chapter3/Chapter3StationController.cs');
    add('今天你不准跟來。把'+weapon+'留下，回家休息！想清楚自己能承擔什麼，再來見我。','Assets/Chapter3/Chapter3StationController.cs');
  }
  for(const weapon of ['獵刀','村田十八年式步槍'])add('你的'+weapon+'還在。先收好，這裡讓傷者和同伴歇一歇。','Assets/Chapter3/Chapter3Ending.cs');
  add('去吧。','Assets/Chapter3/Chapter3StationController.cs');
  add('比胡……','Assets/Chapter3/Chapter3Ending.cs');
  add('到了。達多就在校門旁，我們過去。','Assets/Chapter3/Chapter3SchoolController.cs');
  add('學校就在前面，達多在校門旁等我們。','Assets/Chapter3/Chapter3SchoolController.cs');
  return [...entries.values()];
}
function chunks(text){
  const result=[];let current='';for(const word of text.split(/\s+/).filter(Boolean)){
    if(word.length>300)throw Error('TTS token exceeds 300 characters');
    if(current.length+word.length+1>280){result.push(current);current='';}
    current+=(current?' ':'')+word;
  }if(current)result.push(current);return result;
}
function combineWav(parts){
  let format;const samples=[];
  for(const bytes of parts){
    if(bytes.toString('ascii',0,4)!=='RIFF'||bytes.toString('ascii',8,12)!=='WAVE')throw Error('Not WAV');
    for(let p=12;p+8<=bytes.length;){const name=bytes.toString('ascii',p,p+4),n=bytes.readUInt32LE(p+4),body=bytes.subarray(p+8,p+8+n);
      if(name==='fmt '){if(format&&!format.equals(body))throw Error('Audio formats differ');format=body;}
      if(name==='data')samples.push(body);p+=8+n+(n%2);
    }
  }
  if(!format||!samples.length)throw Error('Empty WAV');
  const sound=Buffer.concat(samples),header=Buffer.alloc(12),fmt=Buffer.alloc(8),data=Buffer.alloc(8);
  header.write('RIFF');header.writeUInt32LE(4+8+format.length+8+sound.length,4);header.write('WAVE',8);
  fmt.write('fmt ');fmt.writeUInt32LE(format.length,4);data.write('data');data.writeUInt32LE(sound.length,4);
  return Buffer.concat([header,fmt,format,data,sound]);
}
if(process.argv[2]==='inventory'){
  const entries=await inventory();
  let previous=[];try{previous=JSON.parse(await fs.readFile(out+'/catalog.json','utf8')).entries;}catch(error){if(error.code!=='ENOENT')throw error;}
  const existing=new Map(previous.map(entry=>[entry.chinese,entry]));
  for(let i=0;i<entries.length;i++)if(existing.has(entries[i].chinese))entries[i]=existing.get(entries[i].chinese);
  await fs.mkdir(out,{recursive:true});await fs.writeFile(out+'/catalog.json',JSON.stringify({dialect:'trv_Tegu',attribution:'ILRDF official AI translation and synthesis; unreviewed draft',entries},null,2));
  console.log(entries.length+' lines');console.log(entries.slice(0,12).map(e=>e.chinese).join('\n'));
}
if(process.argv[2]==='generate'){
  const catalog=JSON.parse(await fs.readFile(out+'/catalog.json','utf8'));
  await fs.mkdir(out+'/Audio',{recursive:true});
  await call(translationBase,'to_formosan_languages',['賽德克']);await call(speechBase,'lambda',['賽德克']);
  let complete=0;
  for(const entry of catalog.entries){
    if(entry.status==='generated'){complete++;continue;}
    try{
      const simplified={
        '我們回來三個人。比胡受傷後留在林路，達多回去確認，他已經死了。':'我們有三個人回來了。比胡受傷以後，留在森林的路上。達多回去確認，比胡已經死了。',
        '你跟達多進入會場，帶同伴一起撤出。':'你跟Tado進入會場，帶同伴一起撤出。'
      };
      entry.translationInput=simplified[entry.chinese]||entry.chinese;
      entry.seediq=(await call(translationBase,'translate_1',[entry.translationInput,'zho_Hant','trv_Tegu']))[0];
      if(!entry.seediq||/[\u4e00-\u9fff]/.test(entry.seediq))throw Error('Translation empty or contains untranslated Chinese');
      entry.ttsText=await speechText(entry.seediq);
      const audio=[];for(const text of chunks(entry.ttsText)){
        const [file]=await call(speechBase,'default_speaker_tts',['賽德克_德固達雅_男聲',text]);
        const url=new URL(file.url);if(url.protocol!=='https:'||url.host!=='ai-labs.ilrdf.org.tw')throw Error('Unexpected audio host');
        const response=await fetch(url,{signal:AbortSignal.timeout(30000)});if(!response.ok)throw Error('Audio download '+response.status);
        audio.push(Buffer.from(await response.arrayBuffer()));
      }
      const id=crypto.createHash('sha256').update(entry.chinese).digest('hex').slice(0,20);
      const wav=combineWav(audio);await fs.writeFile(out+'/Audio/'+id+'.wav',wav);
      entry.resource='Chapter3Voice/Audio/'+id;entry.status='generated';entry.generatedAt=new Date().toISOString();entry.provider=speechBase;delete entry.error;complete++;
    }catch(error){entry.status='failed';entry.error=error.message;}
    await fs.writeFile(out+'/catalog.json',JSON.stringify(catalog,null,2));
    await fs.writeFile(evidence+'/voice-progress.json',JSON.stringify({complete,total:catalog.entries.length,last:entry.chinese,status:entry.status,error:entry.error},null,2));
    console.log(`${complete}/${catalog.entries.length} ${entry.status}: ${entry.chinese}`);
  }
  console.log('Finished',complete,'/',catalog.entries.length);
}
if(process.argv[2]==='review'){
  const catalog=JSON.parse(await fs.readFile(out+'/catalog.json','utf8'));
  const cell=value=>String(value??'').replaceAll('\t',' ').replaceAll('\r','').replaceAll('\n',' ⏎ ');
  const rows=[['中文字幕（比對鍵，請保留原文）','官方 AI 族語初稿','實際送入 TTS 的文字','翻譯輸入（若曾改寫）','狀態','族語老師已校對','音檔','程式來源']];
  for(const e of catalog.entries)rows.push([e.chinese,e.seediq,e.ttsText,e.translationInput||e.chinese,e.status,e.reviewed?'是':'否','Assets/Resources/'+e.resource+'.wav',e.source]);
  await fs.writeFile('Documentation/Story/第三章_德固達雅配音校對表.tsv','\ufeff'+rows.map(row=>row.map(cell).join('\t')).join('\n')+'\n');
  console.log('Wrote review table; includes prepared text fragments and UI strings, not all rows are spoken in the active story.');
}
if(process.argv[2]==='verify'){
  const catalog=JSON.parse(await fs.readFile(out+'/catalog.json','utf8'));const clips=[];
  for(const e of catalog.entries){
    if(e.status!=='generated')throw Error('Missing speech: '+e.chinese);
    const bytes=await fs.readFile('Assets/Resources/'+e.resource+'.wav');
    let rate=0,count=0,peak=0,channels=0;
    for(let p=12;p+8<=bytes.length;){const name=bytes.toString('ascii',p,p+4),n=bytes.readUInt32LE(p+4);
      if(name==='fmt '){if(bytes.readUInt16LE(p+8)!==1||bytes.readUInt16LE(p+22)!==16)throw Error('Expected 16-bit PCM');channels=bytes.readUInt16LE(p+10);rate=bytes.readUInt32LE(p+12);}
      if(name==='data'){count=n/2;for(let i=p+8;i<p+8+n;i+=2)peak=Math.max(peak,Math.abs(bytes.readInt16LE(i)));}
      p+=8+n+n%2;
    }
    if(!rate||!count||peak<100)throw Error('Empty or silent audio: '+e.chinese);
    clips.push({chinese:e.chinese,seconds:count/rate/channels,peak,resource:e.resource,reviewed:e.reviewed});
  }
  const result={completed:true,count:clips.length,seconds:clips.reduce((n,c)=>n+c.seconds,0),clips};
  await fs.writeFile(evidence+'/report-audio-files.json',JSON.stringify(result,null,2));
  console.log(JSON.stringify({completed:true,count:result.count,seconds:result.seconds}));
}
