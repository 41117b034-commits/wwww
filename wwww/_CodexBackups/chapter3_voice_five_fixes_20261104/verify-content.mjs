import fs from 'node:fs/promises';
const root='_CodexBackups/chapter3_voice_five_fixes_20261104';
const chinese=source=>new Set([...source.matchAll(/"(?:[^"\\]|\\.)*"/g)].map(m=>m[0]).filter(s=>/[\u4e00-\u9fff]/.test(s)));
const result={completed:true,checkedFiles:0,originalChineseLiterals:0,removed:[],newChinese:[]};
const transcript=[];
for(const file of await fs.readdir(root+'/before/Chapter3')){
  if(!file.endsWith('.cs'))continue;
  const before=await fs.readFile(root+'/before/Chapter3/'+file,'utf8'),after=await fs.readFile('Assets/Chapter3/'+file,'utf8');
  const old=chinese(before),current=chinese(after);result.checkedFiles++;result.originalChineseLiterals+=old.size;
  for(const text of old)if(!current.has(text))result.removed.push({file,text});
  for(const text of current){transcript.push(file+'\t'+text);if(!old.has(text))result.newChinese.push({file,text});}
}
result.completed=result.removed.length===0;
await fs.writeFile(root+'/report-chinese-preservation.json',JSON.stringify(result,null,2));
await fs.writeFile(root+'/current-Chapter3-Chinese-literals.txt',transcript.join('\n')+'\n');
console.log(JSON.stringify(result));
if(!result.completed)process.exitCode=1;
