import concurrent.futures, hashlib, json, pathlib, time, urllib.request
path=pathlib.Path('D:/WusheLocalLLM/models/gemma-4-E2B-it-Q4_0.gguf.download')
total=2841481184
start=path.stat().st_size
url='https://huggingface.co/ggml-org/gemma-4-E2B-it-GGUF/resolve/b4243c156154b6dca9324415f8c7ccc098b4aed1/gemma-4-E2B-it-Q4_0.gguf?download=true'
ranges=[(start+(total-start)*i//4,start+(total-start)*(i+1)//4-1) for i in range(4)]
manifest=path.with_suffix('.ranges.json')
manifest.write_text(json.dumps({'start':start,'total':total,'ranges':ranges}),encoding='utf-8')
def download(item):
    i,(first,last)=item
    part=path.with_suffix('.part'+str(i))
    assert not part.exists(), 'Refuse to overwrite a prior chunk'
    for attempt in range(4):
        offset=part.stat().st_size if part.exists() else 0
        if first+offset>last:return part
        try:
            req=urllib.request.Request(url,headers={'Range':f'bytes={first+offset}-{last}'})
            with urllib.request.urlopen(req,timeout=45) as response:
                assert response.status==206 and response.headers['Content-Range'].startswith(f'bytes {first+offset}-')
                with part.open('ab') as file:
                    while block:=response.read(1024*1024): file.write(block)
            assert part.stat().st_size==last-first+1
            print('chunk completed '+str(i),flush=True)
            return part
        except Exception:
            if attempt==3:raise
            time.sleep(1)
with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:
    parts=list(pool.map(download,enumerate(ranges)))
assert path.stat().st_size==start
with path.open('ab') as file:
    for part in parts:
        with part.open('rb') as source:
            while block:=source.read(1024*1024):file.write(block)
digest=hashlib.file_digest(path.open('rb'),'sha256').hexdigest()
assert path.stat().st_size==total and digest=='8e30dff3ac4c8434c49a7036fa15564bdbb6044e42bf04550bf1a096ad7e6a52'
path.rename(path.with_suffix(''))
for part in parts:part.unlink()
manifest.unlink()
print('verified '+digest,flush=True)
