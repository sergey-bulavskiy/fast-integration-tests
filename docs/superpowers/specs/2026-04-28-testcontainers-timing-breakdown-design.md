# Дизайн: детализация времени Testcontainers + фикс stacked bar

**Дата:** 2026-04-28  
**Статус:** Утверждён

## Цель

Добавить в бенчмарк-отчёт честное разложение времени по компонентам:
1. Исправить существующий stacked bar (сейчас показывает суммы, а не per-test average — баг с отрицательной бизнес-логикой)
2. Добавить два новых графика специфично для Testcontainers: "один контейнер" и "весь прогон"

## Контекст

### Проблема с текущим stacked bar

`biz = elapsedSeconds − migrationSeconds − resetSeconds` — математически неверно:  
`elapsedSeconds` — wall-clock, а `migrationSeconds`/`resetSeconds` — суммы по всем потокам.  
При 8 потоках для Testcontainers: `biz = max(0, 296 − 941 − 106) = 0` — бизнес-логика всегда 0.

### Что сейчас замеряется

| Подход | `##BENCH[migration]=` | `##BENCH[reset]=` |
|---|---|---|
| IntegreSQL | создание шаблонной БД (1× на процесс) | `RemoveDatabase` — возврат клона в пул (per test) |
| Respawn | `CREATE DATABASE + MigrateAsync` (1× на класс) | `Respawn.ResetAsync` DELETE (per test) |
| Testcontainers | `MigrateAsync` (per test) | `EnsureDeleted` (per test) |

### Что НЕ замеряется (добавляем)

- IntegreSQL: клонирование БД (`CreateDatabaseGetConnectionString`, per test) → `##BENCH[clone]=`
- Testcontainers: старт контейнера (`ContainerFixture.InitializeAsync`, per class) → `##BENCH[container]=`
- Respawn: старт контейнера (1× на процесс в `RespawnContainerManager`) → `##BENCH[container]=`

## Изменения

### 1. Новые `##BENCH` маркеры — тест-инфраструктура

**`##BENCH[clone]=`** — IntegreSQL, время клонирования БД из шаблона.

`AppServiceTestBase.InitializeAsync()` и `ComponentTestBase.InitializeAsync()`:
```csharp
var sw = Stopwatch.StartNew();
_connectionString = await _initializer.CreateDatabaseGetConnectionString<ShopDbContext>(
    IntegresSqlDefaults.SeedingOptions);
sw.Stop();
BenchmarkLogger.Write("clone", sw.ElapsedMilliseconds);
```

**`##BENCH[container]=`** — Testcontainers, старт контейнера.

`ContainerFixture.InitializeAsync()` — обернуть создание network + `StartAsync()`:
```csharp
var sw = Stopwatch.StartNew();
_network = new NetworkBuilder().Build();
await _network.CreateAsync();
_container = new PostgreSqlBuilder()...Build();
await _container.StartAsync();
sw.Stop();
BenchmarkLogger.Write("container", sw.ElapsedMilliseconds);
```

**`##BENCH[container]=`** — Respawn, старт контейнера.

`RespawnContainerManager` — обернуть запуск контейнера (1× на процесс через Lazy).

### 2. Модель данных (`BenchmarkResult`)

Добавить два свойства:
```csharp
public double ContainerSeconds { get; init; }
public double CloneSeconds { get; init; }
```

### 3. `TestRunner.ParseBenchLines()`

Добавить два новых case в парсер `##BENCH[X]=` строк:
- `##BENCH[container]=` → суммировать в `containerMs`
- `##BENCH[clone]=` → суммировать в `cloneMs`

Вернуть все 4 значения (+ существующие migration, reset).

### 4. HTML-шаблон — три изменения

#### 4a. Фикс существующего stacked bar

**Старое название:** "Состав времени — минимум и максимум миграций"  
**Новое название:** "Измеренный overhead на тест (мс)"

Формула пересчёта:
```js
const testCount = classScale * BASE_TEST_COUNT;
const toMsPerTest = (seconds) => (seconds * 1000 / testCount).toFixed(1);
```

Слои (только ненулевые значения для каждого подхода):
- `container` (amortized): Testcontainers + Respawn
- `migration`: все три
- `clone`: только IntegreSQL
- `reset`: все три

Бизнес-логику не показываем — она не измеряется честно.  
X-ось: 17 мig и 117 мig (как сейчас).

#### 4b. Новая карта: "Testcontainers — один контейнер (мс)"

Константа: `TESTCONTAINERS_BASE_CLASSES = 14` (7 сущностей × 2 вида теста).

Значения:
```js
const containerCount = classScale * TESTCONTAINERS_BASE_CLASSES;
const testsPerContainer = BASE_TEST_COUNT / TESTCONTAINERS_BASE_CLASSES;
const containerStartMs = containerSeconds * 1000 / containerCount;
const migTotalMs = (migrationSeconds * 1000 / testCount) * testsPerContainer;
const resetTotalMs = (resetSeconds * 1000 / testCount) * testsPerContainer;
```

Стэкнутый бар в мс. X-ось: 17 мig / 117 мig.  
Слои: `container_start` | `migrations (×N)` | `resets (×N)`

Смысл: "вот сколько стоит поднять один контейнер и прогнать через него все его тесты — только измеренный overhead".

#### 4c. Новая карта: "Testcontainers — весь прогон (с)"

Стэкнутый бар в **секундах**. X-ось: 17 мig / 117 мig.  
Слои: `containerSeconds` | `migrationSeconds` | `resetSeconds`  
Поверх — отдельная линия `elapsedSeconds` (wall-clock) как dataset типа `line` на той же оси Y.

Смысл: показывает как `migrationSeconds` растёт с числом миграций, тогда как `resetSeconds` остаётся плоским.

## Затрагиваемые файлы

| Файл | Изменение |
|---|---|
| `tests/.../IntegreSQL/Base/AppServiceTestBase.cs` | `##BENCH[clone]=` в `InitializeAsync` |
| `tests/.../IntegreSQL/Base/ComponentTestBase.cs` | `##BENCH[clone]=` в `InitializeAsync` |
| `tests/.../Testcontainers/Fixtures/ContainerFixture.cs` | `##BENCH[container]=` в `InitializeAsync` |
| `tests/.../Respawn/RespawnContainerManager.cs` | `##BENCH[container]=` в Lazy-инициализации |
| `tools/BenchmarkRunner/Models/BenchmarkResult.cs` | +`ContainerSeconds`, +`CloneSeconds` |
| `tools/BenchmarkRunner/Runner/TestRunner.cs` | парсинг `container` и `clone` маркеров |
| `tools/BenchmarkRunner/Report/report-template.html` | три изменения графиков |

## Что остаётся за рамками

- Бизнес-логика как измеримая величина — слишком много допущений
- Время уничтожения контейнера — Ryuk асинхронный, не блокирует тест-поток
- Изменение трёх линейных графиков (Сценарии 1–3) — они корректны
