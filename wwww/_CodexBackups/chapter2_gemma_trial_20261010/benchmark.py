import json, pathlib, re, subprocess, sys, time, urllib.request

root = pathlib.Path(__file__).resolve().parents[2]
out = pathlib.Path(__file__).resolve().parent
model = sys.argv[1]
current = len(sys.argv)>2 and sys.argv[2]=='current'
label = model+('-current' if current else '')
source = ((root if current else out) / 'Assets/Chapter2/Chapter2LocalDialogue.cs').read_text(encoding='utf-8-sig')
world = (root / 'Assets/Resources/Chapter2LocalDialogueContext.txt')
if not world.exists():
    world = next((root/'Assets').rglob('Chapter2LocalDialogueContext.txt'))
world = world.read_text(encoding='utf-8-sig')
roles = [
 ('阿威·比胡','Awi Pihu','你是熟悉林道的賽德克青年，負責帶玩家前往巨木。珍惜森林，說話沉穩，願意介紹眼前環境。'),
 ('中村正雄','Nakamura Masao','你是現場督工的日本警察。語氣簡短嚴肅，要求族人服從伐木命令，但不透露未發生的劇情。'),
 ('都比·阿威','Dupi Awi','你是跟著家人在林邊活動的賽德克小孩。好奇、說話簡單，關心家人和動植物，不懂軍事政治。')]
expression = source.split('return "請扮演',1)[1].split(';\n    }',1)[0]
expression = '"請扮演'+expression
def persona(role):
    values = dict(zip(['actor.DisplayName','actor.romanizedName','role'],role),world=world)
    part=expression
    if current:
        part=part.split('(police?',1)[0]
    result=''.join(json.loads(token) if token.startswith('"') else values[token] for token in re.findall(r'"(?:\\.|[^"\\])*"|actor\.DisplayName|actor\.romanizedName|\brole\b|\bworld\b',part))
    if current:
        boundary=re.findall(r'"(?:\\.|[^"\\])*"',expression.split('(police?',1)[1])
        result+=json.loads(boundary[0 if role==roles[1] else 1 if role==roles[2] else 2])
    return result

files={'qwen':'qwen2.5-1.5b-instruct-q4_k_m.gguf','gemma':'gemma-4-E2B-it-Q4_0.gguf'}
args=['D:/WusheLocalLLM/runtime/llama-server.exe','-m','D:/WusheLocalLLM/models/'+files[model],
 '--alias','wushe-trial','--host','127.0.0.1','--port','18081','-c','4096','-t','4','-tb','4','-np','1','-ngl','0','--no-warmup','--reasoning','off']
report={'model':model,'args':args,'prompt_source':'updated project prompt' if current else 'pre-change project snapshot','temperature':.65,'max_tokens':160,'results':[]}
report['personas']={role[0]:persona(role) for role in roles}
def save(): (out/(label+'-benchmark.json')).write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
def request(messages):
    body=json.dumps({'model':'wushe-trial','messages':messages,'temperature':.65,'max_tokens':160,'stream':False,'seed':42}).encode()
    req=urllib.request.Request('http://127.0.0.1:18081/v1/chat/completions',data=body,headers={'Content-Type':'application/json'})
    with urllib.request.urlopen(req,timeout=90) as response: return json.load(response)

with (out/(label+'-server.log')).open('w',encoding='utf-8') as log:
    process=subprocess.Popen(args,stdout=log,stderr=log,creationflags=subprocess.CREATE_NO_WINDOW)
    try:
        ready=time.perf_counter()
        while time.perf_counter()-ready<75:
            if process.poll() is not None: raise RuntimeError('server exited: '+str(process.returncode))
            try:
                with urllib.request.urlopen('http://127.0.0.1:18081/health',timeout=1) as r:
                    if r.status==200: break
            except Exception: time.sleep(.5)
        else: raise TimeoutError('startup')
        report['startup_seconds']=round(time.perf_counter()-ready,3)
        cases=[]
        for role in roles:
            cases.extend([(role,'guess','你猜我叫甚麼名字',[]),(role,'identity','你叫什麼名字？你在這裡做什麼？',[])])
        cases.extend([(roles[0],'place','西仔希克是誰？',[]),(roles[1],'forest','這片森林對你有什麼意義？',[]),
          (roles[0],'unknown','我們部落的祭典精確有幾個步驟？每個日期和禁忌是什麼？',[]),
          (roles[0],'remember','你還記得我叫什麼名字嗎？',[{'role':'user','content':'我叫阿山。'},{'role':'assistant','content':'阿山，我記住了。'}])])
        for role,case,question,history in cases:
            messages=[{'role':'system','content':persona(role)}]+history+[{'role':'user','content':question}]
            start=time.perf_counter()
            try:
                reply=request(messages)
                item={'role':role[0],'case':case,'question':question,'seconds':round(time.perf_counter()-start,3),'reply':reply}
                print(json.dumps({'model':model,'role':role[0],'case':case,'seconds':item['seconds'],'answer':reply['choices'][0]['message']},ensure_ascii=False),flush=True)
            except Exception as error:
                item={'role':role[0],'case':case,'error':str(error),'seconds':round(time.perf_counter()-start,3)}
                print(json.dumps(item,ensure_ascii=False),flush=True)
            report['results'].append(item);save()
    finally:
        save()
        process.terminate()
        try: process.wait(timeout=10)
        except subprocess.TimeoutExpired: process.kill()
