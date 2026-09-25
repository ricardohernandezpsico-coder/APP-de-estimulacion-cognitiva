import os
ROOT=os.path.abspath(os.path.join(os.path.dirname(__file__),'..','..'))
import numpy as np, math, random
from PIL import Image, ImageDraw, ImageFont, ImageFilter
W,H=1080,2160
FB=''+ROOT+'/unity/NeuroVidaCore/Assets/Resources/Fonts/Fredoka-Bold.ttf'
INK=(26,18,64); CREAM=(255,255,255)
SKY=(76,201,240); GRAPE=(184,164,255); CORAL=(255,107,74); SUN=(255,201,60)
def night():
    t=np.linspace(1,0,H)[:,None,None]
    b=np.array([4,6,28.]); m=np.array([8,14,58.]); tp=np.array([16,26,88.])
    col=np.where(t<0.5,b+(m-b)*(t*2),m+(tp-m)*((t-0.5)*2))
    return np.repeat(col,W,axis=1)
def glow(img,cx,cy,size,rgb,alpha):
    y,x=np.mgrid[0:H,0:W]; d=np.sqrt((x-cx)**2+(y-cy)**2)/(size/2); a=np.clip(1-d,0,1)**2*alpha
    img[:]=img*(1-a[...,None])+np.array(rgb)*a[...,None]
def frame(label,col,step,warp,fill,path):
    img=night()
    glow(img,0.9*W,0.1*H,1950,SKY,0.27); glow(img,0.1*W,0.55*H,1730,GRAPE,0.19); glow(img,0.05*W,0.95*H,1840,CORAL,0.24)
    cx,cy=W/2,H*0.48
    glow(img,cx,cy,1150,col,0.38)
    pil=Image.fromarray(img.clip(0,255).astype(np.uint8)).convert('RGBA'); d=ImageDraw.Draw(pil,'RGBA')
    rnd=random.Random(11); vp=(W*0.5,H*(1-0.52)); reach=math.hypot(W,H)*0.6
    for i in range(90):
        ang=rnd.random()*2*math.pi; dist=0.05+rnd.random()*1.1; s=10*(0.45+rnd.random()*0.75)*(0.35+dist*1.3)*1.0
        px=vp[0]+math.cos(ang)*dist*reach; py=vp[1]-math.sin(ang)*dist*reach
        streak=s*warp*(4+dist*26); a=int(255*(0.45+rnd.random()*0.55))
        c=(255,195,138,a) if i%6==0 else (255,255,255,a)
        ex=px-math.cos(ang)*streak; ey=py+math.sin(ang)*streak
        d.line([(ex,ey),(px,py)],fill=c,width=max(2,int(s)))
        d.ellipse([px-s/2,py-s/2,px+s/2,py+s/2],fill=c)
    ring=750; r_out=ring*0.48; r_in=ring*0.39; rm=(r_out+r_in)/2; th=r_out-r_in
    ov=Image.new('RGBA',pil.size,(0,0,0,0)); od=ImageDraw.Draw(ov); od.ellipse([cx-rm,cy-rm,cx+rm,cy+rm],outline=(255,255,255,36),width=int(th)); pil.alpha_composite(ov); d=ImageDraw.Draw(pil,'RGBA')
    d.arc([cx-rm,cy-rm,cx+rm,cy+rm],start=-90,end=-90+360*fill,fill=col+(255,),width=int(th))
    a=fill*2*math.pi; sx=cx+math.sin(a)*rm*0.998; sy=cy-math.cos(a)*rm
    def sparkle(x,y,size,c):
        for k,(w,h) in enumerate([(size,size*0.12),(size*0.12,size)]):
            d.polygon([(x-w/2,y),(x,y-h/2),(x+w/2,y),(x,y+h/2)],fill=c)
        d.ellipse([x-size*0.12,y-size*0.12,x+size*0.12,y+size*0.12],fill=(255,255,255,255))
    sparkle(sx,sy,132,(255,255,255,235))
    rr=random.Random(step)
    for k in range(6):
        an=rr.random()*2*math.pi; di=rm*(0.62+rr.random()*0.6); sz=(34+rr.random()*36)*3
        sparkle(cx+math.cos(an)*di,cy-math.sin(an)*di,sz*0.8,(255,255,255,200) if k%2==0 else col+(220,))
    f=ImageFont.truetype(FB,420)
    tw=d.textlength(label,font=f); tx=cx-tw/2; ty=cy-420*0.62
    for dx,dy in [(0,21)]: d.text((tx+dx,ty+dy),label,font=f,fill=INK,stroke_width=10,stroke_fill=INK)
    d.text((tx,ty),label,font=f,fill=col,stroke_width=10,stroke_fill=INK)
    ft=ImageFont.truetype(FB,90); title='Tinta o Palabra'; tw=d.textlength(title,font=ft)
    pw=max(600,tw+168); ph=186; px0=cx-pw/2; py0=cy-786-ph/2
    d.rounded_rectangle([px0,py0+15,px0+pw,py0+ph+15],radius=64,fill=INK)
    d.rounded_rectangle([px0,py0,px0+pw,py0+ph],radius=64,fill=INK)
    d.rounded_rectangle([px0+9,py0+9,px0+pw-9,py0+ph-9],radius=56,fill=CREAM)
    d.text((cx-tw/2,py0+ph/2-58),title,font=ft,fill=INK)
    fs=ImageFont.truetype(FB,78); sub='Prepárate'; sw=d.textlength(sub,font=fs)
    d.text((cx-sw/2,cy+786-40),sub,font=fs,fill=(234,240,255,230))
    pil.convert('RGB').resize((W//2,H//2),Image.LANCZOS).save(path)
frame('3',SKY,3,0.07,0.8,'/tmp/cd3.png'); frame('2',GRAPE,2,0.15,0.55,'/tmp/cd2.png'); frame('1',CORAL,1,0.27,0.3,'/tmp/cd1.png'); frame('¡Ya!',SUN,0,0.9,0,'/tmp/cdya.png')
ims=[Image.open(p) for p in ['/tmp/cd3.png','/tmp/cd2.png','/tmp/cd1.png','/tmp/cdya.png']]
sheet=Image.new('RGB',(sum(i.width for i in ims)+50,ims[0].height+20),(10,12,30))
x=10
for i in ims: sheet.paste(i,(x,10)); x+=i.width+10
sheet.save(''+ROOT+'/docs/previews/cuenta-regresiva.png'); print(sheet.size)
