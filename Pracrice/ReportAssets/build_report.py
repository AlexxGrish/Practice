from docx import Document
from docx.shared import Cm, Pt
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT
from docx.oxml.ns import qn
from docx.oxml import OxmlElement
from pathlib import Path

BASE = Path(r"C:\Users\ALX\Desktop\Practice-master")
DOC = BASE / "Pracrice" / "Отчет_РОСПИВ.docx"
ASSETS = BASE / "Pracrice" / "ReportAssets"
doc = Document(str(DOC))

# Remove old unfinished part from 2.5 to the end
start = next(p for p in doc.paragraphs if p.style.name == "Heading 2" and p.text.strip().startswith("2.5 "))
body = doc.element.body
children = list(body)
for child in children[children.index(start._p):]:
    if child.tag != qn('w:sectPr'):
        body.remove(child)


def font(run, size=14, bold=None, name="Times New Roman"):
    run.font.name = name
    run.font.size = Pt(size)
    if bold is not None:
        run.bold = bold


def p(text, bold_prefix=None, indent=True, align=WD_ALIGN_PARAGRAPH.JUSTIFY):
    par = doc.add_paragraph(style="Normal")
    par.alignment = align
    par.paragraph_format.line_spacing = 1.5
    par.paragraph_format.first_line_indent = Cm(1.25) if indent else Cm(0)
    par.paragraph_format.space_after = Pt(0)
    if bold_prefix and text.startswith(bold_prefix):
        r = par.add_run(bold_prefix)
        font(r, 14, True)
        r = par.add_run(text[len(bold_prefix):])
        font(r, 14)
    else:
        font(par.add_run(text), 14)
    return par


def bullet(text):
    par = doc.add_paragraph(style="List Paragraph")
    par.alignment = WD_ALIGN_PARAGRAPH.JUSTIFY
    par.paragraph_format.line_spacing = 1.5
    par.paragraph_format.first_line_indent = Cm(1.25)
    par.paragraph_format.left_indent = Cm(0)
    par.paragraph_format.space_after = Pt(0)
    font(par.add_run("• " + text), 14)


def h1(text):
    par = doc.add_paragraph(style="Heading 1")
    par.paragraph_format.page_break_before = True
    par.paragraph_format.first_line_indent = Cm(0)
    par.paragraph_format.line_spacing = 1.5
    par.paragraph_format.space_before = Pt(0)
    par.paragraph_format.space_after = Pt(12)
    par.paragraph_format.keep_with_next = True
    font(par.add_run(text), 16, True)


def h2(text):
    par = doc.add_paragraph(style="Heading 2")
    par.paragraph_format.first_line_indent = Cm(0)
    par.paragraph_format.line_spacing = 1.5
    par.paragraph_format.space_before = Pt(12)
    par.paragraph_format.space_after = Pt(6)
    par.paragraph_format.keep_with_next = True
    font(par.add_run(text), 14, True)


def caption(text):
    par = doc.add_paragraph(style="Normal")
    par.alignment = WD_ALIGN_PARAGRAPH.CENTER
    par.paragraph_format.first_line_indent = Cm(0)
    par.paragraph_format.line_spacing = 1.5
    par.paragraph_format.space_before = Pt(6)
    par.paragraph_format.space_after = Pt(6)
    font(par.add_run(text), 14)


def figure(name, width, cap):
    par = doc.add_paragraph(style="Normal")
    par.alignment = WD_ALIGN_PARAGRAPH.CENTER
    par.paragraph_format.first_line_indent = Cm(0)
    par.paragraph_format.line_spacing = 1.0
    par.paragraph_format.space_before = Pt(6)
    par.paragraph_format.space_after = Pt(0)
    par.add_run().add_picture(str(ASSETS / name), width=Cm(width))
    caption(cap)


def shade(cell):
    tc = cell._tc.get_or_add_tcPr()
    shd = OxmlElement('w:shd')
    shd.set(qn('w:val'), 'clear')
    shd.set(qn('w:color'), 'auto')
    shd.set(qn('w:fill'), 'DCE6D8')
    tc.append(shd)


def table(headers, rows, widths=None, cap=None, size=11):
    if cap:
        caption(cap)
    t = doc.add_table(rows=1, cols=len(headers))
    t.alignment = WD_TABLE_ALIGNMENT.CENTER
    tblPr = t._tbl.tblPr
    borders = OxmlElement('w:tblBorders')
    for edge in ('top', 'left', 'bottom', 'right', 'insideH', 'insideV'):
        el = OxmlElement(f'w:{edge}')
        el.set(qn('w:val'), 'single')
        el.set(qn('w:sz'), '4')
        el.set(qn('w:space'), '0')
        el.set(qn('w:color'), '808080')
        borders.append(el)
    tblPr.append(borders)
    for i, text in enumerate(headers):
        cell = t.rows[0].cells[i]
        cell.text = ""
        par = cell.paragraphs[0]
        par.alignment = WD_ALIGN_PARAGRAPH.CENTER
        par.paragraph_format.first_line_indent = Cm(0)
        font(par.add_run(text), size, True)
        shade(cell)
    for row in rows:
        cells = t.add_row().cells
        for i, value in enumerate(row):
            cells[i].text = ""
            par = cells[i].paragraphs[0]
            par.alignment = WD_ALIGN_PARAGRAPH.LEFT
            par.paragraph_format.first_line_indent = Cm(0)
            par.paragraph_format.line_spacing = 1.0
            font(par.add_run(str(value)), size)
    if widths:
        for row in t.rows:
            for i, w in enumerate(widths):
                row.cells[i].width = Cm(w)
    doc.add_paragraph().paragraph_format.space_after = Pt(0)


# Fix old figure captions
old = [
    "Рисунок 1 - IDEF0-диаграмма, уровень A-0",
    "Рисунок 2 - IDEF0-диаграмма, уровень A0",
    "Рисунок 3 - IDEF0-диаграмма, уровень A1",
    "Рисунок 4 - IDEF0-диаграмма, уровень A2",
    "Рисунок 5 - IDEF0-диаграмма, уровень A3",
    "Рисунок 6 - IDEF0-диаграмма, уровень A4",
    "Рисунок 7 - IDEF0-диаграмма, уровень A5",
    "Рисунок 8 - IDEF3-диаграмма, контекстный уровень",
    "Рисунок 9 - IDEF3-диаграмма расчёта плана выпуска",
    "Рисунок 10 - IDEF3-диаграмма расчёта инвестиций",
    "Рисунок 11 - IDEF3-диаграмма расчёта касс",
    "Рисунок 12 - IDEF3-диаграмма расчёта касс, детализация",
    "Рисунок 13 - IDEF3-диаграмма расчёта плана перевозок",
    "Рисунок 14 - DFD-диаграмма, уровень A0",
    "Рисунок 15 - DFD-диаграмма, уровень A12",
    "Рисунок 16 - DFD-диаграмма, уровень A15",
    "Рисунок 17 - DFD-диаграмма, уровень A9",
    "Рисунок 18 - DFD-диаграмма, уровень A23",
]
old_caps = [par for par in doc.paragraphs if par.text.strip().startswith("Рисунок")]
for par, text in zip(old_caps, old):
    for r in list(par.runs):
        r._element.getparent().remove(r._element)
    font(par.add_run(text), 14)

h2("2.5 Диаграмма «сущность — связь»")
p("Физическая модель базы данных RosPiv построена средствами Entity Framework Core и включает "
  "пользователей, товары, ресурсы, рецептуры, склады, магазины, остатки, расстояния, инвестиционные "
  "компании, ставки и проекты. На рисунке 19 приведена ER-диаграмма, соответствующая фактической "
  "структуре базы данных.")
figure("diagram_er.png", 16.2, "Рисунок 19 - ER-диаграмма базы данных RosPiv")
p("Связь товаров и ресурсов реализована через Product_recipes с составным первичным ключом "
  "(product_id, resource_id). Аналогично устроены Warehouse_stocks и Store_warehouse_distances. "
  "Для магазинов хранятся потребность, поток покупателей, скорость обслуживания и лимит касс. "
  "Инвестиционные проекты связаны с компанией через company_id.")
p("Структура соответствует третьей нормальной форме: справочные и операционные данные разделены, "
  "повторяющиеся группы отсутствуют, неключевые атрибуты зависят только от первичного ключа. "
  "Параметры оборудования в базе не хранятся: они вводятся в расчётном модуле и используются только "
  "в модели Беллмана.")

h2("2.6 Объектно-ориентированное моделирование системы")
p("Объектная модель отражает фактическую реализацию WPF-приложения: представление образуют окна и "
  "partial-классы MainWindow, бизнес-логику — CalculationService, ReferenceService и ReportService, "
  "доступ к данным — AppDbContext, предметную область — классы моделей.")

h2("2.6.1 Диаграмма прецедентов")
p("Система поддерживает три роли: администратор управляет пользователями и справочниками, "
  "руководитель и менеджер работают с расчётными модулями и отчётами. Прецедент «Авторизация» "
  "является общим для всех ролей, а управление учётными записями доступно только администратору.")
figure("diagram_usecase.png", 15.4, "Рисунок 20 - UML-диаграмма прецедентов")
p("Расчётные прецеденты включают план выпуска, план перевозок, замену оборудования, планирование "
  "касс и распределение инвестиций. Прецеденты формирования отчётов и экспорта доступны из всех "
  "расчётных модулей; после входа система оставляет доступными только разрешённые роли разделы.")

h2("2.6.2 Диаграмма деятельности")
p("Сценарий расчёта плана выпуска начинается с загрузки товаров и рецептур, затем проверяются "
  "лимиты сырья, воды и упаковки. При некорректных данных выводится сообщение, при корректных — "
  "рассчитывается оптимальный план и формируются показатели использования ресурсов.")
figure("diagram_activity.png", 10.2, "Рисунок 21 - UML-диаграмма деятельности расчёта плана выпуска")
p("В сценарии две точки ветвления: проверка положительности лимитов и проверка наличия хотя бы "
  "одного отмеченного товара. Это соответствует обработчику BtnPlanCalc_Click: перед вызовом "
  "CalculationService.CalculatePlan выполняются проверки, а успешная ветвь завершается выводом "
  "таблицы результата.")

h2("2.6.3 Диаграмма последовательности")
p("При расчёте плана MainWindow вызывает CalculationService, который через AppDbContext читает "
  "товары и рецептуры из MySQL. После возврата строк проверяются лимиты, выполняется расчёт, а "
  "результат передаётся в таблицы окна.")
figure("diagram_sequence.png", 16.2, "Рисунок 22 - UML-диаграмма последовательности расчёта плана выпуска")
p("Вызовы базы данных инкапсулированы в CalculationService и AppDbContext, поэтому окно не работает "
  "с SQL напрямую. Возвращаемые значения показаны пунктирными стрелками; такая схема упрощает "
  "тестирование и замену источника данных.")

h2("2.6.4 Диаграмма классов")
p("MainWindow использует сервисы CalculationService, ReferenceService и ReportService; сервисы "
  "обращаются к AppDbContext; контекст содержит DbSet-свойства моделей Product, Store, "
  "InvestmentProject и других.")
figure("diagram_class.png", 16.2, "Рисунок 23 - UML-диаграмма классов")
p("CalculationService содержит методы LoadPlanProductsAsync, CalculatePlan, LoadTransportAsync, "
  "CalculateTransport, CalculateEquipment, LoadCashFlowAsync, CalculateCash, "
  "LoadInvestmentProjectsAsync и CalculateInvestments. ReferenceService реализует универсальные "
  "операции со справочниками, ReportService формирует HTML-отчёты и открывает их в браузере.")

h2("2.7 Техническое задание")
p("Цель разработки — автоматизировать планирование производства, перевозок, замены оборудования, "
  "расчёта касс и распределения инвестиций, а также ведение справочников и формирование отчётов.")
for text in [
    "авторизация по логину и паролю с хранением BCrypt-хэшей;",
    "роли «Администратор», «Руководитель» и «Менеджер»;",
    "добавление, редактирование и удаление пользователей администратором;",
    "ведение товаров, ресурсов, складов, магазинов, рецептур, остатков, расстояний, компаний, ставок и проектов;",
    "расчёт плана выпуска с учётом лимитов сырья, воды и упаковки;",
    "решение закрытой и открытой транспортной задачи;",
    "расчёт графика замены оборудования методом Беллмана без хранения параметров в БД;",
    "расчёт количества касс по потоку покупателей и скорости обслуживания;",
    "распределение инвестиционного бюджета между отмеченными проектами;",
    "формирование HTML-отчётов, экспорт и печать через браузер."
]:
    bullet(text)
p("Нефункциональные требования: Windows 10/11, .NET 9, WPF, MySQL 5.7, подключение через config.ini, "
  "устойчивость к некорректному вводу, время отклика типового расчёта до 2 секунд, хранение "
  "изображений товаров в папке images. Входные данные — учётные данные, справочники и параметры "
  "расчёта; выходные — таблицы планов, график замен, портфель, HTML-отчёты и сообщения об ошибках.")

h1("3 ПРОЕКТИРОВАНИЕ И РАЗРАБОТКА ПРОГРАММНЫХ ИНТЕРФЕЙСОВ И СТРУКТУР ХРАНЕНИЯ ДАННЫХ")
h2("3.1 Проектирование экранных форм")
p("Интерфейс выполнен в едином фирменном стиле: тёмно-зелёный фон, золотистые акценты, единые "
  "отступы и состояния кнопок. На рисунке 24 показано окно входа; пароль вводится в защищённое поле, "
  "при ошибке выводится сообщение без раскрытия причины.")
figure("01_login.png", 15.4, "Рисунок 24 - Окно авторизации")
p("После входа открывается главное окно с боковой навигацией и строкой состояния. На рисунке 25 "
  "представлен модуль плана выпуска после расчёта: слева нормы расхода и чекбоксы, справа лимиты, "
  "снизу оптимальный план, прибыль и использование ресурсов.")
figure("02_plan.png", 15.4, "Рисунок 25 - Модуль «План выпуска продукции»")
p("Модуль перевозок позволяет выбрать склады и магазины, задать тип задачи и получить матрицу "
  "отгрузок. На рисунке 26 показан открытый вариант: система добавляет резерв, если запас превышает "
  "потребность магазинов.")
figure("03_transport.png", 15.4, "Рисунок 26 - Результат решения транспортной задачи")
p("В модуле замены оборудования пользователь задаёт возраст, горизонт, стоимость замены, доход, "
  "эксплуатационные затраты, рост затрат и срок службы. Справа выводится таблица Беллмана F(k, s), "
  "снизу — оптимальный график замен с прибылью и остаточной стоимостью.")
figure("04_equipment.png", 15.4, "Рисунок 27 - Расчёт замены оборудования методом Беллмана")
p("Модуль планирования касс использует данные магазинов и целевые ограничения по ожиданию и "
  "загрузке. Результат содержит количество касс, загрузку и ожидаемое время обслуживания.")
figure("05_cash.png", 15.4, "Рисунок 28 - Планирование касс")
p("Модуль инвестиций загружает проекты и компании из базы, позволяет включать проекты чекбоксами "
  "и добавлять или изменять их в отдельной форме. Результат содержит рекомендуемый портфель, "
  "суммы вложений и доли проектов.")
figure("06_invest.png", 15.4, "Рисунок 29 - Распределение инвестиций")
p("Административный раздел доступен только администратору: он позволяет добавлять, изменять и "
  "удалять учётные записи и показывает матрицу прав по ролям.")
figure("07_admin.png", 15.4, "Рисунок 30 - Администрирование пользователей")
p("Центр отчётов формирует HTML-документы по товарам, остаткам, рецептурам, ставкам и "
  "пользователям. Отчёт открывается в браузере, печать выполняется через Ctrl+P.")
figure("08_reports.png", 15.4, "Рисунок 31 - Центр отчётов")

h2("3.2 Описание структуры проекта")
p("Проект разделён по слоям: Configuration, Data, Models, Services, Windows, Database, images и "
  "Assets. Главное окно разделено на partial-файлы: навигация, расчёты, товары и пользователи.")
table(
    ["Папка / файл", "Назначение"],
    [
        ["Configuration", "config.ini и строка подключения"],
        ["Data", "AppDbContext и инициализация базы"],
        ["Models", "сущности и строки расчётных таблиц"],
        ["Services", "расчёты, справочники, отчёты, авторизация"],
        ["Windows", "окна редактирования и диалоги"],
        ["Database", "SQL-скрипты структуры и тестовых данных"],
        ["images, Assets", "изображения товаров, логотип, иконка"],
        ["MainWindow.xaml и partial-классы", "главное окно и обработчики модулей"]
    ],
    widths=[5.5, 10.5],
    cap="Таблица 1 - Структура проекта"
)

h2("3.3 Описание классов")
p("CalculationService реализует симплекс-метод, транспортную задачу, динамическое программирование "
  "Беллмана, расчёт СМО и задачу о рюкзаке. Методы загрузки возвращают строки для DataGrid, а "
  "расчётные методы не зависят от окон. ReferenceService предоставляет LoadAsync, BuildFormAsync, "
  "SaveAsync и DeleteAsync для всех справочников.")
p("AppDbContext описывает таблицы, ключи и связи, DatabaseInitializer создаёт базу и проверяет "
  "наличие новых колонок и таблиц. ReportService формирует HTML с логотипом, сохраняет файл в "
  "Reports и открывает его через BrowserLauncher. Пароли хранятся только в виде BCrypt-хэшей.")

h1("4 ТЕСТИРОВАНИЕ ПРОГРАММНЫХ МОДУЛЕЙ")
h2("4.1 Подготовка тест-кейсов")
p("Тестирование проводилось вручную по сценариям авторизации, ролей, справочников, расчётных "
  "модулей и экспорта. Для каждого сценария определялись входные данные, ожидаемый результат и "
  "критерий успешности. Основные тест-кейсы приведены в таблице 2.")
table(
    ["№", "Сценарий", "Ожидаемый результат", "Статус"],
    [
        ["1", "Вход admin / 123456", "Открывается главное окно, доступны все разделы", "Пройден"],
        ["2", "Неверный пароль", "Сообщение об ошибке, вход не выполняется", "Пройден"],
        ["3", "Добавление пользователя", "Запись сохраняется после перезапуска", "Пройден"],
        ["4", "Роль «Менеджер»", "Скрыто администрирование", "Пройден"],
        ["5", "Расчёт плана выпуска", "План, прибыль и проценты ресурсов", "Пройден"],
        ["6", "Транспортная задача", "Матрица отгрузок, маршруты и резерв", "Пройден"],
        ["7", "Замена оборудования", "Таблица Беллмана и график замен", "Пройден"],
        ["8", "Планирование касс", "Количество касс, загрузка и ожидание", "Пройден"],
        ["9", "Инвестиции", "Портфель, суммы и доли проектов", "Пройден"],
        ["10", "Экспорт HTML", "Файл создаётся и открывается в браузере", "Пройден"]
    ],
    widths=[1.0, 4.2, 8.4, 2.4],
    cap="Таблица 2 - Основные тест-кейсы",
    size=10
)

h2("4.2 Выполнение отладки и тестирования")
p("Проверялись загрузка данных из MySQL, передача строк в окно, обработка пустых выборок, "
  "сохранение справочников и повторное открытие отчётов. Особое внимание уделялось отрицательным "
  "числам, пустым полям, отсутствию выбранных строк и нехватке товара на складах.")
p("Исправлены ошибки отображения имени оборудования, сохранения параметров касс, конфликта "
  "контекста базы, формирования отчёта без изображения, смещения заголовков транспортной таблицы "
  "и открытия отчёта в фоновом режиме. Сборка Release проходит без ошибок и предупреждений.")

h1("5 РАЗРАБОТКА ЭКСПЛУАТАЦИОННОЙ ДОКУМЕНТАЦИИ")
h2("5.1 Руководство пользователя")
p("Для запуска необходимо запустить MySQL, открыть Pracrice.exe и ввести логин и пароль. Тестовые "
  "учётные записи приведены в приложении А; для всех установлен пароль 123456.")
for text in [
    "Выберите модуль в боковой панели или в меню «Вычисления».",
    "Заполните исходные данные и при необходимости снимите чекбоксы с записей, не участвующих в расчёте.",
    "Нажмите кнопку расчёта; ошибки ввода выводятся отдельным диалогом.",
    "Для сохранения результата нажмите «Экспорт»; HTML-отчёт откроется в браузере, печать — Ctrl+P.",
    "Справочники открываются из раздела «Таблицы справочников»; добавление и редактирование выполняются в отдельных окнах.",
    "Администратор управляет пользователями в разделе «Администрирование»; свою учётную запись удалить нельзя."
]:
    bullet(text)

h2("5.2 Руководство системного программиста")
p("Для сборки требуются .NET 9 SDK и MySQL 5.7. Параметры подключения задаются в config.ini. При "
  "первом запуске приложение создаёт базу RosPiv, таблицы и администратора. Для ручного развёртывания "
  "используется Database/rospiv_schema.sql, для тестового наполнения — Database/test_data.sql.")
p("Сборка выполняется командой dotnet build Pracrice.csproj -c Release; результат находится в "
  "bin/Release/net9.0-windows. Папка images должна находиться рядом с исполняемым файлом, отчёты "
  "сохраняются в Reports, ошибки — в Logs.")
p("При ошибке подключения проверьте, запущен ли MySQL, совпадают ли порт и имя базы в config.ini. "
  "Если изображение не найдено, показывается фирменная заглушка; если отчёт не открылся, файл можно "
  "открыть вручную из папки Reports.")

h1("ЗАКЛЮЧЕНИЕ")
p("Разработана информационная система ООО «РОСПИВ», объединяющая справочники, пять расчётных "
  "модулей и формирование отчётов. Система реализована на WPF и Entity Framework Core, использует "
  "MySQL и единое фирменное оформление интерфейса.")
p("Реализованы алгоритмы плана выпуска, транспортной задачи, замены оборудования методом Беллмана, "
  "расчёта касс и распределения инвестиций. Для оборудования рассчитывается таблица F(k, s), "
  "определяются рекомендуемые годы замены, прибыль, затраты и остаточная стоимость.")
p("Система разграничивает роли, хранит пароли в виде BCrypt-хэшей, поддерживает справочники, "
  "экспорт HTML и печать через браузер. Тестирование подтвердило работоспособность основных "
  "сценариев; дальнейшее развитие может включать прогнозирование спроса и экспорт в Excel.")

h1("СПИСОК ИСПОЛЬЗОВАННЫХ ИСТОЧНИКОВ")
sources = [
    "ГОСТ 19.201-78. Единая система программной документации. Техническое задание.",
    "ГОСТ 34.601-90. Информационная технология. Автоматизированные системы. Стадии создания.",
    "Microsoft. Документация по .NET 9. https://learn.microsoft.com/dotnet/",
    "Microsoft. Windows Presentation Foundation. https://learn.microsoft.com/dotnet/desktop/wpf/",
    "Microsoft. Entity Framework Core. https://learn.microsoft.com/ef/core/",
    "Oracle. MySQL 5.7 Reference Manual. https://dev.mysql.com/doc/refman/5.7/en/",
    "Pomelo Foundation. Pomelo.EntityFrameworkCore.MySql. https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql",
    "BCrypt.Net-Next. https://github.com/BcryptNet/bcrypt.net",
    "Беллман Р. Динамическое программирование. — М.: Иностранная литература, 1960.",
    "Вентцель Е. С. Исследование операций. — М.: Наука, 1988.",
    "Новиков Ф. А. Дискретная математика для программистов. — СПб.: Питер, 2016."
]
for i, text in enumerate(sources, 1):
    par = doc.add_paragraph(style="Normal")
    par.alignment = WD_ALIGN_PARAGRAPH.JUSTIFY
    par.paragraph_format.line_spacing = 1.5
    par.paragraph_format.first_line_indent = Cm(1.25)
    par.paragraph_format.space_after = Pt(0)
    font(par.add_run(f"{i}. {text}"), 14)

h1("ПРИЛОЖЕНИЯ")
p("Приложение А. Тестовые учётные записи", bold_prefix="Приложение А.")
table(
    ["Логин", "Пароль", "Роль", "ФИО"],
    [
        ["admin", "123456", "Администратор", "Иванов Иван Иванович"],
        ["director", "123456", "Руководитель", "Смирнов Алексей Владимирович"],
        ["manager", "123456", "Менеджер", "Петрова Анна Сергеевна"],
        ["manager2", "123456", "Менеджер", "Соколов Дмитрий Андреевич"],
        ["manager3", "123456", "Менеджер", "Кузнецова Мария Викторовна"],
        ["storekeeper", "123456", "Менеджер", "Волков Павел Дмитриевич"]
    ],
    widths=[3.5, 3.0, 4.0, 5.5],
    cap="Таблица 3 - Тестовые учётные записи",
    size=10
)
p("Приложение Б. Пример файла config.ini", bold_prefix="Приложение Б.")
for line in ["[Database]", "Host=127.0.0.1", "Port=3306", "User=root", "Password=", "DB=RosPiv"]:
    par = doc.add_paragraph(style="Normal")
    par.paragraph_format.first_line_indent = Cm(0)
    par.paragraph_format.left_indent = Cm(1.0)
    par.paragraph_format.line_spacing = 1.0
    par.paragraph_format.space_after = Pt(0)
    r = par.add_run(line)
    font(r, 11)
    r.font.name = "Consolas"
p("Приложение В. Основные таблицы базы данных: Users, Products, Resources, Product_recipes, "
  "Warehouses, Warehouse_stocks, Stores, Store_warehouse_distances, Investment_companies, "
  "Investment_rates, Investment_projects.", bold_prefix="Приложение В.")

# Global formatting
normal = doc.styles["Normal"]
normal.font.name = "Times New Roman"
normal.font.size = Pt(14)
normal.paragraph_format.line_spacing = 1.5
normal.paragraph_format.first_line_indent = Cm(1.25)
normal.paragraph_format.space_after = Pt(0)
for name, size in [("Heading 1", 16), ("Heading 2", 14)]:
    st = doc.styles[name]
    st.font.name = "Times New Roman"
    st.font.size = Pt(size)
    st.font.bold = True
    st.paragraph_format.first_line_indent = Cm(0)
    st.paragraph_format.line_spacing = 1.5

for i, par in enumerate(doc.paragraphs):
    txt = par.text.strip()
    if i < 36:
        continue
    if txt.startswith("Рисунок "):
        par.alignment = WD_ALIGN_PARAGRAPH.CENTER
        par.paragraph_format.first_line_indent = Cm(0)
        par.paragraph_format.line_spacing = 1.5
        for r in par.runs:
            font(r, 14)
    elif par.style.name == "Heading 1":
        par.paragraph_format.first_line_indent = Cm(0)
        par.paragraph_format.line_spacing = 1.5
        for r in par.runs:
            font(r, 16, True)
    elif par.style.name == "Heading 2":
        par.paragraph_format.first_line_indent = Cm(0)
        par.paragraph_format.line_spacing = 1.5
        for r in par.runs:
            font(r, 14, True)
    elif par.style.name in ("toc 1", "toc 2"):
        par.paragraph_format.line_spacing = 1.5
        for r in par.runs:
            font(r, 14)
    else:
        par.alignment = WD_ALIGN_PARAGRAPH.JUSTIFY
        par.paragraph_format.line_spacing = 1.5
        par.paragraph_format.first_line_indent = Cm(1.25)
        for r in par.runs:
            if r.font.name != "Consolas":
                r.font.name = "Times New Roman"
            if r.font.size is None or r.font.size < Pt(10):
                r.font.size = Pt(11 if r.font.name == "Consolas" else 14)

# Page number in footer
sec = doc.sections[0]
sec.different_first_page_header_footer = True
footer = sec.footer.paragraphs[0]
footer.alignment = WD_ALIGN_PARAGRAPH.CENTER
for r in list(footer.runs):
    r._element.getparent().remove(r._element)
fld = OxmlElement('w:fldSimple')
fld.set(qn('w:instr'), ' PAGE ')
r = OxmlElement('w:r')
t = OxmlElement('w:t')
t.text = '1'
r.append(t)
fld.append(r)
footer._p.append(fld)
for run in footer.runs:
    font(run, 11)

doc.save(str(DOC))
print("saved", DOC)
