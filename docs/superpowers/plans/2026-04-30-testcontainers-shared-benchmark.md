# TestcontainersShared: интеграция в BenchmarkRunner — план реализации

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Добавить `TestcontainersShared` как полноценный 4-й подход в BenchmarkRunner — в прогон, в HTML-отчёт (кнопка, цвет, три аналитических графика), и убрать устаревшие пометки «не включён» из `CLAUDE.md`.

**Architecture:** Три точечных добавления в C# (`Program.cs`, `TestRunner.cs`, `ClassScaleManager.cs`) расширяют массивы строк/путей. HTML-шаблон получает новый цвет (фиолетовый), кнопку-тоггл и три JS-функции по образцу существующих Testcontainers-специфических графиков, но с `containerCount = 1` вместо `classScale * 14`. Модели данных (`BenchmarkScenario`, `BenchmarkResult`) не трогаются.

**Tech Stack:** C# 12 / .NET 8, Chart.js 4, vanilla JS, xUnit MaxParallelThreads через CLI-аргумент.

---

### Task 1: C# — добавить TestcontainersShared в массивы BenchmarkRunner

**Files:**
- Modify: `tools/BenchmarkRunner/Program.cs:36`
- Modify: `tools/BenchmarkRunner/Runner/TestRunner.cs:31-36`
- Modify: `tools/BenchmarkRunner/Scale/ClassScaleManager.cs:17-22`

- [ ] **Шаг 1: Добавить подход в `Program.cs`**

В файле `tools/BenchmarkRunner/Program.cs`, строка 36:

```csharp
// было:
var approaches = new[] { "IntegreSQL", "Respawn", "Testcontainers" };
// стало:
var approaches = new[] { "IntegreSQL", "Respawn", "Testcontainers", "TestcontainersShared" };
```

- [ ] **Шаг 2: Добавить проект в `TestRunner.Build()`**

В файле `tools/BenchmarkRunner/Runner/TestRunner.cs`, строки 31–36:

```csharp
var projects = new[]
{
    "tests/FastIntegrationTests.Tests.IntegreSQL",
    "tests/FastIntegrationTests.Tests.Respawn",
    "tests/FastIntegrationTests.Tests.Testcontainers",
    "tests/FastIntegrationTests.Tests.TestcontainersShared",
};
```

- [ ] **Шаг 3: Добавить путь в `ClassScaleManager`**

В файле `tools/BenchmarkRunner/Scale/ClassScaleManager.cs`, строки 17–22:

```csharp
_testProjectPaths =
[
    Path.Combine(repoRoot, "tests", "FastIntegrationTests.Tests.IntegreSQL"),
    Path.Combine(repoRoot, "tests", "FastIntegrationTests.Tests.Respawn"),
    Path.Combine(repoRoot, "tests", "FastIntegrationTests.Tests.Testcontainers"),
    Path.Combine(repoRoot, "tests", "FastIntegrationTests.Tests.TestcontainersShared"),
];
```

- [ ] **Шаг 4: Собрать BenchmarkRunner**

```bash
dotnet build tools/BenchmarkRunner --nologo -v minimal
```

Ожидаемо: `Build succeeded.  0 Error(s)  0 Warning(s)`

- [ ] **Шаг 5: Коммит**

```bash
git add tools/BenchmarkRunner/Program.cs \
        tools/BenchmarkRunner/Runner/TestRunner.cs \
        tools/BenchmarkRunner/Scale/ClassScaleManager.cs
git commit -m "feat: BenchmarkRunner — добавить TestcontainersShared в прогон"
```

---

### Task 2: HTML — инфраструктура (CSS, кнопка, COLORS, APPROACHES)

**Files:**
- Modify: `tools/BenchmarkRunner/Report/report-template.html`

- [ ] **Шаг 1: Добавить CSS для кнопки TestcontainersShared**

В файле `tools/BenchmarkRunner/Report/report-template.html`, найти блок:

```css
    .approach-btn[data-approach="Testcontainers"] { border-color: rgb(239,68,68);   color: rgb(239,68,68);   }
    .approach-btn[data-approach="Testcontainers"].active { background: rgba(239,68,68,0.1);   }
```

Добавить сразу после него:

```css
    .approach-btn[data-approach="TestcontainersShared"] { border-color: rgb(168,85,247); color: rgb(168,85,247); }
    .approach-btn[data-approach="TestcontainersShared"].active { background: rgba(168,85,247,0.1); }
```

- [ ] **Шаг 2: Добавить кнопку-тоггл**

Найти блок `.approach-toggles`:

```html
    <button class="approach-btn active" data-approach="Testcontainers">Testcontainers</button>
  </div>
```

Заменить на:

```html
    <button class="approach-btn active" data-approach="Testcontainers">Testcontainers</button>
    <button class="approach-btn active" data-approach="TestcontainersShared">TestcontainersShared</button>
  </div>
```

- [ ] **Шаг 3: Расширить объект `COLORS`**

Найти:

```js
    const COLORS = {
      IntegreSQL:     { border: 'rgb(59,130,246)',  bg: 'rgba(59,130,246,0.12)'  },
      Respawn:        { border: 'rgb(34,197,94)',   bg: 'rgba(34,197,94,0.12)'   },
      Testcontainers: { border: 'rgb(239,68,68)',   bg: 'rgba(239,68,68,0.12)'   },
    };
```

Заменить на:

```js
    const COLORS = {
      IntegreSQL:           { border: 'rgb(59,130,246)',  bg: 'rgba(59,130,246,0.12)'  },
      Respawn:              { border: 'rgb(34,197,94)',   bg: 'rgba(34,197,94,0.12)'   },
      Testcontainers:       { border: 'rgb(239,68,68)',   bg: 'rgba(239,68,68,0.12)'   },
      TestcontainersShared: { border: 'rgb(168,85,247)',  bg: 'rgba(168,85,247,0.12)'  },
    };
```

- [ ] **Шаг 4: Расширить массив `APPROACHES`**

Найти:

```js
    const APPROACHES = ['IntegreSQL', 'Respawn', 'Testcontainers'];
```

Заменить на:

```js
    const APPROACHES = ['IntegreSQL', 'Respawn', 'Testcontainers', 'TestcontainersShared'];
```

- [ ] **Шаг 5: Коммит**

```bash
git add tools/BenchmarkRunner/Report/report-template.html
git commit -m "feat: report-template — кнопка и цвет TestcontainersShared"
```

---

### Task 3: HTML — три новых карточки и JS-функции для TestcontainersShared

**Files:**
- Modify: `tools/BenchmarkRunner/Report/report-template.html`

- [ ] **Шаг 1: Добавить три HTML-карточки**

Найти блок:

```html
  <div class="card">
    <h2>Testcontainers — весь прогон (с)</h2>
    <p class="subtitle" id="subtitle-whole-run"></p>
    <canvas id="chart-whole-run"></canvas>
  </div>

  <script>
```

Вставить между `</div>` и `<script>`:

```html
  <div class="card">
    <h2>TestcontainersShared — один изолированный тест (мс)</h2>
    <p class="subtitle" id="subtitle-shared-isolated-test"></p>
    <canvas id="chart-shared-isolated-test"></canvas>
  </div>

  <div class="card">
    <h2>TestcontainersShared — суммарный overhead контейнера (мс)</h2>
    <p class="subtitle" id="subtitle-shared-one-container"></p>
    <canvas id="chart-shared-one-container"></canvas>
  </div>

  <div class="card">
    <h2>TestcontainersShared — весь прогон (с)</h2>
    <p class="subtitle" id="subtitle-shared-whole-run"></p>
    <canvas id="chart-shared-whole-run"></canvas>
  </div>

  <script>
```

- [ ] **Шаг 2: Добавить константу `TESTCONTAINERSSHARED_BASE_CLASSES`**

Найти строку:

```js
    const TESTCONTAINERS_BASE_CLASSES = 14; // 7 сущностей × 2 (Service + API)
```

Добавить сразу после неё:

```js
    const TESTCONTAINERSSHARED_BASE_CLASSES = 14; // 7 сущностей × 2 (Service + API)
```

- [ ] **Шаг 3: Добавить `buildSharedIsolatedTestChart`**

Найти функцию `buildWholeRunChart` — скопировать её закрывающую скобку `})();` и вставить сразу после неё:

```js
    (function buildSharedIsolatedTestChart() {
      const MIN_M = 17, MAX_M = 117;
      const rows = data.results
        .filter(r =>
          r.scenario.scenarioName === 'migrations' &&
          r.scenario.approach === 'TestcontainersShared' &&
          (r.scenario.migrationCount === MIN_M || r.scenario.migrationCount === MAX_M)
        )
        .sort((a, b) => a.scenario.migrationCount - b.scenario.migrationCount);
      if (!rows.length) return;

      const classScale = rows[0].scenario.classScale;
      const testCount  = classScale * BASE_TEST_COUNT;

      document.getElementById('subtitle-shared-isolated-test').textContent =
        `Амортизированная стоимость одного теста: контейнер один на весь прогон (${testCount} тестов).` +
        ` Потоков=${rows[0].scenario.maxParallelThreads}.`;

      const labels    = rows.map(r => r.scenario.migrationCount + ' миграций');
      const contData  = rows.map(r => +((r.containerSeconds || 0) * 1000 / testCount).toFixed(1));
      const migData   = rows.map(r => +((r.migrationSeconds || 0) * 1000 / testCount).toFixed(1));
      const resetData = rows.map(r => +((r.resetSeconds     || 0) * 1000 / testCount).toFixed(1));

      new Chart(document.getElementById('chart-shared-isolated-test'), {
        type: 'bar',
        data: {
          labels,
          datasets: [
            { label: 'Старт контейнера (амортизировано)', data: contData,  backgroundColor: 'rgba(251,146,60,0.85)', stack: 'a' },
            { label: 'Миграция (1×)',                     data: migData,   backgroundColor: 'rgba(99,102,241,0.85)',  stack: 'a' },
            { label: 'DROP DATABASE (1×)',                 data: resetData, backgroundColor: 'rgba(250,204,21,0.85)', stack: 'a' },
          ]
        },
        options: {
          responsive: true,
          interaction: { mode: 'index', intersect: false },
          plugins: {
            legend: { position: 'top' },
            tooltip: { callbacks: { label: ctx => ctx.dataset.label + ': ' + ctx.parsed.y + ' мс' } }
          },
          scales: {
            x: { stacked: true },
            y: { stacked: true, title: { display: true, text: 'мс на один тест' }, beginAtZero: true }
          }
        }
      });
    })();
```

- [ ] **Шаг 4: Добавить `buildSharedOneContainerChart`**

Вставить сразу после закрывающей `})();` предыдущей функции:

```js
    (function buildSharedOneContainerChart() {
      const MIN_M = 17, MAX_M = 117;
      const rows = data.results
        .filter(r =>
          r.scenario.scenarioName === 'migrations' &&
          r.scenario.approach === 'TestcontainersShared' &&
          (r.scenario.migrationCount === MIN_M || r.scenario.migrationCount === MAX_M)
        )
        .sort((a, b) => a.scenario.migrationCount - b.scenario.migrationCount);
      if (!rows.length) return;

      const classScale = rows[0].scenario.classScale;
      const testCount  = classScale * BASE_TEST_COUNT;

      document.getElementById('subtitle-shared-one-container').textContent =
        `Суммарный overhead единственного контейнера за весь прогон: 1 старт + ${testCount} миграций + ${testCount} DROP DATABASE.` +
        ` Потоков=${rows[0].scenario.maxParallelThreads}.`;

      const labels    = rows.map(r => r.scenario.migrationCount + ' mig');
      const contData  = rows.map(r => +((r.containerSeconds || 0) * 1000).toFixed(1));
      const migData   = rows.map(r => +((r.migrationSeconds || 0) * 1000).toFixed(1));
      const resetData = rows.map(r => +((r.resetSeconds     || 0) * 1000).toFixed(1));

      new Chart(document.getElementById('chart-shared-one-container'), {
        type: 'bar',
        data: {
          labels,
          datasets: [
            { label: 'Старт контейнера — 1 раз',                           data: contData,  backgroundColor: 'rgba(251,146,60,0.85)', stack: 'a' },
            { label: `Миграции — ${testCount} раз (новая БД на каждый тест)`, data: migData, backgroundColor: 'rgba(99,102,241,0.85)',  stack: 'a' },
            { label: `DROP DATABASE — ${testCount} раз`,                    data: resetData, backgroundColor: 'rgba(250,204,21,0.85)', stack: 'a' },
          ]
        },
        options: {
          responsive: true,
          interaction: { mode: 'index', intersect: false },
          plugins: {
            legend: { position: 'top' },
            tooltip: { callbacks: { label: ctx => ctx.dataset.label + ': ' + ctx.parsed.y + ' мс' } }
          },
          scales: {
            x: { stacked: true, title: { display: true, text: 'Количество миграций' } },
            y: { stacked: true, title: { display: true, text: 'мс' }, beginAtZero: true }
          }
        }
      });
    })();
```

- [ ] **Шаг 5: Добавить `buildSharedWholeRunChart`**

Вставить сразу после закрывающей `})();` предыдущей функции:

```js
    (function buildSharedWholeRunChart() {
      const rows = data.results
        .filter(r => r.scenario.scenarioName === 'migrations' && r.scenario.approach === 'TestcontainersShared')
        .sort((a, b) => a.scenario.migrationCount - b.scenario.migrationCount);
      if (!rows.length) return;

      document.getElementById('subtitle-shared-whole-run').textContent =
        `${rows[0].scenario.classScale * BASE_TEST_COUNT} тестов, потоков=${rows[0].scenario.maxParallelThreads}` +
        ` | суммарное время по всем потокам (с) vs wall-clock`;

      const labels    = rows.map(r => r.scenario.migrationCount + ' mig');
      const contData  = rows.map(r => +((r.containerSeconds || 0).toFixed(1)));
      const migData   = rows.map(r => +((r.migrationSeconds || 0).toFixed(1)));
      const resetData = rows.map(r => +((r.resetSeconds     || 0).toFixed(1)));
      const elapsed   = rows.map(r => +(r.elapsedSeconds.toFixed(1)));

      new Chart(document.getElementById('chart-shared-whole-run'), {
        type: 'bar',
        data: {
          labels,
          datasets: [
            { type: 'bar',  label: 'Контейнер',              data: contData,  backgroundColor: 'rgba(251,146,60,0.85)', stack: 'a' },
            { type: 'bar',  label: 'Миграции',                data: migData,   backgroundColor: 'rgba(99,102,241,0.85)',  stack: 'a' },
            { type: 'bar',  label: 'DROP DATABASE',           data: resetData, backgroundColor: 'rgba(250,204,21,0.85)', stack: 'a' },
            { type: 'line', label: 'Wall-clock (elapsed)',    data: elapsed,
              borderColor: 'rgb(168,85,247)', backgroundColor: 'rgba(168,85,247,0.1)',
              borderWidth: 2, pointRadius: 5, tension: 0.15, fill: false },
          ]
        },
        options: {
          responsive: true,
          interaction: { mode: 'index', intersect: false },
          plugins: {
            legend: { position: 'top' },
            tooltip: { callbacks: { label: ctx => ctx.dataset.label + ': ' + formatSeconds(ctx.parsed.y) } }
          },
          scales: {
            x: { stacked: true, title: { display: true, text: 'Количество миграций' } },
            y: { stacked: true, title: { display: true, text: 'Время (секунды)' }, beginAtZero: true }
          }
        }
      });
    })();
```

- [ ] **Шаг 6: Финальная сборка и проверка**

```bash
dotnet build tools/BenchmarkRunner --nologo -v minimal
```

Ожидаемо: `Build succeeded.  0 Error(s)  0 Warning(s)`

- [ ] **Шаг 7: Коммит**

```bash
git add tools/BenchmarkRunner/Report/report-template.html
git commit -m "feat: report-template — три аналитических графика TestcontainersShared"
```

---

### Task 4: CLAUDE.md — убрать устаревшие пометки, обновить счётчики

**Files:**
- Modify: `CLAUDE.md`

- [ ] **Шаг 1: Убрать «Не включён в BenchmarkRunner» из описания подхода**

Найти строку:

```markdown
- **Не включён в BenchmarkRunner** — добавить по аналогии с Testcontainers, заменив проект и суффикс классов `Container` → `Shared`.
```

Удалить её полностью (строка 95 на текущий момент).

- [ ] **Шаг 2: Убрать «Не включён в BenchmarkRunner» из раздела «Архитектура»**

Найти в строке про `Tests.TestcontainersShared`:

```markdown
... `<Entity>ServiceSharedTests`, `<Entity>sApiSharedTests`. **Не включён в BenchmarkRunner.**
```

Заменить на:

```markdown
... `<Entity>ServiceSharedTests`, `<Entity>sApiSharedTests`.
```

- [ ] **Шаг 3: Обновить заголовок «Три сценария» и счётчик точек данных**

Найти:

```markdown
### Три сценария
```

Оставить без изменений (сценарии как были три).

Найти строку:

```markdown
Итого 42 точки данных (5+5+4 × 3 подхода).
```

Заменить на:

```markdown
Итого 56 точек данных (5+5+4 × 4 подхода).
```

- [ ] **Шаг 4: Обновить описание Benchmark Runner**

Найти:

```markdown
Консольный инструмент для сравнительного бенчмарка трёх подходов по трём сценариям.
```

Заменить на:

```markdown
Консольный инструмент для сравнительного бенчмарка четырёх подходов по трём сценариям.
```

- [ ] **Шаг 5: Коммит**

```bash
git add CLAUDE.md
git commit -m "docs: CLAUDE.md — TestcontainersShared включён в BenchmarkRunner"
```
