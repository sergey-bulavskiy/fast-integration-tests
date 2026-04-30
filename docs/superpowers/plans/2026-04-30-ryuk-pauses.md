# Ryuk Pauses Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Добавить паузы 10s до и после `StartAsync` контейнера в `RespawnContainerManager` и `IntegresSqlContainerManager`, плюс изолированную сеть в Respawn-менеджер; синхронизировать комментарии в `ContainerFixture`.

**Архитектура:** Точечные изменения трёх файлов. Никаких новых классов, типов, тестов. Логика паузы тривиальная — `await Task.Delay(TimeSpan.FromSeconds(10))` до и после `StartAsync`. В `RespawnContainerManager` дополнительно добавляется изолированная сеть через `NetworkBuilder`. Спек: [`docs/superpowers/specs/2026-04-30-ryuk-pauses-design.md`](../specs/2026-04-30-ryuk-pauses-design.md).

**Tech Stack:** .NET 8, xUnit, Testcontainers.PostgreSql 4.x, DotNet.Testcontainers.

---

## File Structure

| Файл | Что меняется |
|---|---|
| `tests/FastIntegrationTests.Tests.Respawn/Infrastructure/RespawnContainerManager.cs` | Добавить `using DotNet.Testcontainers.Builders` и `using DotNet.Testcontainers.Networks`. В `StartAsync`: пауза-до 10s, создание изолированной сети, привязка контейнера к сети, пауза-после 10s. Комментарии — стандартный текст из секции «Стандартные комментарии» ниже. |
| `tests/FastIntegrationTests.Tests.IntegreSQL/Infrastructure/IntegreSQL/IntegresSqlContainerManager.cs` | В `InitializeAsync`: пауза-до 10s в самом начале (до создания сети), пауза-после 10s после `integreSqlContainer.StartAsync()` и до `NpgsqlDatabaseInitializer`. Сеть уже есть. Комментарии — стандартный текст. |
| `tests/FastIntegrationTests.Tests.Testcontainers/Infrastructure/Fixtures/ContainerFixture.cs` | Только комментарии. Привести к стандартному тексту (заменить существующие два блока комментариев перед паузами). Код пауз и сети не трогаем. |

### Стандартные комментарии (использовать дословно во всех трёх файлах)

**До `StartAsync`:**
```csharp
// Пауза ДО старта контейнера. Защищает от хвоста зачистки предыдущего
// процесса (или предыдущей фикстуры, если несколько в одном процессе):
// iptables NAT-правила, освобождение IP в bridge-подсети и хост-портов
// Docker daemon делает АСИНХРОННО после удаления контейнера. docker ps
// уже не показывает Ryuk, но ядро ещё держит стейл-правила. Без этой
// паузы новый bind() ловит "address already in use".
await Task.Delay(TimeSpan.FromSeconds(10));
```

**После `StartAsync`:**
```csharp
// Пауза ПОСЛЕ старта контейнера. await StartAsync() возвращается, когда
// Docker рапортует "процесс в контейнере запущен", но NAT-правила и port
// forwarding на хосте прописываются ещё ~сотни мс. Если первый коннект
// уйдёт в это окно — получит TCP RST до того, как правило вступило в силу.
// Пауза гарантирует, что коннекты пойдут уже по живому NAT.
await Task.Delay(TimeSpan.FromSeconds(10));
```

---

## Task 1: Создать фича-ветку

**Files:** нет

- [ ] **Step 1: Проверить чистоту рабочего дерева**

Run: `git status`
Expected: `working tree clean` либо только untracked в `.vscode/`, `docs/.vscode/`. Если есть uncommitted в файлах из плана — остановиться.

- [ ] **Step 2: Создать ветку**

Run: `git checkout -b fix/ryuk-pauses-everywhere`
Expected: `Switched to a new branch 'fix/ryuk-pauses-everywhere'`

---

## Task 2: Правка RespawnContainerManager

**Files:**
- Modify: `tests/FastIntegrationTests.Tests.Respawn/Infrastructure/RespawnContainerManager.cs`

- [ ] **Step 1: Добавить using-директивы**

В верх файла после строки `using System.Diagnostics;` добавить:

```csharp
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Networks;
```

После: верхушка файла должна выглядеть так:

```csharp
using System.Diagnostics;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Networks;
using Testcontainers.PostgreSql;
```

- [ ] **Step 2: Заменить тело `StartAsync` целиком**

Старое (строки 24-46):

```csharp
    private static async Task<PostgreSqlContainer> StartAsync()
    {
        // Параметры производительности PostgreSQL для тестовой среды.
        // Рекомендованы авторами IntegreSQL:
        // https://github.com/allaboutapps/integresql/blob/master/README.md
        // ⚠ НИКОГДА не переносить в продакшн — при сбое питания возможна потеря данных.
        var container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithCommand(
                // 14 параллельных классов × connection pool — дефолтных 100 не хватает.
                "-c", "max_connections=500",
                "-c", "fsync=off",
                "-c", "synchronous_commit=off",
                "-c", "full_page_writes=off",
                "-c", "shared_buffers=128MB"
            )
            .Build();
        var sw = Stopwatch.StartNew();
        await container.StartAsync();
        sw.Stop();
        BenchmarkLogger.Write("container", sw.ElapsedMilliseconds);
        return container;
    }
```

Новое:

```csharp
    private static async Task<PostgreSqlContainer> StartAsync()
    {
        // Пауза ДО старта контейнера. Защищает от хвоста зачистки предыдущего
        // процесса (или предыдущей фикстуры, если несколько в одном процессе):
        // iptables NAT-правила, освобождение IP в bridge-подсети и хост-портов
        // Docker daemon делает АСИНХРОННО после удаления контейнера. docker ps
        // уже не показывает Ryuk, но ядро ещё держит стейл-правила. Без этой
        // паузы новый bind() ловит "address already in use".
        await Task.Delay(TimeSpan.FromSeconds(10));

        // Изолированная сеть на менеджер. Внутри Respawn-процесса контейнер один,
        // но сеть всё равно полезна: при network.DisposeAsync() Docker убирает
        // все iptables-правила сети атомарно, не оставляя стейлов для следующего
        // процесса. И симметрично с IntegresSqlContainerManager / ContainerFixture.
        var network = new NetworkBuilder().Build();
        await network.CreateAsync();

        // Параметры производительности PostgreSQL для тестовой среды.
        // Рекомендованы авторами IntegreSQL:
        // https://github.com/allaboutapps/integresql/blob/master/README.md
        // ⚠ НИКОГДА не переносить в продакшн — при сбое питания возможна потеря данных.
        var container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithNetwork(network)
            .WithCommand(
                // 14 параллельных классов × connection pool — дефолтных 100 не хватает.
                "-c", "max_connections=500",
                "-c", "fsync=off",
                "-c", "synchronous_commit=off",
                "-c", "full_page_writes=off",
                "-c", "shared_buffers=128MB"
            )
            .Build();
        var sw = Stopwatch.StartNew();
        await container.StartAsync();
        sw.Stop();
        BenchmarkLogger.Write("container", sw.ElapsedMilliseconds);

        // Пауза ПОСЛЕ старта контейнера. await StartAsync() возвращается, когда
        // Docker рапортует "процесс в контейнере запущен", но NAT-правила и port
        // forwarding на хосте прописываются ещё ~сотни мс. Если первый коннект
        // уйдёт в это окно — получит TCP RST до того, как правило вступило в силу.
        // Пауза гарантирует, что коннекты пойдут уже по живому NAT.
        await Task.Delay(TimeSpan.FromSeconds(10));

        return container;
    }
```

Замечание про `Stopwatch`: паузы стоят ДО `Stopwatch.StartNew()` и ПОСЛЕ `Stopwatch.Stop()`. Поэтому время «container» в `BenchmarkLogger.Write` не меняется — это важно для отчёта.

- [ ] **Step 3: Билд проекта**

Run: `dotnet build tests/FastIntegrationTests.Tests.Respawn --nologo -v minimal`
Expected: `Build succeeded.` без warning, exit code 0.

Если падает — типичные причины: забытое `using`, опечатка в `WithNetwork`. Сверить с предыдущим шагом.

- [ ] **Step 4: Локальный коммит**

Run:
```bash
git add tests/FastIntegrationTests.Tests.Respawn/Infrastructure/RespawnContainerManager.cs
git commit -m "fix(respawn): паузы 10s до/после StartAsync + изолированная сеть"
```

---

## Task 3: Правка IntegresSqlContainerManager

**Files:**
- Modify: `tests/FastIntegrationTests.Tests.IntegreSQL/Infrastructure/IntegreSQL/IntegresSqlContainerManager.cs`

- [ ] **Step 1: Добавить пауза-до в начало `InitializeAsync`**

Заменить строки 27-30 (начало метода):

Старое:
```csharp
    private static async Task<IntegresSqlState> InitializeAsync()
    {
        var network = new NetworkBuilder().Build();
        await network.CreateAsync();
```

Новое:
```csharp
    private static async Task<IntegresSqlState> InitializeAsync()
    {
        // Пауза ДО старта контейнера. Защищает от хвоста зачистки предыдущего
        // процесса (или предыдущей фикстуры, если несколько в одном процессе):
        // iptables NAT-правила, освобождение IP в bridge-подсети и хост-портов
        // Docker daemon делает АСИНХРОННО после удаления контейнера. docker ps
        // уже не показывает Ryuk, но ядро ещё держит стейл-правила. Без этой
        // паузы новый bind() ловит "address already in use".
        await Task.Delay(TimeSpan.FromSeconds(10));

        var network = new NetworkBuilder().Build();
        await network.CreateAsync();
```

- [ ] **Step 2: Добавить пауза-после после старта `integreSqlContainer`**

Найти строку `await integreSqlContainer.StartAsync();` (около строки 83).

Сразу после неё, перед `var initializer = new NpgsqlDatabaseInitializer(`, вставить блок:

```csharp
        // Пауза ПОСЛЕ старта контейнера. await StartAsync() возвращается, когда
        // Docker рапортует "процесс в контейнере запущен", но NAT-правила и port
        // forwarding на хосте прописываются ещё ~сотни мс. Если первый коннект
        // уйдёт в это окно — получит TCP RST до того, как правило вступило в силу.
        // Пауза гарантирует, что коннекты пойдут уже по живому NAT.
        await Task.Delay(TimeSpan.FromSeconds(10));

```

После этой правки фрагмент должен выглядеть так:

```csharp
        await integreSqlContainer.StartAsync();

        // Пауза ПОСЛЕ старта контейнера. await StartAsync() возвращается, когда
        // Docker рапортует "процесс в контейнере запущен", но NAT-правила и port
        // forwarding на хосте прописываются ещё ~сотни мс. Если первый коннект
        // уйдёт в это окно — получит TCP RST до того, как правило вступило в силу.
        // Пауза гарантирует, что коннекты пойдут уже по живому NAT.
        await Task.Delay(TimeSpan.FromSeconds(10));

        var initializer = new NpgsqlDatabaseInitializer(
            integreSqlUri: new Uri(
                $"http://localhost:{integreSqlContainer.GetMappedPublicPort(5000)}/api/v1/"),
```

Важно: пауза-после ставится после ВТОРОГО `StartAsync` (`integreSqlContainer`), а не после первого. Иначе между PG и IntegreSQL получится 10s паузы — IntegreSQL зависнет в ожидании PG, который только-только встал. Правильно: дать обоим контейнерам стартануть, потом одну паузу.

- [ ] **Step 3: Билд проекта**

Run: `dotnet build tests/FastIntegrationTests.Tests.IntegreSQL --nologo -v minimal`
Expected: `Build succeeded.` без warning, exit code 0.

- [ ] **Step 4: Локальный коммит**

Run:
```bash
git add tests/FastIntegrationTests.Tests.IntegreSQL/Infrastructure/IntegreSQL/IntegresSqlContainerManager.cs
git commit -m "fix(integresql): паузы 10s до/после старта контейнеров"
```

---

## Task 4: Синхронизация комментариев в ContainerFixture

**Files:**
- Modify: `tests/FastIntegrationTests.Tests.Testcontainers/Infrastructure/Fixtures/ContainerFixture.cs`

- [ ] **Step 1: Заменить комментарий перед паузой-до**

Найти в `InitializeAsync` (около строки 21):

```csharp
        // Предыдущий Ryuk мог не успеть дочистить сеть до начала новой инициализации.
        await Task.Delay(TimeSpan.FromSeconds(10));
```

Заменить на:

```csharp
        // Пауза ДО старта контейнера. Защищает от хвоста зачистки предыдущего
        // процесса (или предыдущей фикстуры, если несколько в одном процессе):
        // iptables NAT-правила, освобождение IP в bridge-подсети и хост-портов
        // Docker daemon делает АСИНХРОННО после удаления контейнера. docker ps
        // уже не показывает Ryuk, но ядро ещё держит стейл-правила. Без этой
        // паузы новый bind() ловит "address already in use".
        await Task.Delay(TimeSpan.FromSeconds(10));
```

- [ ] **Step 2: Заменить комментарий перед паузой-после**

Найти в конце `InitializeAsync` (около строки 68):

```csharp
        // Ryuk от предыдущей фикстуры дочищает сети асинхронно после DisposeAsync,
        // а новый Ryuk не всегда успевает полностью подняться к моменту старта тестов.
        // Пауза даёт время обоим завершить инициализацию/очистку.
        await Task.Delay(TimeSpan.FromSeconds(10));
```

Заменить на:

```csharp
        // Пауза ПОСЛЕ старта контейнера. await StartAsync() возвращается, когда
        // Docker рапортует "процесс в контейнере запущен", но NAT-правила и port
        // forwarding на хосте прописываются ещё ~сотни мс. Если первый коннект
        // уйдёт в это окно — получит TCP RST до того, как правило вступило в силу.
        // Пауза гарантирует, что коннекты пойдут уже по живому NAT.
        await Task.Delay(TimeSpan.FromSeconds(10));
```

- [ ] **Step 3: Билд проекта**

Run: `dotnet build tests/FastIntegrationTests.Tests.Testcontainers --nologo -v minimal`
Expected: `Build succeeded.` без warning, exit code 0.

- [ ] **Step 4: Локальный коммит**

Run:
```bash
git add tests/FastIntegrationTests.Tests.Testcontainers/Infrastructure/Fixtures/ContainerFixture.cs
git commit -m "docs(testcontainers): унифицировать комментарии у пауз вокруг StartAsync"
```

---

## Task 5: Финальная проверка билда

**Files:** нет

- [ ] **Step 1: Билд всего решения**

Run: `dotnet build --nologo -v minimal`
Expected: `Build succeeded.` без warning, exit code 0.

- [ ] **Step 2: Проверить, что тесты обнаруживаются**

Run:
```bash
dotnet test tests/FastIntegrationTests.Tests.Respawn --no-build --list-tests --nologo 2>&1 | grep -c "FastIntegrationTests.Tests.Respawn\."
```
Expected: число > 100 (примерно 195 базовых тестов; точное число зависит от текущего состояния `BenchmarkScaleClasses.cs`).

Run:
```bash
dotnet test tests/FastIntegrationTests.Tests.IntegreSQL --no-build --list-tests --nologo 2>&1 | grep -c "FastIntegrationTests.Tests.IntegreSQL\."
```
Expected: число > 100.

Это не запуск тестов — только дискавери. Подтверждает, что наши правки не сломали структуру тест-классов.

---

## Task 6: Слить в main и запушить

**Files:** нет

- [ ] **Step 1: Переключиться на main и обновить**

Run: `git checkout main && git pull --ff-only origin main`
Expected: `Already up to date.` (или fast-forward без конфликтов).

Если pull выдаст конфликт — остановиться и сообщить пользователю.

- [ ] **Step 2: Squash-merge ветки в main**

По соглашению проекта (CLAUDE.md): фича-ветки сквошатся в один коммит на main.

Run:
```bash
git merge --squash fix/ryuk-pauses-everywhere
```
Expected: изменения закоммичены в индекс, но коммит не создан.

- [ ] **Step 3: Создать squash-коммит**

Run:
```bash
git commit -m "$(cat <<'EOF'
fix: паузы 10s до/после StartAsync во всех container-менеджерах

Бенчмарк падал на m=117 у Respawn сразу после IntegreSQL: Ryuk
исчезает из docker ps, но iptables/NAT очищаются ещё несколько секунд
асинхронно. Новый процесс ловил "address already in use" в этом окне.

Добавлены паузы 10s до и после StartAsync в RespawnContainerManager
и IntegresSqlContainerManager (в ContainerFixture уже были). В Respawn
дополнительно изолированная сеть для симметрии. Комментарии к паузам
унифицированы во всех трёх местах.

Время "container" в BenchmarkLogger не меняется — паузы вне Stopwatch.
Wall-clock прогона удлиняется на ~10 минут (по 20s × 14 точек × 2 подхода).

См. docs/superpowers/specs/2026-04-30-ryuk-pauses-design.md
EOF
)"
```
Expected: коммит создан, `git log -1 --oneline` показывает свежий коммит.

- [ ] **Step 4: Запушить main**

Run: `git push origin main`
Expected: `To https://github.com/...` без ошибок.

- [ ] **Step 5: Удалить локальную ветку**

Run: `git branch -d fix/ryuk-pauses-everywhere`
Expected: `Deleted branch fix/ryuk-pauses-everywhere`.

Если git ругается «not fully merged» — нормально, это последствие squash-merge. Использовать `git branch -D fix/ryuk-pauses-everywhere`.

- [ ] **Step 6: Проверить итоговое состояние**

Run: `git status && git log -1 --stat`
Expected: working tree clean, последний коммит — squash-fix с тремя изменёнными файлами:
- `tests/FastIntegrationTests.Tests.Respawn/Infrastructure/RespawnContainerManager.cs`
- `tests/FastIntegrationTests.Tests.IntegreSQL/Infrastructure/IntegreSQL/IntegresSqlContainerManager.cs`
- `tests/FastIntegrationTests.Tests.Testcontainers/Infrastructure/Fixtures/ContainerFixture.cs`

---

## Self-Review Notes

Покрытие спека:
- ✅ Respawn пауза-до + пауза-после + сеть → Task 2
- ✅ IntegreSQL пауза-до + пауза-после → Task 3
- ✅ ContainerFixture только комментарии → Task 4
- ✅ Без правок WaitForRyukToStop / TestRunner → не трогаем
- ✅ Унифицированные комментарии — в Task 2/3/4 один и тот же текст
- ✅ Билд-проверка (без полного `dotnet test`, по предпочтению пользователя) → Task 5
- ✅ Squash merge + push по соглашению проекта → Task 6
