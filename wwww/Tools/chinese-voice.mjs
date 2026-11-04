import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
const root='Assets/Resources/ChineseVoice',evidence='_CodexBackups/voice_languages_20261104';
const previous=JSON.parse(await fs.readFile(root+'/catalog.json','utf8').catch(()=>'{}'));
const previousEntries=new Map((previous.entries??[]).map(e=>[e.chinese,e]));
const entries=new Map();
function add(chinese,source,female=false){if(!chinese||!/[\u4e00-\u9fff]/.test(chinese))return;let e=entries.get(chinese);if(!e){const key=crypto.createHash('sha256').update(chinese).digest('hex').slice(0,20);e={chinese,source,resource:'ChineseVoice/Audio/'+key+'_m',femaleResource:'',provider:'Windows installed Taiwan Chinese voice (Microsoft Zhiwei / Hanhan)'};entries.set(chinese,e);}if(female)e.femaleResource=e.resource.slice(0,-1)+'f';}
function decode(b){const utf=b.toString('utf8');return utf.includes('\ufffd')?new TextDecoder('big5').decode(b):utf;}
async function scan(folder,chapter){for(const file of await fs.readdir(folder)){if(!file.endsWith('.cs'))continue;const s=decode(await fs.readFile(path.join(folder,file)));for(const match of s.matchAll(/"(?:[^"\\]|\\.)*"/g)){let value;try{value=JSON.parse(match[0]);}catch{continue;}if(value.length<2||value.length>320||!/[。？！!…，,]$/.test(value)||/[{}]|\\|<color|<size/.test(value))continue;add(value,chapter+':'+file,chapter==='Chapter1'&&(/謝謝|收下|祖靈庇佑|放開我|新人|今晚|婚禮/.test(value)));}}}
await scan('Assets/腳本/第一章演出','Chapter1');await scan('Assets/Chapter2','Chapter2');
const scene=await fs.readFile('Assets/Scenes/第二章.unity','utf8');for(const m of scene.matchAll(/^    chinese: (".*")$/gm))add(JSON.parse(m[1].replace(/\\x([0-9a-f]{2})/gi,'\\u00$1')),'Chapter2 scene');
for(const name of ['第一章新版警察','第二章']){const scene=await fs.readFile('Assets/Scenes/'+name+'.unity','utf8');for(const m of scene.matchAll(/"(?:[^"\\]|\\.)*"/g)){try{const value=JSON.parse(m[0].replace(/\\x([0-9a-f]{2})/gi,'\\u00$1'));if(value.length<320&&/[。？！!…]$/.test(value))add(value,name+' scene');}catch{}}}
const c3=JSON.parse(await fs.readFile('Assets/Resources/Chapter3Voice/catalog.json','utf8'));for(const e of c3.entries)add(e.chinese,'Chapter3:'+e.source);
add('謝謝你。','Chapter1 delivery',true);add('放開我！','Chapter1 female',true);
await fs.mkdir(root+'/Audio',{recursive:true});
// Inventory refresh must retain authored neural voice variants and provenance.
for(const entry of entries.values()){const old=previousEntries.get(entry.chinese);if(old){Object.assign(entry,{...old,source:entry.source});}}
const catalog={language:'zh-TW',male:'Microsoft Zhiwei - Chinese (Traditional, Taiwan)',female:'Microsoft Hanhan - Chinese (Traditional, Taiwan)',rate:-1,...previous,entries:[...entries.values()]};
await fs.writeFile(root+'/catalog.json',JSON.stringify(catalog,null,2));
const jobs=[];for(const e of catalog.entries)for(const [female,resource] of [[false,e.resource],[true,e.femaleResource]])if(resource){const file=path.resolve('Assets/Resources',resource+'.wav');try{if((await fs.stat(file)).size>100)continue;}catch{}jobs.push({text:e.chinese,path:file,female});}
await fs.writeFile(evidence+'/chinese-jobs.json',JSON.stringify({jobs},null,2));
console.log(JSON.stringify({entries:entries.size,jobs:jobs.length,chapter1:catalog.entries.filter(e=>e.source.startsWith('Chapter1')).length,chapter2:catalog.entries.filter(e=>e.source.startsWith('Chapter2')).length,chapter3:catalog.entries.filter(e=>e.source.startsWith('Chapter3')).length}));
