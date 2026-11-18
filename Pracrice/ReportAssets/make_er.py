from PIL import Image, ImageDraw, ImageFont
from pathlib import Path
import math
OUT=Path(r"C:\Users\ALX\Desktop\Practice-master\Pracrice\ReportAssets")
FONT=r"C:\Windows\Fonts\times.ttf"; FONTB=r"C:\Windows\Fonts\timesbd.ttf"
def font(s,b=False): return ImageFont.truetype(FONTB if b else FONT,s)
def tsize(d,t,f):
    b=d.textbbox((0,0),t,font=f); return b[2]-b[0],b[3]-b[1]
def box(d,x,y,w,h,title,rows,head="#2F4F4F"):
    d.rounded_rectangle([x,y,x+w,y+h],radius=12,fill="#F7F7F7",outline="#222",width=3)
    hh=46
    d.rounded_rectangle([x,y,x+w,y+hh],radius=12,fill=head,outline="#222",width=3)
    d.rectangle([x,y+hh-12,x+w,y+hh],fill=head)
    d.text((x+w/2,y+hh/2),title,font=font(28,True),fill="white",anchor="mm")
    cy=y+hh+12
    for r in rows:
        d.text((x+14,cy),r,font=font(23),fill="#111"); cy+=33
    return (x,y,x+w,y+h)
def arrow(d,pts,label=None,dashed=False,color="#222",width=4):
    if dashed:
        for i in range(len(pts)-1):
            x1,y1=pts[i]; x2,y2=pts[i+1]; dx=x2-x1; dy=y2-y1; dist=math.hypot(dx,dy)
            if not dist: continue
            for t in range(0,int(dist),22):
                a=t/dist; b=min((t+13)/dist,1)
                d.line([x1+dx*a,y1+dy*a,x1+dx*b,y1+dy*b],fill=color,width=width)
    else:
        d.line(pts,fill=color,width=width,joint="curve")
    fx,fy=pts[-2]; tx,ty=pts[-1]
    ang=math.atan2(ty-fy,tx-fx); L=18; W=9
    p1=(tx-L*math.cos(ang)+W*math.sin(ang),ty-L*math.sin(ang)-W*math.cos(ang))
    p2=(tx-L*math.cos(ang)-W*math.sin(ang),ty-L*math.sin(ang)+W*math.cos(ang))
    d.polygon([(tx,ty),p1,p2],fill=color)
    if label:
        lx,ly=pts[0] if len(pts)==2 else (pts[0][0]+pts[1][0])/2,(pts[0][1]+pts[1][1])/2
        f=font(22,True); b=d.textbbox((0,0),label,font=f)
        d.rectangle([lx-b[2]/2-5,ly-b[3]/2-3,lx+b[2]/2+5,ly+b[3]/2+5],fill="white")
        d.text((lx,ly),label,font=f,fill="#8B0000",anchor="mm")
def tag(d,x,y,label):
    f=font(22,True); b=d.textbbox((0,0),label,font=f)
    d.rectangle([x-b[2]/2-5,y-b[3]/2-3,x+b[2]/2+5,y+b[3]/2+5],fill="white")
    d.text((x,y),label,font=f,fill="#8B0000",anchor="mm")

W,H=3000,1950
img=Image.new("RGB",(W,H),"white"); d=ImageDraw.Draw(img)
d.text((W/2,50),"ER-диаграмма базы данных RosPiv",font=font(46,True),fill="#111",anchor="mm")
E={}
E['Users']=box(d,80,150,600,260,"Users (Пользователи)",["id : INT PK","login : VARCHAR(50) UQ","password_hash : VARCHAR(100)","role : VARCHAR(20)","full_name : VARCHAR(150)"])
E['Products']=box(d,80,560,600,260,"Products (Товары)",["id : INT PK","name : VARCHAR(150)","sku : VARCHAR(50) UQ","price : DECIMAL(15,2)","image_path : VARCHAR(255)"])
E['Resources']=box(d,80,990,600,220,"Resources (Ресурсы)",["id : INT PK","name : VARCHAR(100)","unit : VARCHAR(20)"])
E['Warehouses']=box(d,80,1370,600,220,"Warehouses (Склады)",["id : INT PK","name : VARCHAR(100)","address : VARCHAR(255)"])
E['Product_recipes']=box(d,850,620,560,270,"Product_recipes",["product_id : INT PK,FK","resource_id : INT PK,FK","quantity : DECIMAL(12,3)"],head="#4A5D23")
E['Warehouse_stocks']=box(d,850,1100,560,270,"Warehouse_stocks",["warehouse_id : INT PK,FK","product_id : INT PK,FK","quantity : INT"],head="#4A5D23")
E['Stores']=box(d,1580,150,620,330,"Stores (Магазины)",["id : INT PK","name : VARCHAR(100)","address : VARCHAR(255)","phone : VARCHAR(20)","demand : INT","customer_flow : INT","service_rate : INT","register_limit : INT"])
E['Store_warehouse_distances']=box(d,1580,620,620,250,"Store_warehouse_distances",["store_id : INT PK,FK","warehouse_id : INT PK,FK","distance_km : DECIMAL(8,2)"],head="#4A5D23")
E['Investment_companies']=box(d,1580,1000,620,220,"Investment_companies",["id : INT PK","name : VARCHAR(100)"])
E['Investment_rates']=box(d,1580,1370,620,270,"Investment_rates",["id : INT PK","company_id : INT FK","min_amount : DECIMAL(15,2)","max_amount : DECIMAL(15,2)"])
E['Investment_projects']=box(d,2340,150,580,310,"Investment_projects",["id : INT PK","company_id : INT FK","name : VARCHAR(150)","investment : DECIMAL(15,2)","annual_income : DECIMAL(15,2)"],head="#6B3FA0")

# Products -> recipes
d.line([(680,690),(760,690),(760,720),(850,720)],fill="#222",width=4); arrow(d,[(760,720),(850,720)])
tag(d,735,705,"1:N")
# Resources -> recipes
d.line([(680,1080),(770,1080),(770,810),(850,810)],fill="#222",width=4); arrow(d,[(770,810),(850,810)])
tag(d,750,950,"1:N")
# Products -> warehouse stocks
d.line([(680,640),(790,640),(790,1160),(850,1160)],fill="#222",width=4); arrow(d,[(790,1160),(850,1160)])
tag(d,800,900,"1:N")
# Warehouses -> stocks
d.line([(680,1470),(800,1470),(800,1240),(850,1240)],fill="#222",width=4); arrow(d,[(800,1240),(850,1240)])
tag(d,805,1360,"1:N")
# Stores -> distances (left corridor)
d.line([(1580,700),(1500,700),(1500,800),(1580,800)],fill="#222",width=4); arrow(d,[(1500,800),(1580,800)])
tag(d,1540,750,"N:M")
# distances -> warehouses (bottom corridor)
d.line([(2200,760),(2280,760),(2280,1760),(640,1760),(640,1450)],fill="#222",width=4); arrow(d,[(640,1760),(640,1450)])
tag(d,1500,1760,"N:M")
# companies -> rates
d.line([(1890,1220),(1890,1370)],fill="#222",width=4); arrow(d,[(1890,1300),(1890,1370)])
tag(d,1925,1295,"1:N")
# companies -> projects
arrow(d,[(2200,1100),(2340,300)])
tag(d,2270,760,"1:N")

d.text((30,1865),"Связи: товар—рецептура—ресурс; склад—остаток—товар; магазин—расстояние—склад; компания—ставка—проект",font=font(21),fill="#444")
d.text((W-30,1865),"PK — первичный ключ; FK — внешний ключ; UQ — уникальное поле",font=font(21),fill="#444",anchor="rs")
img.save(OUT/"diagram_er.png")
print("ok")
