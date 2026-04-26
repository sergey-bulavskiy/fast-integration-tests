# Декомпозиция времени: измерение миграций и сброса данных

> **Примечание:** Механизм `TestRepeat` заменён на `ClassScale` — см. `docs/superpowers/specs/2026-04-26-class-scale-scenario2-design.md`.

## Цель

Добавить в HTML-отчёт бенчмарка четвёртый график — stacked bar chart, показывающий из чего складывается время выполнения тестов для каждого подхода: миграции / сброс данных / бизнес-логика. Данные собираются по всем точкам Сценария 1, отображаются только крайние (17 и 117 миграций).

## Инструментирование тест-инфраструктуры

Каждая fixture / базовый класс выводит в stdout строку сразу после каждой операции:

```
##BENCH[migration]=234
##BENCH[migration]=231
##BENCH[reset]=12
##BENCH[reset]=15
```

Значения в миллисекундах, целые. BenchmarkRunner суммирует все строки с одним ключом. Никакой общей переменной — каждый компонент отвечает только за себя.

### Точки измерения по подходам

**IntegreSQL** (`Tests.IntegreSQL`):
- Миграции — `IntegresSqlContainerManager`: Stopwatch вокруг `MigrateAsync`, одна строка `##BENCH[migration]=` после завершения
- Сброс — `AppServiceTestBase` / `ComponentTestBase`: Stopwatch вокруг `ReturnDatabaseAsync`, одна строка `##BENCH[reset]=` на каждый тест

**Respawn** (`Tests.Respawn`):
- Миграции — `RespawnFixture.InitializeAsync()`: Stopwatch вокруг `MigrateAsync`, одна строка `##BENCH[migration]=` на класс
- Сброс — cleanup между тестами: Stopwatch вокруг `_respawner.ResetAsync()`, одна строка `##BENCH[reset]=` на каждый тест

**Testcontainers** (`Tests.Testcontainers`):
- Миграции — базовый класс перед каждым тестом: Stopwatch вокруг `EnsureDeleted + MigrateAsync`, одна строка `##BENCH[migration]=` на каждый тест
- Сброс — отсутствует (пересоздание БД входит в миграционный шаг)

## BenchmarkRunner

### Парсинг stdout

`TestRunner.cs` уже захватывает stdout (для failure log). Добавляем парсинг `##BENCH[...]` строк:

```csharp
long migrationMs = 0, resetMs = 0;
foreach (var line in stdout.Split('\n'))
{
    if (line.StartsWith("##BENCH[migration]=") &&
        long.TryParse(line["##BENCH[migration]=".Length..], out var m))
        migrationMs += m;
    else if (line.StartsWith("##BENCH[reset]=") &&
        long.TryParse(line["##BENCH[reset]=".Length..], out var r))
        resetMs += r;
}
```

### Изменения в модели

```csharp
public record BenchmarkResult(
    BenchmarkScenario Scenario,
    double ElapsedSeconds,
    double MigrationSeconds,  // новое
    double ResetSeconds,       // новое
    bool Success
);
```

`BusinessSeconds` не хранится — вычисляется в шаблоне: `elapsed - migration - reset`.

## Новый график в отчёте

**Тип:** stacked bar chart (Chart.js, тот же v4).

**Данные:** из Scenario 1 (`scenarioName = "migrations"`) берутся только точки `migrationCount = 17` и `migrationCount = 117`. Все промежуточные точки сохраняются в `results.json` но в этом графике не используются.

**Структура:** 3 группы по подходам, в каждой 2 бара.

```
Группа "IntegreSQL"   │ бар: 17 миг │ бар: 117 миг
Группа "Respawn"      │ бар: 17 миг │ бар: 117 миг
Группа "Testcontainers"│ бар: 17 миг │ бар: 117 миг
```

**Слои каждого бара:**
| Слой | Цвет | Значение |
|------|------|----------|
| Миграции | оранжевый `rgb(251,146,60)` | `migrationSeconds` |
| Сброс данных | жёлтый `rgb(250,204,21)` | `resetSeconds` |
| Бизнес-логика | цвет подхода (синий/зелёный/красный) | `elapsed - migration - reset` |

**Заголовок:** `Сценарий 4 — Состав времени: min / max миграций`

**Подзаголовок:** фиксированные параметры: TEST_REPEAT и maxParallelThreads.

## Ограничения

- `ElapsedSeconds` включает startup .NET runtime и сборку тест-инфраструктуры — они войдут в "бизнес-логика". Это приемлемо: overhead константен и одинаков для всех подходов.
- Если `##BENCH` строки отсутствуют (ошибка или старая версия тестов), `MigrationSeconds` и `ResetSeconds` будут 0 — бар покажет только общее время без декомпозиции.
