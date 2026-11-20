from pathlib import Path
import re
import uuid

path = Path(r"C:\Users\ALX\Desktop\Practice-master\UML.mdl")
text = path.read_text(encoding="cp1251")


def matching(text: str, open_index: int) -> int:
    depth = 0
    in_string = False
    escaped = False
    for index in range(open_index, len(text)):
        char = text[index]
        if in_string:
            if escaped:
                escaped = False
            elif char == "\\":
                escaped = True
            elif char == '"':
                in_string = False
            continue
        if char == '"':
            in_string = True
        elif char == "(":
            depth += 1
        elif char == ")":
            depth -= 1
            if depth == 0:
                return index
    raise ValueError("Unbalanced parentheses")


def find_blocks(content: str, pattern: str):
    result = []
    for match in re.finditer(pattern, content, re.MULTILINE):
        start = match.start()
        end = matching(content, start)
        result.append(content[start:end + 1])
    return result


def new_quid() -> str:
    return uuid.uuid4().hex[:12].upper()


state_start = text.index("\t    states     \t(list States")
state_open = text.index("(", state_start)
state_close = matching(text, state_open)
state_content = text[state_open + 1:state_close]
state_blocks = find_blocks(
    state_content,
    r"^\t\t\(object (?:State|ActivityState|Decision) \"")

transition_start = text.index("\t    transitions \t(list transition_list")
transition_open = text.index("(", transition_start)
transition_close = matching(text, transition_open)
transition_content = text[transition_open + 1:transition_close]
transition_blocks = find_blocks(transition_content, r"^\t\t\(object State_Transition")

diagram_start = text.index("\t    statediagrams \t(list StateDiagrams")
diagram_open = text.index("(", diagram_start)
diagram_close = matching(text, diagram_open)
diagram_content = text[diagram_open + 1:diagram_close]
diagram_blocks = find_blocks(diagram_content, r"^\t\t\(object ActivityDiagram \"")
old_diagram = diagram_blocks[0]
old_diagram_title_match = re.search(r'\(object ActivityDiagram "([^"]+)"', old_diagram)
old_diagram_title = old_diagram_title_match.group(1)

# Extract old state names and quids.
old_states = []
for block in state_blocks:
    name_match = re.search(r'\(object (?:State|ActivityState|Decision) "([^"]+)"', block)
    quid_match = re.search(r'quid\s+"([0-9A-Fa-f]{12})"', block)
    if name_match and quid_match:
        old_states.append((name_match.group(1), quid_match.group(1)))

old_transitions = []
for block in transition_blocks:
    quid_match = re.search(r'quid\s+"([0-9A-Fa-f]{12})"', block)
    if quid_match:
        old_transitions.append(quid_match.group(1))

start_name = next(name for name, _ in old_states if name.startswith("$UNNAMED$") and "22" in name)
end_name = next(name for name, _ in old_states if name.startswith("$UNNAMED$") and "23" in name)

modules = [
    {
        "title": "План выпуска",
        "prefix": "Выпуск",
        "map": {
            start_name: "$UNNAMED$100",
            "Открыть модуль «План перевозок»": "Выпуск: открыть модуль «План выпуска»",
            "Загрузить склады, магазины и расстояния": "Выпуск: загрузить продукты и лимиты ресурсов",
            "Запасы достаточны?": "Выпуск: лимиты ресурсов достаточны?",
            "Открыть задачу с резервом или дефицитом": "Выпуск: показать предупреждение о лимитах",
            "Выбрать склады и магазины": "Выпуск: выбрать продукты для расчёта",
            "Данные корректны?": "Выпуск: данные корректны?",
            "Показать сообщение об ошибке": "Выпуск: показать сообщение об ошибке",
            "Решить транспортную задачу": "Выпуск: решить задачу симплекс-методом",
            "План допустим?": "Выпуск: план допустим?",
            "Пересчитать распределение": "Выпуск: пересчитать остатки ресурсов",
            "Сформировать маршруты": "Выпуск: сформировать план выпуска",
            "Показать результат и экспорт": "Выпуск: показать результат и экспорт",
            "Построить начальный план": "Выпуск: построить начальный опорный план",
            end_name: "$UNNAMED$101",
        },
    },
    {
        "title": "Оборудование",
        "prefix": "Оборудование",
        "map": {
            start_name: "$UNNAMED$102",
            "Открыть модуль «План перевозок»": "Оборудование: открыть модуль",
            "Загрузить склады, магазины и расстояния": "Оборудование: загрузить параметры оборудования",
            "Запасы достаточны?": "Оборудование: срок службы корректен?",
            "Открыть задачу с резервом или дефицитом": "Оборудование: показать сообщение о параметрах",
            "Выбрать склады и магазины": "Оборудование: выбрать горизонт планирования",
            "Данные корректны?": "Оборудование: данные корректны?",
            "Показать сообщение об ошибке": "Оборудование: показать сообщение об ошибке",
            "Решить транспортную задачу": "Оборудование: рассчитать таблицу Беллмана",
            "План допустим?": "Оборудование: замена выгодна?",
            "Пересчитать распределение": "Оборудование: пересчитать стратегию",
            "Сформировать маршруты": "Оборудование: сформировать график замен",
            "Показать результат и экспорт": "Оборудование: показать результат и экспорт",
            "Построить начальный план": "Оборудование: построить начальное состояние",
            end_name: "$UNNAMED$103",
        },
    },
    {
        "title": "Кассы",
        "prefix": "Кассы",
        "map": {
            start_name: "$UNNAMED$104",
            "Открыть модуль «План перевозок»": "Кассы: открыть модуль",
            "Загрузить склады, магазины и расстояния": "Кассы: загрузить потоки магазинов",
            "Запасы достаточны?": "Кассы: потоки заданы?",
            "Открыть задачу с резервом или дефицитом": "Кассы: показать сообщение о потоках",
            "Выбрать склады и магазины": "Кассы: выбрать целевые показатели",
            "Данные корректны?": "Кассы: данные корректны?",
            "Показать сообщение об ошибке": "Кассы: показать сообщение об ошибке",
            "Решить транспортную задачу": "Кассы: рассчитать число кассиров",
            "План допустим?": "Кассы: очередь допустима?",
            "Пересчитать распределение": "Кассы: увеличить число кассиров",
            "Сформировать маршруты": "Кассы: сформировать план касс",
            "Показать результат и экспорт": "Кассы: показать результат и экспорт",
            "Построить начальный план": "Кассы: построить начальную очередь",
            end_name: "$UNNAMED$105",
        },
    },
    {
        "title": "Инвестиции",
        "prefix": "Инвестиции",
        "map": {
            start_name: "$UNNAMED$106",
            "Открыть модуль «План перевозок»": "Инвестиции: открыть модуль",
            "Загрузить склады, магазины и расстояния": "Инвестиции: загрузить проекты и бюджет",
            "Запасы достаточны?": "Инвестиции: бюджет задан?",
            "Открыть задачу с резервом или дефицитом": "Инвестиции: показать сообщение о бюджете",
            "Выбрать склады и магазины": "Инвестиции: выбрать проекты",
            "Данные корректны?": "Инвестиции: данные корректны?",
            "Показать сообщение об ошибке": "Инвестиции: показать сообщение об ошибке",
            "Решить транспортную задачу": "Инвестиции: решить задачу о ранце",
            "План допустим?": "Инвестиции: портфель допустим?",
            "Пересчитать распределение": "Инвестиции: пересчитать распределение",
            "Сформировать маршруты": "Инвестиции: сформировать портфель",
            "Показать результат и экспорт": "Инвестиции: показать результат и экспорт",
            "Построить начальный план": "Инвестиции: построить начальный портфель",
            end_name: "$UNNAMED$107",
        },
    },
]


def replace_all(value: str, mapping: dict[str, str]) -> str:
    for old in sorted(mapping, key=len, reverse=True):
        value = value.replace(old, mapping[old])
    return value


new_state_texts = []
new_transition_texts = []
new_diagram_texts = []

for module in modules:
    name_map = module["map"]
    state_quid_map = {old_quid: new_quid() for _, old_quid in old_states}
    transition_quid_map = {old_quid: new_quid() for old_quid in old_transitions}

    for block in state_blocks:
        name_match = re.search(r'\(object (?:State|ActivityState|Decision) "([^"]+)"', block)
        quid_match = re.search(r'quid\s+"([0-9A-Fa-f]{12})"', block)
        if not name_match or not quid_match:
            continue
        old_name = name_match.group(1)
        old_quid = quid_match.group(1)
        if old_name not in name_map:
            continue
        block = block.replace(f'"{old_name}"', f'"{name_map[old_name]}"', 1)
        block = block.replace(f'"{old_quid}"', f'"{state_quid_map[old_quid]}"', 1)
        new_state_texts.append(block)

    for block in transition_blocks:
        old_transition_quid = re.search(r'quid\s+"([0-9A-Fa-f]{12})"', block).group(1)
        block = replace_all(block, name_map)
        for old_quid, new_value in state_quid_map.items():
            block = block.replace(old_quid, new_value)
        block = block.replace(old_transition_quid, transition_quid_map[old_transition_quid])
        block = re.sub(
            r'(sendEvent\s+\(object sendEvent\s+quid\s+)"[0-9A-Fa-f]{12}"',
            lambda match: match.group(1) + '"' + new_quid() + '"',
            block,
        )
        new_transition_texts.append(block)

    diagram = old_diagram
    diagram = diagram.replace(f'"{old_diagram_title}"', f'"{module["title"]}"', 1)
    diagram = replace_all(diagram, name_map)
    for old_quid, new_value in {**state_quid_map, **transition_quid_map}.items():
        diagram = diagram.replace(old_quid, new_value)
    diagram = diagram.replace(f'"{old_transition_quid}"', f'"{new_quid()}"')
    # Replace the diagram's own quid with a unique one.
    diagram_quid = re.search(r'quid\s+"([0-9A-Fa-f]{12})"', diagram).group(1)
    diagram = diagram.replace(f'"{diagram_quid}"', f'"{new_quid()}"', 1)
    new_diagram_texts.append(diagram)

insertions = [
    (state_close, "\n" + "\n".join(new_state_texts)),
    (transition_close, "\n" + "\n".join(new_transition_texts)),
    (diagram_close, "\n" + "\n".join(new_diagram_texts)),
]

for position, insertion in sorted(insertions, key=lambda item: item[0], reverse=True):
    text = text[:position] + insertion + text[position:]

# Balance check.
depth = 0
in_string = False
escaped = False
for char in text:
    if in_string:
        if escaped:
            escaped = False
        elif char == "\\":
            escaped = True
        elif char == '"':
            in_string = False
        continue
    if char == '"':
        in_string = True
    elif char == "(":
        depth += 1
    elif char == ")":
        depth -= 1
        if depth < 0:
            raise ValueError("Negative nesting")

if depth != 0:
    raise ValueError(f"Unbalanced output: {depth}")

path.write_text(text, encoding="cp1251")
print("states", len(new_state_texts))
print("transitions", len(new_transition_texts))
print("diagrams", len(new_diagram_texts))
print("size", len(text))
