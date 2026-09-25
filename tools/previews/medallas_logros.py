# Réplica aproximada (PIL) de ui/components/AchievementMedal.kt: las medallas de logros (arriba conseguidas,
# la última fila bloqueadas), para ver el diseño sin compilar.
import os, math
from PIL import Image, ImageDraw, ImageFont
ROOT=os.path.abspath(os.path.join(os.path.dirname(__file__),'..','..'))
FB=ROOT+'/unity/NeuroVidaCore/Assets/Resources/Fonts/Fredoka-Bold.ttf'
FS=ROOT+'/unity/NeuroVidaCore/Assets/Resources/Fonts/Fredoka-SemiBold.ttf'
INK=(26,18,64); CREAM=(255,251,242); SUN=(255,201,60); CORAL=(255,107,74); SKY=(76,201,240); GRAPE=(184,164,255); LIME=(155,229,100)
TIER={'PLATA':(183,191,204),'ORO':(245,179,1),'PLATINO':(95,211,230)}
A=[('primer_paso','Primer paso','PLAY',None,None),('racha_3','En marcha','FLAME',3,None),('racha_7','Una semana','FLAME',7,None),('racha_14','Dos semanas','FLAME',14,None),
   ('racha_30','Un mes entero','FLAME',30,None),('racha_100','Imparable','FLAME',100,None),('dia_completo','Día completo','CALENDAR',3,None),('explorador','Explorador','COMPASS',9,None),
   ('dominios','Mente completa','HEXAGON',6,None),('brillante','Brillante','STAR',90,None),('perfecto','Perfecto','STAR',100,None),('reto_10','Contrarreloj','BOLT',10,None),
   ('partidas_25','Constante','CHECK',25,None),('partidas_100','Centenario','CHECK',100,None),('liga_plata','Plata','SHIELD',None,'PLATA'),('liga_oro','Oro','SHIELD',None,'ORO'),
   ('liga_platino','Platino','SHIELD',None,'PLATINO'),('general_oro','Oro general','SHIELD',None,'ORO')]
COL={'PLAY':SUN,'CALENDAR':SUN,'FLAME':CORAL,'COMPASS':SKY,'BOLT':SKY,'HEXAGON':LIME,'CHECK':LIME,'STAR':GRAPE}
def lerp(a,b,t): return tuple(int(x+(y-x)*t) for x,y in zip(a,b))
S=300; COLS=4; rows=math.ceil(len(A)/COLS)+1
im=Image.new('RGBA',(S*COLS,int(S*1.35)*rows),(8,14,58,255))
def medal(ox,oy,a,unlocked):
    gid,title,g,num,tier=a; d=ImageDraw.Draw(im,'RGBA'); w=S*0.8; cx,cy=ox+S/2,oy+S*0.5-w*0.03; r=w*0.42; u=(r*(1.3 if (num is not None and unlocked) else 1.45))/2
    col=TIER[tier] if tier else COL[g]
    gy=cy-(r*.1 if (num is not None and unlocked) else 0); P=lambda x,y:(cx+x*u,gy+y*u); sw=int(w*0.03)
    fill=CREAM if unlocked else lerp((8,14,58),(255,255,255),.26); out=INK if unlocked else None
    def shape(pts,f=None):
        f=f or fill; d.polygon(pts,fill=f)
        if out: d.line(pts+[pts[0]],fill=out,width=sw,joint='curve')
    if not unlocked:
        d.ellipse([cx-r,cy-r,cx+r,cy+r],fill=lerp((8,14,58),(255,255,255),.07),outline=lerp((8,14,58),(255,255,255),.22),width=int(w*.02))
    else:
        d.ellipse([cx-r,cy-r+w*.045,cx+r,cy+r+w*.045],fill=INK); d.ellipse([cx-r,cy-r,cx+r,cy+r],fill=col)
        ri=r*.76; d.ellipse([cx-ri,cy-ri,cx+ri,cy+ri],fill=lerp(col,(255,255,255),.3)); d.ellipse([cx-r,cy-r,cx+r,cy+r],outline=INK,width=int(w*.035))
        d.ellipse([cx-r*.52,cy-r*.86,cx+r*.28,cy-r*.6],fill=lerp(lerp(col,(255,255,255),.3),(255,255,255),.38))
    if g=='PLAY': shape([P(-.32,-.5),P(.52,-.02),P(-.32,.46)])
    elif g=='FLAME':
        def fl(k,lift):
            def q(x,y): return (cx+x*1.25*u*k,gy+(y*k+lift)*u)
            def cub(p0,p1,p2,p3): return [tuple((1-t)**3*a_+3*(1-t)**2*t*b+3*(1-t)*t*t*c+t**3*e for a_,b,c,e in zip(p0,p1,p2,p3)) for t in [i/12 for i in range(1,13)]]
            segs=[((.04,-.64),(.16,-.40),(.46,-.22),(.46,.16)),((.46,.16),(.46,.42),(.26,.58),(0,.58)),((0,.58),(-.26,.58),(-.46,.42),(-.46,.16)),
                  ((-.46,.16),(-.46,-.06),(-.30,-.18),(-.22,-.34)),((-.22,-.34),(-.14,-.20),(-.10,-.14),(-.04,-.12)),((-.04,-.12),(.02,-.30),(.02,-.48),(.04,-.64))]
            pts=[q(*segs[0][0])]
            for a_,b_,c_,e_ in segs: pts+=cub(q(*a_),q(*b_),q(*c_),q(*e_))
            return pts
        shape(fl(1,-.02))
        if unlocked: shape(fl(.46,.26),SUN)
    elif g=='CALENDAR':
        d.rounded_rectangle([*P(-.5,-.4),*P(.5,.5)],radius=u*.14,fill=fill,outline=out,width=sw)
        d.rounded_rectangle([*P(-.5,-.4),*P(.5,-.12)],radius=u*.14,fill=CORAL if unlocked else fill,outline=out,width=sw)
        if unlocked: d.line([P(-.2,.15),P(-.02,.32),P(.26,.02)],fill=INK,width=int(sw*1.4),joint='curve')
    elif g=='COMPASS':
        d.ellipse([*P(-.52,-.52),*P(.52,.52)],fill=fill,outline=out,width=sw)
        if unlocked: d.polygon([P(0,-.4),P(.14,0),P(-.14,0)],fill=CORAL); d.polygon([P(0,.4),P(.14,0),P(-.14,0)],fill=lerp(INK,CREAM,.45))
    elif g=='HEXAGON':
        hx=lambda rr:[(cx+math.cos(math.radians(-90+i*60))*rr*u,gy+math.sin(math.radians(-90+i*60))*rr*u) for i in range(6)]
        shape(hx(.56))
        if unlocked: shape(hx(.26),SKY)
    elif g=='STAR': shape([(cx+math.cos(math.radians(-90+i*36))*(.58 if i%2==0 else .26)*u,gy+math.sin(math.radians(-90+i*36))*(.58 if i%2==0 else .26)*u-u*.04) for i in range(10)])
    elif g=='BOLT': shape([P(.1,-.58),P(-.34,.06),P(-.02,.06),P(-.12,.58),P(.34,-.1),P(.02,-.1)])
    elif g=='CHECK':
        if unlocked: d.line([P(-.4,.02),P(-.1,.32),P(.44,-.28)],fill=INK,width=int(u*.34),joint='curve')
        d.line([P(-.4,.02),P(-.1,.32),P(.44,-.28)],fill=fill,width=int(u*.2),joint='curve')
    elif g=='SHIELD':
        if unlocked:
            sw_=u*1.05; sh=sw_*1.12; l,t=cx-sw_/2,gy-sh/2; tc=TIER[tier]
            d.polygon([(cx,t),(l+sw_,t+sh*.12),(l+sw_,t+sh*.5),(cx,t+sh),(l,t+sh*.5),(l,t+sh*.12)],fill=lerp(tc,(255,255,255),.25),outline=lerp(tc,(0,0,0),.4),width=4)
            d.polygon([(cx+math.cos(math.radians(-90+i*36))*(sw_*.2 if i%2==0 else sw_*.09),t+sh*.47+math.sin(math.radians(-90+i*36))*(sw_*.2 if i%2==0 else sw_*.09)) for i in range(10)],fill=(255,255,255,235))
        else: d.ellipse([*P(-.4,-.45),*P(.4,.45)],fill=fill)
    if num is not None and unlocked:
        f=ImageFont.truetype(FB,int(w*.15)); tw=d.textlength(str(num),font=f); pw=tw+w*.12; ph=w*.2; top=cy+r*.62
        d.rounded_rectangle([cx-pw/2,top+w*.02,cx+pw/2,top+ph+w*.02],radius=ph/2,fill=INK)
        d.rounded_rectangle([cx-pw/2,top,cx+pw/2,top+ph],radius=ph/2,fill=SUN,outline=INK,width=int(w*.022))
        d.text((cx,top+ph/2),str(num),font=f,fill=INK,anchor='mm')
    f=ImageFont.truetype(FS,30); d.text((ox+S/2,oy+S*1.02),title,font=f,fill=(234,240,255) if unlocked else (180,191,234,170),anchor='mm')
for i,a in enumerate(A): medal((i%COLS)*S,(i//COLS)*int(S*1.35),a,True)
for j,i in enumerate([2,7,11,15]): medal(j*S,(rows-1)*int(S*1.35),A[i],False)
out=ROOT+'/docs/previews/medallas-logros.png'
im.convert('RGB').resize((im.width//2,im.height//2),Image.LANCZOS).save(out); print(out)
