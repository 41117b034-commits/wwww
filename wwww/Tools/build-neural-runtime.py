"""Package a relocatable Windows worker, retaining dependency source/licenses."""
from pathlib import Path
import hashlib
import sys
import zipfile
import io

root=Path(__file__).resolve().parents[1]
base=Path(sys.base_prefix)
site=Path(sys.prefix)/'Lib/site-packages'
target=root/'Assets/StreamingAssets/ChineseVoice/neural-runtime.zip'
packages={'edge_tts','aiohttp','aiohappyeyeballs','aiosignal','attr','attrs','certifi','frozenlist','idna','multidict','propcache','yarl','tabulate','typing_extensions'}
stdlib=io.BytesIO()
with zipfile.ZipFile(stdlib,'w',zipfile.ZIP_DEFLATED) as out:
    for path in (base/'Lib').rglob('*.py'):
        rel=path.relative_to(base/'Lib')
        if any(part in {'site-packages','__pycache__','test','tests','idlelib','tkinter','ensurepip'} for part in rel.parts):continue
        out.write(path,str(rel))
with zipfile.ZipFile(target,'w',zipfile.ZIP_DEFLATED) as out:
    for path in base.iterdir():
        if path.suffix in {'.exe','.dll'} or path.name=='LICENSE.txt':out.write(path,path.name)
    out.writestr('python312.zip',stdlib.getvalue())
    out.writestr('python312._pth','python312.zip\n.\nDLLs\nLib/site-packages\nimport site\n')
    for path in (base/'DLLs').iterdir():
        if path.is_file() and path.suffix in {'.pyd','.dll'}:out.write(path,'DLLs/'+path.name)
    for child in site.iterdir():
        name=child.name.split('-')[0].removesuffix('.py')
        if name not in packages:continue
        if child.is_file():out.write(child,'Lib/site-packages/'+child.name)
        else:
            for path in child.rglob('*'):
                if path.is_file() and '__pycache__' not in path.parts and path.suffix!='.pyc':out.write(path,'Lib/site-packages/'+str(path.relative_to(site)))
checksum=hashlib.sha256(target.read_bytes()).hexdigest()
target.with_suffix('.sha256.txt').write_text(checksum,encoding='ascii')
print({'bytes':target.stat().st_size,'sha256':checksum,'python':sys.version.split()[0]})
