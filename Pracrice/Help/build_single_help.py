from pathlib import Path
import base64
import re

root = Path(r"C:\Users\ALX\Desktop\Practice-master\Pracrice\Help")
pages = [
    ("index", "Главная"),
    ("interface", "Интерфейс"),
    ("authorization", "Вход в систему"),
    ("plan", "План выпуска"),
    ("transport", "План перевозок"),
    ("equipment", "Оборудование"),
    ("cash", "Кассы"),
    ("investments", "Инвестиции"),
    ("references", "Справочники"),
    ("reports", "Отчёты"),
    ("admin", "Администрирование"),
    ("faq", "Вопросы и ошибки"),
]

content_pattern = re.compile(
    r'<div class="content">(.*?)</div>\s*</div>\s*<div class="footer">',
    re.DOTALL,
)

sections = []
for page, title in pages:
    source = (root / f"{page}.html").read_text(encoding="utf-8")
    match = content_pattern.search(source)
    if not match:
        raise ValueError(f"content not found: {page}")
    body = match.group(1).strip()
    sections.append(f'<section class="page" id="{page}">{body}</section>')

nav = "\n".join(
    f'<a class="nav-link" href="#{page}">{title}</a>'
    for page, title in pages
)

logo = base64.b64encode((root / "logo.png").read_bytes()).decode("ascii")
css = (root / "help.css").read_text(encoding="utf-8")

# Layout additions for the single-file help.
css += """

/* Single-file workspace */
body { background: #eef3ed; }
.app { display: flex; min-height: 100vh; align-items: stretch; }
.sidebar {
  width: 255px;
  flex: 0 0 255px;
  background: #1c3b30;
  color: #dce9dd;
  padding: 22px 14px;
  position: sticky;
  top: 0;
  height: 100vh;
  overflow-y: auto;
  border-right: 4px solid var(--gold);
}
.sidebar img { width: 74px; height: 74px; object-fit: contain; display: block; margin: 0 auto 8px; }
.sidebar .brand { text-align: center; font-size: 10px; letter-spacing: 1px; text-transform: uppercase; color: #9fc3a8; }
.sidebar h1 { text-align: center; margin: 4px 0 20px; font-size: 16px; line-height: 1.25; color: #fff; font-weight: 600; }
.nav-link { display: block; padding: 8px 12px; margin: 3px 0; border-radius: 6px; color: #dce9dd; text-decoration: none; font-size: 13px; }
.nav-link:hover { background: #2b5748; color: #fff; }
.main { flex: 1 1 auto; min-width: 0; padding: 24px; }
.page { max-width: 940px; margin: 0 auto 20px; background: #fff; border: 1px solid var(--line); border-radius: 10px; padding: 25px 31px; box-shadow: 0 6px 24px rgba(28,59,48,.06); }
.page + .page { margin-top: 18px; }
.page .breadcrumb { display: none; }
.page h2:first-of-type { margin-top: 0; }
@media (max-width: 720px) {
  .app { display: block; }
  .sidebar { width: 100%; height: auto; position: static; }
  .main { padding: 12px; }
  .page { padding: 18px; }
}
@media print {
  .sidebar { display: none; }
  .main { padding: 0; }
  .page { box-shadow: none; border: 0; max-width: none; padding: 0; page-break-after: always; }
}
"""

html = f"""<!DOCTYPE html>
<html lang="ru">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Справка ООО «РОСПИВ»</title>
<style>
{css}
</style>
</head>
<body>
<div class="app">
  <aside class="sidebar">
    <img src="data:image/png;base64,{logo}" alt="Логотип ООО «РОСПИВ»">
    <div class="brand">ООО «РОСПИВ»</div>
    <h1>Справка по информационной системе</h1>
    {nav}
  </aside>
  <main class="main">
    {''.join(sections)}
  </main>
</div>
</body>
</html>
"""

(root / "PracriceHelp.html").write_text(html, encoding="utf-8")
print("saved", len(html), "bytes")
