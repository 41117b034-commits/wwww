import concurrent.futures, hashlib, pathlib, subprocess, time
path=pathlib.Path('D:/WusheLocalLLM/models/gemma-4-E2B-it-Q4_0.gguf.download')
total=2841481184; start=path.stat().st_size; chunk=8*1024*1024
url='https://huggingface.co/ggml-org/gemma-4-E2B-it-GGUF/resolve/b4243c156154b6dca9324415f8c7ccc098b4aed1/gemma-4-E2B-it-Q4_0.gguf?download=true'
ranges=[(first,min(total,first+chunk)-1) for first in range(start,total,chunk)]
def download(item):
    i,(first,last)=item; part=path.with_suffix('.chunk'+str(i))
    if part.exists() and part.stat().st_size==last-first+1:return part
    subprocess.run(['curl.exe','-L','--fail','--silent','--show-error','--retry','3','--connect-timeout','15','--max-time','180',
      '--range',f'{first}-{last}','-o',str(part),url],check=True,creationflags=subprocess.CREATE_NO_WINDOW)
    assert part.stat().st_size==last-first+1
    if i%16==0:print(f'completed chunk {i+1}/{len(ranges)}',flush=True)
    return part
with concurrent.futures.ThreadPoolExecutor(max_workers=6) as pool:parts=list(pool.map(download,enumerate(ranges)))
assert path.stat().st_size==start
with path.open('ab') as file:
    for part in parts:
        with part.open('rb') as source:
            while block:=source.read(1024*1024):file.write(block)
with path.open('rb') as file:digest=hashlib.file_digest(file,'sha256').hexdigest()
assert path.stat().st_size==total and digest=='8e30dff3ac4c8434c49a7036fa15564bdbb6044e42bf04550bf1a096ad7e6a52'
path.rename(path.with_suffix(''))
for part in parts:part.unlink()
print('verified '+digest,flush=True)
