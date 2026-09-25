# Réplica aproximada (PIL) de ui/components/GameIcon.kt: los 9 íconos de juego sobre su planeta, para ver el
# diseño sin compilar. Lienzo de cada ícono = 100x100 unidades, como en el código.
import os, math
from PIL import Image, ImageDraw, ImageFont
ROOT=os.path.abspath(os.path.join(os.path.dirname(__file__),'..','..'))
FB=ROOT+'/unity/NeuroVidaCore/Assets/Resources/Fonts/Fredoka-Bold.ttf'
FS=ROOT+'/unity/NeuroVidaCore/Assets/Resources/Fonts/Fredoka-SemiBold.ttf'
INK=(26,18,64); CREAM=(255,251,242); SUN=(255,201,60); CORAL=(255,107,74); SKY=(76,201,240); GRAPE=(184,164,255); WHITE=(255,255,255)
DOM={'memoria':(59,130,246),'atencion':(245,158,11),'razonamiento':(139,92,246),'lenguaje':(16,185,129),'calculo':(13,148,136),'velocidad':(244,63,94)}
GAMES=[('secuencia','Secuencia Lumínica','memoria'),('parejas','Parejas Ocultas','memoria'),('rutatesoro','Ruta del Tesoro','memoria'),
       ('stroop','Tinta o Palabra','atencion'),('cambiochip','Cambio de Chip','atencion'),('series','Detective de Series','razonamiento'),
       ('anagramas','Anagramas','lenguaje'),('calculo','Cálculo Sereno','calculo'),('comparacion','Comparación','velocidad')]
S=6  # px por unidad dentro del ícono (ícono de 600 px, se reduce al final)

class Icon:
    def __init__(s,img,ox,oy,k): s.im=img; s.d=ImageDraw.Draw(img); s.ox=ox; s.oy=oy; s.k=k; s.rot=[]
    def P(s,x,y):
        for ang,cx,cy in reversed(s.rot):
            a=math.radians(ang); dx,dy=x-cx,y-cy; x,y=cx+dx*math.cos(a)-dy*math.sin(a), cy+dx*math.sin(a)+dy*math.cos(a)
        return (s.ox+x*s.k, s.oy+y*s.k)
    def poly(s,pts,dy=0): return [s.P(x,y+dy) for x,y in pts]
    def clay(s,shapes,fill,gloss=False,border=5,shadow=True):
        if isinstance(shapes[0],tuple): shapes=[shapes]
        if shadow:
            for sh in shapes: s.d.polygon(s.poly(sh,4.5),fill=INK)
        for sh in shapes: s.d.polygon(s.poly(sh),fill=fill)
        for sh in shapes: s.d.line(s.poly(sh)+[s.poly(sh)[0]],fill=INK,width=int(border*s.k),joint='curve')
        if gloss:
            xs=[p[0] for sh in shapes for p in sh]; ys=[p[1] for sh in shapes for p in sh]
            l,t,w,h=min(xs),min(ys),max(xs)-min(xs),max(ys)-min(ys)
            o=Image.new('RGBA',s.im.size,(0,0,0,0)); ImageDraw.Draw(o).ellipse([*s.P(l+w*.18,t+h*.14),*s.P(l+w*.52,t+h*.30)],fill=(255,255,255,140)); s.im.alpha_composite(o); s.d=ImageDraw.Draw(s.im)
    def stroke(s,pts,col,w,dy=0): 
        q=s.poly(pts,dy); s.d.line(q,fill=col,width=int(w*s.k),joint='curve')
        r=w*s.k/2
        for x,y in (q[0],q[-1]): s.d.ellipse([x-r,y-r,x+r,y+r],fill=col)
    def claystroke(s,segs,col,w):
        for seg in segs: s.stroke(seg,INK,w+10,4.5)
        for seg in segs: s.stroke(seg,INK,w+10)
        for seg in segs: s.stroke(seg,col,w)

def rrect(x,y,w,h,r,n=8):
    pts=[]
    for cx,cy,a0 in [(x+w-r,y+r,-90),(x+w-r,y+h-r,0),(x+r,y+h-r,90),(x+r,y+r,180)]:
        for i in range(n+1): a=math.radians(a0+90*i/n); pts.append((cx+r*math.cos(a),cy+r*math.sin(a)))
    return pts
def circ(cx,cy,r,n=48): return [(cx+r*math.cos(2*math.pi*i/n),cy+r*math.sin(2*math.pi*i/n)) for i in range(n)]
def quad(p0,p1,p2,n=10): return [((1-t)**2*p0[0]+2*(1-t)*t*p1[0]+t*t*p2[0],(1-t)**2*p0[1]+2*(1-t)*t*p1[1]+t*t*p2[1]) for t in [i/n for i in range(1,n+1)]]
def cub(p0,p1,p2,p3,n=12): return [tuple((1-t)**3*a+3*(1-t)**2*t*b+3*(1-t)*t*t*c+t**3*d for a,b,c,d in zip(p0,p1,p2,p3)) for t in [i/n for i in range(1,n+1)]]
def star(cx,cy,outer,inner,n,round_=False):
    m=n*2; pts=[(cx+math.cos(math.radians(-90+i*360/m))*(outer if i%2==0 else inner),cy+math.sin(math.radians(-90+i*360/m))*(outer if i%2==0 else inner)) for i in range(m)]
    if not round_: return pts
    mid=lambda a,b:((a[0]+b[0])/2,(a[1]+b[1])/2); cur=mid(pts[-1],pts[0]); out=[cur]
    for i in range(m): nx=mid(pts[i],pts[(i+1)%m]); out+=quad(cur,pts[i],nx); cur=nx
    return out
def drop(cx,cy,r):
    p0=(cx,cy-r*1.9); out=[p0]
    out+=cub(p0,(cx+r*.35,cy-r*1.2),(cx+r,cy-r*.55),(cx+r,cy)); out+=cub((cx+r,cy),(cx+r,cy+r*.56),(cx+r*.56,cy+r),(cx,cy+r))
    out+=cub((cx,cy+r),(cx-r*.56,cy+r),(cx-r,cy+r*.56),(cx-r,cy)); out+=cub((cx-r,cy),(cx-r,cy-r*.55),(cx-r*.35,cy-r*1.2),p0)
    return out
def sparkle(ic,cx,cy,r,col):
    c=(cx,cy); pts=[(cx,cy-r)]+quad((cx,cy-r),c,(cx+r,cy))+quad((cx+r,cy),c,(cx,cy+r))+quad((cx,cy+r),c,(cx-r,cy))+quad((cx-r,cy),c,(cx,cy-r))
    ic.d.polygon(ic.poly(pts),fill=col)
def plus(cx,cy,l,b): return [rrect(cx-l/2,cy-b/2,l,b,b/2),rrect(cx-b/2,cy-l/2,b,l,b/2)]

def draw(ic,gid):
    if gid=='secuencia':
        for i,(x,y) in enumerate([(13,13),(53,13),(13,53),(53,53)]): ic.clay(rrect(x,y,34,34,9),SUN if i==1 else CREAM,gloss=i==1)
        sparkle(ic,84,12,11,WHITE)
    elif gid=='parejas':
        ic.rot.append((-12,36,52)); ic.clay(rrect(14,20,42,60,8),GRAPE); ic.clay([(35,36),(45,50),(35,64),(25,50)],CREAM,border=3.5,shadow=False); ic.rot.pop()
        ic.rot.append((10,64,48)); ic.clay(rrect(42,16,42,60,8),CREAM); ic.clay(star(63,46,15,7,5),CORAL,border=4,shadow=False); ic.rot.pop()
    elif gid=='rutatesoro':
        ic.clay(star(50,53,42,17,5,True),CORAL)
        for i in range(5):
            a=math.radians(-90+i*72); x,y=50+math.cos(a)*20,53+math.sin(a)*20; ic.d.ellipse([*ic.P(x-3.2,y-3.2),*ic.P(x+3.2,y+3.2)],fill=CREAM)
        ic.d.ellipse([*ic.P(46,49),*ic.P(54,57)],fill=CREAM)
    elif gid=='stroop':
        ic.clay(rrect(34,50,54,34,9),CREAM); ic.stroke([(44,62),(78,62)],SKY,7); ic.stroke([(44,73),(66,73)],CORAL,7)
        ic.clay(drop(30,44,20),CORAL,gloss=True)
    elif gid=='cambiochip':
        ic.clay(rrect(24,24,52,52,12),CREAM)
        ic.stroke([(50,67),(50,33)],INK,13); ic.stroke([(39.5,39.5),(50,33),(60.5,39.5)],INK,13)
        ic.stroke([(50,67),(50,33)],CORAL,8); ic.stroke([(39.5,39.5),(50,33),(60.5,39.5)],CORAL,8)
        for st in (200,20):
            arc=[(50+44*math.cos(math.radians(st+70*i/20)),50+44*math.sin(math.radians(st+70*i/20))) for i in range(21)]
            e=math.radians(st+70); tip=(50+44*math.cos(e),50+44*math.sin(e)); t=(-math.sin(e),math.cos(e)); n=(math.cos(e),math.sin(e)); h=9
            head=[(tip[0]-t[0]*h+n[0]*h,tip[1]-t[1]*h+n[1]*h),(tip[0]+t[0]*2,tip[1]+t[1]*2),(tip[0]-t[0]*h-n[0]*h,tip[1]-t[1]*h-n[1]*h)]
            ic.claystroke([arc,head],SKY,6)
    elif gid=='series':
        ic.rot.append((45,40,40)); ic.clay(rrect(64,31.5,34,17,8.5),SUN); ic.rot.pop()
        ic.clay(circ(40,40,31),CREAM,gloss=True)
        for x,y,r,c in [(23,47,5,SKY),(38,43,7,CORAL),(56,37,9.5,SUN)]: ic.clay(circ(x,y,r),c,border=3.5,shadow=False)
    elif gid=='anagramas':
        f=ImageFont.truetype(FB,int(34*ic.k*0.95))
        for rot,x,col,ch in [(-10,12,CREAM,'A'),(9,48,SUN,'Z')]:
            ic.rot.append((rot,x+20,50)); ic.clay(rrect(x,28,40,44,10),col)
            cx,cy=ic.P(x+20,50); ic.d.text((cx,cy),ch,font=f,fill=INK,anchor='mm'); ic.rot.pop()
    elif gid=='calculo':
        b=9; ic.clay(plus(28,28,34,b),CORAL); ic.clay(rrect(56,28-b/2,34,b,b/2),CREAM)
        ic.rot.append((45,28,72)); ic.clay(plus(28,72,34,b),SUN); ic.rot.pop()
        ic.clay(rrect(56,64-b/2,34,b,b/2),CREAM); ic.clay(rrect(56,80-b/2,34,b,b/2),CREAM)
    elif gid=='comparacion':
        ic.clay(circ(22,50,20),CREAM,gloss=True); ic.clay(circ(87,50,10),CREAM)
        ic.claystroke([[(58-7.8,37),(58+7.8,50),(58-7.8,63)]],SUN,7)

CELL=600; W,H=CELL*3,int(CELL*3*1.12)
im=Image.new('RGBA',(W,H),(8,14,58,255)); d=ImageDraw.Draw(im)
for idx,(gid,name,dom) in enumerate(GAMES):
    cx=(idx%3)*CELL+CELL/2; cy=(idx//3)*CELL*1.12+CELL*0.47; R=CELL*0.36
    o=Image.new('RGBA',im.size,(0,0,0,0)); od=ImageDraw.Draw(o)
    for k in range(40,0,-1): rr=R*1.35*k/40; od.ellipse([cx-rr,cy-rr,cx+rr,cy+rr],fill=DOM[dom]+(int(90*(1-k/40)),))
    im.alpha_composite(o); d=ImageDraw.Draw(im)
    d.ellipse([cx-R,cy-R+14,cx+R,cy+R+14],fill=INK); d.ellipse([cx-R,cy-R,cx+R,cy+R],fill=DOM[dom],outline=INK,width=10)
    o=Image.new('RGBA',im.size,(0,0,0,0)); ImageDraw.Draw(o).ellipse([cx-R*.45,cy-R*.86,cx+R*.45,cy-R*.62],fill=(255,255,255,80)); im.alpha_composite(o)
    k=R*1.2/100; ic=Icon(im,cx-50*k,cy-50*k+R*0.04,k); draw(ic,gid)
    d=ImageDraw.Draw(im); f=ImageFont.truetype(FB,44); w=d.textlength(name,font=f); d.text((cx-w/2,cy+R+40),name,font=f,fill=WHITE)
out=ROOT+'/docs/previews/iconos-juegos.png'
im.convert('RGB').resize((W//2,H//2),Image.LANCZOS).save(out); print(out)
