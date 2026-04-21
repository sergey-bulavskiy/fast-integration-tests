# Benchmark Runner — Design Spec

**Дата:** 2026-04-21  
**Цель:** сравнить три подхода к изоляции интеграционных тестов (IntegreSQL, Respawn, Testcontainers) по трём измерениям и сгенерировать HTML-отчёт для конференции / статьи.

---

## Контекст

Проект демонстрирует три подхода к database-per-test изоляции на PostgreSQL. Нужен воспроизводимый бенчмарк, результаты которого можно показать в виде графиков на слайдах и опубликовать в статье.

Живой запуск на конференции нецелесообразен (время, нестабильность). Вместо этого — заранее прогнанный отчёт (`report.html`) со скриншотами.

---

## Архитектура

```
tools/
└── BenchmarkRunner/
    ├── BenchmarkRunner.csproj
    ├── Program.cs                  — точка входа, оркестрация сценариев
    ├── Models/
    │   ├── BenchmarkScenario.cs    — описание одного запуска
    │   ├── BenchmarkResult.cs      — результат одного запуска
    │   └── BenchmarkReport.cs      — полный отчёт (все сценарии)
    ├── Runner/
    │   └── TestRunner.cs           — запускает dotnet test, меряет время
    ├── Migrations/
    │   └── MigrationManager.cs     — добавляет/удаляет фейковые миграции
    └── Report/
        ├── ReportGenerator.cs      — читает шаблон, инлайнит JSON, пишет HTML
        └── report-template.html    — Chart.js шаблон с плейсхолдером
```

Выходные файлы пишутся в `benchmark-results/` в корне репозитория:
- `report.html` — итоговый отчёт, открывается в браузере
- `results.json` — сырые данные для воспроизведения и статьи

---

## Сценарии

### Сценарий 1 — Влияние числа миграций

Фиксируем: TEST_REPEAT=10, MaxParallelThreads=4.  
Варьируем: число миграций — 17 (текущие), 67 (+50 фейковых), 117 (+100 фейковых).

Для каждой точки:
1. `MigrationManager` добавляет нужное количество пустых миграций (через `dotnet ef migrations add`)
2. Прогоняем все три подхода
3. `MigrationManager` откатывает фейковые миграции

**Ожидаемый результат:** Testcontainers растёт линейно, IntegreSQL почти не меняется, Respawn растёт пропорционально числу тест-классов (не тестов).

### Сценарий 2 — Масштаб числа тестов

Фиксируем: 17 миграций, MaxParallelThreads=4.  
Варьируем: TEST_REPEAT — 1, 5, 10, 20, 50.

**Ожидаемый результат:** все три подхода растут, но наклон у Testcontainers значительно круче.

### Сценарий 3 — Параллелизм

Фиксируем: 17 миграций, TEST_REPEAT=20.  
Варьируем: MaxParallelThreads — 1, 2, 4, 8.

**Ожидаемый результат:** IntegreSQL хорошо масштабируется (каждый тест независим), Respawn практически не выигрывает (тесты внутри класса sequential), Testcontainers — умеренный выигрыш.

---

## Модели данных

```csharp
record BenchmarkScenario(
    string Approach,        // "IntegreSQL" | "Respawn" | "Testcontainers"
    string ScenarioName,    // "migrations" | "scale" | "parallelism"
    int MigrationCount,
    int TestRepeat,
    int MaxParallelThreads
);

record BenchmarkResult(
    BenchmarkScenario Scenario,
    double ElapsedSeconds,
    bool Success
);

record BenchmarkReport(
    DateTime GeneratedAt,
    string MachineName,
    List<BenchmarkResult> Results
);
```

`BenchmarkReport` сериализуется в `results.json` через `System.Text.Json`.

---

## TestRunner

Запускает `dotnet test` как дочерний процесс через `Process`:

```
dotnet test tests/FastIntegrationTests.Tests
  --filter "FullyQualifiedName~Tests.<Approach>"
  --no-build
  -- xUnit.MaxParallelThreads=<N>
```

Переменная `TEST_REPEAT` передаётся через `ProcessStartInfo.Environment`.  
Время меряется `Stopwatch` вокруг `process.WaitForExit()`.  
stdout/stderr пробрасываются в консоль в реальном времени (пользователь видит прогресс).

`--no-build` используется после первичной сборки в начале прогона.

---

## MigrationManager

Добавляет фейковые пустые миграции и откатывает их после замера:

- Добавить: `dotnet ef migrations add Fake_NNN --project ... --startup-project ...`
- Применить: `dotnet ef database update` — **не нужно**, Testcontainers сам применяет через `MigrateAsync`
- Удалить после: удаление файлов `Benchmark_Fake_*.cs` и `Benchmark_Fake_*Designer.cs` напрямую из `Migrations/` — надёжнее чем последовательные `dotnet ef migrations remove` (тот удаляет только последнюю)

Фейковые миграции именуются `Benchmark_Fake_001` ... `Benchmark_Fake_NNN`, что позволяет их надёжно идентифицировать и удалить.

---

## ReportGenerator

1. Читает `report-template.html` из директории сборки
2. Сериализует `BenchmarkReport` в JSON
3. Заменяет плейсхолдер `/*INJECT_JSON*/` на JSON-строку
4. Пишет результат в `benchmark-results/report.html`

```html
<!-- report-template.html -->
<script>
  const BENCHMARK_DATA = /*INJECT_JSON*/;
</script>
```

JSON инлайнится (не fetch) — отчёт работает по `file://` без сервера.

---

## report-template.html

Standalone HTML файл:
- **Chart.js** подключается через CDN (`https://cdn.jsdelivr.net/npm/chart.js`)
- Три секции — по одному графику на сценарий
- Каждый график: линейный (`type: 'line'`), три серии (по одной на подход), подписи осей на русском
- Цвета: IntegreSQL — синий, Respawn — зелёный, Testcontainers — красный (чтобы зрительно считывалось)
- Шапка с датой генерации и именем машины из `BENCHMARK_DATA`

---

## Запуск

```bash
# Из корня репозитория
dotnet run --project tools/BenchmarkRunner

# Открыть отчёт
start benchmark-results/report.html   # Windows
open benchmark-results/report.html    # macOS
```

Полный прогон всех сценариев займёт 15–30 минут в зависимости от машины.

---

## Ограничения и допущения

- Docker должен быть запущен перед стартом
- Сборка (`dotnet build`) выполняется один раз в начале, затем `--no-build`
- Фейковые миграции удаляются после каждого сценария — репозиторий возвращается в исходное состояние
- Chart.js требует интернет для загрузки с CDN; для офлайн-режима можно скачать и положить рядом
- Результаты зависят от железа — `MachineName` в отчёте помогает воспроизвести контекст
