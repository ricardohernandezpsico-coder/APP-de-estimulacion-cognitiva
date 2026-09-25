# Réplica aproximada (PIL) de ui/components/ShareCard.kt: la imagen de 1080x1920 que se comparte al subir de liga.
import os, math, random
import numpy as np
from PIL import Image, ImageDraw, ImageFont
ROOT=os.path.abspath(os.path.join(os.path.dirname(__file__),'..','..'))
FB=ROOT+'/unity/NeuroVidaCore/Assets/Resources/Fonts/Fredoka-Bold.ttf'
FS=ROOT+'/unity/NeuroVidaCore/Assets/Resources/Fonts/Fredoka-SemiBold.ttf'
W,H=1080,1920
INK=(26,18,64); SUN=(255,201,60); CORAL=(255,107,74); SKY=(76,201,240); GRAPE=(184,164,255); DIM=(180,191,234)
# Plata (LeagueShield.palette)
LIGHT=(244,246,250); BASE=(183,191,204); DARK=(110,119,135); GLOW=(221,230,245)
HEAD,SUB,RATING,STREAK='Subí a Plata','en Secuencia Lumínica',255,7
t=np.linspace(0,1,H)[:,None,None]; top=np.array([16,26,88.]); mid=np.array([8,14,58.]); bot=np.array([4,6,28.])
bg=np.repeat(np.where(t<0.5,top+(mid-top)*(t*2),mid+(bot-mid)*((t-0.5)*2)),W,axis=1)
Y,X=np.mgrid[0:H,0:W]
def glow(cx,cy,r,rgb,a):
    d=np.clip(1-np.sqrt((X-cx)**2+(Y-cy)**2)/r,0,1)*a; bg[:]=bg*(1-d[...,None])+np.array(rgb)*d[...,None]
glow(W*.88,H*.10,950,SKY,.30); glow(W*.08,H*.92,950,CORAL,.26); glow(W*.15,H*.45,700,GRAPE,.16)
glow(W/2,720,560,GLOW,.55)
im=Image.fromarray(bg.clip(0,255).astype(np.uint8)).convert('RGBA')
def over(fn):
    o=Image.new('RGBA',im.size,(0,0,0,0)); fn(ImageDraw.Draw(o)); im.alpha_composite(o)
rnd=random.Random(7)
def stars(d):
    for i in range(170):
        x,y=rnd.random()*W,rnd.random()*H; r=1.5+rnd.random()*3.5; a=int(255*(0.3+rnd.random()*0.7))
        d.ellipse([x-r,y-r,x+r,y+r],fill=((255,195,138) if i%6==0 else (255,255,255))+(a,))
over(stars)
# rayos
c=(W/2,720)
def rays(d):
    for i in range(14):
        a=math.radians(i*360/14-90); h=.085; R=640
        for k in range(20,0,-1):
            rr=R*k/20; al=int(255*.30*(1-k/20))
            d.polygon([c,(c[0]+math.cos(a-h)*rr,c[1]+math.sin(a-h)*rr),(c[0]+math.cos(a+h)*rr,c[1]+math.sin(a+h)*rr)],fill=LIGHT+(al//3,))
over(rays)
# escudo (forma de LeagueShield.shieldPath, simplificada)
sw=470; sh=sw*1.12; L0=c[0]-sw/2; T0=c[1]-sh/2
def shield(inset):
    l,r,t,b=L0+inset,L0+sw-inset,T0+inset,T0+sh-inset; cx=(l+r)/2; pts=[]
    def cub(p0,p1,p2,p3,n=16): return [tuple((1-u)**3*a+3*(1-u)**2*u*bb+3*(1-u)*u*u*cc+u**3*dd for a,bb,cc,dd in zip(p0,p1,p2,p3)) for u in [i/n for i in range(1,n+1)]]
    H_=b-t; p=(cx,t); pts=[p]
    pts+=cub(p,(cx+(r-cx)*.35,t+H_*.05),(r-(r-cx)*.25,t+H_*.06),(r,t+H_*.12)); pts.append((r,t+H_*.5))
    pts+=cub((r,t+H_*.5),(r,t+H_*.76),(cx+(r-cx)*.55,t+H_*.9),(cx,b)); pts+=cub((cx,b),(cx-(r-cx)*.55,t+H_*.9),(l,t+H_*.76),(l,t+H_*.5))
    pts.append((l,t+H_*.12)); pts+=cub((l,t+H_*.12),(l+(r-cx)*.25,t+H_*.06),(cx-(r-cx)*.35,t+H_*.05),(cx,t)); return pts
def grad_fill(pts,cols):
    mask=Image.new('L',im.size,0); ImageDraw.Draw(mask).polygon(pts,fill=255)
    g=np.zeros((H,W,3)); tt=np.clip((Y-T0)/sh,0,1)[...,None]
    if len(cols)==2: g=np.array(cols[0])+(np.array(cols[1])-np.array(cols[0]))*tt
    else: g=np.where(tt<.5,np.array(cols[0])+(np.array(cols[1])-np.array(cols[0]))*tt*2,np.array(cols[1])+(np.array(cols[2])-np.array(cols[1]))*(tt-.5)*2)
    gi=Image.fromarray(g.astype(np.uint8)).convert('RGBA'); gi.putalpha(mask); im.alpha_composite(gi)
grad_fill(shield(sw*.03),[LIGHT,DARK]); grad_fill(shield(sw*.085),[LIGHT,BASE,DARK])
def star(d,cx,cy,r,col):
    d.polygon([(cx+(r if i%2==0 else r*.45)*math.cos(-math.pi/2+i*math.pi/5),cy+(r if i%2==0 else r*.45)*math.sin(-math.pi/2+i*math.pi/5)) for i in range(10)],fill=col)
over(lambda d:(d.polygon([(L0+sw*.22,T0+sh*.16),(L0+sw*.72,T0+sh*.15),(L0+sw*.22,T0+sh*.36)],fill=(255,255,255,80)),star(d,c[0],T0+sh*.47+5,sw*.2,DARK+(140,)),star(d,c[0],T0+sh*.47,sw*.2,(255,255,255,235)),star(d,c[0],T0+sh*.72,sw*.05,(255,255,255,240))))
d=ImageDraw.Draw(im)
def centered(text,font,y,fill,shadow=None):
    w=d.textlength(text,font=font)
    if shadow: d.text((W/2-w/2,y+10),text,font=font,fill=shadow)
    d.text((W/2-w/2,y),text,font=font,fill=fill); b=d.textbbox((0,y),text,font=font); return b[3]
centered('NeuroVida',ImageFont.truetype(FB,64),130,(255,255,255))
over(lambda dd: star(dd,W/2+190,170,22,SUN+(255,)))
d=ImageDraw.Draw(im)
y=centered(HEAD,ImageFont.truetype(FB,116),1070,LIGHT,INK)
y=centered(SUB,ImageFont.truetype(FS,54),y+30,(255,255,255))
top_=y+110
for i,(n,l) in enumerate([(str(RATING),'trofeos'),(str(STREAK),'días de racha')]):
    cx=W/4+i*W/2; fn=ImageFont.truetype(FB,92); fl=ImageFont.truetype(FB.replace('Bold','SemiBold'),38)
    d.text((cx-d.textlength(n,font=fn)/2,top_),n,font=fn,fill=SUN); d.text((cx-d.textlength(l,font=fl)/2,top_+112),l,font=fl,fill=DIM)
d.ellipse([W/2-7,top_+63,W/2+7,top_+77],fill=DIM)
centered('Juega. Entrena. Sube de liga.',ImageFont.truetype(FS,40),H-170,(255,255,255,180))
out=ROOT+'/docs/previews/tarjeta-compartir.png'
im.convert('RGB').resize((W//2,H//2),Image.LANCZOS).save(out); print(out)
