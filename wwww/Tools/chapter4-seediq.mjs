// Offline Tgdaya drafts from the same official service used by Chapter 3.
// Run with an explicit evidence directory; all entries remain unreviewed.
import fs from 'node:fs/promises';
import crypto from 'node:crypto';
import path from 'node:path';
import {call, translationBase, speechBase} from './seediq-voice.mjs';
const evidence=process.argv[2];
if(!evidence||!path.isAbsolute(evidence))throw Error('Absolute evidence directory required');
const catalogPath='Assets/Resources/Chapter4Voice/catalog.json';
const bank=JSON.parse(await fs.readFile(catalogPath,'utf8'));
await fs.mkdir('Assets/Resources/Chapter4Voice/Seediq',{recursive:true});
await call(translationBase,'to_formosan_languages',['賽德克']);
const voices=await call(speechBase,'lambda',['賽德克']);
await fs.writeFile(path.join(evidence,'seediq-voices.json'),JSON.stringify(voices,null,2));
const numbers=new Map();
async function normalize(value){
  let text=value.replace(/…+|\.{2,}/g,'.').replace(/["「」『』“”()（）]/g,'').replace(/[:;：；]/g,',');
  for(const number of new Set(text.match(/\d+/g)||[])){
    if(!numbers.has(number))numbers.set(number,(await call(translationBase,'translate_1',[number.split('').map(d=>'零一二三四五六七八九'[+d]).join(''),'zho_Hant','trv_Tegu']))[0].replace(/[.!?,]+$/,''));
    text=text.replace(new RegExp('\\b'+number+'\\b','g'),numbers.get(number));
  }
  if(/[\u4e00-\u9fff\d]/.test(text))throw Error('Untranslated text: '+text);
  return text;
}
function combine(parts){
  let format;const samples=[];
  for(const b of parts)for(let p=12;p+8<=b.length;){
    const n=b.readUInt32LE(p+4),body=b.subarray(p+8,p+8+n),name=b.toString('ascii',p,p+4);
    if(name==='fmt '){if(format&&!format.equals(body))throw Error('Formats differ');format=body;}
    if(name==='data')samples.push(body);p+=8+n+n%2;
  }
  if(!format||!samples.length)throw Error('Empty WAV');
  const sound=Buffer.concat(samples),h=Buffer.alloc(12),f=Buffer.alloc(8),d=Buffer.alloc(8);
  h.write('RIFF');h.writeUInt32LE(4+8+format.length+8+sound.length,4);h.write('WAVE',8);
  f.write('fmt ');f.writeUInt32LE(format.length,4);d.write('data');d.writeUInt32LE(sound.length,4);
  return Buffer.concat([h,f,format,d,sound]);
}
let failed=0;
for(const entry of bank.entries){
  if(entry.seediq && (entry.role!=='narrator' || Array.isArray(entry.translationParts)))continue;
  try{
    entry.translationInput=entry.chinese.replaceAll('1930','一九三零').replaceAll('1931','一九三一').replaceAll('莫那・魯道','Mona Rudao').replaceAll('莫那','Mona').replaceAll('巴萬','Bawan').replaceAll('比胡','Pihu').replaceAll('達多','Tado').replaceAll('古慕','Gumu').replaceAll('露比','Lubi').replaceAll('歐冰','Obing');
    // Translate each paragraph separately so the service cannot silently drop
    // the final paragraph of a long historical card or a personal ending.
    entry.translationParts=[];
    for(const input of entry.translationInput.split(/\n\n/)){
      const translated=(await call(translationBase,'translate_1',[input,'zho_Hant','trv_Tegu']))[0];
      if(!translated)throw Error('Empty paragraph translation');
      entry.translationParts.push({input,translation:translated,ttsText:await normalize(translated)});
    }
    entry.translation=entry.translationParts.map(p=>p.translation).join('\n\n');
    entry.ttsText=entry.translationParts.map(p=>p.ttsText).join('\n\n');
    const chunks=[];let chunk='';
    for(const word of entry.ttsText.split(/\s+/).filter(Boolean)){
      if(word.length>280)throw Error('Word too long');
      if(chunk.length+word.length+1>280){chunks.push(chunk);chunk='';}chunk+=(chunk?' ':'')+word;
    }if(chunk)chunks.push(chunk);
    const parts=[];
    const female=['gumu','lubi','obing'].includes(entry.role);
    entry.seediqVoice=female&&JSON.stringify(voices).includes('賽德克_德固達雅_女聲') ? '賽德克_德固達雅_女聲' : '賽德克_德固達雅_男聲';
    for(const text of chunks){
      const [file]=await call(speechBase,'default_speaker_tts',[entry.seediqVoice,text]);
      const url=new URL(file.url);if(url.protocol!=='https:'||url.hostname!=='ai-labs.ilrdf.org.tw')throw Error('Unexpected host');
      const r=await fetch(url,{signal:AbortSignal.timeout(30000)});if(!r.ok)throw Error('Download '+r.status);
      parts.push(Buffer.from(await r.arrayBuffer()));
    }
    const id=crypto.createHash('sha256').update(entry.role+'|'+entry.chinese).digest('hex').slice(0,20);
    entry.seediq='Chapter4Voice/Seediq/'+id;entry.reviewed=false;entry.seediqProvider=speechBase;delete entry.error;
    await fs.writeFile('Assets/Resources/'+entry.seediq+'.wav',combine(parts));
    console.log('Seediq',entry.role,id);
  }catch(e){entry.seediq='';entry.error=e.message;failed++;console.log('FAILED',entry.chinese,e.message);}
  // Mandarin generation finishes before this pass; preserve both tracks.
  await fs.writeFile(catalogPath,JSON.stringify(bank,null,2));
  await fs.writeFile(path.join(evidence,'seediq-progress.json'),JSON.stringify({complete:bank.entries.filter(e=>e.seediq).length,total:bank.entries.length,failed,last:entry.chinese}));
}
const cell=v=>String(v??'').replaceAll('\t',' ').replaceAll('\n',' ⏎ ');
await fs.writeFile('Documentation/Story/第四章_德固達雅配音校對表.tsv','\ufeff'+[['角色','中文字幕','翻譯輸入','官方 AI 初稿','TTS 文字','教師已校對','音檔'],...bank.entries.map(e=>[e.role,e.chinese,e.translationInput,e.translation,e.ttsText,'否',e.seediq])].map(r=>r.map(cell).join('\t')).join('\n')+'\n');
if(failed)process.exitCode=1;
