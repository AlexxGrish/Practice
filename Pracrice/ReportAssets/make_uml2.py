from PIL import Image, ImageDraw, ImageFont
from pathlib import Path
import math
OUT=Path(r"C:\Users\ALX\Desktop\Practice-master\Pracrice\ReportAssets")
FONT=r"C:\Windows\Fonts\times.ttf"; FONTB=r"C:\Windows\Fonts\timesbd.ttf"; FONTI=r"C:\Windows\Fonts\timesi.ttf"
def font(s,b=False,i=False): return ImageFont.truetype(FONTB if b else (FONTI if i else FONT),s)
def ts(d,t,f):
    b=d.textbbox((0,0),t,font=f); return b[2]-b[0],b[3]-b[1]
def wrap(d,t,f,w):
    out=[]; cur=""
    for word in t.split():
        test=(cur+" "+word).strip()
        if ts(d,test,f)[0]<=w or not cur: cur=test
        else: out.append(cur); cur=word
    if cur: out.append(cur)
    return out
def line(d,pts,color="#333",width=3,dashed=False):
    if dashed:
        for i in range(len(pts)-1):
            x1,y1=pts[i]; x2,y2=pts[i+1]; dx=x2-x1; dy=y2-y1; dist=math.hypot(dx,dy)
            for t in range(0,int(dist),22):
                a=t/dist; b=min((t+12)/dist,1)
                d.line([x1+dx*a,y1+dy*a,x1+dx*b,y1+dy*b],fill=color,width=width)
    else: d.line(pts,fill=color,width=width,joint="curve")
def arrow_head(d,p_from,p_to,color="#333",size=14):
    ang=math.atan2(p_to[1]-p_from[1],p_to[0]-p_from[0]); L=size; W=size*0.55
    p1=(p_to[0]-L*math.cos(ang)+W*math.sin(ang),p_to[1]-L*math.sin(ang)-W*math.cos(ang))
    p2=(p_to[0]-L*math.cos(ang)-W*math.sin(ang),p_to[1]-L*math.sin(ang)+W*math.cos(ang))
    d.polygon([p_to,p1,p2],fill=color)
def arrow(d,pts,color="#333",width=3,dashed=False):
    line(d,pts,color,width,dashed); arrow_head(d,pts[-2],pts[-1],color)
def actor(d,x,y,label,scale=1.0):
    d.ellipse([x-20*scale,y-100*scale,x+20*scale,y-60*scale],outline="#111",width=4)
    d.line([(x,y-60*scale),(x,y+10*scale)],fill="#111",width=4)
    d.line([(x-52*scale,y-28*scale),(x+52*scale,y-28*scale)],fill="#111",width=4)
    d.line([(x,y+10*scale),(x-42*scale,y+100*scale)],fill="#111",width=4)
    d.line([(x,y+10*scale),(x+42*scale,y+100*scale)],fill="#111",width=4)
    d.text((x,y+125*scale),label,font=font(26,True),fill="#111",anchor="mm")
def uc(d,cx,cy,w,h,text):
    d.ellipse([cx-w/2,cy-h/2,cx+w/2,cy+h/2],fill="#E8F1E4",outline="#2F4F4F",width=4)
    lines=wrap(d,text,font(21),w-35); yy=cy-(len(lines)-1)*13
    for ln in lines: d.text((cx,yy),ln,font=font(21),fill="#111",anchor="mm"); yy+=26
def edge(cx,cy,rx,ry,px,py):
    vx=px-cx; vy=py-cy
    t=1.0/math.sqrt((vx/rx)**2+(vy/ry)**2)
    return (cx+vx*t,cy+vy*t)

# ================= USE CASE =================
W,H=2200,1500
img=Image.new("RGB",(W,H),"white"); d=ImageDraw.Draw(img)
d.text((W/2,50),"UML-диаграмма прецедентов информационной системы",font=font(42,True),fill="#111",anchor="mm")
d.rounded_rectangle([470,105,1730,1320],radius=24,outline="#2F4F4F",width=5)
d.text((1100,145),"Система «РОСПИВ»",font=font(30,True),fill="#2F4F4F",anchor="mm")
actor(d,180,430,"Администратор")
actor(d,180,820,"Руководитель")
actor(d,180,1180,"Менеджер")
U={
 'auth':(760,300,300,90,"Авторизация"),
 'users':(1430,300,300,90,"Управление пользователями"),
 'refs':(1430,470,300,90,"Ведение справочников"),
 'plan':(760,540,300,90,"План выпуска"),
 'transport':(1430,640,300,90,"План перевозок"),
 'equipment':(760,720,300,90,"Замена оборудования"),
 'cash':(1430,810,300,90,"Планирование касс"),
 'invest':(760,900,300,90,"Распределение инвестиций"),
 'reports':(1430,980,300,90,"Формирование отчётов"),
 'export':(1100,1140,340,90,"Экспорт и печать")
}
for k,(x,y,w,h,t) in U.items(): uc(d,x,y,w,h,t)
links=[('auth',(235,430)),('users',(235,430)),('refs',(235,430)),
       ('plan',(235,820)),('transport',(235,820)),('equipment',(235,820)),('cash',(235,820)),('invest',(235,820)),('reports',(235,820)),
       ('plan',(235,1180)),('transport',(235,1180)),('cash',(235,1180)),('invest',(235,1180)),('reports',(235,1180)),('export',(235,1180))]
for key,actor_pt in links:
    cx,cy,w,h,t=U[key]
    p=edge(cx,cy,w/2,h/2,*actor_pt)
    line(d,[actor_pt,p],width=3)
d.text((40,1435),"Сплошные линии — основные прецеденты; роли разграничивают доступ к разделам",font=font(21),fill="#444")
img.save(OUT/"diagram_usecase.png")

# ================= ACTIVITY =================
W,H=1600,2250
img=Image.new("RGB",(W,H),"white"); d=ImageDraw.Draw(img)
d.text((W/2,50),"UML-диаграмма деятельности: расчёт плана выпуска",font=font(36,True),fill="#111",anchor="mm")
def act(y,text,fill="#E8F1E4"):
    d.rounded_rectangle([500,y,1100,y+86],radius=24,fill=fill,outline="#2F4F4F",width=4)
    lines=wrap(d,text,font(22),540); yy=y+43-(len(lines)-1)*14
    for ln in lines: d.text((800,yy),ln,font=font(22),fill="#111",anchor="mm"); yy+=28
    return y+86
def decision(y,text):
    d.polygon([(800,y-75),(1055,y),(800,y+75),(545,y)],fill="#FFF2CC",outline="#2F4F4F",width=4)
    lines=wrap(d,text,font(21),320); yy=y-(len(lines)-1)*14
    for ln in lines: d.text((800,yy),ln,font=font(21),fill="#111",anchor="mm"); yy+=27
    return y+75
def down(y1,y2): arrow(d,[(800,y1),(800,y2)])
def error(y,text):
    d.rectangle([80,y,460,y+80],fill="#F7F7F7",outline="#8B0000",width=4)
    lines=wrap(d,text,font(20),360); yy=y+40-(len(lines)-1)*13
    for ln in lines: d.text((270,yy),ln,font=font(20),fill="#111",anchor="mm"); yy+=25
    return y+80
def end(x,y):
    d.ellipse([x-24,y-24,x+24,y+24],outline="#2F4F4F",width=5)
    d.ellipse([x-12,y-12,x+12,y+12],fill="#2F4F4F")
d.ellipse([776,120,824,168],fill="#2F4F4F")
down(168,205)
y=act(205,"Открыть модуль «План выпуска»")
down(y,y+38); y=act(y+38,"Загрузить товары, рецептуры и остатки")
down(y,y+38); y=act(y+38,"Ввести лимиты сырья, воды и упаковки")
down(y,y+38); y=decision(y+38,"Данные положительные и заполнены?")
# no branch
line(d,[(545,y),(300,y),(300,y+100)]); arrow(d,[(300,y+50),(300,y+100)])
err_end=error(y+100,"Показать сообщение об ошибке")
line(d,[(270,err_end),(270,err_end+35)]); arrow(d,[(270,err_end+10),(270,err_end+35)]); end(270,err_end+60)
# yes
line(d,[(1055,y),(1330,y),(1330,y+170),(800,y+170)]); arrow(d,[(1000,y+170),(800,y+170)])
y=act(y+170,"Выбрать товары с признаком IsIncluded")
down(y,y+38); y=decision(y+38,"Выбраны товары?")
line(d,[(545,y),(300,y),(300,y+100)]); arrow(d,[(300,y+50),(300,y+100)])
err2=error(y+100,"Показать предупреждение")
line(d,[(270,err2),(270,err2+35)]); arrow(d,[(270,err2+10),(270,err2+35)]); end(270,err2+60)
line(d,[(1055,y),(1330,y),(1330,y+150),(800,y+150)]); arrow(d,[(1000,y+150),(800,y+150)])
y=act(y+150,"Симплекс-метод и целочисленное дополнение")
down(y,y+38); y=act(y+38,"Сформировать план, прибыль и использование ресурсов")
down(y,y+38); y=act(y+38,"Отобразить результат и сохранить отчёт")
down(y,y+40); end(800,y+64)
d.text((40,2210),"Ветви «нет» завершают сценарий сообщением об ошибке; успешный сценарий — выводом плана",font=font(20),fill="#444")
img.save(OUT/"diagram_activity.png")

# ================= SEQUENCE =================
W,H=2200,1450
img=Image.new("RGB",(W,H),"white"); d=ImageDraw.Draw(img)
d.text((W/2,50),"UML-диаграмма последовательности: расчёт плана выпуска",font=font(36,True),fill="#111",anchor="mm")
ll=[(220,"Пользователь"),(650,"MainWindow"),(1080,"CalculationService"),(1500,"AppDbContext"),(1880,"MySQL")]
for x,name in ll:
    d.rectangle([x-140,130,x+140,180],fill="#2F4F4F")
    d.text((x,155),name,font=font(24,True),fill="white",anchor="mm")
    d.line([(x,180),(x,1320)],fill="#666",width=3)
    d.rectangle([x-8,190,x+8,1280],fill="#E6EFE6",outline="#999")
def msg(y,a,b,text,dashed=False):
    x1=ll[a][0]; x2=ll[b][0]
    line(d,[(x1,y),(x2,y)],dashed=dashed,width=3)
    arrow_head(d,(x1,y),(x2,y))
    f=font(18); lines=wrap(d,text,f,abs(x2-x1)-70)
    yy=y-12*len(lines)-4
    for ln in lines:
        d.text(((x1+x2)/2,yy),ln,font=f,fill="#111",anchor="mm"); yy+=22
def selfmsg(y,x,text):
    line(d,[(x,y),(x+35,y),(x+35,y+30),(x,y+30)],width=3)
    arrow_head(d,(x+35,y+30),(x,y+30))
    d.text((x+45,y+12),text,font=font(18),fill="#111",anchor="lm")
msg(235,0,1,"Нажать «Рассчитать план»")
msg(305,1,2,"LoadPlanProductsAsync()")
msg(375,2,3,"Запрос товаров и рецептур")
msg(445,3,4,"SELECT Products, Product_recipes")
msg(515,4,3,"Набор данных",True)
msg(585,3,2,"Список товаров",True)
msg(655,2,1,"PlanProductRow[]",True)
selfmsg(725,650,"Проверка лимитов")
msg(795,1,2,"CalculatePlan(products, limits)")
selfmsg(865,1080,"Симплекс-метод")
msg(935,2,1,"PlanCalculationResult",True)
selfmsg(1005,650,"Обновление таблиц")
msg(1085,1,0,"План выпуска на экране",True)
d.text((40,1380),"Пунктирные стрелки — возвращаемые значения; вызовы БД инкапсулированы в CalculationService",font=font(20),fill="#444")
img.save(OUT/"diagram_sequence.png")

# ================= CLASS =================
W,H=2400,1800
img=Image.new("RGB",(W,H),"white"); d=ImageDraw.Draw(img)
d.text((W/2,45),"UML-диаграмма классов программного комплекса",font=font(40,True),fill="#111",anchor="mm")
def cls(x,y,w,h,name,attrs,methods,head="#2F4F4F"):
    d.rounded_rectangle([x,y,x+w,y+h],radius=10,fill="#F7F7F7",outline="#222",width=3)
    d.rectangle([x,y,x+w,y+44],fill=head,outline="#222",width=3)
    d.text((x+w/2,y+22),name,font=font(25,True),fill="white",anchor="mm")
    yy=y+54
    for a in attrs: d.text((x+12,yy),a,font=font(19),fill="#111"); yy+=25
    d.line([(x,yy+2),(x+w,yy+2)],fill="#777",width=2); yy+=10
    for m in methods: d.text((x+12,yy),m,font=font(19),fill="#111"); yy+=25
    return (x,y,x+w,y+h)
MW=cls(60,110,500,300,"MainWindow",["— _planProducts","— _transportData","— _equipmentResult"],["+ LoadPlanAsync()","+ BtnPlanCalc_Click()","+ CalculateEquipmentView()","+ ExportAsync()"])
CS=cls(700,110,640,480,"CalculationService",["+ TransportCostPerKm"],["+ LoadPlanProductsAsync()","+ CalculatePlan()","+ LoadTransportAsync()","+ CalculateTransport()","+ CalculateEquipment()","+ LoadCashFlowAsync()","+ CalculateCash()","+ LoadInvestmentProjectsAsync()","+ CalculateInvestments()"])
RS=cls(1480,110,560,280,"ReferenceService",["— _sections"],["+ LoadAsync()","+ BuildFormAsync()","+ SaveAsync()","+ DeleteAsync()"])
RPS=cls(1480,470,560,250,"ReportService",["— _reportsRoot"],["+ GenerateAsync()","+ GenerateTableReportAsync()","+ OpenReport()"])
DB=cls(700,720,640,300,"AppDbContext",["+ Users","+ Products","+ Stores","+ InvestmentProjects"],["+ OnModelCreating()","+ SaveChangesAsync()"])
MOD=cls(60,520,500,450,"Models (пакет)",["Product","Store","User","Warehouse","InvestmentProject","InvestmentCompany"],["+ ToString()"])
def conn(a,b,label,from_side,to_side,dashed=False,color="#333"):
    # simple orthogonal between edges
    x1,y1,x2,y2=a[0],a[1],b[0],b[1]
    if from_side=='r': p1=(a[2],(a[1]+a[3])/2)
    elif from_side=='l': p1=(a[0],(a[1]+a[3])/2)
    elif from_side=='t': p1=((a[0]+a[2])/2,a[1])
    else: p1=((a[0]+a[2])/2,a[3])
    if to_side=='l': p2=(b[0],(b[1]+b[3])/2)
    elif to_side=='r': p2=(b[2],(b[1]+b[3])/2)
    elif to_side=='t': p2=((b[0]+b[2])/2,b[1])
    else: p2=((b[0]+b[2])/2,b[3])
    # elbow
    if abs(p1[0]-p2[0])>abs(p1[1]-p2[1]):
        mid=((p1[0]+p2[0])/2,p1[1]); pts=[p1,mid,(mid[0],p2[1]),p2]
    else:
        mid=(p1[0],(p1[1]+p2[1])/2); pts=[p1,mid,(p2[0],mid[1]),p2]
    line(d,pts,dashed=dashed,width=3)
    arrow_head(d,pts[-2],pts[-1])
    mx=sum(p[0] for p in pts)/len(pts); my=sum(p[1] for p in pts)/len(pts)
    f=font(18,True); b=d.textbbox((0,0),label,font=f)
    d.rectangle([mx-b[2]/2-4,my-b[3]/2-3,mx+b[2]/2+4,my+b[3]/2+4],fill="white")
    d.text((mx,my),label,font=f,fill="#8B0000",anchor="mm")
conn(MW,CS,"использует",'r','l')
conn(MW,RPS,"использует",'b','l',True)
conn(CS,DB,"читает/пишет",'b','t')
conn(RS,DB,"читает/пишет",'b','r',True)
conn(DB,MOD,"1..*",'l','r')
d.text((40,1735),"Классы соответствуют слоям WPF-приложения: представление, бизнес-логика, доступ к данным, модели",font=font(21),fill="#444")
img.save(OUT/"diagram_class.png")
print("ok")
