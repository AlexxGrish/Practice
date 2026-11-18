from docx import Document
from docx.shared import Cm, Pt
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.oxml.ns import qn
from pathlib import Path
import re

ROOT = Path(r"C:\Users\ALX\Desktop\Practice-master\Pracrice")
DOC = ROOT / "Отчет_РОСПИВ.docx"

def strip_cs(src: str) -> str:
    out=[]; i=0; n=len(src); state='normal'
    while i<n:
        c=src[i]
        if state=='normal':
            if c=='/' and i+1<n and src[i+1]=='/':
                i+=2
                while i<n and src[i] not in '\r\n': i+=1
                continue
            if c=='/' and i+1<n and src[i+1]=='*':
                i+=2
                while i+1<n and not (src[i]=='*' and src[i+1]=='/'):
                    if src[i] in '\r\n': out.append(src[i])
                    i+=1
                i+=2
                continue
            if c=='@' and i+1<n and src[i+1]=='"':
                out.append('@"'); i+=2; state='verbatim'; continue
            if c=='"':
                out.append(c); i+=1; state='string'; continue
            if c=="'":
                out.append(c); i+=1; state='char'; continue
            out.append(c); i+=1
        elif state=='string':
            out.append(c); i+=1
            if c=='\\' and i<n:
                out.append(src[i]); i+=1
            elif c=='"':
                state='normal'
        elif state=='verbatim':
            out.append(c); i+=1
            if c=='"':
                if i<n and src[i]=='"':
                    out.append(src[i]); i+=1
                else:
                    state='normal'
        elif state=='char':
            out.append(c); i+=1
            if c=='\\' and i<n:
                out.append(src[i]); i+=1
            elif c=="'":
                state='normal'
    return ''.join(out)

def clean(text: str, suffix: str) -> str:
    if suffix == '.cs':
        text = strip_cs(text)
    elif suffix == '.xaml':
        text = re.sub(r'<!--.*?-->', '', text, flags=re.S)
    # remove CSS comment-only lines inside string literals
    lines=[]
    for line in text.splitlines():
        t=line.strip()
        if t.startswith('/*') and t.endswith('*/'):
            continue
        lines.append(line.rstrip('\r'))
    text='\n'.join(lines)
    # trim excessive empty lines
    text=re.sub(r'\n{3,}', '\n\n', text)
    return text.strip('\n')

files=sorted([p for p in ROOT.rglob('*') if p.is_file() and p.suffix.lower() in ('.cs','.xaml') and not any(x in p.parts for x in ('bin','obj','.vs','ReportAssets'))], key=lambda p: str(p.relative_to(ROOT)).lower())
print('files',len(files),'lines',sum(len(p.read_text(encoding='utf-8',errors='replace').splitlines()) for p in files))

doc=Document(str(DOC))
# Appendix page break + heading
p=doc.add_paragraph()
p.paragraph_format.page_break_before=True
p.alignment=WD_ALIGN_PARAGRAPH.RIGHT
r=p.add_run('ПРИЛОЖЕНИЕ Г'); r.font.name='Times New Roman'; r.font.size=Pt(14); r.bold=False; r.italic=False
p2=doc.add_paragraph(); p2.alignment=WD_ALIGN_PARAGRAPH.CENTER
r=p2.add_run('Листинг исходного кода'); r.font.name='Times New Roman'; r.font.size=Pt(14); r.bold=False; r.italic=False
p3=doc.add_paragraph(); p3.alignment=WD_ALIGN_PARAGRAPH.JUSTIFY
r=p3.add_run('В листинге приведены исходные файлы C# и XAML без комментариев. Служебные каталоги bin, obj и .vs не включены.')
r.font.name='Times New Roman'; r.font.size=Pt(14); r.bold=False; r.italic=False

for idx,path in enumerate(files,1):
    rel=path.relative_to(ROOT)
    text=clean(path.read_text(encoding='utf-8',errors='replace'), path.suffix.lower())
    # file caption, no bold, keep with code
    fp=doc.add_paragraph()
    fp.paragraph_format.space_before=Pt(8); fp.paragraph_format.space_after=Pt(2); fp.paragraph_format.keep_with_next=True
    r=fp.add_run(f"Файл {idx}. {rel}")
    r.font.name='Times New Roman'; r.font.size=Pt(10); r.bold=False; r.italic=False
    for line in text.splitlines():
        cp=doc.add_paragraph()
        cp.paragraph_format.first_line_indent=Cm(0)
        cp.paragraph_format.line_spacing=1.0
        cp.paragraph_format.space_before=Pt(0); cp.paragraph_format.space_after=Pt(0)
        run=cp.add_run(line if line else ' ')
        run.font.name='Consolas'; run.font.size=Pt(8); run.bold=False; run.italic=False
        run._r.set(qn('xml:space'),'preserve')

doc.save(str(DOC))
print('saved')
