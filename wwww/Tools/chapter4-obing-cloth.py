"""Bake Chapter 4's travel cloth onto the original UVs; head and limbs keep their albedo."""
import argparse
import struct
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

root = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
parser.add_argument('--projection', type=Path, required=True,
                    help='Little endian: vertex/index counts, UV + body-space XYZ per vertex, then triangle indices.')
args = parser.parse_args()
source = next((root / 'Assets/人/日本女性/賽德克新娘').rglob('*basecolor.JPEG'))
image = Image.open(source).convert('RGB')
width, height = image.size
with args.projection.open('rb') as stream:
    count, indices = struct.unpack('<ii', stream.read(8))
    vertices = np.fromfile(stream, '<f4', count=count*5).reshape(-1,5)
    tris = np.fromfile(stream, '<i4', count=indices).reshape(-1,3)

uv = np.rint(vertices[:,:2]*[width-1,1-height]+[0,height-1]).astype(np.int32)
positions = vertices[:,2:]
quantized = np.clip(np.rint((positions[tris].mean(axis=1)+[.28,0,.25])*[450,136,470]),0,255).astype(np.uint8)
atlas = Image.new('RGB', image.size)
painter = ImageDraw.Draw(atlas)
for start in range(0, len(tris), 32768):
    batch=tris[start:start+32768]
    for coords, color in zip(uv[batch].reshape(-1,6).tolist(), quantized[start:start+len(batch)].tolist()):
        painter.polygon(coords, fill=tuple(color))
field = np.asarray(atlas,dtype=np.float32)
filled = field[:,:,1]>0
for _ in range(9):
    sums=np.zeros_like(field); weights=np.zeros_like(field[:,:,0])
    for axis,step in ((0,1),(0,-1),(1,1),(1,-1)):
        valid=np.roll(filled,step,axis)
        sums+=np.roll(field,step,axis)*valid[:,:,None]; weights+=valid
    new=(~filled)&(weights>0)
    field[new]=sums[new]/weights[new,None];filled|=new
field=np.asarray(Image.fromarray(np.uint8(field)).filter(ImageFilter.GaussianBlur(.85)),dtype=np.float32)
x,y,z = field[:,:,0]/450-.28, field[:,:,1]/136, field[:,:,2]/470-.25
rgb=np.asarray(image,dtype=np.float32); red,green,blue=rgb[:,:,0],rgb[:,:,1],rgb[:,:,2]
def ramp(a,b,value):
    t=np.clip((value-a)/(b-a),0,1);return t*t*(3-2*t)
skin = ramp(6,17,green-blue)*ramp(10,27,red-green)*ramp(.55,.68,green/np.maximum(red,1))
cloth = ramp(.23,.26,y)*(1-ramp(1.47,1.515,y))*(1-skin)*filled
light = np.clip((rgb @ np.array([.21,.68,.11],dtype=np.float32)) / 100, .10, 1.45)
shade = .77 + .34*light
warp = np.sin(1260*y+np.sin(40*x)*.8)*1.2 + np.cos(940*x+23*y)*.9
upper = ramp(1.095,1.14,y)
lower = np.array([72,68,54],dtype=np.float32)
flax = np.array([194,182,150],dtype=np.float32)
fabric = lower+(flax-lower)*upper[:,:,None]
def stripe(center,half):
    return 1-ramp(half-.006,half+.006,abs(y-center))
def band(base,mark,tint):
    return base*(1-mark[:,:,None])+np.asarray(tint,dtype=np.float32)*mark[:,:,None]
for center,half,tint in ((1.155,.013,[100,78,64]),(1.205,.016,[133,72,51]),(1.25,.008,[102,86,66]),
                         (.44,.012,[132,111,85]),(.398,.020,[117,72,53]),(.348,.010,[151,131,98])):
    fabric=band(fabric,stripe(center,half),tint)
sash = 1-ramp(.040,.048,np.abs(x-((y-1.11)*.55-.06)))
sash *= ramp(.82,.88,y)*(1-ramp(1.43,1.48,y))*ramp(.025,.065,np.abs(z))
fabric=band(fabric,sash,[153,105,73])
fabric=fabric*shade[:,:,None]+warp[:,:,None]
result=np.uint8(np.clip(rgb+(fabric-rgb)*cloth[:,:,None],0,255))
out=root/'Assets/Chapter4/Environment/Obing travel cloth.png'
Image.fromarray(result).save(out)
