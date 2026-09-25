# Réplica aproximada (PIL) de ui/components/GameLoadingScreen.kt (pantalla de carga de un juego) para ver el
# diseño sin compilar. 1 dp = 3 px (teléfono ~xxhdpi). La luna se dibuja en un solo instante de su órbita.
import os, math, random
import numpy as np
from PIL import Image, ImageDraw, ImageFont
ROOT=os.path.abspath(os.path.join(os.path.dirname(__file__),'..','..'))
FB=ROOT+'/unity/NeuroVidaCore/Assets/Resources/Fonts/Fredoka-Bold.ttf'
FS=ROOT+'/unity/NeuroVidaCore/Assets/Resources/Fonts/Fredoka-SemiBold.ttf'
EMOJI='/usr/share/fonts/truetype/noto/NotoColorEmoji.ttf'
DP=3; W,H=412*DP,915*DP
INK=(26,18,64); CREAM=(255,255,255); SUN=(255,201,60); CORAL=(255,107,74); SKY=(76,201,240); GRAPE=(184,164,255); SOFT=(180,191,234)
GAME=dict(title='Secuencia Lumínica', emoji='💡', color=(59,130,246), level='Nivel 3 · Intermedio', mode='Reto',
          instr='Observa con atención la secuencia de luces de colores y reprodúcela en el orden exacto.')
def f(p,s): return ImageFont.truetype(p,int(s*DP))
t=np.linspace(1,0,H)[:,None,None]; b=np.array([4,6,28.]); m=np.array([8,14,58.]); tp=np.array([16,26,88.])
bg=np.repeat(np.where(t<0.5,b+(m-b)*(t*2),m+(tp-m)*((t-0.5)*2)),W,axis=1)
def glow(cx,cy,r,rgb,a):
    y,x=np.mgrid[0:H,0:W]; d=np.clip(1-np.sqrt((x-cx)**2+(y-cy)**2)/r,0,1)**2*a; bg[:]=bg*(1-d[...,None])+np.array(rgb)*d[...,None]
glow(0.9*W,0.1*H,0.9*W,SKY,0.27); glow(0.1*W,0.55*H,0.8*W,GRAPE,0.19); glow(0.05*W,0.95*H,0.85*W,CORAL,0.24)
im=Image.fromarray(bg.clip(0,255).astype(np.uint8)).convert('RGBA')
class Blend:
    # ImageDraw sobre RGBA reemplaza píxeles en vez de mezclarlos: cada figura se dibuja en una capa y se compone.
    def __getattr__(self,name):
        def call(*a,**k):
            ov=Image.new('RGBA',im.size,(0,0,0,0)); getattr(ImageDraw.Draw(ov),name)(*a,**k); im.alpha_composite(ov)
        return call
    def textlength(self,*a,**k): return ImageDraw.Draw(im).textlength(*a,**k)
d=Blend()
rnd=random.Random(3)
for i in range(110):
    x,y=rnd.random()*W,rnd.random()*H; r=rnd.random()*2.2*DP/2+1; a=int(90+rnd.random()*140)
    d.ellipse([x-r,y-r,x+r,y+r],fill=(255,195,138,a) if i%6==0 else (255,255,255,a))
def wrap(s,font,maxw):
    lines=[]; cur=''
    for w in s.split():
        nxt=(cur+' '+w).strip()
        if d.textlength(nxt,font=font)<=maxw: cur=nxt
        else: lines.append(cur); cur=w
    return lines+[cur]
def ctext(y,s,font,fill): w=d.textlength(s,font=font); d.text((W/2-w/2,y),s,font=font,fill=fill)
fi=f(FS,18); lines=wrap(GAME['instr'],fi,W-64*DP)
# Alto total de la columna (centrada en vertical como Arrangement.Center)
col=220+18+36+6+22+34+18+8+26*len(lines)+40+20
y0=H/2-col*DP/2
# --- planeta con órbita ---
bw,bh=260*DP,220*DP; cx,cy=W/2,y0+bh/2; R=132*DP/2; pulse=0.5
ov=Image.new('RGBA',im.size,(0,0,0,0)); od=ImageDraw.Draw(ov)
hr=R*(1.95+0.12*pulse)
for k in range(60,0,-1):
    rr=hr*k/60; a=int(255*(0.30+0.14*pulse)*(1-k/60)**1.6); od.ellipse([cx-rr,cy-rr,cx+rr,cy+rr],fill=GAME['color']+(a,))
im.alpha_composite(ov)
rx,ry,tilt=bw*0.46,bh*0.17,-0.28
def op(tt):
    a=tt*2*math.pi; x=math.cos(a)*rx; y=math.sin(a)*ry
    return (cx+x*math.cos(tilt)-y*math.sin(tilt), cy+x*math.sin(tilt)+y*math.cos(tilt))
def orbit(back):
    n=48
    for i in range(n):
        t0,t1=i/n,(i+1)/n
        if (math.sin((t0+t1)*math.pi)<0)!=back: continue
        d.line([op(t0),op(t1)],fill=(255,255,255,int(255*(0.12 if back else 0.26))),width=2*DP)
def moon(ang):
    p=op(ang); dep=(math.sin(ang*2*math.pi)+1)/2; r=(9+5*dep)*DP
    d.ellipse([p[0]-r,p[1]-r+2.5*DP,p[0]+r,p[1]+r+2.5*DP],fill=INK)
    d.ellipse([p[0]-r,p[1]-r,p[0]+r,p[1]+r],fill=SUN,outline=INK,width=int(2.5*DP))
    s=r*0.28; d.ellipse([p[0]-r*0.3-s,p[1]-r*0.32-s,p[0]-r*0.3+s,p[1]-r*0.32+s],fill=(255,255,255,140))
ANG=0.62
orbit(True)
if math.sin(ANG*2*math.pi)<0: moon(ANG)
d.ellipse([cx-R,cy-R+6*DP,cx+R,cy+R+6*DP],fill=INK)
d.ellipse([cx-R,cy-R,cx+R,cy+R],fill=GAME['color'])
r2=R*0.78; d.ellipse([cx-R*0.18-r2,cy-R*0.2-r2,cx-R*0.18+r2,cy-R*0.2+r2],fill=(255,255,255,46))
d.ellipse([cx-R,cy-R,cx+R,cy+R],outline=INK,width=int(3.5*DP))
d.ellipse([cx-R*0.42,cy-R*0.8,cx+R*0.42,cy-R*0.8+R*0.26],fill=(255,255,255,87))
em=Image.new('RGBA',(160,160),(0,0,0,0)); ImageDraw.Draw(em).text((0,0),GAME['emoji'],font=ImageFont.truetype(EMOJI,109),embedded_color=True)
es=int(60*DP); em=em.resize((es,es),Image.LANCZOS); im.alpha_composite(em,(int(cx-es/2),int(cy-es/2)))
orbit(False)
if math.sin(ANG*2*math.pi)>=0: moon(ANG)
y=y0+bh+18*DP
ctext(y,GAME['title'],f(FB,32),CREAM); y+=36*DP+6*DP
ctext(y,GAME['level']+'  ·  '+GAME['mode'],f(FS,16),SUN); y+=22*DP+34*DP
fl=f(FS,13); lab='Cómo se juega'
ctext(y,lab,fl,SOFT); y+=18*DP+8*DP
for ln in lines: ctext(y,ln,fi,(255,255,255,235)); y+=26*DP
y+=40*DP
ctext(y,'Preparando el juego...',f(FS,15),(255,255,255,158))
out=ROOT+'/docs/previews/pantalla-carga.png'
im.convert('RGB').resize((W//2,H//2),Image.LANCZOS).save(out); print(out)
