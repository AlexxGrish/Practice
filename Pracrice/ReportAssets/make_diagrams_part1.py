from PIL import Image, ImageDraw, ImageFont
from pathlib import Path
import math

OUT = Path(r"C:\Users\ALX\Desktop\Practice-master\Pracrice\ReportAssets")
OUT.mkdir(parents=True, exist_ok=True)
FONT = r"C:\Windows\Fonts\times.ttf"
FONTB = r"C:\Windows\Fonts\timesbd.ttf"
FONTI = r"C:\Windows\Fonts\timesi.ttf"

def font(size, bold=False, italic=False):
    return ImageFont.truetype(FONTB if bold else (FONTI if italic else FONT), size)

def text_size(draw, text, f):
    b = draw.textbbox((0,0), text, font=f)
    return b[2]-b[0], b[3]-b[1]

def wrap(draw, text, f, maxw):
    words = text.split()
    lines=[]; cur=""
    for w in words:
        test=(cur+" "+w).strip()
        if text_size(draw,test,f)[0] <= maxw or not cur:
            cur=test
        else:
            lines.append(cur); cur=w
    if cur: lines.append(cur)
    return lines

def box(draw, x, y, w, h, title, rows, fill="#F7F7F7", header="#2F4F4F", title_size=28, row_size=23):
    r=12
    draw.rounded_rectangle([x,y,x+w,y+h], radius=r, fill=fill, outline="#222222", width=3)
    hh=46
    draw.rounded_rectangle([x,y,x+w,y+hh], radius=r, fill=header, outline="#222222", width=3)
    draw.rectangle([x,y+hh-10,x+w,y+hh], fill=header)
    tf=font(title_size, True)
    draw.text((x+w/2, y+hh/2), title, font=tf, fill="white", anchor="mm")
    rf=font(row_size)
    cy=y+hh+12
    for row in rows:
        draw.text((x+14, cy), row, font=rf, fill="#111111")
        cy += row_size+10
    return (x,y,x+w,y+h)

def arrow(draw, pts, label=None, dashed=False, color="#222222", width=4, both=False):
    if dashed:
        # draw dashed polyline
        for i in range(len(pts)-1):
            x1,y1=pts[i]; x2,y2=pts[i+1]
            dx=x2-x1; dy=y2-y1; dist=math.hypot(dx,dy)
            if dist==0: continue
            step=18
            n=int(dist//step)
            for k in range(n+1):
                t0=k*step/dist; t1=min((k*step+step*0.55)/dist,1)
                draw.line([x1+dx*t0,y1+dy*t0,x1+dx*t1,y1+dy*t1], fill=color, width=width)
    else:
        draw.line(pts, fill=color, width=width, joint="curve")
    def head(p_from,p_to):
        ang=math.atan2(p_to[1]-p_from[1],p_to[0]-p_from[0])
        L=18; W=9
        p1=(p_to[0]-L*math.cos(ang)+W*math.sin(ang), p_to[1]-L*math.sin(ang)-W*math.cos(ang))
        p2=(p_to[0]-L*math.cos(ang)-W*math.sin(ang), p_to[1]-L*math.sin(ang)+W*math.cos(ang))
        draw.polygon([p_to,p1,p2], fill=color)
    head(pts[-2],pts[-1])
    if both:
        head(pts[1],pts[0])
    if label:
        mx=sum(p[0] for p in pts)/len(pts); my=sum(p[1] for p in pts)/len(pts)
        f=font(22, True)
        b=draw.textbbox((0,0),label,font=f)
        draw.rectangle([mx-b[2]/2-5,my-b[3]/2-3,mx+b[2]/2+5,my+b[3]/2+5], fill="white")
        draw.text((mx,my),label,font=f,fill="#8B0000",anchor="mm")

# ---------------- ER ----------------
W,H=3000,1950
img=Image.new("RGB",(W,H),"white"); d=ImageDraw.Draw(img)
d.text((W/2,50),"ER-диаграмма базы данных RosPiv",font=font(46,True),fill="#111111",anchor="mm")

# entities: (x,y,w,title,rows)
E={}
E['Users']=box(d,80,150,620,270,"Users (Пользователи)",["id : INT PK","login : VARCHAR(50) UQ","password_hash : VARCHAR(100)","role : VARCHAR(20)","full_name : VARCHAR(150)"])
E['Products']=box(d,80,560,620,270,"Products (Товары)",["id : INT PK","name : VARCHAR(150)","sku : VARCHAR(50) UQ","price : DECIMAL(15,2)","image_path : VARCHAR(255)"])
E['Resources']=box(d,80,970,620,230,"Resources (Ресурсы)",["id : INT PK","name : VARCHAR(100)","unit : VARCHAR(20)"])
E['Warehouses']=box(d,80,1340,620,230,"Warehouses (Склады)",["id : INT PK","name : VARCHAR(100)","address : VARCHAR(255)"])

E['Product_recipes']=box(d,840,650,560,270,"Product_recipes",["product_id : INT PK,FK","resource_id : INT PK,FK","quantity : DECIMAL(12,3)"], header="#4A5D23")
E['Warehouse_stocks']=box(d,840,1100,560,270,"Warehouse_stocks",["warehouse_id : INT PK,FK","product_id : INT PK,FK","quantity : INT"], header="#4A5D23")

E['Stores']=box(d,1580,150,620,330,"Stores (Магазины)",["id : INT PK","name : VARCHAR(100)","address : VARCHAR(255)","phone : VARCHAR(20)","demand : INT","customer_flow : INT","service_rate : INT","register_limit : INT"])
E['Store_warehouse_distances']=box(d,1580,620,620,250,"Store_warehouse_distances",["store_id : INT PK,FK","warehouse_id : INT PK,FK","distance_km : DECIMAL(8,2)"], header="#4A5D23")
E['Investment_companies']=box(d,1580,1010,620,220,"Investment_companies",["id : INT PK","name : VARCHAR(100)"])
E['Investment_rates']=box(d,1580,1370,620,270,"Investment_rates",["id : INT PK","company_id : INT FK","min_amount : DECIMAL(15,2)","max_amount : DECIMAL(15,2)"])

E['Investment_projects']=box(d,2320,150,610,310,"Investment_projects",["id : INT PK","company_id : INT FK","name : VARCHAR(150)","investment : DECIMAL(15,2)","annual_income : DECIMAL(15,2)"], header="#6B3FA0")

def center(e): x1,y1,x2,y2=E[e]; return (x1+x2)//2,(y1+y2)//2

# Product recipes relationships
arrow(d,[(700,700),(840,760)],"1 : N")
arrow(d,[(700,1080),(840,790)],"1 : N")
# Warehouse stocks
arrow(d,[(700,620),(840,1160)],"1 : N")
arrow(d,[(700,1470),(840,1230)],"1 : N")
# distances
arrow(d,[(1580,720),(700,780)],"N : M", dashed=False)
# company rates/projects
arrow(d,[(1890,1230),(1890,1370)],"1 : N")
arrow(d,[(2200,1110),(2320,300)],"1 : N")
# labels boxes
d.text((W-30,1850),"PK — первичный ключ; FK — внешний ключ; UQ — уникальное поле",font=font(24),fill="#444444",anchor="rs")
d.text((30,1850),"Связи: товар—рецептура—ресурс; склад—остаток—товар; магазин—расстояние—склад; компания—ставка—проект",font=font(22),fill="#444444")
img.save(OUT/"diagram_er.png")

print("ER done")
