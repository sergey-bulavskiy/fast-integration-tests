---
title: 117 миграций как базовое состояние репозитория
date: 2026-04-26
status: approved
---

# 117 миграций как базовое состояние репозитория

## Проблема

Scenario 2 (масштаб числа тестов) и Scenario 3 (параллелизм) бенчмарка запускаются при
17 миграциях. При таком числе все три подхода инициализируют схему примерно одинаково быстро
и главное преимущество IntegreSQL — миграции применяются **один раз** как шаблон — не
проявляется. График не показывает ожидаемого разрыва.

## Решение

Добавляем 100 настоящих EF Core миграций в репозиторий: итого 117 миграций как постоянная база.
BenchmarkRunner теряет Add/Remove механизм и получает Hide/Restore для Scenario 1 (единственного,
где число миграций варьируется). Scenarios 2 и 3 работают на 117 без манипуляций.

---

## 1. Генерируем 100 миграций через `dotnet ef migrations add`

**Путь:** `src/FastIntegrationTests.Infrastructure/Migrations/`

100 настоящих EF Core миграций с реалистичными бизнес-именами, сгенерированных через
`dotnet ef migrations add`. Каждая пара `.cs` + `.Designer.cs` создаётся штатным инструментом:
Designer.cs содержит полный snapshot модели (как у любой реальной миграции).

**Именование** — бизнес-фичи, не имеющие отношения к бенчмарку в названии:

Примеры нечётных (CREATE + seed): `AddShippingRates`, `CreateProductVariants`,
`AddLoyaltyPoints`, `CreateWarehouseSlots`, `AddReturnRequests`, ...

Примеры чётных (DROP предыдущей): `DropShippingRates`, `DropProductVariants`, ...

**Схема чередования SQL-содержимого:**

| Индекс | Up | Down |
|---|---|---|
| Нечётный N | `CREATE TABLE load_tmp_NNN` + INSERT 300 строк | `DROP TABLE IF EXISTS load_tmp_NNN` |
| Чётный N | `DROP TABLE IF EXISTS load_tmp_{N-1}` | `CREATE TABLE load_tmp_{N-1}` + INSERT 300 строк |

**Почему create/drop:**

После применения всех 100 миграций (100 — чётное) схема остаётся чистой: последняя чётная
удаляет таблицу предыдущей нечётной. `dotnet ef database update` не оставляет лишних таблиц.

При скрытии нечётного числа миграций (точки 42 и 92 в Scenario 1) в тестовой БД останется одна
транзиентная таблица (напр., `load_tmp_075`). Допустимо — тестовые БД эфемерны.

**Нагрузка:**
- Нечётные: `~10–20 мс` (DDL + 300 INSERT)
- Чётные: `~2–5 мс` (DDL)

**Временны́е метки:**

Все 100 миграций используют timestamp-prefix `20990101` с шестизначным порядковым номером:
`20990101000001_AddShippingRates.cs` — `20990101000100_DropWarehouseSlots.cs`.

Год 2099 гарантирует что они всегда сортируются после любых реальных миграций (2026+).
Имена файлов выглядят как обычные EF Core миграции — никаких "Fake" или суффиксов.

**Как генерировать:**

PowerShell-скрипт `tools/BenchmarkRunner/Scripts/GenerateBenchmarkMigrations.ps1` создаёт
100 пар файлов напрямую (без `dotnet ef migrations add`):

1. `.cs` — код миграции с `migrationBuilder.Sql(...)` для create/drop
2. `.Designer.cs` — копия `BuildTargetModel` из последней реальной миграции (#17
   `20260421152746_AddCategoryCustomerSupplierReviewDiscount.Designer.cs`),
   только `[Migration("...")]` атрибут меняется. Модель не изменяется — snapshot идентичен.

После генерации: `dotnet build` → убедиться что всё компилируется → коммит.

**Идентификация для HideMigrations:**

`HideMigrations(int count)` скрывает последние `count` миграций по имени файла (алфавитная
сортировка = хронологическая, т.к. имена начинаются с timestamp). Поскольку все 100 benchmark
миграций имеют timestamp `20990101...`, они всегда последние — никакого суффикса не нужно.

---

## 2. MigrationManager: замена Add/Remove на Hide/Restore

**Удаляются:** `AddFakeMigrations(int count)`, `RemoveFakeMigrations()`

**Добавляются:**

```
HideMigrations(int count)
  — сортирует .cs файлы в Migrations/ по имени (алфавитно = хронологически)
  — берёт последние count пар (.cs + .Designer.cs) — это всегда benchmark-миграции (timestamp 2099...)
  — перемещает их в Migrations/__hidden/
  — build вызывает сам вызывающий код (Program.cs)

RestoreHiddenMigrations()
  — перемещает все файлы из __hidden/ обратно в Migrations/
  — если __hidden/ пуст или не существует — no-op
```

**`__hidden/` находится внутри папки Migrations** — рядом с файлами которыми управляет,
путь `src/FastIntegrationTests.Infrastructure/Migrations/__hidden/`.

### Infrastructure.csproj

Добавить exclusion чтобы компилятор игнорировал `__hidden/`:

```xml
<ItemGroup>
  <Compile Remove="Migrations\__hidden\**" />
</ItemGroup>
```

### .gitignore

```
**/Migrations/__hidden/
```

---

## 3. Program.cs: новая структура

**Новая константа:**
```csharp
const int BaseMigrations = 17;   // реальных миграций в репозитории
const int MaxMigrations  = 117;  // с benchmark-миграциями — базовое состояние бенчмарка
```

**Startup cleanup** (восстановить состояние после прерванного прогона):
```csharp
migrationManager.RestoreHiddenMigrations();  // было: RemoveFakeMigrations()
classScaleManager.RemoveScaleClasses();
runner.Build();
```

**Scenario 1 — инверсия логики** (скрывать вместо добавлять):

```
classScaleManager.AddScaleClasses(defaultClassScale); build;
try {
  foreach migrationCount in [17, 42, 67, 92, 117]:
    fakesToHide = MaxMigrations - migrationCount
    try {
      if fakesToHide > 0: migrationManager.HideMigrations(fakesToHide); build;
      foreach approach: RunOrAbort(... migrationCount, defaultThreads, defaultClassScale)
    } finally {
      if fakesToHide > 0: migrationManager.RestoreHiddenMigrations(); build;
    }
} finally { classScaleManager.RemoveScaleClasses(); build; }
```

**Scenario 2 — никаких манипуляций с миграциями** (MigrationCount = MaxMigrations):

```
foreach scale in [1, 5, 10, 20, 50]:
  try {
    if scale > 1: classScaleManager.AddScaleClasses(scale); build;
    foreach approach: RunOrAbort(... MaxMigrations, defaultThreads, scale)
  } finally {
    if scale > 1: classScaleManager.RemoveScaleClasses(); build;
  }
```

**Scenario 3 — никаких манипуляций** (MigrationCount = MaxMigrations):

```
classScaleManager.AddScaleClasses(defaultClassScale); build;
try {
  foreach threads in [1, 2, 4, 8]:
    foreach approach: RunOrAbort(... MaxMigrations, threads, defaultClassScale)
} finally { classScaleManager.RemoveScaleClasses(); build; }
```

**Warmup** — MigrationCount = MaxMigrations (117 миграций уже в репозитории):
```csharp
runner.Warmup(new BenchmarkScenario(approach, "warmup", MaxMigrations, defaultThreads));
```

---

## 4. Документация

**CLAUDE.md и README.md** — таблица сценариев:

| Сценарий | Было | Стало |
|---|---|---|
| 2 — Масштаб числа тестов | 17 миграций, `--threads` | **117 миграций**, `--threads` |
| 3 — Параллелизм | 17 миграций, `--scale` | **117 миграций**, `--scale` |

**report-template.html** — изменений не требует: subtitle Scenarios 2 и 3 уже читают
`migrationCount` из данных динамически.

---

## Затрагиваемые файлы

| Файл | Изменение |
|---|---|
| `src/FastIntegrationTests.Infrastructure/Migrations/20990101000NNN_*.cs` (×200) | новые (100 пар .cs + .Designer.cs) |
| `tools/BenchmarkRunner/Scripts/GenerateBenchmarkMigrations.ps1` | новый (одноразовый скрипт генерации) |
| `src/FastIntegrationTests.Infrastructure/FastIntegrationTests.Infrastructure.csproj` | добавить `<Compile Remove>` |
| `.gitignore` | добавить `**/Migrations/__hidden/` |
| `tools/BenchmarkRunner/Migrations/MigrationManager.cs` | Remove Add/Remove, Add Hide/Restore |
| `tools/BenchmarkRunner/Program.cs` | MaxMigrations, новая структура сценариев |
| `CLAUDE.md` | Scenarios 2 и 3: 17 → 117 миграций |
| `README.md` | то же |

## Тест-план

1. `dotnet build` проходит после добавления 100 benchmark-миграций
2. `dotnet test tests/FastIntegrationTests.Tests.IntegreSQL --list-tests` показывает 223 теста (без изменений)
3. `migrationManager.HideMigrations(25)` → `__hidden/` содержит 50 файлов (25 пар), `dotnet build` → без ошибок
4. `migrationManager.RestoreHiddenMigrations()` → `__hidden/` пуст, `dotnet build` → без ошибок
5. `dotnet test tests/FastIntegrationTests.Tests.IntegreSQL` проходит (117 миграций применяются, схема чистая)
6. BenchmarkRunner стартует: Scenario 1 точка migrationCount=17 скрывает 100 миграций, пересобирается, запускает тесты
