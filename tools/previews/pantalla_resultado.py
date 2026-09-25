# Réplica aproximada (PIL) de GameResultScreen.kt para ver el diseño sin compilar. 1 dp = 3 px (teléfono ~xxhdpi).
import os, math, random
import numpy as np
from PIL import Image, ImageDraw, ImageFont
ROOT=os.path.abspath(os.path.join(os.path.dirname(__file__),'..','..'))
FB=ROOT+'/unity/NeuroVidaCore/Assets/Resources/Fonts/Fredoka-Bold.ttf'
FS=ROOT+'/unity/NeuroVidaCore/Assets/Resources/Fonts/Fredoka-SemiBold.ttf'
DP=3; W,H=412*DP,915*DP
INK=(26,18,64); CREAM=(255,255,255); SUN=(255,201,60); CORAL=(255,107,74); SKY=(76,201,240); GRAPE=(184,164,255); LIME=(155,229,100); SOFT=(180,191,234)
ATENCION=(245,158,11)
def f(p,s): return ImageFont.truetype(p,int(s*DP))
t=np.linspace(1,0,H)[:,None,None]; b=np.array([4,6,28.]); m=np.array([8,14,58.]); tp=np.array([16,26,88.])
bg=np.repeat(np.where(t<0.5,b+(m-b)*(t*2),m+(tp-m)*((t-0.5)*2)),W,axis=1)
def glow(cx,cy,r,rgb,a):
    y,x=np.mgrid[0:H,0:W]; d=np.clip(1-np.sqrt((x-cx)**2+(y-cy)**2)/r,0,1)**2*a; bg[:]=bg*(1-d[...,None])+np.array(rgb)*d[...,None]
glow(0.9*W,0.1*H,0.9*W,SKY,0.27); glow(0.1*W,0.55*H,0.8*W,GRAPE,0.19); glow(0.05*W,0.95*H,0.85*W,CORAL,0.24)
im=Image.fromarray(bg.clip(0,255).astype(np.uint8)).convert('RGBA'); d=ImageDraw.Draw(im,'RGBA')
rnd=random.Random(3)
for i in range(110):
    x,y=rnd.random()*W,rnd.random()*H; r=rnd.random()*2.2*DP/2+1; a=int(90+rnd.random()*140)
    d.ellipse([x-r,y-r,x+r,y+r],fill=(255,195,138,a) if i%6==0 else (255,255,255,a))
def ctext(y,s,font,fill): w=d.textlength(s,font=font); d.text((W/2-w/2,y),s,font=font,fill=fill); return w
y=28*DP
ctext(y,'Tinta o Palabra',f(FB,26),CREAM); y+=36*DP
w=d.textlength('Atención',font=f(FS,15)); x0=W/2-(w+14*DP)/2
d.ellipse([x0,y+6*DP,x0+8*DP,y+14*DP],fill=ATENCION); d.text((x0+14*DP,y),'Atención',font=f(FS,15),fill=SOFT); y+=24*DP
box_top=y; box_h=250*DP; c=(W/2,box_top+box_h*0.58)
ov=Image.new('RGBA',im.size,(0,0,0,0)); od=ImageDraw.Draw(ov)
R=min(W-48*DP,box_h)*0.55
for k in range(60,0,-1):
    rr=R*k/60; a=int(255*0.40*(1-k/60)**2); od.ellipse([c[0]-rr,c[1]-rr,c[0]+rr,c[1]+rr],fill=ATENCION+(a,))
im.alpha_composite(ov); d=ImageDraw.Draw(im,'RGBA')
rb=random.Random(7); reach=min(W-48*DP,box_h)*0.62; prog=0.55
for i in range(26):
    ang=rb.random()*2*math.pi; sp=0.45+rb.random()*0.55; dd=reach*sp*(1-(1-prog)**2)
    px,py=c[0]+math.cos(ang)*dd,c[1]+math.sin(ang)*dd; s=(10+rb.random()*16)*(1-prog*0.5)*DP/1.2
    col=[SUN,CORAL,SKY,GRAPE,LIME,CREAM][i%6]+(int(255*(1-prog)),)
    d.polygon([(px,py-s),(px+s*.22,py-s*.22),(px+s,py),(px+s*.22,py+s*.22),(px,py+s),(px-s*.22,py+s*.22),(px-s,py),(px-s*.22,py-s*.22)],fill=col)
def star(cx,cy,size,earned):
    pts=[]
    for i in range(10):
        r=size*(0.48 if i%2==0 else 0.21); a=-math.pi/2+i*math.pi/5; pts.append((cx+r*math.cos(a),cy+r*math.sin(a)))
    if earned:
        d.line(pts+[pts[0]],fill=INK,width=int(size*0.12),joint='curve'); d.polygon(pts,fill=SUN)
        d.ellipse([cx-size*0.1-size*0.07,cy-size*0.14-size*0.07,cx-size*0.1+size*0.07,cy-size*0.14+size*0.07],fill=(255,255,255,140))
    else:
        o=Image.new('RGBA',im.size,(0,0,0,0)); od2=ImageDraw.Draw(o); od2.polygon(pts,fill=(255,255,255,26)); od2.line(pts+[pts[0]],fill=(255,255,255,77),width=int(size*0.05)); im.alpha_composite(o)
sizes=[46,58,46]; gap=10*DP; total=sum(sizes)*DP+2*gap
# columna centrada: estrellas + número + "puntos"
col_h=58*DP+4*DP+128*DP+20*DP; ytop=box_top+box_h/2-col_h/2
x=W/2-total/2
for i,sz in enumerate(sizes):
    s=sz*DP; cy=ytop+58*DP-s/2-(8*DP if i==1 else 0); star(x+s/2,cy,s,i<2); x+=s+gap
fnum=f(FB,104); num='78'; nw=d.textlength(num,font=fnum); ny=ytop+62*DP-18*DP
for dx,dy in [(0,6*DP)]: d.text((W/2-nw/2+dx,ny+dy),num,font=fnum,fill=INK,stroke_width=8,stroke_fill=INK)
d.text((W/2-nw/2,ny),num,font=fnum,fill=SUN,stroke_width=8,stroke_fill=INK)
ctext(ytop+62*DP+122*DP,'puntos',f(FS,15),SOFT)
y=box_top+box_h
ctext(y,'¡Muy bien!',f(FB,28),CREAM); y+=38*DP
ctext(y,'Buen equilibrio entre precisión y velocidad.',f(FS,16) if False else ImageFont.truetype(FS,16*DP),SOFT); y+=22*DP+18*DP
stats=[('10/12','aciertos'),('Nivel 3','Intermedio'),('Reto','modo')]
fv=f(FB,20); fl=ImageFont.truetype(FS,14*DP)
widths=[max(d.textlength(v,font=fv),d.textlength(l,font=fl))+12*DP for v,l in stats]; tw=sum(widths)+2*21*DP; x=W/2-tw/2
for i,(v,l) in enumerate(stats):
    cx=x+widths[i]/2; d.text((cx-d.textlength(v,font=fv)/2,y),v,font=fv,fill=CREAM); d.text((cx-d.textlength(l,font=fl)/2,y+28*DP),l,font=fl,fill=SOFT); x+=widths[i]
    if i<2: d.ellipse([x+8*DP,y+18*DP,x+13*DP,y+23*DP],fill=SOFT+(150,)); x+=21*DP
y+=52*DP+18*DP
# píldora lima
pt='¡Subes al nivel 4!'; fp=f(FB,14); pw=d.textlength(pt,font=fp)+24*DP; ph=26*DP
d.rounded_rectangle([W/2-pw/2,y,W/2+pw/2,y+ph],radius=ph/2,fill=LIME,outline=INK,width=3*DP); d.text((W/2-pw/2+12*DP,y+3*DP),pt,font=fp,fill=INK); y+=ph+18*DP
# liga
sh=40*DP; sx=W/2-110*DP
d.polygon([(sx,y),(sx+sh,y),(sx+sh,y+sh*0.6),(sx+sh/2,y+sh*1.12),(sx,y+sh*0.6)],fill=(205,127,50),outline=(138,75,20),width=3)
d.text((sx+sh+10*DP,y),'Bronce 3',font=f(FB,17),fill=CREAM); d.text((sx+sh+10*DP,y+22*DP),'115 trofeos en este juego',font=ImageFont.truetype(FS,14*DP),fill=SOFT); y+=sh*1.12+20*DP
# sesión de hoy
ctext(y,'Sesión de hoy',ImageFont.truetype(FS,15*DP),SOFT); y+=26*DP
n=3; nd=30*DP; gl=26*DP; tw=n*nd+(n-1)*gl; x=W/2-tw/2
for i in range(n):
    if i>0: d.line([(x-gl,y+nd/2),(x,y+nd/2)],fill=CORAL if i<=2 else (255,255,255,46),width=4*DP)
    if i<2:
        d.ellipse([x,y+2*DP,x+nd,y+nd+2*DP],fill=INK); d.ellipse([x,y,x+nd,y+nd],fill=INK); d.ellipse([x+3*DP,y+3*DP,x+nd-3*DP,y+nd-3*DP],fill=CORAL)
        r=nd/2; d.line([(x+r*0.55,y+r*1.02),(x+r*0.88,y+r*1.34),(x+r*1.45,y+r*0.70)],fill=INK,width=3*DP,joint='curve')
    else: d.ellipse([x+DP,y+DP,x+nd-DP,y+nd-DP],outline=(255,255,255,77),width=2*DP)
    x+=nd+gl
y+=nd+28*DP
def clay_button(y,text,color):
    x0,x1=24*DP,W-24*DP; h=52*DP; dep=5*DP
    d.rounded_rectangle([x0,y+dep,x1,y+h+dep],radius=20*DP,fill=INK); d.rounded_rectangle([x0,y,x1,y+h],radius=20*DP,fill=color,outline=INK,width=3*DP)
    ft=f(FB,17); tw=d.textlength(text,font=ft); d.text((W/2-tw/2,y+h/2-12*DP),text,font=ft,fill=INK)
clay_button(y,'Siguiente juego (3 de 3)',SUN); y+=52*DP+5*DP+14*DP
clay_button(y,'Jugar de nuevo',CREAM)
out=im.convert('RGB').resize((W//2,H//2),Image.LANCZOS)
os.makedirs(ROOT+'/docs/previews',exist_ok=True); out.save(ROOT+'/docs/previews/pantalla-resultado.png'); print(out.size)
