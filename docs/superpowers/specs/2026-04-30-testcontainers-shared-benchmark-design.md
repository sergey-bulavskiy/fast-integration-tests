# TestcontainersShared: интеграция в BenchmarkRunner

## Цель

Добавить `TestcontainersShared` как полноценный 4-й подход в `tools/BenchmarkRunner`.
После этого HTML-отчёт будет сравнивать все четыре подхода на одних сценариях.

---

## Что меняется

### 1. `tools/BenchmarkRunner/Program.cs`

```diff
-var approaches = new[] { "IntegreSQL", "Respawn", "Testcontainers" };
+var approaches = new[] { "IntegreSQL", "Respawn", "Testcontainers", "TestcontainersShared" };
```

`SetTotalRuns` пересчитывается автоматически из длины массивов — ничего дополнительно трогать не нужно.

### 2. `tools/BenchmarkRunner/Runner/TestRunner.cs` → `Build()`

```diff
 var projects = new[]
 {
     "tests/FastIntegrationTests.Tests.IntegreSQL",
     "tests/FastIntegrationTests.Tests.Respawn",
     "tests/FastIntegrationTests.Tests.Testcontainers",
+    "tests/FastIntegrationTests.Tests.TestcontainersShared",
 };
```

### 3. `tools/BenchmarkRunner/Scale/ClassScaleManager.cs`

```diff
 _testProjectPaths =
 [
     Path.Combine(repoRoot, "tests", "FastIntegrationTests.Tests.IntegreSQL"),
     Path.Combine(repoRoot, "tests", "FastIntegrationTests.Tests.Respawn"),
     Path.Combine(repoRoot, "tests", "FastIntegrationTests.Tests.Testcontainers"),
+    Path.Combine(repoRoot, "tests", "FastIntegrationTests.Tests.TestcontainersShared"),
 ];
```

`ClassScaleManager.DiscoverTestClasses` фильтрует `[Fact]`/`[Theory]` и ищет `FixtureType` через паттерн `public\s+\w+\((\w+Fixture)\s+fixture\)`.
TestcontainersShared использует `IAsyncLifetime` без фикстуры — `FixtureType = null`, классы масштабируются как `public class Foo_2 : Foo {}`. Это корректно.

### 4. `tools/BenchmarkRunner/Report/report-template.html`

#### 4а. CSS — новый цвет для TestcontainersShared

Добавить правило:
```css
.approach-btn[data-approach="TestcontainersShared"] { border-color: rgb(168,85,247); color: rgb(168,85,247); }
.approach-btn[data-approach="TestcontainersShared"].active { background: rgba(168,85,247,0.1); }
```

#### 4б. HTML — новая кнопка-тоггл

```html
<button class="approach-btn active" data-approach="TestcontainersShared">TestcontainersShared</button>
```

#### 4в. JS — константа `COLORS`

```diff
 const COLORS = {
   IntegreSQL:     { border: 'rgb(59,130,246)',  bg: 'rgba(59,130,246,0.12)'  },
   Respawn:        { border: 'rgb(34,197,94)',   bg: 'rgba(34,197,94,0.12)'   },
   Testcontainers: { border: 'rgb(239,68,68)',   bg: 'rgba(239,68,68,0.12)'   },
+  TestcontainersShared: { border: 'rgb(168,85,247)', bg: 'rgba(168,85,247,0.12)' },
 };
```

#### 4г. JS — константа `APPROACHES`

```diff
-const APPROACHES = ['IntegreSQL', 'Respawn', 'Testcontainers'];
+const APPROACHES = ['IntegreSQL', 'Respawn', 'Testcontainers', 'TestcontainersShared'];
```

#### 4д. Timing chart — `updateApproachVisibility`

Функция фильтрует `timingItems` по `visibleApproaches` — добавлять ничего не нужно, она универсальная.

#### 4е. Три новых карточки HTML для TestcontainersShared

По аналогии с существующими для Testcontainers:
- `chart-shared-isolated-test` — "TestcontainersShared — один изолированный тест (мс)"
- `chart-shared-one-container` — "TestcontainersShared — один контейнер (мс)"
- `chart-shared-whole-run` — "TestcontainersShared — весь прогон (с)"

Разница с Testcontainers:
- `containerCount` считается иначе: у Testcontainers — один контейнер **на класс** (`classScale * 14`); у TestcontainersShared — **один контейнер на весь прогон** (просто `1`). Это ключевая аналитическая разница, которую и показывают эти графики.
- `TESTCONTAINERSSHARED_BASE_CLASSES = 14` (то же, что `TESTCONTAINERS_BASE_CLASSES`).

#### 4ж. JS — три новые JS-функции

```js
const TESTCONTAINERSSHARED_BASE_CLASSES = 14;

function buildSharedIsolatedTestChart() { ... }  // аналог buildIsolatedTestChart
function buildSharedOneContainerChart()  { ... }  // аналог buildOneContainerChart
function buildSharedWholeRunChart()      { ... }  // аналог buildWholeRunChart
```

В `buildSharedIsolatedTestChart` (амортизированная стоимость одного теста):
- фильтр по `approach === 'TestcontainersShared'`
- `contData = containerSeconds * 1000 / testCount` (контейнер один, стоимость раскидывается по всем тестам)
- `migData = migrationSeconds * 1000 / testCount`
- `resetData = resetSeconds * 1000 / testCount`
- subtitle: «Амортизированная стоимость одного теста при одном контейнере на прогон»

В `buildSharedOneContainerChart` (суммарный overhead одного контейнера за всю жизнь):
- фильтр по `approach === 'TestcontainersShared'`
- `contData = containerSeconds * 1000` (один старт — не делим)
- `migData = migrationSeconds * 1000` (суммарно все миграции)
- `resetData = resetSeconds * 1000` (суммарно все сбросы)
- ось Y в мс, показывает общий overhead всего прогона в разбивке; аналог OneContainer Testcontainers, но при N=1 контейнере
- subtitle объясняет: один контейнер на весь прогон — весь overhead через него

В `buildSharedWholeRunChart`:
- фильтр по `approach === 'TestcontainersShared'`
- состав тот же: контейнер + миграции + reset + wall-clock overlay

---

## `CLAUDE.md` — правки

1. Удалить из раздела «Идеи для развития» пункт «Не включён в BenchmarkRunner — добавить по аналогии...» (это уже сделано).
2. Убрать из описания **TestcontainersShared** фразу «Не включён в BenchmarkRunner».
3. Добавить его в таблицу сравнения рядом с остальными тремя подходами (если не было).

---

## Затронутые файлы

### Изменяются
- `tools/BenchmarkRunner/Program.cs`
- `tools/BenchmarkRunner/Runner/TestRunner.cs`
- `tools/BenchmarkRunner/Scale/ClassScaleManager.cs`
- `tools/BenchmarkRunner/Report/report-template.html`
- `CLAUDE.md`

### Не изменяются
- Все тест-проекты (`tests/**`)
- Модели `BenchmarkScenario`, `BenchmarkResult`, `BenchmarkReport`
- `MigrationManager`, `ReportGenerator`
- PowerShell-скрипты

---

## Проверка

1. `dotnet build tools/BenchmarkRunner` — 0 errors.
2. Открыть `report-template.html` в браузере с тестовым JSON (4 подхода) — убедиться, что кнопка фиолетовая, линия рисуется, три новых графика отображаются.
3. Вручную запустить `dotnet run --project tools/BenchmarkRunner -- --scale 1 --threads 4` и убедиться, что TestcontainersShared появляется в прогоне.
