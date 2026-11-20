from pathlib import Path
from docx import Document
from docx.enum.table import WD_TABLE_ALIGNMENT
from docx.enum.text import WD_ALIGN_PARAGRAPH, WD_LINE_SPACING
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.shared import Cm, Pt, RGBColor

report_path = Path(r"C:\Users\ALX\Desktop\Practice-master\Pracrice\Отчет_РОСПИВ.docx")
tests_dir = Path(r"C:\Users\ALX\Desktop\Practice-master\Pracrice.Tests")
document = Document(report_path)

TNR = "Times New Roman"
CODE = "Consolas"


def set_run_font(run, name=TNR, size=14, bold=False, italic=False):
    run.font.name = name
    run.font.size = Pt(size)
    run.font.bold = bold
    run.font.italic = italic
    run.font.color.rgb = RGBColor(0, 0, 0)
    rpr = run._element.get_or_add_rPr()
    rfonts = rpr.find(qn("w:rFonts"))
    if rfonts is None:
        rfonts = OxmlElement("w:rFonts")
        rpr.insert(0, rfonts)
    for attr in ("w:ascii", "w:hAnsi", "w:cs", "w:eastAsia"):
        rfonts.set(qn(attr), name)


def set_paragraph(paragraph, align=WD_ALIGN_PARAGRAPH.JUSTIFY, first_indent=Cm(1.25),
                  line=1.5, before=0, after=0, page_break=False, keep_next=False):
    paragraph.alignment = align
    fmt = paragraph.paragraph_format
    fmt.first_line_indent = first_indent
    fmt.line_spacing = line
    fmt.space_before = Pt(before)
    fmt.space_after = Pt(after)
    fmt.page_break_before = page_break
    fmt.keep_with_next = keep_next


def add_text(paragraph, text, size=14, name=TNR, align=WD_ALIGN_PARAGRAPH.JUSTIFY,
             first_indent=Cm(1.25), line=1.5, before=0, after=0, keep_next=False):
    paragraph.clear()
    run = paragraph.add_run(text)
    set_run_font(run, name=name, size=size)
    set_paragraph(paragraph, align=align, first_indent=first_indent,
                  line=line, before=before, after=after, keep_next=keep_next)


def set_cell_border(cell, color="33443D", size="4"):
    tc_pr = cell._tc.get_or_add_tcPr()
    borders = tc_pr.first_child_found_in("w:tcBorders")
    if borders is None:
        borders = OxmlElement("w:tcBorders")
        tc_pr.append(borders)
    for edge in ("top", "left", "bottom", "right"):
        tag = "w:{}".format(edge)
        element = borders.find(qn(tag))
        if element is None:
            element = OxmlElement(tag)
            borders.append(element)
        element.set(qn("w:val"), "single")
        element.set(qn("w:sz"), size)
        element.set(qn("w:space"), "0")
        element.set(qn("w:color"), color)


def shade_cell(cell, fill="E8F0E9"):
    tc_pr = cell._tc.get_or_add_tcPr()
    shd = OxmlElement("w:shd")
    shd.set(qn("w:val"), "clear")
    shd.set(qn("w:color"), "auto")
    shd.set(qn("w:fill"), fill)
    tc_pr.append(shd)


heading5 = next(
    p for p in document.paragraphs
    if p.style.name == "Heading 1" and p.text.strip().startswith("5 РАЗРАБОТКА")
)

heading = heading5.insert_paragraph_before("4.3 Автоматизированное тестирование", style="Heading 2")
heading.clear()
run = heading.add_run("4.3 Автоматизированное тестирование")
set_run_font(run)
set_paragraph(heading, align=WD_ALIGN_PARAGRAPH.JUSTIFY, first_indent=Cm(1.25),
              line=1.0, before=6, after=6, keep_next=True)

intro1 = heading5.insert_paragraph_before()
add_text(
    intro1,
    "Для автоматизированной проверки бизнес-логики создан отдельный проект Pracrice.Tests "
    "на платформе xUnit. Он расположен вне каталога WPF-приложения, поэтому тестовые файлы "
    "не попадают в сборку программы. Тесты не подключаются к MySQL: проверяются пароли, "
    "расчётные алгоритмы и обработка граничных значений.",
)
intro2 = heading5.insert_paragraph_before()
add_text(
    intro2,
    "Набор включает 39 тестов, объединённых в семь классов. Состав набора приведён в таблице 3.",
)

caption = heading5.insert_paragraph_before()
add_text(caption, "Таблица 3 - Состав модульных тестов", align=WD_ALIGN_PARAGRAPH.LEFT,
         first_indent=Cm(0), line=1.0, before=6, after=6, keep_next=True)

summary = heading5.insert_paragraph_before()
add_text(
    summary,
    "Все 39 тестов завершились успешно. Проверка выполняется без запуска приложения и без "
    "изменения базы данных, поэтому её можно повторять после каждого изменения расчётной логики.",
)

command = heading5.insert_paragraph_before()
add_text(
    command,
    "Запуск тестов: dotnet test Pracrice.Tests\\Pracrice.Tests.csproj. "
    "Сборка проекта тестов выполняется вместе с основным решением Pracrice.sln.",
    first_indent=Cm(0), line=1.5,
)

rows = [
    ("Класс тестов", "Кол-во", "Что проверяется"),
    ("PasswordServiceTests", "7", "BCrypt, legacy SHA-256, неверные и пустые пароли"),
    ("PlanCalculationTests", "5", "Симплекс-метод, лимиты ресурсов, исключённые товары"),
    ("TransportCalculationTests", "5", "Транспортная задача, резерв, дефицит, ошибки"),
    ("EquipmentCalculationTests", "4", "Беллман, график замен, прибыль и остаток"),
    ("CashCalculationTests", "4", "Кассиры, загрузка, лимит касс, несколько магазинов"),
    ("InvestmentCalculationTests", "4", "Портфель, бюджет, исключённые проекты"),
    ("RegressionCalculationTests", "10", "Границы значений и дополнительные сценарии"),
]

table = document.add_table(rows=len(rows), cols=3)
table.alignment = WD_TABLE_ALIGNMENT.CENTER
table.autofit = False
widths = [Cm(7.0), Cm(2.2), Cm(7.3)]
for row_index, values in enumerate(rows):
    for column_index, value in enumerate(values):
        cell = table.cell(row_index, column_index)
        cell.width = widths[column_index]
        set_cell_border(cell)
        if row_index == 0:
            shade_cell(cell)
        paragraph = cell.paragraphs[0]
        paragraph.clear()
        run = paragraph.add_run(value)
        set_run_font(run, size=12)
        set_paragraph(paragraph, align=WD_ALIGN_PARAGRAPH.LEFT, first_indent=Cm(0),
                      line=1.0, before=0, after=0)
        paragraph.paragraph_format.space_before = Pt(2)
        paragraph.paragraph_format.space_after = Pt(2)

# Put the table between the caption and the summary paragraph.
summary._p.addprevious(table._tbl)

# The account table in Appendix A becomes table 4.
for paragraph in document.paragraphs:
    if paragraph.text.strip() == "Таблица 3 - Тестовые учётные записи":
        if paragraph.runs:
            paragraph.runs[0].text = "Таблица 4 - Тестовые учётные записи"
            for extra in paragraph.runs[1:]:
                extra.text = ""
        break

# Appendix D: xUnit test listing.
label = document.add_paragraph("ПРИЛОЖЕНИЕ Д")
label.alignment = WD_ALIGN_PARAGRAPH.RIGHT
label.paragraph_format.page_break_before = True
set_paragraph(label, align=WD_ALIGN_PARAGRAPH.RIGHT, first_indent=Cm(0), line=1.0,
              page_break=True)
for run in label.runs:
    set_run_font(run)

title = document.add_paragraph("Листинг юнит-тестов")
set_paragraph(title, align=WD_ALIGN_PARAGRAPH.CENTER, first_indent=Cm(0), line=1.0)
for run in title.runs:
    set_run_font(run)

intro = document.add_paragraph(
    "В листинге приведены файлы xUnit-тестов без комментариев. "
    "Тесты проверяют бизнес-логику приложения без подключения к базе данных."
)
for run in intro.runs:
    set_run_font(run)
set_paragraph(intro, align=WD_ALIGN_PARAGRAPH.JUSTIFY, first_indent=Cm(1.25), line=1.5)

test_files = [
    "PasswordServiceTests.cs",
    "PlanCalculationTests.cs",
    "TransportCalculationTests.cs",
    "EquipmentCalculationTests.cs",
    "CashCalculationTests.cs",
    "InvestmentCalculationTests.cs",
    "RegressionCalculationTests.cs",
]

for file_index, file_name in enumerate(test_files, start=1):
    file_heading = document.add_paragraph(f"Файл {file_index}. {file_name}")
    set_paragraph(file_heading, align=WD_ALIGN_PARAGRAPH.LEFT, first_indent=Cm(0),
                  line=1.0, before=8, after=2, keep_next=True)
    for run in file_heading.runs:
        set_run_font(run, size=10)

    lines = (tests_dir / file_name).read_text(encoding="utf-8").splitlines()
    for line in lines:
        code_paragraph = document.add_paragraph()
        set_paragraph(code_paragraph, align=WD_ALIGN_PARAGRAPH.LEFT, first_indent=Cm(0),
                      line=1.0, before=0, after=0)
        code_paragraph.paragraph_format.widow_control = False
        run = code_paragraph.add_run(line.replace("\t", "    ") if line else "")
        set_run_font(run, name=CODE, size=8)

document.save(report_path)
print("saved", report_path, "paragraphs", len(document.paragraphs), "tables", len(document.tables))
