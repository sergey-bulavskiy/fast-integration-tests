# PostgreSQL tmpfs Toggle (IntegreSQL) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Добавить ручной переключатель (закомментированная строка `WithTmpfsMount` + развёрнутый объясняющий комментарий) в `IntegresSqlContainerManager.cs`, чтобы на машинах с медленным/троттлящимся диском можно было перенести data-каталог PostgreSQL в RAM и убрать массовые `Npgsql EndOfStream` на больших scale бенчмарка.

**Architecture:** Чисто инфраструктурная правка одного файла + дополнение `CLAUDE.md` коротким troubleshooting-разделом. Переключатель сделан комментарием, а не env-флагом — это редкий ручной кейс, runtime-конфиг тут избыточен. Никакого нового кода: лишь комментарий и закомментированная строка билдера в существующей цепочке `PostgreSqlBuilder()`.

**Tech Stack:** Testcontainers .NET (`PostgreSqlBuilder.WithTmpfsMount`), `MccSoft.IntegreSql.EF`, .NET 8.

**Spec:** `docs/superpowers/specs/2026-04-30-postgres-tmpfs-toggle-design.md`

---

## File Structure

| Файл | Что меняется |
|---|---|
| `tests/FastIntegrationTests.Tests.Shared/Infrastructure/IntegreSQL/IntegresSqlContainerManager.cs` | Добавить блок-комментарий + закомментированную `.WithTmpfsMount("/var/lib/postgresql/data")` в цепочку `PostgreSqlBuilder()` |
| `CLAUDE.md` | Добавить короткий troubleshooting-блок в секцию IntegreSQL про переключатель |

Никаких новых файлов. Никаких изменений в `Tests.Respawn`, `Tests.Testcontainers`, `Tests.TestcontainersShared`, `BenchmarkRunner`, README.md.

---

## Pre-flight

- [ ] **Step 0: убедиться, что репозиторий чист и мы на feature-ветке**

Project workflow (см. CLAUDE.md): фича-ветки мержатся в `main` через squash. Если сейчас на `main`:

```bash
git status
git checkout -b feat/postgres-tmpfs-toggle
```

Ожидаемо: рабочее дерево чистое (или содержит только этот план как untracked, тогда продолжаем — план закоммитится первым же коммитом). Текущая ветка после команды — `feat/postgres-tmpfs-toggle`.

---

## Task 1: Добавить tmpfs-переключатель в IntegresSqlContainerManager

**Files:**
- Modify: `tests/FastIntegrationTests.Tests.Shared/Infrastructure/IntegreSQL/IntegresSqlContainerManager.cs` (около строк 40–47, перед существующим блоком про параметры PG)

### Контекст файла

В `IntegresSqlContainerManager.InitializeAsync()` есть блок:

```csharp
// Параметры производительности PostgreSQL для тестовой среды.
// Рекомендованы авторами IntegreSQL в официальном docker-compose.yml:
// https://github.com/allaboutapps/integresql/blob/master/README.md
// Совокупно дают ~30% ускорение за счёт отключения гарантий долговечности WAL,
// которые необходимы в продакшне, но бессмысленны для эфемерных тестовых данных.
// ⚠ НИКОГДА не переносить в продакшн — при сбое питания/краше возможна потеря данных.
var pgContainer = new PostgreSqlBuilder()
    .WithImage("postgres:16-alpine")
    .WithNetwork(network)
    .WithNetworkAliases("postgres")
    .WithCommand(
        ...
```

Цель: вставить новый блок-комментарий ПЕРЕД существующим (он останется на своём месте, добавляется ВЫШЕ), и закомментированную строку `.WithTmpfsMount(...)` ВНУТРИ цепочки билдера сразу после `.WithImage("postgres:16-alpine")`.

- [ ] **Step 1: Применить правку — добавить блок-комментарий и закомментированную строку**

Использовать Edit-операцию: заменить кусок от строки с `// Параметры производительности PostgreSQL` через `.WithImage("postgres:16-alpine")` и `.WithNetwork(network)` на новый текст.

```csharp
// ── Опциональный tmpfs для data-каталога PostgreSQL ──────────────────────────
// Если на твоей машине бенчмарк IntegreSQL падает массой Npgsql `EndOfStream`
// на больших scale (s=50+), а disk latency в момент падения уходит в полку —
// расскоментируй WithTmpfsMount ниже.
//
// Симптом: ~thousands × `Npgsql.NpgsqlException : Exception while reading
// from stream` → inner `EndOfStreamException : Attempted to read past the
// end of the stream`. Стек у всех — на стадии `NpgsqlConnection.Open`
// внутри `WaitUntilDatabaseIsCreated` или первого запроса EF.
//
// Причина: при scale=50 за один прогон делается ~9800 CREATE DATABASE
// TEMPLATE. Это синхронные filesystem-операции — fsync=off их не ускоряет
// (он отключает только sync для WAL, а не для data-файлов). На медленном
// или троттлящемся диске PostgreSQL держит ACCESS EXCLUSIVE lock на
// pg_database дольше Npgsql Connection Timeout (15с) — параллельные
// коннекты ловят TCP RST на Open.
//
// Решение: tmpfs кладёт data-файлы в page cache (RAM). CREATE DATABASE
// становится memcpy. Стоит ~1–1.5 GB RAM при scale=50; данные эфемерны
// (контейнер уничтожается между прогонами) — для тестов это безопасно.
//
// Подробнее: docs/superpowers/specs/2026-04-30-postgres-tmpfs-toggle-design.md

// Параметры производительности PostgreSQL для тестовой среды.
// Рекомендованы авторами IntegreSQL в официальном docker-compose.yml:
// https://github.com/allaboutapps/integresql/blob/master/README.md
// Совокупно дают ~30% ускорение за счёт отключения гарантий долговечности WAL,
// которые необходимы в продакшне, но бессмысленны для эфемерных тестовых данных.
// ⚠ НИКОГДА не переносить в продакшн — при сбое питания/краше возможна потеря данных.
var pgContainer = new PostgreSqlBuilder()
    .WithImage("postgres:16-alpine")
    // .WithTmpfsMount("/var/lib/postgresql/data")  // см. блок-комментарий выше
    .WithNetwork(network)
    .WithNetworkAliases("postgres")
```

Точная Edit-операция — заменить старое:

```
        // Параметры производительности PostgreSQL для тестовой среды.
        // Рекомендованы авторами IntegreSQL в официальном docker-compose.yml:
        // https://github.com/allaboutapps/integresql/blob/master/README.md
        // Совокупно дают ~30% ускорение за счёт отключения гарантий долговечности WAL,
        // которые необходимы в продакшне, но бессмысленны для эфемерных тестовых данных.
        // ⚠ НИКОГДА не переносить в продакшн — при сбое питания/краше возможна потеря данных.
        var pgContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithNetwork(network)
```

на новое:

```
        // ── Опциональный tmpfs для data-каталога PostgreSQL ──────────────────────────
        // Если на твоей машине бенчмарк IntegreSQL падает массой Npgsql `EndOfStream`
        // на больших scale (s=50+), а disk latency в момент падения уходит в полку —
        // расскоментируй WithTmpfsMount ниже.
        //
        // Симптом: ~thousands × `Npgsql.NpgsqlException : Exception while reading
        // from stream` → inner `EndOfStreamException : Attempted to read past the
        // end of the stream`. Стек у всех — на стадии `NpgsqlConnection.Open`
        // внутри `WaitUntilDatabaseIsCreated` или первого запроса EF.
        //
        // Причина: при scale=50 за один прогон делается ~9800 CREATE DATABASE
        // TEMPLATE. Это синхронные filesystem-операции — fsync=off их не ускоряет
        // (он отключает только sync для WAL, а не для data-файлов). На медленном
        // или троттлящемся диске PostgreSQL держит ACCESS EXCLUSIVE lock на
        // pg_database дольше Npgsql Connection Timeout (15с) — параллельные
        // коннекты ловят TCP RST на Open.
        //
        // Решение: tmpfs кладёт data-файлы в page cache (RAM). CREATE DATABASE
        // становится memcpy. Стоит ~1–1.5 GB RAM при scale=50; данные эфемерны
        // (контейнер уничтожается между прогонами) — для тестов это безопасно.
        //
        // Подробнее: docs/superpowers/specs/2026-04-30-postgres-tmpfs-toggle-design.md

        // Параметры производительности PostgreSQL для тестовой среды.
        // Рекомендованы авторами IntegreSQL в официальном docker-compose.yml:
        // https://github.com/allaboutapps/integresql/blob/master/README.md
        // Совокупно дают ~30% ускорение за счёт отключения гарантий долговечности WAL,
        // которые необходимы в продакшне, но бессмысленны для эфемерных тестовых данных.
        // ⚠ НИКОГДА не переносить в продакшн — при сбое питания/краше возможна потеря данных.
        var pgContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            // .WithTmpfsMount("/var/lib/postgresql/data")  // см. блок-комментарий выше
            .WithNetwork(network)
```

Сохранить отступ — 8 пробелов перед `//` (метод-уровень внутри класса).

- [ ] **Step 2: Проверить, что закомментированная строка действительно закомментирована (не активна по умолчанию)**

```bash
grep -n "WithTmpfsMount" tests/FastIntegrationTests.Tests.Shared/Infrastructure/IntegreSQL/IntegresSqlContainerManager.cs
```

Ожидаемый вывод: одна строка вида `<line>:            // .WithTmpfsMount("/var/lib/postgresql/data")  // см. блок-комментарий выше` — обязательно с `//` в начале (после отступа). Если нашлась активная строка без `//` в начале — откатить и перепроверить шаг 1.

- [ ] **Step 3: Билд `Tests.Shared` (там лежит изменённый файл)**

```bash
dotnet build tests/FastIntegrationTests.Tests.Shared
```

Ожидаемо: `Build succeeded.`, 0 Warning(s), 0 Error(s).

- [ ] **Step 4: Билд `Tests.IntegreSQL` (зависит от Shared, использует изменённый менеджер)**

```bash
dotnet build tests/FastIntegrationTests.Tests.IntegreSQL
```

Ожидаемо: `Build succeeded.`, 0 Warning(s), 0 Error(s).

- [ ] **Step 5: Билд `Tests.NUnit.IntegreSQL` (тоже использует тот же менеджер через Tests.Shared)**

```bash
dotnet build tests/FastIntegrationTests.Tests.NUnit.IntegreSQL
```

Ожидаемо: `Build succeeded.`, 0 Warning(s), 0 Error(s).

- [ ] **Step 6: Закоммитить инфра-правку**

```bash
git add tests/FastIntegrationTests.Tests.Shared/Infrastructure/IntegreSQL/IntegresSqlContainerManager.cs
git commit -m "feat: ручной tmpfs-переключатель для PostgreSQL в IntegresSqlContainerManager

Закомментированный WithTmpfsMount + блок-комментарий с описанием симптома
(массовый Npgsql EndOfStream на больших scale бенчмарка) и причины
(disk-троттлинг под массой CREATE DATABASE TEMPLATE). Раскомментирование
переносит data-каталог PG в page cache (RAM) и убирает диск из критического
пути.

Спек: docs/superpowers/specs/2026-04-30-postgres-tmpfs-toggle-design.md"
```

Ожидаемо: коммит создан, `git status` показывает чистое дерево (кроме этого плана и спека, которые тоже untracked — их закоммитим отдельно).

---

## Task 2: Добавить troubleshooting-заметку в CLAUDE.md

**Files:**
- Modify: `CLAUDE.md` (добавить блок ПОСЛЕ описания подхода IntegreSQL — в районе строки 75)

### Контекст файла

В `CLAUDE.md` есть секция «Четыре подхода к изоляции», начинающаяся с описания IntegreSQL:

```
**IntegreSQL** (`AppServiceTestBase` / `ComponentTestBase`):
- Один пара контейнеров (PostgreSQL + IntegreSQL) на весь процесс — `IntegresSqlContainerManager` (static Lazy).
- Миграции применяются **один раз** как шаблонная БД `"shop-default"`.
- Каждый тест получает **клон шаблона** и после завершения возвращает его в пул с пометкой «пересоздать» (`DropDatabaseOnRemove=true`).
- Тесты полностью изолированы — параллелизм внутри класса возможен.

**Respawn** (`RespawnServiceTestBase` / `RespawnApiTestBase`):
```

Вставляем заметку между блоком IntegreSQL и блоком Respawn — пятым буллетом или отдельным абзацем.

- [ ] **Step 1: Применить правку — добавить буллет про tmpfs-переключатель**

Edit-операция: найти строку `- Тесты полностью изолированы — параллелизм внутри класса возможен.` и заменить её на ту же строку + новый буллет.

Старое:

```
- Тесты полностью изолированы — параллелизм внутри класса возможен.

**Respawn** (`RespawnServiceTestBase` / `RespawnApiTestBase`):
```

Новое:

```
- Тесты полностью изолированы — параллелизм внутри класса возможен.
- Если бенчмарк IntegreSQL падает на больших scale (s=50+) с массой `Npgsql EndOfStream`, твоя машина под disk-троттлингом. В `IntegresSqlContainerManager.cs` рядом с `pgContainer` закомментирован `WithTmpfsMount("/var/lib/postgresql/data")` — расскоментируй и попробуй (PostgreSQL data-каталог уезжает в RAM, ~1–1.5 GB).

**Respawn** (`RespawnServiceTestBase` / `RespawnApiTestBase`):
```

- [ ] **Step 2: Глазами проверить, что блок встал между IntegreSQL и Respawn**

```bash
grep -n -B1 -A1 "WithTmpfsMount" CLAUDE.md
```

Ожидаемо: одна строка с упоминанием `WithTmpfsMount` находится в секции про IntegreSQL, перед заголовком `**Respawn**`.

- [ ] **Step 3: Закоммитить документацию**

```bash
git add CLAUDE.md
git commit -m "docs: troubleshooting-заметка про tmpfs-переключатель в CLAUDE.md

Короткий буллет в секции IntegreSQL: симптом (Npgsql EndOfStream на
больших scale) → решение (расскоментировать WithTmpfsMount). Подробное
описание причины — в самом IntegresSqlContainerManager.cs."
```

---

## Task 3: Закоммитить спек и план

**Files:**
- Add: `docs/superpowers/specs/2026-04-30-postgres-tmpfs-toggle-design.md`
- Add: `docs/superpowers/plans/2026-04-30-postgres-tmpfs-toggle.md`

- [ ] **Step 1: Закоммитить документацию процесса**

```bash
git add docs/superpowers/specs/2026-04-30-postgres-tmpfs-toggle-design.md \
        docs/superpowers/plans/2026-04-30-postgres-tmpfs-toggle.md
git commit -m "docs: spec+plan — tmpfs-переключатель для PostgreSQL в IntegreSQL"
```

Ожидаемо: коммит создан, `git status` чистый по этой ветке (не считая других несвязанных untracked-файлов из родительской истории).

---

## Final verification

- [ ] **Билд всего решения с нуля — на случай скрытых регрессий по транзитивным зависимостям**

```bash
dotnet build
```

Ожидаемо: `Build succeeded.`, 0 Errors. Warnings допустимы только если они уже были до правки (то есть число warnings не выросло — глазами сверить со свежим логом, если есть, или принять как есть, если их не было).

- [ ] **Финальная проверка состояния репозитория**

```bash
git log --oneline -5
git status
```

Ожидаемо: три новых коммита (`feat: ручной tmpfs-переключатель...`, `docs: troubleshooting-заметка...`, `docs: spec+plan...`), `git status` чистый.

---

## Что НЕ делаем (для самопроверки)

- Не активируем tmpfs (строка остаётся закомментированной).
- Не указываем `size=...` параметр.
- Не трогаем `RespawnContainerManager`, `ContainerFixture`, `SharedContainerManager`.
- Не трогаем `BenchmarkRunner`.
- Не запускаем сами тесты (`dotnet test`) — только `dotnet build`. Этого достаточно: правка не меняет runtime-поведение по умолчанию.
- Не пытаемся squash-мержить в `main` — это решение пользователя после ревью.
