# Бенчмарк: таймстемпы файлов и отображение количества тестов

> **Примечание:** Механизм `TestRepeat` заменён на `ClassScale` — см. `docs/superpowers/specs/2026-04-26-class-scale-scenario2-design.md`.

## Цель

Два независимых улучшения BenchmarkRunner:
1. Сохранять результаты с таймстемпом, чтобы предыдущие прогоны не затирались.
2. Показывать реальное количество тестов вместо TEST_REPEAT.

## Фича 1: Таймстемп в именах файлов

### Изменения

**`tools/BenchmarkRunner/Report/ReportGenerator.cs`**

Добавить поле `_timestamp: string`, инициализировать в конструкторе через `DateTime.Now.ToString("yyyyMMdd-HHmmss")`. Использовать во всех сохранениях:

```
benchmark-results/results-20260425-123456.json
benchmark-results/report-20260425-123456.html
```

`SaveJson()` и `Generate()` используют одну и ту же метку — ту, что создана в конструкторе.

**`tools/BenchmarkRunner/Program.cs`**

Финальная строка вывода: `Open: benchmark-results/report-20260425-123456.html` (с реальным именем файла). `ReportGenerator` возвращает путь к HTML через новый метод или свойство, или Program.cs формирует имя самостоятельно по той же логике.

Проще: `ReportGenerator` выставляет публичное свойство `HtmlPath` после `Generate()`.

## Фича 2: Количество тестов

### Источник данных

Константа в `Program.cs` — хардкод, обновляется вручную при изменении тест-сьюта:

```csharp
// хардкод — обновить при добавлении/удалении тест-методов в тест-проектах
// проверить: dotnet test <проект> --list-tests | Measure-Object -Line
const int BaseTestCount = N;
```

Значение N определяется реальным подсчётом на шаге реализации.

### Изменения в модели

**`tools/BenchmarkRunner/Models/BenchmarkReport.cs`**

Добавить поле `BaseTestCount: int`:

```csharp
public record BenchmarkReport(
    DateTime GeneratedAt,
    string MachineName,
    int BaseTestCount,
    IReadOnlyList<BenchmarkResult> Results
);
```

**`tools/BenchmarkRunner/Program.cs`**

Передавать `BaseTestCount` везде где создаётся `BenchmarkReport`:
- в `RunOrAbort` → `SaveJson`
- финальный `Generate`

### Изменения в шаблоне

**`tools/BenchmarkRunner/Report/report-template.html`**

В начале JS-блока добавить:
```javascript
const BASE_TEST_COUNT = data.baseTestCount;
```

**Сценарий 2 — ось X и субтайтл:**

Функция `buildChart` получает необязательный параметр `xTransform`:
```javascript
function buildChart(canvasId, scenarioName, xField, xLabel, xTransform = x => x)
```

Вызов для scale:
```javascript
buildChart('chart-scale', 'scale', 'testRepeat', 'Количество тестов', x => x * BASE_TEST_COUNT);
```

Субтайтл Сценария 2: `17 миграций, потоков=8 | Ось X: количество тестов`.

**Субтайтлы Сценариев 1, 3, 4:**

Вместо `TEST_REPEAT=12` показывать `${m.scenario.testRepeat * BASE_TEST_COUNT} тестов`:
- Сценарий 1: `${m1.scenario.testRepeat * BASE_TEST_COUNT} тестов, потоков=8 | Ось X: ...`
- Сценарий 3: `17 миграций, ${m3.scenario.testRepeat * BASE_TEST_COUNT} тестов | Ось X: ...`
- Сценарий 4 (stacked bar): `${m1.scenario.testRepeat * BASE_TEST_COUNT} тестов, потоков=8 | Слои: ...`

### Обновление CLAUDE.md

В секцию `## Benchmark Runner` добавить примечание:

```markdown
> **Важно:** `BaseTestCount` в `tools/BenchmarkRunner/Program.cs` — хардкод.
> При добавлении или удалении тест-методов обновить константу вручную.
> Проверить актуальное значение: `dotnet test tests/FastIntegrationTests.Tests.IntegreSQL --list-tests | Measure-Object -Line`
```

## Ограничения

- `BaseTestCount` — суммарное число тест-методов в одном тест-проекте (IntegreSQL, Respawn, Testcontainers — у всех одинаковое количество, они зеркальны). Если проекты расходятся — нужна отдельная константа на подход, но сейчас это не нужно.
- Старые `results.json` без поля `baseTestCount` вернут `undefined` в шаблоне → `BASE_TEST_COUNT` будет `NaN` → формулы вернут `NaN`. Это допустимо: старые файлы открываться не обязаны.
