# TestcontainersShared Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Добавить четвёртый тест-проект `Tests.TestcontainersShared` рядом с тремя существующими (IntegreSQL / Respawn / Testcontainers): контейнер один на процесс, БД создаётся/мигрируется/дропается на каждый тест с явным `NpgsqlConnection.ClearPool` перед `DROP`.

**Architecture:** `SharedContainerManager` (static `Lazy<Task<PostgreSqlContainer>>`) клонирует паттерн `RespawnContainerManager`. `SharedDbHandle` (internal helper) инкапсулирует CREATE/MIGRATE/DROP логику. Базовые классы `SharedServiceTestBase` и `SharedApiTestBase` независимо реализуют `IAsyncLifetime` через `SharedDbHandle`. 14 тест-классов — точная копия из `Tests.Testcontainers` с заменой базового класса и namespace. В `BenchmarkRunner` пока **не** интегрируется.

**Tech Stack:** .NET 8, xUnit, Testcontainers.PostgreSql 4.4.0, Npgsql, EF Core, ASP.NET Core (TestServer)

**Spec:** `docs/superpowers/specs/2026-04-30-testcontainers-shared-design.md`

---

## Соглашения

- После каждой задачи **build чистый** (0 errors, 0 warnings) перед коммитом.
- Полный `dotnet test` **не запускаем** — только `build` и `--list-tests` (см. соглашение в memory).
- Сообщения коммитов — на русском, в стиле существующих: `feat(testcontainers-shared): ...`.
- Все новые `.cs`-файлы — UTF-8 без BOM, LF; `// SPDX-License-Identifier: ...` не нужен (его нет в существующих).
- Каждый файл инфраструктуры — XML-доки на публичных типах и членах (соглашение проекта).

---

### Task 1: Скелет нового проекта

**Files:**
- Create: `tests/FastIntegrationTests.Tests.TestcontainersShared/FastIntegrationTests.Tests.TestcontainersShared.csproj`
- Create: `tests/FastIntegrationTests.Tests.TestcontainersShared/GlobalUsings.cs`
- Create: `tests/FastIntegrationTests.Tests.TestcontainersShared/xunit.runner.json`
- Modify: `FastIntegrationTests.slnx`

- [ ] **Step 1: Создать `.csproj`**

Записать в `tests/FastIntegrationTests.Tests.TestcontainersShared/FastIntegrationTests.Tests.TestcontainersShared.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Testcontainers.PostgreSql" Version="4.4.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="coverlet.collector" Version="6.0.4">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\FastIntegrationTests.Application\FastIntegrationTests.Application.csproj" />
    <ProjectReference Include="..\..\src\FastIntegrationTests.Infrastructure\FastIntegrationTests.Infrastructure.csproj" />
    <ProjectReference Include="..\..\src\FastIntegrationTests.WebApi\FastIntegrationTests.WebApi.csproj" />
    <ProjectReference Include="..\FastIntegrationTests.Tests.Shared\FastIntegrationTests.Tests.Shared.csproj" />
  </ItemGroup>

  <ItemGroup>
    <None Update="xunit.runner.json">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </None>
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Создать `GlobalUsings.cs`**

Записать в `tests/FastIntegrationTests.Tests.TestcontainersShared/GlobalUsings.cs` (точная копия GlobalUsings из `Tests.Testcontainers`, без последней строки `Factories` — у нас её не будет):

```csharp
global using System.Net;
global using System.Net.Http.Json;
global using FastIntegrationTests.Application.DTOs;
global using FastIntegrationTests.Application.Entities;
global using FastIntegrationTests.Application.Enums;
global using FastIntegrationTests.Application.Exceptions;
global using FastIntegrationTests.Application.Interfaces;
global using FastIntegrationTests.Application.Services;
global using FastIntegrationTests.Infrastructure.Data;
global using FastIntegrationTests.Infrastructure.Repositories;
global using FastIntegrationTests.Tests.Infrastructure;
global using FastIntegrationTests.Tests.Infrastructure.Base;
global using FastIntegrationTests.Tests.Infrastructure.WebApp;
global using Microsoft.EntityFrameworkCore;
global using Xunit;
```

- [ ] **Step 3: Создать `xunit.runner.json`**

Записать в `tests/FastIntegrationTests.Tests.TestcontainersShared/xunit.runner.json`:

```json
{
  "$schema": "https://xunit.net/schema/current/xunit.runner.schema.json",
  "parallelizeAssembly": true,
  "parallelizeTestCollections": true,
  "maxParallelThreads": 8
}
```

- [ ] **Step 4: Зарегистрировать проект в `FastIntegrationTests.slnx`**

В `FastIntegrationTests.slnx`, в группу `<Folder Name="/tests/">`, после строки с `Tests.Testcontainers`, добавить:

```xml
    <Project Path="tests/FastIntegrationTests.Tests.TestcontainersShared/FastIntegrationTests.Tests.TestcontainersShared.csproj" />
```

- [ ] **Step 5: Build + verify**

Выполнить:
```bash
dotnet build tests/FastIntegrationTests.Tests.TestcontainersShared
```
Ожидание: **Build succeeded** (0 errors, 0 warnings). Тестов ещё нет — это ожидаемо, проект просто пустой.

- [ ] **Step 6: Commit**

```bash
git add tests/FastIntegrationTests.Tests.TestcontainersShared/ FastIntegrationTests.slnx
git commit -m "feat(testcontainers-shared): скелет нового тест-проекта"
```

---

### Task 2: SharedContainerManager (контейнер на процесс)

**Files:**
- Create: `tests/FastIntegrationTests.Tests.TestcontainersShared/Infrastructure/SharedContainerManager.cs`

- [ ] **Step 1: Создать файл**

Записать в `tests/FastIntegrationTests.Tests.TestcontainersShared/Infrastructure/SharedContainerManager.cs`:

```csharp
using System.Diagnostics;
using Testcontainers.PostgreSql;

namespace FastIntegrationTests.Tests.Infrastructure;

/// <summary>
/// Запускает один PostgreSQL-контейнер один раз на весь процесс.
/// Все тесты разделяют этот контейнер; каждый создаёт свою БД через <see cref="SharedDbHandle"/>.
/// </summary>
/// <remarks>
/// Контейнер не останавливается явно — Ryuk-агент Testcontainers убирает его после завершения процесса.
/// </remarks>
public static class SharedContainerManager
{
    private static readonly Lazy<Task<PostgreSqlContainer>> _container =
        new(() => StartAsync(), LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// Возвращает запущенный контейнер. Первый вызов стартует контейнер;
    /// последующие возвращают кешированный результат.
    /// </summary>
    public static Task<PostgreSqlContainer> GetContainerAsync() => _container.Value;

    private static async Task<PostgreSqlContainer> StartAsync()
    {
        // Ryuk от предыдущего dotnet test (или soak'а) мог не успеть дочистить
        // сеть/контейнеры. На быстрых машинах Docker иначе переиспользует IP до
        // того, как iptables очистит правила → "address already in use".
        await Task.Delay(TimeSpan.FromSeconds(10));

        // Параметры производительности PostgreSQL для тестовой среды.
        // Рекомендованы авторами IntegreSQL:
        // https://github.com/allaboutapps/integresql/blob/master/README.md
        // ⚠ НИКОГДА не переносить в продакшн — при сбое питания возможна потеря данных.
        var container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithCommand(
                // 195 тестов × scale=50 = ~9750 уникальных connection strings →
                // ~9750 пулов в одном процессе. Дефолт max_connections=100 заведомо мало.
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

        // Новому Ryuk нужно успеть полностью подняться, иначе первые тесты
        // могут упереться в незавершённый init.
        await Task.Delay(TimeSpan.FromSeconds(10));

        return container;
    }
}
```

- [ ] **Step 2: Build + verify**

```bash
dotnet build tests/FastIntegrationTests.Tests.TestcontainersShared
```
Ожидание: **Build succeeded** (0 errors, 0 warnings).

- [ ] **Step 3: Commit**

```bash
git add tests/FastIntegrationTests.Tests.TestcontainersShared/Infrastructure/SharedContainerManager.cs
git commit -m "feat(testcontainers-shared): SharedContainerManager (static Lazy контейнера)"
```

---

### Task 3: SharedDbHandle (CREATE/DROP с ClearPool)

**Files:**
- Create: `tests/FastIntegrationTests.Tests.TestcontainersShared/Infrastructure/SharedDbHandle.cs`

- [ ] **Step 1: Создать файл**

Записать в `tests/FastIntegrationTests.Tests.TestcontainersShared/Infrastructure/SharedDbHandle.cs`:

```csharp
using System.Diagnostics;
using Npgsql;

namespace FastIntegrationTests.Tests.Infrastructure;

/// <summary>
/// Внутренний хелпер для жизненного цикла одной тестовой БД на общем контейнере:
/// создание + миграция при старте теста, очистка пула + DROP DATABASE при завершении.
/// </summary>
/// <remarks>
/// Используется обоими базовыми классами <see cref="Base.SharedServiceTestBase"/>
/// и <see cref="Base.SharedApiTestBase"/>, чтобы не дублировать ~25 строк lifecycle.
/// </remarks>
internal sealed class SharedDbHandle
{
    /// <summary>Строка подключения к созданной БД. Доступна после <see cref="CreateAndMigrateAsync"/>.</summary>
    public string ConnectionString { get; private set; } = null!;

    private string _dbName = null!;

    /// <summary>
    /// Создаёт уникальную БД <c>test_{guid}</c> на общем контейнере и применяет миграции EF Core.
    /// При сбое миграции делает best-effort <c>DROP DATABASE</c> и перебрасывает исключение —
    /// xUnit при провале <c>InitializeAsync</c> не вызывает <c>DisposeAsync</c>.
    /// </summary>
    public async Task CreateAndMigrateAsync()
    {
        var container = await SharedContainerManager.GetContainerAsync();
        _dbName = $"test_{Guid.NewGuid():N}";
        var adminCs = container.GetConnectionString();

        await using (var admin = new NpgsqlConnection(adminCs))
        {
            await admin.OpenAsync();
            await using var cmd = admin.CreateCommand();
            cmd.CommandText = $"CREATE DATABASE \"{_dbName}\"";
            await cmd.ExecuteNonQueryAsync();
        }

        ConnectionString = new NpgsqlConnectionStringBuilder(adminCs) { Database = _dbName }
            .ConnectionString;

        var options = new DbContextOptionsBuilder<ShopDbContext>()
            .UseNpgsql(ConnectionString).Options;
        await using var ctx = new ShopDbContext(options);

        var sw = Stopwatch.StartNew();
        try
        {
            await ctx.Database.MigrateAsync();
        }
        catch
        {
            // БД создана, но миграция упала — нужно прибрать, иначе она будет висеть в контейнере
            // до завершения процесса (DisposeAsync xUnit не вызовет).
            try { await DropAsync(); } catch { /* swallow — cleanup best-effort */ }
            throw;
        }
        sw.Stop();
        BenchmarkLogger.Write("migration", sw.ElapsedMilliseconds);
    }

    /// <summary>
    /// Освобождает пул соединений Npgsql и дропает БД через admin-соединение.
    /// </summary>
    public async Task DropAsync()
    {
        var sw = Stopwatch.StartNew();

        // Без ClearPool DROP DATABASE упадёт с "database is being accessed by other users"
        // из-за idle-соединений, висящих в пуле Npgsql.
        await using (var conn = new NpgsqlConnection(ConnectionString))
            NpgsqlConnection.ClearPool(conn);

        var container = await SharedContainerManager.GetContainerAsync();
        await using var admin = new NpgsqlConnection(container.GetConnectionString());
        await admin.OpenAsync();
        await using var cmd = admin.CreateCommand();
        cmd.CommandText = $"DROP DATABASE IF EXISTS \"{_dbName}\"";
        await cmd.ExecuteNonQueryAsync();

        sw.Stop();
        BenchmarkLogger.Write("reset", sw.ElapsedMilliseconds);
    }
}
```

- [ ] **Step 2: Build + verify**

```bash
dotnet build tests/FastIntegrationTests.Tests.TestcontainersShared
```
Ожидание: **Build succeeded**.

- [ ] **Step 3: Commit**

```bash
git add tests/FastIntegrationTests.Tests.TestcontainersShared/Infrastructure/SharedDbHandle.cs
git commit -m "feat(testcontainers-shared): SharedDbHandle (CREATE/MIGRATE/DROP с ClearPool)"
```

---

### Task 4: SharedServiceTestBase (service-уровень)

**Files:**
- Create: `tests/FastIntegrationTests.Tests.TestcontainersShared/Infrastructure/Base/SharedServiceTestBase.cs`

- [ ] **Step 1: Создать файл**

Записать в `tests/FastIntegrationTests.Tests.TestcontainersShared/Infrastructure/Base/SharedServiceTestBase.cs`:

```csharp
namespace FastIntegrationTests.Tests.Infrastructure.Base;

/// <summary>
/// Базовый класс для интеграционных тестов сервисного уровня через TestcontainersShared.
/// Контейнер общий на процесс (<see cref="SharedContainerManager"/>);
/// БД создаётся, мигрируется и дропается на каждый тест.
/// </summary>
public abstract class SharedServiceTestBase : IAsyncLifetime
{
    private readonly SharedDbHandle _db = new();

    /// <summary>Контекст тестовой БД. Доступен после <see cref="InitializeAsync"/>.</summary>
    protected ShopDbContext Context { get; private set; } = null!;

    /// <inheritdoc />
    public virtual async Task InitializeAsync()
    {
        await _db.CreateAndMigrateAsync();
        Context = new ShopDbContext(new DbContextOptionsBuilder<ShopDbContext>()
            .UseNpgsql(_db.ConnectionString).Options);
    }

    /// <inheritdoc />
    public virtual async Task DisposeAsync()
    {
        await Context.DisposeAsync();
        await _db.DropAsync();
    }
}
```

- [ ] **Step 2: Build + verify**

```bash
dotnet build tests/FastIntegrationTests.Tests.TestcontainersShared
```
Ожидание: **Build succeeded**.

- [ ] **Step 3: Commit**

```bash
git add tests/FastIntegrationTests.Tests.TestcontainersShared/Infrastructure/Base/SharedServiceTestBase.cs
git commit -m "feat(testcontainers-shared): SharedServiceTestBase для service-уровня"
```

---

### Task 5: SharedApiTestBase (HTTP-уровень)

**Files:**
- Create: `tests/FastIntegrationTests.Tests.TestcontainersShared/Infrastructure/Base/SharedApiTestBase.cs`

- [ ] **Step 1: Создать файл**

Записать в `tests/FastIntegrationTests.Tests.TestcontainersShared/Infrastructure/Base/SharedApiTestBase.cs`:

```csharp
namespace FastIntegrationTests.Tests.Infrastructure.Base;

/// <summary>
/// Базовый класс для интеграционных тестов HTTP-уровня через TestcontainersShared.
/// Контейнер общий на процесс; БД и TestServer создаются на каждый тест.
/// </summary>
public abstract class SharedApiTestBase : IAsyncLifetime
{
    private readonly SharedDbHandle _db = new();
    private TestWebApplicationFactory _factory = null!;

    /// <summary>HTTP-клиент для обращений к тестируемому API.</summary>
    protected HttpClient Client { get; private set; } = null!;

    /// <inheritdoc />
    public virtual async Task InitializeAsync()
    {
        await _db.CreateAndMigrateAsync();
        _factory = new TestWebApplicationFactory(_db.ConnectionString);
        Client = _factory.CreateClient();
    }

    /// <inheritdoc />
    public virtual async Task DisposeAsync()
    {
        Client?.Dispose();
        if (_factory is not null)
            // PhysicalFilesWatcher внутри WebApplicationFactory может бросить NullReferenceException
            // при диспозе под высокой параллельностью — баг в ASP.NET Core FileSystemWatcher.
            try { await _factory.DisposeAsync(); } catch (NullReferenceException) { }
        await _db.DropAsync();
    }
}
```

- [ ] **Step 2: Build + verify**

```bash
dotnet build tests/FastIntegrationTests.Tests.TestcontainersShared
```
Ожидание: **Build succeeded**.

- [ ] **Step 3: Commit**

```bash
git add tests/FastIntegrationTests.Tests.TestcontainersShared/Infrastructure/Base/SharedApiTestBase.cs
git commit -m "feat(testcontainers-shared): SharedApiTestBase для HTTP-уровня"
```

---

## Tasks 6–12: Копирование тест-классов (по одной задаче на сущность)

Все 7 задач (6..12) идут по одинаковому шаблону. Дальше описан общий **алгоритм трансформации**, потом — таблица с конкретными именами файлов.

### Алгоритм трансформации одного файла

**Источник** — соответствующий файл в `tests/FastIntegrationTests.Tests.Testcontainers/<Folder>/`.
**Цель** — `tests/FastIntegrationTests.Tests.TestcontainersShared/<Folder>/`.

#### Для service-теста (`<Entity>ServiceContainerTests.cs` → `<Entity>ServiceSharedTests.cs`):

1. Скопировать содержимое исходного файла дословно.
2. **namespace** в первой строке:
   - `FastIntegrationTests.Tests.Testcontainers.<Folder>` → `FastIntegrationTests.Tests.TestcontainersShared.<Folder>`
3. **summary над классом** — заменить:
   - `/// Каждый тест создаёт изолированную БД в реальном контейнере PostgreSQL.`
   - На:
   - `/// Контейнер общий на процесс, БД создаётся/мигрируется/дропается на каждый тест.`
4. **Объявление класса** — заменить:
   - `public class <Entity>ServiceContainerTests : IAsyncLifetime, IClassFixture<ContainerFixture>`
   - На:
   - `public class <Entity>ServiceSharedTests : SharedServiceTestBase`
5. **Удалить** поля:
   - `private readonly ContainerFixture _fixture;`
   - `private ShopDbContext _context = null!;`
6. **Удалить** конструктор целиком (вместе с XML-доками над ним):
   - `public <Entity>ServiceContainerTests(ContainerFixture fixture) => _fixture = fixture;`
7. **InitializeAsync** — заменить тело и сигнатуру:
   - Старое:
     ```csharp
     public async Task InitializeAsync()
     {
         _context = await new TestDbFactory(_fixture).CreateAsync();
         Sut = new <Service>(new <Repo>(_context));
     }
     ```
   - Новое:
     ```csharp
     public override async Task InitializeAsync()
     {
         await base.InitializeAsync();
         Sut = new <Service>(new <Repo>(Context));
     }
     ```
   - Если конструкторов сервисов несколько (например, у `Order` берётся `OrderRepository` + `ProductRepository`), заменить `_context` → `Context` во всех вхождениях.
8. **DisposeAsync** — удалить целиком (вместе с `/// <inheritdoc />` над ним). Базовый класс уже дропает БД.
9. **Заменить** оставшиеся вхождения `_context` (если есть в `[Fact]`-методах) на `Context`.

#### Для API-теста (`<Entity>sApiContainerTests.cs` → `<Entity>sApiSharedTests.cs`):

1. Скопировать содержимое.
2. **namespace** — как выше.
3. **summary над классом** — заменить:
   - `/// Каждый тест создаёт изолированную БД в реальном контейнере PostgreSQL и отдельный TestServer.`
   - На:
   - `/// Контейнер общий на процесс; БД и TestServer создаются на каждый тест.`
4. **Объявление класса** — заменить:
   - `public class <Entity>sApiContainerTests : ContainerApiTestBase`
   - На:
   - `public class <Entity>sApiSharedTests : SharedApiTestBase`
5. **Удалить** конструктор целиком (вместе с XML-доками):
   - `public <Entity>sApiContainerTests(ContainerFixture fixture) : base(fixture) { }`
6. **Не трогать** `[Fact]`-методы, private helpers (`CreateXxxAsync`), и т.д. — они работают через `Client` из базового класса.

#### Контрольный чек-лист правильной трансформации (применить к каждому файлу):

- В файле **не осталось** упоминаний `ContainerFixture`, `TestDbFactory`, `_fixture`, `_context` (для service-тестов).
- Класс наследует именно `SharedServiceTestBase` или `SharedApiTestBase`.
- В service-тестах `InitializeAsync` начинается с `await base.InitializeAsync();`.
- Список `[Fact]`-методов идентичен исходному (та же сигнатура, то же тело).
- Helper-методы (`CreateCategoryAsync`, и т.п.) скопированы дословно.

---

### Таблица «откуда — куда»

| Сущность | Service src → dst | API src → dst |
|---|---|---|
| Categories | `Categories/CategoryServiceContainerTests.cs` → `Categories/CategoryServiceSharedTests.cs` | `Categories/CategoriesApiContainerTests.cs` → `Categories/CategoriesApiSharedTests.cs` |
| Customers | `Customers/CustomerServiceContainerTests.cs` → `Customers/CustomerServiceSharedTests.cs` | `Customers/CustomersApiContainerTests.cs` → `Customers/CustomersApiSharedTests.cs` |
| Discounts | `Discounts/DiscountServiceContainerTests.cs` → `Discounts/DiscountServiceSharedTests.cs` | `Discounts/DiscountsApiContainerTests.cs` → `Discounts/DiscountsApiSharedTests.cs` |
| Orders | `Orders/OrderServiceContainerTests.cs` → `Orders/OrderServiceSharedTests.cs` | `Orders/OrdersApiContainerTests.cs` → `Orders/OrdersApiSharedTests.cs` |
| Products | `Products/ProductServiceContainerTests.cs` → `Products/ProductServiceSharedTests.cs` | `Products/ProductsApiContainerTests.cs` → `Products/ProductsApiSharedTests.cs` |
| Reviews | `Reviews/ReviewServiceContainerTests.cs` → `Reviews/ReviewServiceSharedTests.cs` | `Reviews/ReviewsApiContainerTests.cs` → `Reviews/ReviewsApiSharedTests.cs` |
| Suppliers | `Suppliers/SupplierServiceContainerTests.cs` → `Suppliers/SupplierServiceSharedTests.cs` | `Suppliers/SuppliersApiContainerTests.cs` → `Suppliers/SuppliersApiSharedTests.cs` |

### Шаги для каждой из задач 6–12

Используя данные строки таблицы (например, для Task 6 — Categories), выполнить:

- [ ] **Step 1: Создать service-файл** по алгоритму выше (Service src → dst).
- [ ] **Step 2: Создать API-файл** по алгоритму выше (API src → dst).
- [ ] **Step 3: Build + verify**

```bash
dotnet build tests/FastIntegrationTests.Tests.TestcontainersShared
```
Ожидание: **Build succeeded** (0 errors, 0 warnings).

- [ ] **Step 4: Commit**

```bash
git add tests/FastIntegrationTests.Tests.TestcontainersShared/<Folder>/
git commit -m "feat(testcontainers-shared): тесты для <Entity>"
```

Где `<Folder>` = `Categories` / `Customers` / ... и `<Entity>` соответственно.

### Распределение задач 6–12

- **Task 6:** Categories
- **Task 7:** Customers
- **Task 8:** Discounts
- **Task 9:** Orders
- **Task 10:** Products
- **Task 11:** Reviews
- **Task 12:** Suppliers

---

### Task 13: PowerShell скрипт `run-testcontainers-shared.ps1`

**Files:**
- Create: `run-testcontainers-shared.ps1`

- [ ] **Step 1: Создать скрипт**

Записать в `run-testcontainers-shared.ps1` (корень репозитория):

```powershell
param(
    [int]$Threads = 4
)

$start = Get-Date

Write-Host "TestcontainersShared | threads=$Threads"
dotnet test tests/FastIntegrationTests.Tests.TestcontainersShared `
    -- xUnit.MaxParallelThreads=$Threads

$elapsed = (Get-Date) - $start
Write-Host "`nВремя выполнения: $($elapsed.ToString('mm\:ss\.fff'))"
```

- [ ] **Step 2: Commit**

```bash
git add run-testcontainers-shared.ps1
git commit -m "feat(testcontainers-shared): PowerShell-скрипт для запуска"
```

---

### Task 14: Обновить `CLAUDE.md`

**Files:**
- Modify: `CLAUDE.md`

- [ ] **Step 1: Раздел «Запустить отдельный подход»**

В разделе с `# Запустить один подход`, после строк с тремя существующими подходами, добавить **четвёртую** строку:

```bash
dotnet test tests/FastIntegrationTests.Tests.TestcontainersShared
```

И в разделе «# Запустить тесты отдельного класса», после трёх существующих примеров, добавить:

```bash
dotnet test tests/FastIntegrationTests.Tests.TestcontainersShared --filter "FullyQualifiedName~CategoryServiceSharedTests"
```

- [ ] **Step 2: Раздел «Три подхода к изоляции» — переименовать и расширить**

Найти заголовок `#### Три подхода к изоляции` и заменить на `#### Четыре подхода к изоляции`.

После блока `**Testcontainers** (...)` добавить новый блок:

```markdown
**TestcontainersShared** (`SharedServiceTestBase` / `SharedApiTestBase`):
- Один контейнер PostgreSQL **на весь процесс** — `SharedContainerManager` (static Lazy).
- Каждый тест создаёт свою БД (`CREATE DATABASE test_{guid}`), применяет миграции, сбрасывает пул и дропает БД.
- Миграции применяются **на каждый тест** (как у обычного Testcontainers).
- TestServer и HttpClient создаются **на каждый тест**.
- Тесты внутри одного класса выполняются **параллельно** (изоляция per-test).
- **Зачем нужен:** показать вклад «контейнер per-class vs per-process» в чистом виде — всё остальное идентично подходу Testcontainers.
```

- [ ] **Step 3: Таблица «Сравнение по ключевым параметрам»**

Найти таблицу с заголовком `| | IntegreSQL | Respawn | Testcontainers |` и заменить на расширенную:

```markdown
| | IntegreSQL | Respawn | Testcontainers | TestcontainersShared |
|---|---|---|---|---|
| Контейнер | 1 на процесс (PG + IntegreSQL) | 1 на процесс | 1 на класс | 1 на процесс |
| Миграции | 1 раз на процесс | 1 раз на класс | на каждый тест | на каждый тест |
| Сброс данных | возврат клона в пул (recreate) | DELETE по FK-порядку | новая БД `test_{guid}` + `MigrateAsync`, потом `EnsureDeleted` | новая БД `test_{guid}` + `MigrateAsync`, потом `ClearPool` + `DROP` |
| TestServer (API) | новый на каждый тест | 1 на класс | новый на каждый тест | новый на каждый тест |
| Параллелизм внутри класса | да | нет | да | да |
```

- [ ] **Step 4: Раздел «PowerShell скрипты»**

В блоке кода с тремя существующими скриптами добавить четвёртую строку перед блоком с примером переопределения:

```powershell
.\run-testcontainers-shared.ps1
```

- [ ] **Step 5: Раздел «Архитектура»**

После абзаца, начинающегося с `- **Tests.Testcontainers** ...`, добавить новый абзац:

```markdown
- **Tests.TestcontainersShared** (`tests/FastIntegrationTests.Tests.TestcontainersShared/`) — интеграционные тесты с **общим** контейнером PostgreSQL на процесс (~195 тестов). Инфраструктура: `SharedServiceTestBase`, `SharedApiTestBase`, `SharedContainerManager` (static Lazy), `SharedDbHandle`. Те же 7 папок и 14 классов с суффиксом `Shared`: `<Entity>ServiceSharedTests`, `<Entity>sApiSharedTests`.

  **Не интегрирован в BenchmarkRunner** — добавлен как отдельный подход для будущего сравнения. Запускается через `run-testcontainers-shared.ps1` или `dotnet test`. Чтобы включить в бенчмарк, нужно расширить три места: `tools/BenchmarkRunner/Program.cs:approaches`, `tools/BenchmarkRunner/Runner/TestRunner.cs:Build()`, `tools/BenchmarkRunner/Scale/ClassScaleManager.cs:_testProjectPaths`.
```

- [ ] **Step 6: Раздел «Идеи для развития бенчмарка»**

В конец раздела добавить новый подпункт:

```markdown
### Включить TestcontainersShared в BenchmarkRunner

Сейчас новый подход (`Tests.TestcontainersShared`) живёт отдельно от бенчмарка. Чтобы он появился в HTML-отчёте 4-й линией, нужно: добавить `"TestcontainersShared"` в массив `approaches` в `Program.cs`, путь к проекту в `_testProjectPaths` (`ClassScaleManager.cs`) и в `Build()` (`TestRunner.cs`); проверить `BaseTestCount`, если число тестов изменится.
```

- [ ] **Step 7: Commit**

```bash
git add CLAUDE.md
git commit -m "docs: добавить TestcontainersShared в CLAUDE.md (4-й подход, не в бенчмарке)"
```

---

### Task 15: Финальная верификация

**Files:** none (только проверки)

- [ ] **Step 1: Build всего solution**

```bash
dotnet build
```
Ожидание: **Build succeeded** для всех проектов (0 errors, 0 warnings). Особое внимание — не сломал ли spelling/неймспейсы в трёх существующих тест-проектах.

- [ ] **Step 2: `--list-tests` показывает 195 тестов**

```bash
dotnet test tests/FastIntegrationTests.Tests.TestcontainersShared --list-tests
```

Подсчитать число строк-тестов:
```bash
dotnet test tests/FastIntegrationTests.Tests.TestcontainersShared --list-tests 2>/dev/null | grep -c "FastIntegrationTests.Tests.TestcontainersShared\."
```
Ожидание: **195**.

Если число отличается — найти расхождение по сравнению с Testcontainers:
```bash
diff <(dotnet test tests/FastIntegrationTests.Tests.Testcontainers --list-tests 2>/dev/null | grep "Tests.Testcontainers\." | sed 's/Testcontainers/X/g' | sort) \
     <(dotnet test tests/FastIntegrationTests.Tests.TestcontainersShared --list-tests 2>/dev/null | grep "Tests.TestcontainersShared\." | sed 's/TestcontainersShared/X/g' | sort)
```

Должно вернуть пустой diff (имена методов и классов идентичны после нормализации суффикса).

- [ ] **Step 3: Smoke-test (вручную, отдаётся пользователю)**

> **Не запускать** автоматически — `dotnet test` (полный прогон) запрещён правилом проекта.

Передать пользователю команду:
```powershell
.\run-testcontainers-shared.ps1 -Threads 4
```

И что проверить вручную в выводе:
- Все 195 тестов зелёные.
- Маркер `##BENCH[container]=` встречается **ровно один раз** (доказательство — контейнер общий).
- `##BENCH[migration]=` — 195 раз.
- `##BENCH[reset]=` — 195 раз.
- В `docker ps` во время прогона — ровно один `postgres` и один `ryuk` (можно проверить отдельным окном терминала).

- [ ] **Step 4: Финальный коммит — нет**

Если предыдущие задачи всё закоммитили, отдельный финальный коммит не нужен. Если осталось что-то незастейдженное — сделать `git status` и решить, что попало в commits, а что осталось.

---

## Self-Review

**Spec coverage** — соответствие со spec'ом по разделам:

- ✓ Архитектура (структура проекта) — Task 1
- ✓ Зависимости .csproj — Task 1
- ✓ Регистрация в .slnx — Task 1
- ✓ SharedContainerManager — Task 2
- ✓ SharedDbHandle — Task 3
- ✓ SharedServiceTestBase — Task 4
- ✓ SharedApiTestBase — Task 5
- ✓ 14 тест-классов (7 папок × 2) — Tasks 6–12
- ✓ run-testcontainers-shared.ps1 — Task 13
- ✓ Документация в CLAUDE.md (6 правок) — Task 14
- ✓ Финальная верификация (build, --list-tests = 195) — Task 15
- ✓ Не трогаем BenchmarkRunner — соблюдено (нет ни одной задачи на изменения в `tools/BenchmarkRunner/`)
- ✓ Edge cases (best-effort drop при сбое миграции, swallow в DropAsync, пауза в StartAsync) — встроены в код Tasks 2 и 3.

**Placeholder scan** — нет «TBD»/«similar to N»/«TODO»/«add validation»/etc. Все шаги содержат код или точные команды.

**Type consistency**:
- `SharedDbHandle.ConnectionString`, `_dbName`, `CreateAndMigrateAsync`, `DropAsync` — употреблены одинаково в Tasks 3, 4, 5.
- `SharedContainerManager.GetContainerAsync()` — используется в Task 2 и Task 3.
- `Context` (свойство `SharedServiceTestBase`) — используется в алгоритме трансформации service-тестов.
- `Client` (свойство `SharedApiTestBase`) — используется в API-тестах.
- `BenchmarkLogger.Write` — единый интерфейс из `Tests.Shared`, используется в Tasks 2 и 3 с ключами `container` / `migration` / `reset` (те же, что и в Respawn/IntegreSQL).
