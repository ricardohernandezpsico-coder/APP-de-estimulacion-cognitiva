# Réplica aproximada (PIL) de Games/Shared/GameHud.cs (marcador común, Reto y Precisión) y de
# Games/Shared/FinishCurtain.cs (cierre "¡Listo!"). Unidades del canvas de Unity (1080 de ancho).
import os, math, random
from PIL import Image, ImageDraw, ImageFont
ROOT=os.path.abspath(os.path.join(os.path.dirname(__file__),'..','..'))
FB=ROOT+'/unity/NeuroVidaCore/Assets/Resources/Fonts/Fredoka-Bold.ttf'; FS=ROOT+'/unity/NeuroVidaCore/Assets/Resources/Fonts/Fredoka-SemiBold.ttf'
INK=(26,18,64); SUN=(255,201,60); CORAL=(255,107,74); SKY=(76,201,240); CREAM=(255,255,255); SURF=(27,36,102)
W=1080; M=48
def night(h):
    im=Image.new('RGB',(W,h)); d=ImageDraw.Draw(im)
    for y in range(h):
        t=y/h; c=[int(a+(b-a)*t) for a,b in zip((16,26,88),(4,6,28))]; d.line([(0,y),(W,y)],fill=tuple(c))
    rnd=random.Random(4)
    for _ in range(40):
        x,y=rnd.random()*W,rnd.random()*h; r=rnd.random()*3+1; d.ellipse([x-r,y-r,x+r,y+r],fill=(200,210,255))
    return im
def clay(d,box,fill,r,border=4,depth=8):
    x0,y0,x1,y1=box
    d.rounded_rectangle([x0,y0+depth,x1,y1+depth],radius=r,fill=INK)
    d.rounded_rectangle(box,radius=r,fill=fill,outline=INK,width=border)
def sparkle(d,cx,cy,s,col):
    pts=[]
    for (a,b) in [((cx,cy-s),(cx+s,cy)),((cx+s,cy),(cx,cy+s)),((cx,cy+s),(cx-s,cy)),((cx-s,cy),(cx,cy-s))]:
        for i in range(1,9):
            t=i/8; pts.append(((1-t)**2*a[0]+2*(1-t)*t*cx+t*t*b[0],(1-t)**2*a[1]+2*(1-t)*t*cy+t*t*b[1]))
    d.polygon(pts,fill=col)
def hud(title,level,info,streak,label):
    im=night(260); d=ImageDraw.Draw(im)
    d.text((M,20),title,font=ImageFont.truetype(FB,70),fill=CREAM)
    x=M
    for txt,col in [(f'Nivel {level}',SKY),(info,SUN)]:
        f=ImageFont.truetype(FB,40); w=d.textlength(txt,font=f)+52
        clay(d,(x,118,x+w,182),SURF,32,3,6); d.text((x+w/2,150),txt,font=f,fill=col,anchor='mm'); x+=w+14
    if streak is not None:
        px1=W-M; px0=px1-220; clay(d,(px0,26,px1,144),SURF,40)
        hot=streak>=3; sparkle(d,px0+52,85,30,CORAL if hot else (90,98,150))
        d.text((px0+92+(220-106)/2,64),str(streak),font=ImageFont.truetype(FB,60),fill=SUN if hot else CREAM,anchor='mm')
        d.text((px0+92+(220-106)/2,118),'racha',font=ImageFont.truetype(FS,28),fill=(200,200,215),anchor='mm')
    d.text((W-M,236),label,font=ImageFont.truetype(FS,26),fill=(150,160,200),anchor='rm')
    return im
a=hud('Tinta o Palabra',4,'1.250 pts',6,'Modo Reto')
b=hud('Detective de Series',2,'3 de 8',1,'Modo Precisión')
# cierre
c=night(900); d=ImageDraw.Draw(c); cx,cy=W/2,420
rnd=random.Random(9)
for _ in range(60):
    ang=rnd.random()*math.tau; r0=rnd.random()*120+40; r1=r0+rnd.random()*260+80
    d.line([(cx+math.cos(ang)*r0,cy+math.sin(ang)*r0),(cx+math.cos(ang)*r1,cy+math.sin(ang)*r1)],fill=(220,228,255),width=3)
for i in range(22):
    ang=i/22*math.tau; r=300+(i%3)*60; sparkle(d,cx+math.cos(ang)*r,cy+math.sin(ang)*r,14+(i%4)*5,SUN)
f=ImageFont.truetype(FB,170)
d.text((cx,cy+16),'¡Listo!',font=f,fill=INK,anchor='mm',stroke_width=8,stroke_fill=INK)
d.text((cx,cy),'¡Listo!',font=f,fill=SUN,anchor='mm',stroke_width=8,stroke_fill=INK)
d.text((cx,cy+170),'Veamos cómo te fue',font=ImageFont.truetype(FB,54),fill=CREAM,anchor='mm')
out=Image.new('RGB',(W,260*2+900+40),(0,0,0)); out.paste(a,(0,0)); out.paste(b,(0,280)); out.paste(c,(0,560))
p=ROOT+'/docs/previews/marcador-y-cierre.png'; out.resize((W//2,out.height//2),Image.LANCZOS).save(p); print(p)
