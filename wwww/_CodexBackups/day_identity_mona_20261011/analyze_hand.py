import csv,json
from pathlib import Path
p=Path('_CodexBackups/day_identity_mona_20261011'); L=.1287387
v={int(r['index']):[float(r[a])/L for a in ['x','y','z']] for r in csv.DictReader((p/'hand-vertices.csv').open())}
tris=list(map(int,(p/'hand-triangles.txt').read_text().split(',')))
for cut in [.48,.53,.58,.62]:
 ids={i for i,q in v.items() if q[2]>cut}; parent={i:i for i in ids}
 def find(a):
  while parent[a]!=a: parent[a]=parent[parent[a]];a=parent[a]
  return a
 def union(a,b):
  a,b=find(a),find(b)
  if a!=b:parent[a]=b
 weld={}
 for i in ids:
  k=tuple(round(x,4) for x in v[i])
  if k in weld:union(i,weld[k])
  else:weld[k]=i
 for k in range(0,len(tris),3):
  a,b,c=tris[k:k+3]
  for x,y in [(a,b),(b,c),(c,a)]:
   if x in ids and y in ids:union(x,y)
 groups={}
 for i in ids:groups.setdefault(find(i),[]).append(i)
 out=[]
 for group in sorted(groups.values(),key=len,reverse=True)[:10]:
  out.append({'n':len(group),'center':[round(sum(v[i][a] for i in group)/len(group),4) for a in range(3)],'min':[round(min(v[i][a] for i in group),4) for a in range(3)],'max':[round(max(v[i][a] for i in group),4) for a in range(3)]})
 print(cut,json.dumps(out))
 if cut==.53:
  selected=min((g for g in groups.values() if len(g)>100),key=lambda g:abs(sum(v[i][0] for i in g)/len(g)+.10))
  (p/'index-vertices.txt').write_text(','.join(map(str,selected)))
