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

def actor(d,x,y,label,scale=1.0):
    # stick figure
    d.ellipse([x-22*scale,y-105*scale,x+22*scale,y-61*scale],outline="#111",width=4)
    d.line([(x,y-61*scale),(x,y+15*scale)],fill="#111",width=4)
    d.line([(x-55*scale,y-30*scale),(x+55*scale,y-30*scale)],fill="#111",width=4)
    d.line([(x,y+15*scale),(x-45*scale,y+105*scale)],fill="#111",width=4)
    d.line([(x,y+15*scale),(x+45*scale,y+105*scale)],fill="#111",width=4)
    d.text((x,y+130*scale),label,font=font(26,True),fill="#111",anchor="mm")

def ellipse(d,cx,cy,w,h,text,fill="#E8F1E4",outline="#2F4F4F"):
    d.ellipse([cx-w/2,cy-h/2,cx+w/2,cy+h/2],fill=fill,outline=outline,width=4)
    lines=wrap(d,text,font(22),w-40)
    total=len(lines)*27
    yy=cy-total/2+13
    for line in lines:
        d.text((cx,yy),line,font=font(22),fill="#111",anchor="mm"); yy+=27

def line(d,pts,color="#333",width=3,dashed=False):
    if dashed:
        for i in range(len(pts)-1):
            x1,y1=pts[i]; x2,y2=pts[i+1]; dx=x2-x1; dy=y2-y1; dist=math.hypot(dx,dy)
            for t in range(0,int(dist),20):
                a=t/dist; b=min((t+11)/dist,1)
                d.line([x1+dx*a,y1+dy*a,x1+dx*b,y1+dy*b],fill=color,width=width)
    else: d.line(pts,fill=color,width=width,joint="curve")

# ================= USE CASE =================
W,H=2200,1500
img=Image.new("RGB",(W,H),"white"); d=ImageDraw.Draw(img)
d.text((W/2,55),"UML-диаграмма прецедентов информационной системы",font=font(42,True),fill="#111",anchor="mm")
d.rounded_rectangle([470,110,1730,1320],radius=24,outline="#2F4F4F",width=5)
d.text((1100,150),"Система «РОСПИВ»",font=font(30,True),fill="#2F4F4F",anchor="mm")
actor(d,180,430,"Администратор",1.0)
actor(d,180,820,"Руководитель",1.0)
actor(d,180,1180,"Менеджер",1.0)
U={}
U['auth']=(760,300,300,90,"Авторизация")
U['users']=(1430,300,300,90,"Управление пользователями")
U['refs']=(1430,470,300,90,"Ведение справочников")
U['plan']=(760,540,300,90,"План выпуска")
U['transport']=(1430,640,300,90,"План перевозок")
U['equipment']=(760,720,300,90,"Замена оборудования")
U['cash']=(1430,810,300,90,"Планирование касс")
U['invest']=(760,900,300,90,"Распределение инвестиций")
U['reports']=(1430,980,300,90,"Формирование отчётов")
U['export']=(1100,1130,340,90,"Экспорт и печать")
for k,(x,y,w,h,t) in U.items(): ellipse(d,x,y,w,h,t)
# actor links
def link(a,b,color="#333"):
    line(d,[a,b],color=color,width=3)
link((235,430),(610,300)); link((235,430),(1280,300)); link((235,430),(1280,470))
link((235,820),(610,540)); link((235,820),(1280,640)); link((235,820),(610,720)); link((235,820),(1280,810)); link((235,820),(610,900)); link((235,820),(1280,980))
link((235,1180),(610,540)); link((235,1180),(1280,640)); link((235,1180),(1280,810)); link((235,1180),(610,900)); link((235,1180),(1280,980)); link((235,1180),(930,1130))
d.text((40,1440),"Сплошные линии — основные прецеденты; роли разграничивают доступ к разделам",font=font(21),fill="#444")
img.save(OUT/"diagram_usecase.png")

# ================= ACTIVITY =================
W,H=1600,2200
img=Image.new("RGB",(W,H),"white"); d=ImageDraw.Draw(img)
d.text((W/2,55),"UML-диаграмма деятельности: расчёт плана выпуска",font=font(36,True),fill="#111",anchor="mm")
def act(y,text,h=86):
    d.rounded_rectangle([520,y,1080,y+h],radius=24,fill="#E8F1E4",outline="#2F4F4F",width=4)
    lines=wrap(d,text,font(23),500)
    yy=y+h/2-(len(lines)-1)*15
    for ln in lines: d.text((800,yy),ln,font=font(23),fill="#111",anchor="mm"); yy+=30
    return (800,y+h)
def diamond(y,text):
    d.polygon([(800,y-80),(1050,y),(800,y+80),(550,y)],fill="#FFF2CC",outline="#2F4F4F")
    lines=wrap(d,text,font(22),330)
    yy=y-(len(lines)-1)*15
    for ln in lines: d.text((800,yy),ln,font=font(22),fill="#111",anchor="mm"); yy+=28
    return (800,y+80)
def va(y1,y2): d.line([(800,y1),(800,y2)],fill="#333",width=4); d.polygon([(800,y2),(790,y2-14),(810,y2-14)],fill="#333")
def varrow(pts):
    line(d,pts,width=4)
    d.polygon([(pts[-1][0],pts[-1][1]),(pts[-1][0]-14,pts[-1][1]-10),(pts[-1][0]+14,pts[-1][1]-10)],fill="#333")
# start
d.ellipse([770,135,830,195],fill="#2F4F4F"); d.text((800,165),"",anchor="mm")
start=195
va(start,235)
act(235,"Открыть модуль «План выпуска»")
va(321,360)
act(360,"Загрузить товары, рецептуры и остатки")
va(446,485)
act(485,"Ввести лимиты сырья, воды и упаковки")
va(571,610)
diamond(690,"Данные положительные и заполнены?")
# no branch left
line(d,[(550,690),(300,690),(300,980)],width=4); d.polygon([(300,980),(290,966),(310,966)],fill="#333")
act(980,"Показать сообщение об ошибке")  # center; shift? act uses center 800; need custom left
# overwrite left error using custom box
d.rectangle([60,980,500,1066],fill="#F7F7F7",outline="#8B0000",width=4)
for i,ln in enumerate(wrap(d,"Показать сообщение об ошибке",font(21),410)):
    d.text((280,1000+i*25),ln,font=font(21),fill="#111",anchor="mm")
line(d,[(300,1066),(300,1130),(800,1130)],width=4)
# yes branch right
line(d,[(1050,690),(1330,690),(1330,1130),(800,1130)],width=4)
# merge arrow down
va(1130,1190)
act(1190,"Выбрать товары с признаком IsIncluded")
va(1276,1315)
diamond(1395,"Выбраны товары?")
line(d,[(550,1395),(300,1395),(300,1500)],width=4)
d.rectangle([60,1500,500,1586],fill="#F7F7F7",outline="#8B0000",width=4)
d.text((280,1543),"Предупреждение",font=font(21),fill="#111",anchor="mm")
line(d,[(300,1586),(300,1620),(800,1620)],width=4)
line(d,[(1050,1395),(1330,1395),(1330,1620),(800,1620)],width=4)
va(1620,1680)
act(1680,"Симплекс-метод и целочисленное дополнение")
va(1766,1805)
act(1805,"Сформировать план, прибыль и использование ресурсов")
va(1891,1930)
act(1930,"Отобразить результат и сохранить отчёт")
va(2016,2050)
d.ellipse([760,2050,840,2110],outline="#2F4F4F",width=5); d.ellipse([780,2070,820,2090],fill="#2F4F4F")
d.text((40,2155),"Ветви «нет» возвращают пользователя к вводу данных; успешный сценарий завершается выводом плана",font=font(20),fill="#444")
img.save(OUT/"diagram_activity.png")

# ================= SEQUENCE =================
W,H=2200,1450
img=Image.new("RGB",(W,H),"white"); d=ImageDraw.Draw(img)
d.text((W/2,50),"UML-диаграмма последовательности: расчёт плана выпуска",font=font(36,True),fill="#111",anchor="mm")
lifelines=[(220,"Пользователь"),(650,"MainWindow"),(1080,"CalculationService"),(1500,"AppDbContext"),(1880,"MySQL")]
for x,name in lifelines:
    d.rectangle([x-140,130,x+140,180],fill="#2F4F4F")
    d.text((x,155),name,font=font(24,True),fill="white",anchor="mm")
    d.line([(x,180),(x,1320)],fill="#555",width=3)
def msg(y,a,b,text,dashed=False):
    x1=lifelines[a][0]; x2=lifelines[b][0]
    line(d,[(x1,y),(x2,y)],dashed=dashed,width=3)
    # arrow
    direction=1 if x2>x1 else -1
    d.polygon([(x2,y),(x2-16*direction,y-9),(x2-16*direction,y+9)],fill="#333")
    f=font(20); lines=wrap(d,text,f,abs(x2-x1)-50)
    yy=y-13*len(lines)
    for ln in lines:
        d.text(((x1+x2)/2,yy),ln,font=f,fill="#111",anchor="mm"); yy+=24
# activation bars
for x in [650,1080,1500,1880]:
    d.rectangle([x-12,190,x+12,1250],fill="#DDE7DD",outline="#777")
acts={0:1,1:2,2:3,3:4,4:5}
msg(240,0,1,"Нажать «Рассчитать план»")
msg(320,1,2,"LoadPlanProductsAsync()")
msg(400,2,3,"Запрос товаров и рецептур")
msg(480,3,4,"SELECT Products, Product_recipes")
msg(560,4,3,"Набор данных",dashed=True)
msg(640,3,2,"Список товаров",dashed=True)
msg(720,2,1,"PlanProductRow[]",dashed=True)
msg(800,1,1,"Проверка лимитов")
msg(880,1,2,"CalculatePlan(products, limits)")
msg(960,2,2,"Симплекс-метод")
msg(1040,2,1,"PlanCalculationResult",dashed=True)
msg(1120,1,1,"Обновление таблиц и итогов")
msg(1200,1,0,"План выпуска на экране",dashed=True)
d.text((40,1380),"Пунктирные стрелки — возвращаемые значения; вызовы БД инкапсулированы в CalculationService",font=font(20),fill="#444")
img.save(OUT/"diagram_sequence.png")

# ================= CLASS =================
W,H=2400,1800
img=Image.new("RGB",(W,H),"white"); d=ImageDraw.Draw(img)
d.text((W/2,50),"UML-диаграмма классов программного комплекса",font=font(42,True),fill="#111",anchor="mm")
def cls(x,y,w,h,name,attrs,methods,head="#2F4F4F"):
    d.rounded_rectangle([x,y,x+w,y+h],radius=10,fill="#F7F7F7",outline="#222",width=3)
    d.rectangle([x,y,x+w,y+46],fill=head,outline="#222",width=3)
    d.text((x+w/2,y+23),name,font=font(26,True),fill="white",anchor="mm")
    yy=y+56
    for a in attrs:
        d.text((x+12,yy),a,font=font(20),fill="#111"); yy+=27
    d.line([(x,yy+2),(x+w,yy+2)],fill="#777",width=2); yy+=10
    for m in methods:
        d.text((x+12,yy),m,font=font(20),fill="#111"); yy+=27
    return (x,y,x+w,y+h)
M=cls(70,120,620,360,"MainWindow",["— _planProducts","— _transportData","— _equipmentResult"],["+ LoadPlanAsync()","+ BtnPlanCalc_Click()","+ CalculateEquipmentView()","+ ExportAsync()"])
CS=cls(790,120,700,520,"CalculationService",["+ TransportCostPerKm"],["+ LoadPlanProductsAsync()","+ CalculatePlan()","+ LoadTransportAsync()","+ CalculateTransport()","+ CalculateEquipment()","+ LoadCashFlowAsync()","+ CalculateCash()","+ LoadInvestmentProjectsAsync()","+ CalculateInvestments()"])
RS=cls(1600,120,640,300,"ReferenceService",["— _sections"],["+ LoadAsync()","+ BuildFormAsync()","+ SaveAsync()","+ DeleteAsync()"])
RPS=cls(1600,500,640,260,"ReportService",["— _reportsRoot"],["+ GenerateAsync()","+ GenerateTableReportAsync()","+ OpenReport()"])
DB=cls(790,760,700,330,"AppDbContext",["+ Users","+ Products","+ Stores","+ InvestmentProjects"],["+ OnModelCreating()","+ SaveChangesAsync()"])
P=cls(70,600,620,280,"Product / Store",["+ Id, Name, Price","+ Demand, CustomerFlow","+ ServiceRate, RegisterLimit"],["+ ToString()"])
IP=cls(790,1200,700,260,"InvestmentProject",["+ Id, Name","+ CompanyId, Investment","+ AnnualIncome"],["+ ToString()"])
# relationships
def assoc(a,b,label="",dashed=False,color="#333"):
    # connect bottom/top nearest centers; use elbows
    x1=(a[0]+a[2])/2; y1=(a[1]+a[3])/2; x2=(b[0]+b[2])/2; y2=(b[1]+b[3])/2
    line(d,[(x1,y1),(x1,(y1+y2)/2),(x2,(y1+y2)/2),(x2,y2)],dashed=dashed,color=color,width=3)
    if label: d.text(((x1+x2)/2,(y1+y2)/2-14),label,font=font(20,True),fill="#8B0000",anchor="mm")
assoc(M,CS,"использует")
assoc(M,RS,"использует")
assoc(M,RPS,"использует")
assoc(CS,DB,"читает/пишет")
assoc(RS,DB,"читает/пишет")
assoc(DB,P,"1..*")
assoc(DB,IP,"1..*")
assoc(CS,IP,"анализирует")
d.text((40,1730),"Классы соответствуют слоям WPF-приложения: представление, бизнес-логика, доступ к данным, модели",font=font(21),fill="#444")
img.save(OUT/"diagram_class.png")
print("diagrams done")
