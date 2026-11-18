from PIL import Image, ImageDraw, ImageFont
from pathlib import Path
OUT=Path(r"C:\Users\ALX\Desktop\Practice-master\Pracrice\ReportAssets")
FONT=r"C:\Windows\Fonts\times.ttf"; FONTB=r"C:\Windows\Fonts\timesbd.ttf"
def font(s,b=False): return ImageFont.truetype(FONTB if b else FONT,s)
def ts(d,t,f):
    b=d.textbbox((0,0),t,font=f); return b[2]-b[0],b[3]-b[1]
def cls(d,x,y,w,h,name,attrs,methods,head="#2F4F4F"):
    d.rounded_rectangle([x,y,x+w,y+h],radius=10,fill="#F7F7F7",outline="#222",width=3)
    d.rectangle([x,y,x+w,y+44],fill=head,outline="#222",width=3)
    d.text((x+w/2,y+22),name,font=font(25,True),fill="white",anchor="mm")
    yy=y+54
    for a in attrs: d.text((x+12,yy),a,font=font(19),fill="#111"); yy+=25
    d.line([(x,yy+2),(x+w,yy+2)],fill="#777",width=2); yy+=10
    for m in methods: d.text((x+12,yy),m,font=font(19),fill="#111"); yy+=25
    return (x,y,x+w,y+h)
def arrow(d,pts,label="",dashed=False,label_pos=None):
    if dashed:
        import math
        for i in range(len(pts)-1):
            x1,y1=pts[i]; x2,y2=pts[i+1]; dx=x2-x1; dy=y2-y1; dist=math.hypot(dx,dy)
            for t in range(0,int(dist),22):
                a=t/dist; b=min((t+12)/dist,1)
                d.line([x1+dx*a,y1+dy*a,x1+dx*b,y1+dy*b],fill="#333",width=3)
    else: d.line(pts,fill="#333",width=3)
    import math
    fx,fy=pts[-2]; tx,ty=pts[-1]; ang=math.atan2(ty-fy,tx-fx); L=14; W=8
    p1=(tx-L*math.cos(ang)+W*math.sin(ang),ty-L*math.sin(ang)-W*math.cos(ang))
    p2=(tx-L*math.cos(ang)-W*math.sin(ang),ty-L*math.sin(ang)+W*math.cos(ang))
    d.polygon([(tx,ty),p1,p2],fill="#333")
    if label:
        mx,my=label_pos if label_pos else (sum(p[0] for p in pts)/len(pts),sum(p[1] for p in pts)/len(pts))
        f=font(18,True); b=d.textbbox((0,0),label,font=f)
        d.rectangle([mx-b[2]/2-4,my-b[3]/2-3,mx+b[2]/2+4,my+b[3]/2+4],fill="white")
        d.text((mx,my),label,font=f,fill="#8B0000",anchor="mm")

W,H=2400,1800
img=Image.new("RGB",(W,H),"white"); d=ImageDraw.Draw(img)
d.text((W/2,45),"UML-диаграмма классов программного комплекса",font=font(40,True),fill="#111",anchor="mm")
MW=cls(d,60,120,500,300,"MainWindow",["— _planProducts","— _transportData","— _equipmentResult"],["+ LoadPlanAsync()","+ BtnPlanCalc_Click()","+ CalculateEquipmentView()","+ ExportAsync()"])
CS=cls(d,680,120,620,480,"CalculationService",["+ TransportCostPerKm"],["+ LoadPlanProductsAsync()","+ CalculatePlan()","+ LoadTransportAsync()","+ CalculateTransport()","+ CalculateEquipment()","+ LoadCashFlowAsync()","+ CalculateCash()","+ LoadInvestmentProjectsAsync()","+ CalculateInvestments()"])
RS=cls(d,1420,120,560,280,"ReferenceService",["— _sections"],["+ LoadAsync()","+ BuildFormAsync()","+ SaveAsync()","+ DeleteAsync()"])
RPS=cls(d,1420,520,560,250,"ReportService",["— _reportsRoot"],["+ GenerateAsync()","+ GenerateTableReportAsync()","+ OpenReport()"])
DB=cls(d,680,760,620,300,"AppDbContext",["+ Users","+ Products","+ Stores","+ InvestmentProjects"],["+ OnModelCreating()","+ SaveChangesAsync()"])
MOD=cls(d,60,560,500,460,"Models (пакет)",["Product","Store","User","Warehouse","InvestmentProject","InvestmentCompany"],["+ ToString()"])
# MainWindow -> CalculationService
arrow(d,[(560,270),(680,270)],"использует",label_pos=(610,242))
# MainWindow -> ReportService via top corridor
arrow(d,[(310,120),(310,90),(1700,90),(1700,520)],"использует",True,label_pos=(1100,72))
# CalculationService -> DB
arrow(d,[(990,600),(990,760)],"читает/пишет",label_pos=(1070,680))
# ReferenceService -> DB via right corridor
arrow(d,[(1980,260),(2100,260),(2100,880),(1300,880)],"читает/пишет",True,label_pos=(2200,560))
# DB -> Models
arrow(d,[(680,900),(560,900)],"1..*",label_pos=(620,875))
d.text((40,1735),"Классы соответствуют слоям WPF-приложения: представление, бизнес-логика, доступ к данным, модели",font=font(21),fill="#444")
img.save(OUT/"diagram_class.png")
print("ok")
