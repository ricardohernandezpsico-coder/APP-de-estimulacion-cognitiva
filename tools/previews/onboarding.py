# Réplica aproximada (PIL) de ui/screens/OnboardingScreen.kt: paso 1 (bienvenida, los 9 juegos orbitando) y
# paso 5 (así funciona). Reusa el dibujo de los íconos de tools/previews/iconos_juegos.py. 1 dp = 3 px.
import os, sys, math, random, importlib.util
import numpy as np
from PIL import Image, ImageDraw, ImageFont
HERE=os.path.dirname(__file__); ROOT=os.path.abspath(os.path.join(HERE,'..','..'))
spec=importlib.util.spec_from_file_location('iconos',os.path.join(HERE,'iconos_juegos.py')); ic=importlib.util.module_from_spec(spec); spec.loader.exec_module(ic)
FB=ROOT+'/unity/NeuroVidaCore/Assets/Resources/Fonts/Fredoka-Bold.ttf'; FS=ROOT+'/unity/NeuroVidaCore/Assets/Resources/Fonts/Fredoka-SemiBold.ttf'
DP=3; W,H=412*DP,915*DP; INK=(26,18,64); SUN=(255,201,60); CREAM=(255,255,255); DIM=(180,191,234)
def sky():
    t=np.linspace(1,0,H)[:,None,None]; b=np.array([4,6,28.]); m=np.array([8,14,58.]); tp=np.array([16,26,88.])
    bg=np.repeat(np.where(t<0.5,b+(m-b)*(t*2),m+(tp-m)*((t-0.5)*2)),W,axis=1); Y,X=np.mgrid[0:H,0:W]
    for cx,cy,r,rgb,a in [(0.9*W,0.1*H,0.9*W,(76,201,240),.27),(0.05*W,0.95*H,0.85*W,(255,107,74),.24)]:
        d=np.clip(1-np.sqrt((X-cx)**2+(Y-cy)**2)/r,0,1)**2*a; bg[:]=bg*(1-d[...,None])+np.array(rgb)*d[...,None]
    im=Image.fromarray(bg.clip(0,255).astype(np.uint8)).convert('RGBA'); d=ImageDraw.Draw(im); rnd=random.Random(3)
    for i in range(110):
        x,y=rnd.random()*W,rnd.random()*H; r=rnd.random()*3+1; d.ellipse([x-r,y-r,x+r,y+r],fill=(255,255,255,int(90+rnd.random()*140)))
    return im
def ctext(d,y,s,font,fill,maxw=None):
    lines=[s]
    if maxw:
        lines=[]; cur=''
        for w_ in s.split():
            n=(cur+' '+w_).strip()
            if d.textlength(n,font=font)<=maxw: cur=n
            else: lines.append(cur); cur=w_
        lines.append(cur)
    for ln in lines: d.text((W/2-d.textlength(ln,font=font)/2,y),ln,font=font,fill=fill); y+=font.size*1.35
    return y
def button(d,y,label,fill):
    x0,x1=24*DP,W-24*DP; h=58*DP
    d.rounded_rectangle([x0,y+5*DP,x1,y+h+5*DP],radius=20*DP,fill=INK); d.rounded_rectangle([x0,y,x1,y+h],radius=20*DP,fill=fill,outline=INK,width=3*DP)
    f=ImageFont.truetype(FB,20*DP); d.text((W/2,y+h/2),label,font=f,fill=INK,anchor='mm')
def dots(d,page):
    x=W/2-(22+8*4+6*4)*DP/2; y=30*DP
    for i in range(5):
        w=(22 if i==page else 8)*DP; d.rounded_rectangle([x,y,x+w,y+8*DP],radius=4*DP,fill=SUN if i<=page else (60,68,110)); x+=w+6*DP
# ---- paso 1 ----
im=sky(); d=ImageDraw.Draw(im)
dots(d,0); cx,cy=W/2,300*DP; R=120*DP
d.ellipse([cx-R,cy-R,cx+R,cy+R],outline=(70,80,130),width=2*DP)
o=Image.new('RGBA',im.size,(0,0,0,0)); od=ImageDraw.Draw(o)
for k in range(40,0,-1): rr=66*DP*k/40; od.ellipse([cx-rr,cy-rr,cx+rr,cy+rr],fill=SUN+(int(120*(1-k/40)),))
im.alpha_composite(o); d=ImageDraw.Draw(im); s=33*DP
pts=[(cx,cy-s)]+ic.quad((cx,cy-s),(cx,cy),(cx+s,cy))+ic.quad((cx+s,cy),(cx,cy),(cx,cy+s))+ic.quad((cx,cy+s),(cx,cy),(cx-s,cy))+ic.quad((cx-s,cy),(cx,cy),(cx,cy-s)); d.polygon(pts,fill=SUN)
for i,(gid,name,dom) in enumerate(ic.GAMES):
    a=math.radians(20+i*40); px,py=cx+math.cos(a)*R,cy+math.sin(a)*R; pr=26*DP
    d.ellipse([px-pr,py-pr,px+pr,py+pr],fill=ic.DOM[dom],outline=INK,width=int(2.5*DP))
    k=34*DP/100; icon=ic.Icon(im,px-50*k,py-50*k,k); ic.draw(icon,gid); d=ImageDraw.Draw(im)
y=ctext(d,470*DP,'NeuroVida',ImageFont.truetype(FB,44*DP),CREAM)
ctext(d,y+8*DP,'9 juegos cortos para entrenar memoria, atención, razonamiento, lenguaje, cálculo y velocidad.',ImageFont.truetype(FS,18*DP),DIM,W-56*DP)
button(d,H-110*DP,'Empezar',SUN); p1=im
# ---- paso 5 ----
im=sky(); d=ImageDraw.Draw(im); dots(d,4)
d.ellipse([24*DP,20*DP,68*DP,64*DP],fill=CREAM,outline=INK,width=3*DP); d.text((46*DP,42*DP),'←',font=ImageFont.truetype(FB,22*DP),fill=INK,anchor='mm')
y=ctext(d,170*DP,'Así funciona',ImageFont.truetype(FB,32*DP),CREAM)+26*DP
rows=[('Tu camino de cada día','3 juegos distintos, unos 5 minutos. La dificultad se ajusta a ti: ni muy fácil, ni imposible.','path'),
      ('Sube de liga','Cada partida suma trofeos. De Bronce a Maestro, en cada juego y en general.','shield'),
      ('Racha y logros','Vuelve cada día para mantener tu racha y desbloquear medallas.','medal')]
ft=ImageFont.truetype(FB,19*DP); fx=ImageFont.truetype(FS,15*DP)
for t_,tx,art in rows:
    ax,ay=60*DP,y+30*DP
    if art=='path':
        for (x0,y0,rr,c) in [(-12,-16,9,(155,229,100)),(10,0,12,SUN),(-8,18,7,(80,90,140))]: d.ellipse([ax+x0*DP-rr*DP,ay+y0*DP-rr*DP,ax+x0*DP+rr*DP,ay+y0*DP+rr*DP],fill=c,outline=INK if c!=(80,90,140) else None,width=int(2.5*DP))
    elif art=='shield':
        sw=46*DP; l,t=ax-sw/2,ay-sw*.56; d.polygon([(ax,t),(l+sw,t+sw*.13),(l+sw,t+sw*.56),(ax,t+sw*1.12),(l,t+sw*.56),(l,t+sw*.13)],fill=(205,127,50),outline=(138,75,20),width=6)
    else:
        rr=26*DP; d.ellipse([ax-rr,ay-rr+4*DP,ax+rr,ay+rr+4*DP],fill=INK); d.ellipse([ax-rr,ay-rr,ax+rr,ay+rr],fill=(255,107,74),outline=INK,width=3*DP)
        d.ellipse([ax-rr*.76,ay-rr*.76,ax+rr*.76,ay+rr*.76],fill=(255,151,128)); d.text((ax,ay),'3',font=ImageFont.truetype(FB,20*DP),fill=INK,anchor='mm')
    tx0=110*DP; d.text((tx0,y),t_,font=ft,fill=CREAM); yy=y+26*DP; cur=''
    for w_ in tx.split():
        n=(cur+' '+w_).strip()
        if d.textlength(n,font=fx)<=W-tx0-24*DP: cur=n
        else: d.text((tx0,yy),cur,font=fx,fill=DIM); yy+=20*DP; cur=w_
    d.text((tx0,yy),cur,font=fx,fill=DIM); y=max(yy+20*DP,y+60*DP)+22*DP
button(d,H-150*DP,'Jugar mi primera sesión',SUN)
d.text((W/2,H-62*DP),'Explorar primero',font=ImageFont.truetype(FS,16*DP),fill=(234,240,255),anchor='mm'); p5=im
out=Image.new('RGB',(W*2+40,H),(0,0,0)); out.paste(p1.convert('RGB'),(0,0)); out.paste(p5.convert('RGB'),(W+40,0))
path=ROOT+'/docs/previews/onboarding.png'; out.resize((out.width//3,out.height//3),Image.LANCZOS).save(path); print(path)
