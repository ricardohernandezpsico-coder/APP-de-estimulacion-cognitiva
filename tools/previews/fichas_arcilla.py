import os
ROOT=os.path.abspath(os.path.join(os.path.dirname(__file__),'..','..'))
import numpy as np
from PIL import Image, ImageDraw, ImageFont
N=256; AA=0.014; SS=0.86
def rb(x,y,cy,hx=0.93,hy=0.86,r=0.32):
    qx=np.abs(x)-hx+r; qy=np.abs(y-cy)-hy+r
    ox=np.maximum(qx,0); oy=np.maximum(qy,0)
    return np.sqrt(ox*ox+oy*oy)+np.minimum(np.maximum(qx,qy),0)-r
es=lambda s: np.clip(0.5-s/AA,0,1)
def over(c,a,g,al):
    c=g*al+c*(1-al); a=al+a*(1-al); return c,a
def new(press):
    y,x=np.mgrid[0:N,0:N]; nx=((x+0.5)/N*2-1)/SS; ny=-((y+0.5)/N*2-1)/SS
    c=np.zeros((N,N)); a=np.zeros((N,N)); fy=0.06-press
    c,a=over(c,a,0.0,es(rb(nx,ny,0.06-0.14)-0.075))
    face=rb(nx,ny,fy); c,a=over(c,a,0.0,es(face-0.075))
    fm=es(face); t=np.clip((0.93-(ny-fy))/1.8,0,1); fill=0.95+(0.84-0.95)*t
    ish=np.clip((ny-fy+0.86)/0.26,0,1); fill*=0.80+(1-0.80)*ish
    c,a=over(c,a,fill,fm)
    rim=np.clip(1-np.abs(face+0.07)/0.05,0,1)*np.clip((ny-fy)/0.5,0,1)
    c,a=over(c,a,1.0,rim*0.45*fm)
    gx=(nx+0.30)/0.46; gy=(ny-fy-0.50)/0.17; gl=np.clip(1-(gx*gx+gy*gy),0,1)
    c,a=over(c,a,1.0,np.sqrt(gl)*0.42*fm)
    g=np.where(a>1e-4,c/np.maximum(a,1e-4),0); return np.clip(g,0,1),np.clip(a,0,1)
def old():
    y,x=np.mgrid[0:N,0:N]; nx=((x+0.5)/N*2-1); ny=-((y+0.5)/N*2-1); sx=nx/SS; sy=ny/SS
    c=np.zeros((N,N)); a=np.zeros((N,N))
    c,a=over(c,a,0.5,es(rb(sx,sy,-0.05)))
    face=rb(sx,sy,0.07); e=0.01
    gx=rb(sx+e,sy,0.07)-rb(sx-e,sy,0.07); gy=rb(sx,sy+e,0.07)-rb(sx,sy-e,0.07); gl=np.sqrt(gx*gx+gy*gy)+1e-5
    lit=np.clip((-gx/gl*0.6+gy/gl*0.8)*0.5+0.5,0,1); c,a=over(c,a,0.74+0.26*lit,es(face))
    inner=es(face+0.11); t=np.clip((0.93-sy)/1.7,0,1); c,a=over(c,a,1-0.1*t,inner)
    gs=np.clip((sy-0.30)/0.55,0,1); c,a=over(c,a,1.0,gs*gs*0.2*inner)
    g=np.where(a>1e-4,c/np.maximum(a,1e-4),0)
    sh=0.42*(1-np.clip(((rb(sx,sy+0.16,-0.05)*SS)+0.03)/0.24,0,1)**2*(3-2*np.clip(((rb(sx,sy+0.16,-0.05)*SS)+0.03)/0.24,0,1)))
    outA=a+sh*(1-a); k=np.where(outA>1e-4,a/np.maximum(outA,1e-4),0)
    return np.clip(g*k,0,1),np.clip(outA,0,1)
def tint(g,a,rgb):
    col=np.stack([g*rgb[0],g*rgb[1],g*rgb[2]],-1)
    return col,a
def night(W,H):
    t=np.linspace(1,0,H)[:,None]
    b=np.array([4,6,28])/255; m=np.array([8,14,58])/255; tp=np.array([16,26,88])/255
    col=np.where(t[...,None]<0.5, b+(m-b)*(t[...,None]*2), m+(tp-m)*((t[...,None]-0.5)*2))
    return np.repeat(col,W,axis=1)
pal={'Coral':(1,.42,.29),'Sol':(1,.79,.24),'Cielo':(.30,.79,.94),'Uva':(.72,.64,1),'Lima':(.61,.9,.39),'Crema':(1,1,1)}
S=200; W=40+len(pal)*(S+20); H=3*(S+60)+40
bg=night(W,H); img=bg.copy()
rows=[('Antes',old()),('Nueva',new(0)),('Nueva presionada',new(0.10))]
for ri,(name,(g,a)) in enumerate(rows):
    gi=np.array(Image.fromarray((g*255).astype(np.uint8)).resize((S,S),Image.LANCZOS))/255
    ai=np.array(Image.fromarray((a*255).astype(np.uint8)).resize((S,S),Image.LANCZOS))/255
    for ci,(pn,rgb) in enumerate(pal.items()):
        col,al=tint(gi,ai,rgb); x0=40+ci*(S+20); y0=50+ri*(S+60)
        reg=img[y0:y0+S,x0:x0+S]; img[y0:y0+S,x0:x0+S]=reg*(1-al[...,None])+col*al[...,None]
out=Image.fromarray((img*255).astype(np.uint8)); d=ImageDraw.Draw(out)
try: f=ImageFont.truetype(''+ROOT+'/unity/NeuroVidaCore/Assets/Resources/Fonts/Fredoka-Bold.ttf',26)
except: f=None
for ri,(name,_) in enumerate(rows): d.text((40,14+ri*(S+60)),name,fill=(234,240,255),font=f)
for ci,pn in enumerate(pal): d.text((40+ci*(S+20)+S//2-30,H-30),pn,fill=(180,191,234),font=f)
out.save(''+ROOT+'/docs/previews/fichas-arcilla.png') if __import__('os').makedirs(''+ROOT+'/docs/previews',exist_ok=True) is None else None
print(out.size)
