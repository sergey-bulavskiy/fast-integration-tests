---
title: Сценарий 2 — масштаб по числу классов + удаление TEST_REPEAT
date: 2026-04-26
status: approved
---

# Сценарий 2: масштаб по числу классов + удаление TEST_REPEAT

## Проблема

Сценарий 2 бенчмарка варьирует `TEST_REPEAT` — число итераций внутри существующих классов.
Это занижает per-class overhead Respawn и Testcontainers: fixture (контейнер + миграции) создаётся
один раз на класс независимо от `TEST_REPEAT`. В реальном проекте рост числа тестов = рост числа
тест-классов, а не итераций внутри них.

Одновременно `TEST_REPEAT` усложняет инфраструктуру: env-переменная, специальный класс `TestRepeat`,
`[Theory]` вместо `[Fact]` во всех тестах.

## Решение

Заменить `TEST_REPEAT`-механизм на **class-scaling**: BenchmarkRunner генерирует пустые подклассы
для каждого исходного тест-класса, получая честный per-class overhead на каждую точку данных.
Параллельно удалить `TEST_REPEAT` из всей тест-инфраструктуры и перевести все тесты на `[Fact]`.

Целевой пик в Сценарии 2: **50 × 223 ≈ 11 150 тестов**.

---

## Архитектура

### 1. `ClassScaleManager` (новый)

**Путь:** `tools/BenchmarkRunner/Scale/ClassScaleManager.cs`

Аналог `MigrationManager` — управляет жизненным циклом генерируемых файлов.

**Три test-проекта** (пути от repoRoot):
- `tests/FastIntegrationTests.Tests.IntegreSQL/`
- `tests/FastIntegrationTests.Tests.Respawn/`
- `tests/FastIntegrationTests.Tests.Testcontainers/`

**Алгоритм `AddScaleClasses(int scaleFactor)`:**

1. Для каждого test-проекта: рекурсивно сканирует `.cs` файлы, исключая `obj/`, `Infrastructure/`,
   `GlobalUsings.cs`, `BenchmarkScaleClasses.cs`.
2. Фильтр тест-классов: файл содержит `[Theory]` или `[Fact]`.
3. Из каждого файла regex'ами извлекает:
   - Namespace: `^namespace\s+([\w.]+)\s*[;{]` (file-scoped и block)
   - Имя класса: `public\s+class\s+(\w+)`
   - Тип фикстуры: `public\s+\w+\((\w+Fixture)\s+fixture\)` (null если нет)
4. Генерирует `BenchmarkScaleClasses.cs` в корне каждого проекта.

**Формат сгенерированного файла:**

```csharp
// BenchmarkScaleClasses.cs — сгенерирован BenchmarkRunner, не редактировать
// ReSharper disable All
#pragma warning disable

namespace FastIntegrationTests.Tests.IntegreSQL.Products
{
    public class ProductServiceCrTests_2 : ProductServiceCrTests { }
    public class ProductServiceCrTests_3 : ProductServiceCrTests { }
}
namespace FastIntegrationTests.Tests.IntegreSQL.Categories
{
    public class CategoryServiceCrTests_2 : CategoryServiceCrTests { }
}
```

Для классов с фикстурой (Respawn, Testcontainers):

```csharp
namespace FastIntegrationTests.Tests.Respawn.Products
{
    public class ProductServiceCrRespawnTests_2 : ProductServiceCrRespawnTests
    {
        public ProductServiceCrRespawnTests_2(RespawnFixture fixture) : base(fixture) { }
    }
}
```

**`RemoveScaleClasses()`** — удаляет `BenchmarkScaleClasses.cs` из трёх проектов.

---

### 2. Тест-инфраструктура

**Удаляется:** `tests/FastIntegrationTests.Tests.Shared/TestRepeat.cs`

**Все тест-методы во всех 84 классах** (28 × 3 проекта):

```csharp
// было
[Theory]
[MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
public async Task Foo(int _) { ... }

// стало
[Fact]
public async Task Foo() { ... }
```

Изменение механическое: убрать две строки атрибутов, `[Theory]` → `[Fact]`, убрать `int _`.

---

### 3. `BenchmarkScenario` и Program.cs

**`BenchmarkScenario`:** удалить поле `TestRepeat`, добавить `int ClassScale = 1`.

**CLI-аргумент:** `--repeat` / `-r` переименовывается в `--scale` / `-s`.
Переменная `defaultRepeat` → `defaultClassScale` (по умолчанию 12).

**Поведение по сценариям:**

| Сценарий | ClassScale | Перестройка |
|---|---|---|
| Warmup | 1 (без генерации) | нет |
| 1 — Миграции | `defaultClassScale` (фиксировано) | 1× до цикла, 1× после |
| 2 — Масштаб | `[1, 5, 10, 20, 50]` | per-point (как Сценарий 1 с миграциями) |
| 3 — Параллелизм | `defaultClassScale` (фиксировано) | 1× до цикла, 1× после |

**Псевдокод Сценария 1:**

```
classScaleManager.AddScaleClasses(defaultClassScale); runner.Build();
try {
  for each migrationCount:
    try {
      add fake migrations; build;
      for each approach: RunOrAbort(... ClassScale: defaultClassScale);
    } finally { remove migrations; build; }
} finally {
  classScaleManager.RemoveScaleClasses(); runner.Build();
}
```

**Псевдокод Сценария 2:**

```
for each scale in [1, 5, 10, 20, 50]:
  try {
    if scale > 1: classScaleManager.AddScaleClasses(scale); runner.Build();
    for each approach: RunOrAbort(... ClassScale: scale);
  } finally {
    if scale > 1: classScaleManager.RemoveScaleClasses(); runner.Build();
  }
```

**Псевдокод Сценария 3:** аналогично Сценарию 1 (add once / remove once вокруг цикла).

---

### 4. HTML-шаблон

Заменить `testRepeat` → `classScale` в 4 местах:
- `buildChart` для `'scale'`: `xField = 'classScale'`
- Subtitle Сценария 1 (migrations): `m1.scenario.classScale * BASE_TEST_COUNT`
- Subtitle Сценария 3 (parallelism): `m3.scenario.classScale * BASE_TEST_COUNT`
- Subtitle timing-чарта: `m1.scenario.classScale * BASE_TEST_COUNT`

Subtitle Сценария 2 (`migrationCount` + `maxParallelThreads`) `testRepeat` не содержит — не меняется.

---

## Затрагиваемые файлы

| Файл | Изменение |
|---|---|
| `tools/BenchmarkRunner/Scale/ClassScaleManager.cs` | новый |
| `tools/BenchmarkRunner/Models/BenchmarkScenario.cs` | `TestRepeat` → `ClassScale` |
| `tools/BenchmarkRunner/Program.cs` | все сценарии + `--repeat`→`--scale` |
| `tools/BenchmarkRunner/Report/report-template.html` | `testRepeat` → `classScale` (4 места) |
| `tests/FastIntegrationTests.Tests.Shared/TestRepeat.cs` | удалить |
| 84 тест-класса (28 × 3 проекта) | `[Theory]+MemberData+int _` → `[Fact]` |
| `CLAUDE.md` | обновить: `--repeat`→`--scale`, убрать `TEST_REPEAT` из команд запуска тестов, обновить таблицу аргументов BenchmarkRunner |
| `docs/benchmark-issues/05-test-repeat-vs-real-classes.md` | пометить как решённое (добавить заголовок `## Решение` со ссылкой на этот спек) |
| `docs/superpowers/specs/2026-04-21-benchmark-design.md` | упомянуть замену TEST_REPEAT на ClassScale |
| `docs/superpowers/specs/2026-04-25-benchmark-timestamps-testcount-design.md` | убрать/заменить упоминания `testRepeat` |
| `docs/superpowers/specs/2026-04-25-migration-timing-design.md` | то же |
| `docs/superpowers/specs/2026-04-25-split-test-projects-design.md` | то же |

## Тест-план

1. `dotnet build` проходит без ошибок после удаления `TestRepeat.cs` и замены `[Theory]` → `[Fact]`
2. `dotnet test tests/FastIntegrationTests.Tests.IntegreSQL` запускает 223 теста (был бы 223×N)
3. `ClassScaleManager.AddScaleClasses(3)` генерирует файлы → `dotnet build` → `dotnet test --list-tests` показывает 3×223 тестов
4. `ClassScaleManager.RemoveScaleClasses()` → файлы удалены → `dotnet build` → 223 теста снова
5. Полный прогон BenchmarkRunner завершается успешно, HTML-отчёт содержит 42 точки данных
