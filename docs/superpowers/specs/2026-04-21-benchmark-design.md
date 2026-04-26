# Benchmark Runner — Design Spec

> **Примечание:** Механизм `TestRepeat` заменён на `ClassScale` — см. `docs/superpowers/specs/2026-04-26-class-scale-scenario2-design.md`.

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
Варьируем: число миграций — 16 (текущие), 66 (+50 фейковых), 116 (+100 фейковых).

Для каждой точки:
1. `MigrationManager` добавляет нужное количество пустых миграций (через `dotnet ef migrations add`)
2. Прогоняем все три подхода
3. `MigrationManager` откатывает фейковые миграции

**Ожидаемый результат:** Testcontainers растёт линейно, IntegreSQL почти не меняется, Respawn растёт пропорционально числу тест-классов (не тестов).

### Сценарий 2 — Масштаб числа тестов

Фиксируем: 16 миграций, MaxParallelThreads=4.  
Варьируем: TEST_REPEAT — 1, 5, 10, 20, 50.

**Ожидаемый результат:** все три подхода растут, но наклон у Testcontainers значительно круче.

### Сценарий 3 — Параллелизм

Фиксируем: 16 миграций, TEST_REPEAT=20.  
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
    IReadOnlyList<BenchmarkResult> Results
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

Пишет `.cs` файлы миграций **напрямую** в `src/FastIntegrationTests.Infrastructure/Migrations/` — без `dotnet ef migrations add` (он генерирует пустой `Up`/`Down`, после чего всё равно нужно редактировать файл). Фейковые миграции содержат только `migrationBuilder.Sql(...)` и не затрагивают EF-модель, поэтому snapshot обновлять не нужно.

После замера — удаляет файлы `Benchmark_Fake_*.cs` из папки миграций. Файлы `Designer.cs` не генерируются — фейковые миграции пишутся напрямую и не проходят через `dotnet ef`. Репозиторий возвращается в исходное состояние.

`AddFakeMigrations` вызывается внутри блока `try` — очистка через `RemoveFakeMigrations` выполняется в `finally`, гарантируя удаление файлов даже если `Build()` завершился с ошибкой.

`AddFakeMigrations` проверяет существование директории `_migrationsPath` перед записью файлов и бросает `DirectoryNotFoundException` с понятным сообщением, если путь не найден.

### Именование файлов

Timestamp фейковых миграций начинается с `29990101` чтобы гарантированно идти после всех реальных:

```
29990101000001_Benchmark_Fake_001.cs
29990101000002_Benchmark_Fake_002.cs
...
```

### Два чередующихся типа

Миграции не пустые — пустые выполняются за <1 мс и не показывают разницу.

**Нечётные** — справочная таблица + 300 строк seed-данных (~10–20 мс):

```csharp
migrationBuilder.Sql(@"
CREATE TABLE benchmark_ref_001 (
    id   SERIAL PRIMARY KEY,
    code VARCHAR(20) NOT NULL,
    name VARCHAR(100) NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);
INSERT INTO benchmark_ref_001 (code, name)
SELECT
    'CODE_' || gs,
    'Reference value number ' || gs
FROM generate_series(1, 300) gs;
");
```

**Чётные** — ADD COLUMN + UPDATE + SET NOT NULL (~5–15 мс):

```csharp
migrationBuilder.Sql(@"
ALTER TABLE ""Products"" ADD COLUMN benchmark_col_002 TEXT NULL;
UPDATE ""Products"" SET benchmark_col_002 = 'default_value';
ALTER TABLE ""Products"" ALTER COLUMN benchmark_col_002 SET NOT NULL;
ALTER TABLE ""Products"" ALTER COLUMN benchmark_col_002 SET DEFAULT 'default_value';
");
```

### Правила генерации C# кода

Соответствуют существующим миграциям проекта:

- SQL — в вербатим-строках `@"..."`
- Идентификаторы PostgreSQL в двойных кавычках экранируются как `""` внутри `@"..."` — например `""Products""`, `""Id""`
- Имена справочных таблиц и колонок фейковых миграций — в нижнем регистре без кавычек (не являются EF-сущностями)
- `Down()` — `DROP TABLE IF EXISTS` / `DROP COLUMN IF EXISTS`

### Применение

`dotnet ef database update` не нужен — каждый подход сам применяет миграции:
- Testcontainers: `MigrateAsync()` в `TestDbFactory.CreateAsync()` на каждый тест
- Respawn: `MigrateAsync()` в `RespawnFixture.InitializeAsync()` один раз на класс
- IntegreSQL: `MigrateAsync()` в `IntegresSqlContainerManager` один раз на процесс

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
