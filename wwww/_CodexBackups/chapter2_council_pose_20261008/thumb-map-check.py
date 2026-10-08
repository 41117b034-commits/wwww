import numpy as np
p=np.genfromtxt('_CodexBackups/chapter2_council_pose_20261008/hand-points.csv',delimiter=',',names=True)
Q=np.array([p['x'],p['y'],p['z']]).T;L=Q[:,2].max()
def ss(a,b,v):
 t=np.clip((v-a)/(b-a),0,1);return t*t*(3-2*t)
def mainf(q):
 x,y,z=q.T;xnorm=x/L
 tip=np.interp(xnorm,[-.10,.03,.155,.245],[.976,1,.934,.82])*L
 centre=np.interp(xnorm,[-.10,.03,.155,.245],[.044,.072,.031,-.005])*L
 slope=np.interp(xnorm,[-.10,.03,.155,.245],[-.22,-.26,-.32,-.36])
 start=L*.47;arc=np.maximum(0,z-start);radius=(tip-start)/(np.pi*1.14);angle=arc/radius
 thickness=np.maximum(y-(centre+slope*arc),-radius*.82)
 yp=centre-radius*(1-np.cos(angle))+thickness*np.cos(angle);zp=start+(radius+thickness)*np.sin(angle)
 root=ss(0,L*.075,arc);return np.array([x*(1-.08*root),y*(1-root)+yp*root,z*(1-root)+zp*root]).T
def f(q,par):
 pivot=np.array(par[0])*L;radius=par[1]*L
 u=np.array([-.38,-.1,.92]);u/=np.linalg.norm(u)
 v=np.array(par[2]);v/=np.linalg.norm(v)
 axis=np.cross(u,v);axis/=np.linalg.norm(axis);toward=np.cross(axis,u)
 end=np.arccos(u@v)
 rel=q-pivot;s=rel@u;t=rel@toward;b=rel@axis
 angle=np.clip(s/radius,0,end);tail=np.maximum(0,s-radius*end)
 c=pivot+radius*np.sin(angle[:,None])*u+radius*(1-np.cos(angle[:,None]))*toward+tail[:,None]*v
 turned=t[:,None]*(toward*np.cos(angle[:,None])-u*np.sin(angle[:,None]))+b[:,None]*axis
 tp=c+turned
 tp[s<0]=q[s<0]
 boundary=(-.07-.13*ss(.22*L,.58*L,q[:,2]))*L
 thumb=ss(boundary+par[3]*L,boundary-par[3]*L,q[:,0])*ss(.22*L,.48*L,q[:,2])
 out=mainf(q)*(1-thumb[:,None])+tp*thumb[:,None]
 return q+(out-q)*ss(.05,.75,p['w'])[:,None]
def report(par):
 J=np.stack([(f(Q+np.eye(3)[i]*1e-6,par)-f(Q-np.eye(3)[i]*1e-6,par))/(2e-6) for i in range(3)],axis=2);d=np.linalg.det(J)
 tip=(Q[:,0]<-.029)&(Q[:,2]>.082)
 print(par,'neg',np.sum(d<-.01),'thumb',np.sum((d<-.01)&(Q[:,0]<-.0238)),'min',round(d.min(),3),'tip',np.round(f(Q,par)[tip].mean(axis=0),4))
for pivot in [[-.11,-.03,.23],[-.15,-.04,.28],[-.19,-.055,.37]]:
 for rad in [.14,.18,.22]:
  report((pivot,rad,[.88,-.45,.15],.025))
