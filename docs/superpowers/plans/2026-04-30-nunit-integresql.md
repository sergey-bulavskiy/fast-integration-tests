# NUnit-пример инфраструктуры IntegreSQL — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Добавить мини-проект `FastIntegrationTests.Tests.NUnit.IntegreSQL` с двумя портированными классами (`ProductServiceTests`, `ProductsApiTests`) на NUnit; общую IntegreSQL-инфру вынести в `Tests.Shared`.

**Architecture:** Сначала рефакторинг `Tests.Shared` — переносим фреймворк-агностичные `IntegresSqlContainerManager` / `IntegresSqlDefaults` / `IntegresSqlState` из xUnit-проекта в Shared, чтобы оба проекта (xUnit и NUnit) использовали один и тот же код. Затем создаём NUnit-проект со своими `[SetUp]`/`[TearDown]`-базами и портируем 2 тест-класса.

**Tech Stack:** .NET 8, NUnit 4.2.2, NUnit3TestAdapter 4.6.0, Microsoft.NET.Test.Sdk 17.12.0, MccSoft.IntegreSql.EF 0.12.2, Testcontainers.PostgreSql 4.4.0.

**Spec:** `docs/superpowers/specs/2026-04-30-nunit-integresql-design.md`.

---

## File map

**Создаём:**
- `tests/FastIntegrationTests.Tests.NUnit.IntegreSQL/FastIntegrationTests.Tests.NUnit.IntegreSQL.csproj`
- `tests/FastIntegrationTests.Tests.NUnit.IntegreSQL/GlobalUsings.cs`
- `tests/FastIntegrationTests.Tests.NUnit.IntegreSQL/AssemblyInfo.cs`
- `tests/FastIntegrationTests.Tests.NUnit.IntegreSQL/Infrastructure/Base/AppServiceTestBase.cs`
- `tests/FastIntegrationTests.Tests.NUnit.IntegreSQL/Infrastructure/Base/ComponentTestBase.cs`
- `tests/FastIntegrationTests.Tests.NUnit.IntegreSQL/Products/ProductServiceTests.cs`
- `tests/FastIntegrationTests.Tests.NUnit.IntegreSQL/Products/ProductsApiTests.cs`

**Переносим (xUnit-проект → Shared):**
- `Infrastructure/IntegreSQL/IntegresSqlContainerManager.cs`
- `Infrastructure/IntegreSQL/IntegresSqlDefaults.cs` (плюс меняем `internal` → `public`)
- `Infrastructure/IntegreSQL/IntegresSqlState.cs`

**Модифицируем:**
- `tests/FastIntegrationTests.Tests.Shared/FastIntegrationTests.Tests.Shared.csproj`
- `tests/FastIntegrationTests.Tests.Shared/GlobalUsings.cs`
- `tests/FastIntegrationTests.Tests.IntegreSQL/FastIntegrationTests.Tests.IntegreSQL.csproj`
- `FastIntegrationTests.slnx`
- `CLAUDE.md`

---

## Task 1: Перенос общей IntegreSQL-инфры в `Tests.Shared`

**Files:**
- Move: `tests/FastIntegrationTests.Tests.IntegreSQL/Infrastructure/IntegreSQL/IntegresSqlContainerManager.cs` → `tests/FastIntegrationTests.Tests.Shared/Infrastructure/IntegreSQL/IntegresSqlContainerManager.cs`
- Move: `tests/FastIntegrationTests.Tests.IntegreSQL/Infrastructure/IntegreSQL/IntegresSqlDefaults.cs` → `tests/FastIntegrationTests.Tests.Shared/Infrastructure/IntegreSQL/IntegresSqlDefaults.cs`
- Move: `tests/FastIntegrationTests.Tests.IntegreSQL/Infrastructure/IntegreSQL/IntegresSqlState.cs` → `tests/FastIntegrationTests.Tests.Shared/Infrastructure/IntegreSQL/IntegresSqlState.cs`
- Modify: `tests/FastIntegrationTests.Tests.Shared/FastIntegrationTests.Tests.Shared.csproj`
- Modify: `tests/FastIntegrationTests.Tests.Shared/GlobalUsings.cs`
- Modify: `tests/FastIntegrationTests.Tests.IntegreSQL/FastIntegrationTests.Tests.IntegreSQL.csproj`

- [ ] **Step 1: Создать целевую папку и физически переместить три файла**

```bash
mkdir -p tests/FastIntegrationTests.Tests.Shared/Infrastructure/IntegreSQL
git mv tests/FastIntegrationTests.Tests.IntegreSQL/Infrastructure/IntegreSQL/IntegresSqlContainerManager.cs \
       tests/FastIntegrationTests.Tests.Shared/Infrastructure/IntegreSQL/IntegresSqlContainerManager.cs
git mv tests/FastIntegrationTests.Tests.IntegreSQL/Infrastructure/IntegreSQL/IntegresSqlDefaults.cs \
       tests/FastIntegrationTests.Tests.Shared/Infrastructure/IntegreSQL/IntegresSqlDefaults.cs
git mv tests/FastIntegrationTests.Tests.IntegreSQL/Infrastructure/IntegreSQL/IntegresSqlState.cs \
       tests/FastIntegrationTests.Tests.Shared/Infrastructure/IntegreSQL/IntegresSqlState.cs
rmdir tests/FastIntegrationTests.Tests.IntegreSQL/Infrastructure/IntegreSQL
```

Namespace в файлах — `FastIntegrationTests.Tests.Infrastructure.IntegreSQL` — НЕ меняется (он не привязан к проекту, ничего править не нужно).

- [ ] **Step 2: Сделать `IntegresSqlDefaults` публичным**

В `tests/FastIntegrationTests.Tests.Shared/Infrastructure/IntegreSQL/IntegresSqlDefaults.cs`:

Заменить:
```csharp
internal static class IntegresSqlDefaults
{
    /// <summary>Параметры сидирования шаблонной БД магазина.</summary>
    internal static readonly DatabaseSeedingOptions<ShopDbContext> SeedingOptions =
```

на:
```csharp
public static class IntegresSqlDefaults
{
    /// <summary>Параметры сидирования шаблонной БД магазина.</summary>
    public static readonly DatabaseSeedingOptions<ShopDbContext> SeedingOptions =
```

(Класс и поле — оба `public`, чтобы наследник `AppServiceTestBase`/`ComponentTestBase` в NUnit-проекте мог использовать.)

- [ ] **Step 3: Обновить `Tests.Shared.csproj` — добавить пакеты, убрать xunit**

Полностью заменить содержимое `tests/FastIntegrationTests.Tests.Shared/FastIntegrationTests.Tests.Shared.csproj` на:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.15" />
    <PackageReference Include="MccSoft.IntegreSql.EF" Version="0.12.2" />
    <PackageReference Include="Testcontainers.PostgreSql" Version="4.4.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\FastIntegrationTests.Application\FastIntegrationTests.Application.csproj" />
    <ProjectReference Include="..\..\src\FastIntegrationTests.Infrastructure\FastIntegrationTests.Infrastructure.csproj" />
    <ProjectReference Include="..\..\src\FastIntegrationTests.WebApi\FastIntegrationTests.WebApi.csproj" />
  </ItemGroup>

</Project>
```

Изменения относительно текущего: убран `<PackageReference Include="xunit" Version="2.9.3" />`, добавлены `MccSoft.IntegreSql.EF` и `Testcontainers.PostgreSql`.

- [ ] **Step 4: Убрать `using Xunit;` из Shared/GlobalUsings.cs**

Полностью заменить `tests/FastIntegrationTests.Tests.Shared/GlobalUsings.cs` на:

```csharp
global using FastIntegrationTests.Infrastructure.Data;
global using Microsoft.EntityFrameworkCore;
```

(Убрана строка `global using Xunit;`.)

- [ ] **Step 5: Убрать дубли пакетов из `Tests.IntegreSQL.csproj`**

Полностью заменить `tests/FastIntegrationTests.Tests.IntegreSQL/FastIntegrationTests.Tests.IntegreSQL.csproj` на:

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
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="xunit" Version="2.9.3" />
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

Изменения относительно текущего:
- Добавлен явный `<PackageReference Include="xunit" Version="2.9.3" />` — раньше его не было (он попадал транзитивно из Shared, где теперь убран).
- Убраны `<PackageReference Include="MccSoft.IntegreSql.EF" />` и `<PackageReference Include="Testcontainers.PostgreSql" />` — теперь транзитивны из Shared.

- [ ] **Step 6: Проверить, что xUnit-проект всё ещё собирается и видит свои тесты**

```bash
dotnet build tests/FastIntegrationTests.Tests.IntegreSQL
dotnet test tests/FastIntegrationTests.Tests.IntegreSQL --list-tests --no-build 2>&1 | head -20
dotnet test tests/FastIntegrationTests.Tests.IntegreSQL --list-tests --no-build 2>&1 | grep -c "FastIntegrationTests.Tests.IntegreSQL\."
```

Ожидаемое: build успешен; в выводе видны методы вида `FastIntegrationTests.Tests.IntegreSQL.Products.ProductServiceTests.GetAllAsync_WhenNoProducts_ReturnsEmptyList`; счётчик ≥ 195 (текущее число тестов в xUnit-проекте).

- [ ] **Step 7: Коммит**

```bash
git add tests/FastIntegrationTests.Tests.Shared tests/FastIntegrationTests.Tests.IntegreSQL
git commit -m "$(cat <<'EOF'
refactor: вынести общую IntegreSQL-инфру в Tests.Shared

IntegresSqlContainerManager / Defaults / State фреймворк-агностичны и
переезжают в Tests.Shared, чтобы будущий NUnit-проект мог переиспользовать
их без копирования. IntegresSqlDefaults становится public. Заодно убрана
мёртвая зависимость на xunit из Tests.Shared (ни один тип в Shared её не
использовал) — пакеты MccSoft/Testcontainers переехали туда же.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 2: Создать каркас `Tests.NUnit.IntegreSQL`

**Files:**
- Create: `tests/FastIntegrationTests.Tests.NUnit.IntegreSQL/FastIntegrationTests.Tests.NUnit.IntegreSQL.csproj`
- Create: `tests/FastIntegrationTests.Tests.NUnit.IntegreSQL/GlobalUsings.cs`
- Create: `tests/FastIntegrationTests.Tests.NUnit.IntegreSQL/AssemblyInfo.cs`
- Modify: `FastIntegrationTests.slnx`

- [ ] **Step 1: Создать csproj нового проекта**

`tests/FastIntegrationTests.Tests.NUnit.IntegreSQL/FastIntegrationTests.Tests.NUnit.IntegreSQL.csproj`:

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
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="NUnit" Version="4.2.2" />
    <PackageReference Include="NUnit3TestAdapter" Version="4.6.0" />
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

</Project>
```

- [ ] **Step 2: Создать `GlobalUsings.cs`**

`tests/FastIntegrationTests.Tests.NUnit.IntegreSQL/GlobalUsings.cs`:

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
global using NUnit.Framework;
```

- [ ] **Step 3: Создать `AssemblyInfo.cs`**

`tests/FastIntegrationTests.Tests.NUnit.IntegreSQL/AssemblyInfo.cs`:

```csharp
using NUnit.Framework;

[assembly: Parallelizable(ParallelScope.Fixtures)]
[assembly: LevelOfParallelism(12)]

// Альтернатива: ParallelScope.All — параллелизм и внутри классов.
// IntegreSQL даёт изоляцию на уровне теста (каждый тест получает свой клон БД),
// поэтому ParallelScope.All валиден. Текущий выбор Fixtures соответствует
// поведению xUnit-версии (классы параллельно, тесты внутри — последовательно)
// и упрощает сравнение «один в один».
// [assembly: Parallelizable(ParallelScope.All)]
```

- [ ] **Step 4: Зарегистрировать новый проект в `FastIntegrationTests.slnx`**

В `FastIntegrationTests.slnx` в блоке `<Folder Name="/tests/">` добавить строку после строки с `Tests.IntegreSQL`:

Текущее содержимое блока tests:
```xml
  <Folder Name="/tests/">
    <Project Path="tests/FastIntegrationTests.Tests.Shared/FastIntegrationTests.Tests.Shared.csproj" />
    <Project Path="tests/FastIntegrationTests.Tests.IntegreSQL/FastIntegrationTests.Tests.IntegreSQL.csproj" />
    <Project Path="tests/FastIntegrationTests.Tests.Respawn/FastIntegrationTests.Tests.Respawn.csproj" />
    <Project Path="tests/FastIntegrationTests.Tests.Testcontainers/FastIntegrationTests.Tests.Testcontainers.csproj" />
    <Project Path="tests/FastIntegrationTests.Tests.TestcontainersShared/FastIntegrationTests.Tests.TestcontainersShared.csproj" />
  </Folder>
```

После добавления:
```xml
  <Folder Name="/tests/">
    <Project Path="tests/FastIntegrationTests.Tests.Shared/FastIntegrationTests.Tests.Shared.csproj" />
    <Project Path="tests/FastIntegrationTests.Tests.IntegreSQL/FastIntegrationTests.Tests.IntegreSQL.csproj" />
    <Project Path="tests/FastIntegrationTests.Tests.NUnit.IntegreSQL/FastIntegrationTests.Tests.NUnit.IntegreSQL.csproj" />
    <Project Path="tests/FastIntegrationTests.Tests.Respawn/FastIntegrationTests.Tests.Respawn.csproj" />
    <Project Path="tests/FastIntegrationTests.Tests.Testcontainers/FastIntegrationTests.Tests.Testcontainers.csproj" />
    <Project Path="tests/FastIntegrationTests.Tests.TestcontainersShared/FastIntegrationTests.Tests.TestcontainersShared.csproj" />
  </Folder>
```

- [ ] **Step 5: Проверить, что новый (пустой) проект собирается**

```bash
dotnet build tests/FastIntegrationTests.Tests.NUnit.IntegreSQL
```

Ожидаемое: build успешен, 0 warnings/errors. Ничего ещё не запускается — тестов нет.

- [ ] **Step 6: Коммит**

```bash
git add tests/FastIntegrationTests.Tests.NUnit.IntegreSQL FastIntegrationTests.slnx
git commit -m "$(cat <<'EOF'
feat: каркас Tests.NUnit.IntegreSQL — csproj, slnx, GlobalUsings, AssemblyInfo

NUnit 4.2.2 + NUnit3TestAdapter 4.6.0. Параллелизм:
ParallelScope.Fixtures + LevelOfParallelism(12) — соответствие
xUnit-версии (классы параллельно, внутри — последовательно).
Альтернатива ParallelScope.All закомментирована с пояснением.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 3: Реализовать `AppServiceTestBase` для NUnit

**Files:**
- Create: `tests/FastIntegrationTests.Tests.NUnit.IntegreSQL/Infrastructure/Base/AppServiceTestBase.cs`

- [ ] **Step 1: Создать файл с базовым классом для сервисных тестов**

`tests/FastIntegrationTests.Tests.NUnit.IntegreSQL/Infrastructure/Base/AppServiceTestBase.cs`:

```csharp
using FastIntegrationTests.Tests.Infrastructure.IntegreSQL;
using MccSoft.IntegreSql.EF;
using Npgsql;

namespace FastIntegrationTests.Tests.Infrastructure.Base;

/// <summary>
/// Базовый класс для сервисных интеграционных тестов через IntegreSQL на NUnit.
/// На каждый тест берётся новый клон шаблонной БД (~5 мс) — изоляция полная,
/// тесты могут гоняться параллельно (см. AssemblyInfo).
/// </summary>
public abstract class AppServiceTestBase
{
    private string _connectionString = null!;
    private NpgsqlDatabaseInitializer _initializer = null!;

    /// <summary>Контекст тестовой БД. Доступен после <see cref="BaseSetUp"/>.</summary>
    protected ShopDbContext Context { get; private set; } = null!;

    /// <summary>Запускает контейнеры (при первом вызове) и клонирует шаблон.</summary>
    [SetUp]
    public async Task BaseSetUp()
    {
        var state = await IntegresSqlContainerManager.GetStateAsync();
        _initializer = state.Initializer;
        _connectionString = await _initializer.CreateDatabaseGetConnectionString<ShopDbContext>(
            IntegresSqlDefaults.SeedingOptions);
        var options = new DbContextOptionsBuilder<ShopDbContext>()
            .UseNpgsql(_connectionString).Options;
        Context = new ShopDbContext(options);
    }

    /// <summary>Освобождает контекст и возвращает клонированную БД в пул IntegreSQL.</summary>
    [TearDown]
    public async Task BaseTearDown()
    {
        await Context.DisposeAsync();
        await using var conn = new NpgsqlConnection(_connectionString);
        NpgsqlConnection.ClearPool(conn);
        await _initializer.RemoveDatabase(_connectionString);
    }
}
```

- [ ] **Step 2: Проверить build**

```bash
dotnet build tests/FastIntegrationTests.Tests.NUnit.IntegreSQL
```

Ожидаемое: успех. Если возникнет анализаторное предупреждение `NUnit1032` про IDisposable-поля — оставить как есть (поля очищаются в `TearDown`, диспоз аналогичен xUnit-версии).

---

## Task 4: Реализовать `ComponentTestBase` для NUnit

**Files:**
- Create: `tests/FastIntegrationTests.Tests.NUnit.IntegreSQL/Infrastructure/Base/ComponentTestBase.cs`

- [ ] **Step 1: Создать файл с базовым классом для HTTP-тестов**

`tests/FastIntegrationTests.Tests.NUnit.IntegreSQL/Infrastructure/Base/ComponentTestBase.cs`:

```csharp
using FastIntegrationTests.Tests.Infrastructure.IntegreSQL;
using MccSoft.IntegreSql.EF;
using Npgsql;

namespace FastIntegrationTests.Tests.Infrastructure.Base;

/// <summary>
/// Базовый класс для HTTP-интеграционных тестов через IntegreSQL на NUnit.
/// На каждый тест — свой клон шаблонной БД (~5 мс) и отдельный TestServer.
/// </summary>
public abstract class ComponentTestBase
{
    private string _connectionString = null!;
    private NpgsqlDatabaseInitializer _initializer = null!;
    private TestWebApplicationFactory _factory = null!;

    /// <summary>HTTP-клиент для обращений к тестируемому API.</summary>
    protected HttpClient Client { get; private set; } = null!;

    /// <summary>Запускает контейнеры (при первом вызове), клонирует шаблон, поднимает TestServer.</summary>
    [SetUp]
    public async Task BaseSetUp()
    {
        var state = await IntegresSqlContainerManager.GetStateAsync();
        _initializer = state.Initializer;
        _connectionString = await _initializer.CreateDatabaseGetConnectionString<ShopDbContext>(
            IntegresSqlDefaults.SeedingOptions);

        _factory = new TestWebApplicationFactory(_connectionString);
        Client = _factory.CreateClient();
    }

    /// <summary>Диспозит клиент и фабрику, возвращает БД в пул.</summary>
    [TearDown]
    public async Task BaseTearDown()
    {
        Client?.Dispose();
        if (_factory is not null)
            // PhysicalFilesWatcher внутри WebApplicationFactory может бросить NullReferenceException
            // при диспозе под высокой параллельностью — баг в ASP.NET Core FileSystemWatcher.
            try { await _factory.DisposeAsync(); } catch (NullReferenceException) { }
        await using var conn = new NpgsqlConnection(_connectionString);
        NpgsqlConnection.ClearPool(conn);
        await _initializer.RemoveDatabase(_connectionString);
    }
}
```

- [ ] **Step 2: Проверить build**

```bash
dotnet build tests/FastIntegrationTests.Tests.NUnit.IntegreSQL
```

Ожидаемое: успех.

- [ ] **Step 3: Коммит обоих базовых классов**

```bash
git add tests/FastIntegrationTests.Tests.NUnit.IntegreSQL/Infrastructure
git commit -m "$(cat <<'EOF'
feat: NUnit-базы AppServiceTestBase и ComponentTestBase

Аналоги xUnit-баз: получают клон шаблонной БД из IntegreSQL в [SetUp]
и возвращают в пул в [TearDown]. ComponentTestBase дополнительно
поднимает TestServer и HttpClient. BenchmarkLogger не вызывается —
NUnit-проект не участвует в бенчмарке.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 5: Портировать `ProductServiceTests`

**Files:**
- Create: `tests/FastIntegrationTests.Tests.NUnit.IntegreSQL/Products/ProductServiceTests.cs`

- [ ] **Step 1: Создать файл с портированными тестами сервисного уровня**

`tests/FastIntegrationTests.Tests.NUnit.IntegreSQL/Products/ProductServiceTests.cs`:

```csharp
namespace FastIntegrationTests.Tests.NUnit.IntegreSQL.Products;

/// <summary>
/// Тесты сервисного уровня: GetAll, GetById, Create, Update, Delete для ProductService.
/// Каждый тест получает изолированный клон БД через IntegreSQL (~5 мс).
/// </summary>
[TestFixture]
public class ProductServiceTests : AppServiceTestBase
{
    private IProductService _sut = null!;
    private IOrderService _orders = null!;

    [SetUp]
    public void SetUpServices()
    {
        var productRepo = new ProductRepository(Context);
        var orderRepo = new OrderRepository(Context);
        _sut = new ProductService(productRepo);
        _orders = new OrderService(orderRepo, productRepo);
    }

    [Test]
    public async Task GetAllAsync_WhenNoProducts_ReturnsEmptyList()
    {
        var result = await _sut.GetAllAsync();
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetAllAsync_WhenProductsExist_ReturnsAllProducts()
    {
        await _sut.CreateAsync(new CreateProductRequest { Name = "Товар 1", Description = "Описание 1", Price = 100m });
        await _sut.CreateAsync(new CreateProductRequest { Name = "Товар 2", Description = "Описание 2", Price = 200m });
        var result = await _sut.GetAllAsync();
        Assert.That(result.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task GetByIdAsync_WhenProductExists_ReturnsProduct()
    {
        var created = await _sut.CreateAsync(new CreateProductRequest { Name = "Ноутбук", Description = "Core i9", Price = 50_000m });
        var result = await _sut.GetByIdAsync(created.Id);
        Assert.That(result.Id, Is.EqualTo(created.Id));
        Assert.That(result.Name, Is.EqualTo("Ноутбук"));
        Assert.That(result.Description, Is.EqualTo("Core i9"));
        Assert.That(result.Price, Is.EqualTo(50_000m));
    }

    [Test]
    public async Task GetByIdAsync_WhenProductNotFound_ThrowsNotFoundException()
    {
        await Assert.ThatAsync(() => _sut.GetByIdAsync(999), Throws.TypeOf<NotFoundException>());
    }

    [Test]
    public async Task CreateAsync_PersistsProductAndReturnsWithAssignedId()
    {
        var request = new CreateProductRequest { Name = "Мышь", Description = "Беспроводная", Price = 2_500m };
        var result = await _sut.CreateAsync(request);
        Assert.That(result.Id, Is.GreaterThan(0));
        Assert.That(result.Name, Is.EqualTo("Мышь"));
        Assert.That(result.Description, Is.EqualTo("Беспроводная"));
        Assert.That(result.Price, Is.EqualTo(2_500m));
    }

    [Test]
    public async Task CreateAsync_SetsCreatedAtAutomatically()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var result = await _sut.CreateAsync(new CreateProductRequest { Name = "Клавиатура", Price = 3_000m });
        var after = DateTime.UtcNow.AddSeconds(1);
        Assert.That(result.CreatedAt, Is.InRange(before, after));
    }

    [Test]
    public async Task UpdateAsync_UpdatesProductFieldsInDatabase()
    {
        var created = await _sut.CreateAsync(new CreateProductRequest { Name = "Старое название", Price = 1_000m });
        var updateRequest = new UpdateProductRequest { Name = "Новое название", Description = "Новое описание", Price = 1_500m };
        var updated = await _sut.UpdateAsync(created.Id, updateRequest);
        Assert.That(updated.Name, Is.EqualTo("Новое название"));
        Assert.That(updated.Description, Is.EqualTo("Новое описание"));
        Assert.That(updated.Price, Is.EqualTo(1_500m));
        var fetched = await _sut.GetByIdAsync(created.Id);
        Assert.That(fetched.Name, Is.EqualTo("Новое название"));
        Assert.That(fetched.Price, Is.EqualTo(1_500m));
    }

    [Test]
    public async Task UpdateAsync_WhenProductNotFound_ThrowsNotFoundException()
    {
        var request = new UpdateProductRequest { Name = "Название", Description = string.Empty, Price = 100m };
        await Assert.ThatAsync(() => _sut.UpdateAsync(999, request), Throws.TypeOf<NotFoundException>());
    }

    [Test]
    public async Task DeleteAsync_RemovesProductFromDatabase()
    {
        var created = await _sut.CreateAsync(new CreateProductRequest { Name = "Временный товар", Price = 500m });
        await _sut.DeleteAsync(created.Id);
        await Assert.ThatAsync(() => _sut.GetByIdAsync(created.Id), Throws.TypeOf<NotFoundException>());
    }

    [Test]
    public async Task DeleteAsync_WhenProductNotFound_ThrowsNotFoundException()
    {
        await Assert.ThatAsync(() => _sut.DeleteAsync(999), Throws.TypeOf<NotFoundException>());
    }

    [Test]
    public async Task DeleteAsync_WhenProductHasOrderItems_ThrowsDbUpdateException()
    {
        var product = await _sut.CreateAsync(new CreateProductRequest { Name = "Товар в заказе", Price = 1_000m });
        await _orders.CreateAsync(new CreateOrderRequest
        {
            Items = new List<OrderItemRequest> { new() { ProductId = product.Id, Quantity = 1 } }
        });
        // FK Restrict: нельзя удалить товар, на который ссылаются позиции заказа
        await Assert.ThatAsync(() => _sut.DeleteAsync(product.Id), Throws.TypeOf<DbUpdateException>());
    }

    /// <summary>
    /// Создаёт несколько товаров, читает через GetAll и GetById — проверяет согласованность данных.
    /// </summary>
    [Test]
    public async Task CreateMultiple_GetAll_GetByIdEach_ReturnsConsistentData()
    {
        var a = await _sut.CreateAsync(new CreateProductRequest { Name = "Товар А", Price = 100m });
        var b = await _sut.CreateAsync(new CreateProductRequest { Name = "Товар Б", Price = 200m });
        var c = await _sut.CreateAsync(new CreateProductRequest { Name = "Товар В", Price = 300m });
        var all = await _sut.GetAllAsync();
        Assert.That(all.Count, Is.EqualTo(3));
        Assert.That((await _sut.GetByIdAsync(a.Id)).Name, Is.EqualTo("Товар А"));
        Assert.That((await _sut.GetByIdAsync(b.Id)).Name, Is.EqualTo("Товар Б"));
        Assert.That((await _sut.GetByIdAsync(c.Id)).Name, Is.EqualTo("Товар В"));
        for (var i = 0; i < 4; i++)
        {
            var extra = await _sut.CreateAsync(new CreateProductRequest { Name = $"Доп {i}", Price = 500m + i * 50m });
            await _sut.GetByIdAsync(extra.Id);
        }
        await _sut.GetAllAsync();
    }

    /// <summary>
    /// Создаёт товар, обновляет поля, проверяет персистентность, удаляет — полный цикл записи.
    /// </summary>
    [Test]
    public async Task CreateUpdateDelete_VerifyEachStep_AllPersist()
    {
        var created = await _sut.CreateAsync(new CreateProductRequest { Name = "Монитор", Price = 20_000m });
        var updated = await _sut.UpdateAsync(created.Id, new UpdateProductRequest { Name = "Монитор 4K", Description = "UHD", Price = 25_000m });
        Assert.That(updated.Name, Is.EqualTo("Монитор 4K"));
        Assert.That(updated.Price, Is.EqualTo(25_000m));
        var fetched = await _sut.GetByIdAsync(created.Id);
        Assert.That(fetched.Name, Is.EqualTo("Монитор 4K"));
        await _sut.DeleteAsync(created.Id);
        await Assert.ThatAsync(() => _sut.GetByIdAsync(created.Id), Throws.TypeOf<NotFoundException>());
        for (var i = 0; i < 4; i++)
        {
            var extra = await _sut.CreateAsync(new CreateProductRequest { Name = $"Доп {i}", Price = 1_000m + i * 100m });
            await _sut.UpdateAsync(extra.Id, new UpdateProductRequest { Name = $"Доп {i} v2", Price = 1_100m + i * 100m });
            await _sut.GetByIdAsync(extra.Id);
        }
        await _sut.GetAllAsync();
    }
}
```

Замечания по портированию:
- `[Fact]` → `[Test]`. Класс получает явный `[TestFixture]`.
- `Sut` (PascalCase в xUnit) → `_sut` (с подчёркиванием) — соответствует частному полю в нашем коде. Имя `_orders` оставлено как было.
- `Assert.ThatAsync(...)` возвращает `Task` — каждое использование обязательно `await`-ится, и метод теста объявлен `async Task`. Без `await` компилятор выдаст CS4014.
- `Assert.Empty(result)` → `Assert.That(result, Is.Empty)`.
- `Assert.Equal(a, b)` → `Assert.That(b, Is.EqualTo(a))` (порядок аргументов меняется на actual-then-expected).
- `Assert.True(x > 0)` → `Assert.That(x, Is.GreaterThan(0))` (констрейнт идиоматичнее).
- `Assert.InRange(v, lo, hi)` → `Assert.That(v, Is.InRange(lo, hi))`.
- `Assert.ThrowsAsync<T>(f)` → `Assert.ThatAsync(f, Throws.TypeOf<T>())`.

- [ ] **Step 2: Проверить build и discovery**

```bash
dotnet build tests/FastIntegrationTests.Tests.NUnit.IntegreSQL
dotnet test tests/FastIntegrationTests.Tests.NUnit.IntegreSQL --list-tests --no-build 2>&1 | head -30
dotnet test tests/FastIntegrationTests.Tests.NUnit.IntegreSQL --list-tests --no-build 2>&1 | grep -c "FastIntegrationTests.Tests.NUnit.IntegreSQL.Products.ProductServiceTests"
```

Ожидаемое: build успешен; в выводе видны 12 методов класса `ProductServiceTests`; счётчик = 12.

- [ ] **Step 3: Коммит**

```bash
git add tests/FastIntegrationTests.Tests.NUnit.IntegreSQL/Products/ProductServiceTests.cs
git commit -m "$(cat <<'EOF'
feat: ProductServiceTests на NUnit — порт 12 тестов сервисного уровня

[Fact] → [Test], constraint-model ассерты (Assert.That ... Is.EqualTo,
Is.Empty, Is.InRange, Throws.TypeOf). Sut-поля инициализируются
в [SetUp] с другим именем — после базового [SetUp] из AppServiceTestBase.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 6: Портировать `ProductsApiTests`

**Files:**
- Create: `tests/FastIntegrationTests.Tests.NUnit.IntegreSQL/Products/ProductsApiTests.cs`

- [ ] **Step 1: Создать файл с портированными HTTP-тестами**

`tests/FastIntegrationTests.Tests.NUnit.IntegreSQL/Products/ProductsApiTests.cs`:

```csharp
namespace FastIntegrationTests.Tests.NUnit.IntegreSQL.Products;

/// <summary>
/// Тесты HTTP-уровня: GetAll, GetById, Create, Update, Delete для ProductsController.
/// Каждый тест получает изолированный клон БД через IntegreSQL (~5 мс) и отдельный TestServer.
/// </summary>
[TestFixture]
public class ProductsApiTests : ComponentTestBase
{
    [Test]
    public async Task GetAll_WhenNoProducts_Returns200WithEmptyArray()
    {
        var response = await Client.GetAsync("/api/products");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>();
        Assert.That(products, Is.Empty);
    }

    [Test]
    public async Task GetAll_WhenProductsExist_Returns200WithProducts()
    {
        await CreateProductAsync("Товар 1", 100m);
        await CreateProductAsync("Товар 2", 200m);

        var response = await Client.GetAsync("/api/products");
        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(products!.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task GetById_WhenProductExists_Returns200WithProduct()
    {
        var created = await CreateProductAsync("Ноутбук", 50_000m);

        var response = await Client.GetAsync($"/api/products/{created.Id}");
        var product = await response.Content.ReadFromJsonAsync<ProductDto>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(product!.Id, Is.EqualTo(created.Id));
        Assert.That(product.Name, Is.EqualTo("Ноутбук"));
    }

    [Test]
    public async Task GetById_WhenProductNotFound_Returns404()
    {
        var response = await Client.GetAsync("/api/products/999");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Create_ValidRequest_Returns201WithLocationHeaderAndId()
    {
        var request = new CreateProductRequest { Name = "Монитор", Description = "4K", Price = 25_000m };

        var response = await Client.PostAsJsonAsync("/api/products", request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(response.Headers.Location, Is.Not.Null);
        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        Assert.That(product!.Id, Is.GreaterThan(0));
        Assert.That(product.Name, Is.EqualTo("Монитор"));
    }

    [Test]
    public async Task CreateThenGetById_DataMatchesExactly()
    {
        var createRequest = new CreateProductRequest { Name = "Системный блок", Description = "Core i9", Price = 80_000m };
        var createResponse = await Client.PostAsJsonAsync("/api/products", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductDto>();

        var getResponse = await Client.GetAsync($"/api/products/{created!.Id}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<ProductDto>();

        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(fetched!.Id, Is.EqualTo(created.Id));
        Assert.That(fetched.Name, Is.EqualTo("Системный блок"));
        Assert.That(fetched.Description, Is.EqualTo("Core i9"));
        Assert.That(fetched.Price, Is.EqualTo(80_000m));
    }

    [Test]
    public async Task Update_WhenProductExists_Returns200WithUpdatedFields()
    {
        var created = await CreateProductAsync("Старое", 100m);
        var updateRequest = new UpdateProductRequest { Name = "Новое", Description = "Обновлено", Price = 200m };

        var response = await Client.PutAsJsonAsync($"/api/products/{created.Id}", updateRequest);
        var updated = await response.Content.ReadFromJsonAsync<ProductDto>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(updated!.Name, Is.EqualTo("Новое"));
        Assert.That(updated.Description, Is.EqualTo("Обновлено"));
        Assert.That(updated.Price, Is.EqualTo(200m));
    }

    [Test]
    public async Task Update_WhenProductNotFound_Returns404()
    {
        var request = new UpdateProductRequest { Name = "Название", Description = string.Empty, Price = 100m };

        var response = await Client.PutAsJsonAsync("/api/products/999", request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Delete_WhenProductExists_Returns204()
    {
        var created = await CreateProductAsync("Удаляемый", 100m);

        var response = await Client.DeleteAsync($"/api/products/{created.Id}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test]
    public async Task Delete_WhenProductNotFound_Returns404()
    {
        var response = await Client.DeleteAsync("/api/products/999");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>
    /// Создаёт несколько товаров через API, проверяет GetAll и GetById каждого.
    /// </summary>
    [Test]
    public async Task CreateMultiple_GetAll_GetByIdEach_ReturnsConsistentData()
    {
        var a = await CreateProductAsync("Товар А", 100m);
        var b = await CreateProductAsync("Товар Б", 200m);
        var c = await CreateProductAsync("Товар В", 300m);

        var all = await Client.GetAsync("/api/products");
        var list = await all.Content.ReadFromJsonAsync<List<ProductDto>>();
        Assert.That(list!.Count, Is.EqualTo(3));

        var fa = await (await Client.GetAsync($"/api/products/{a.Id}")).Content.ReadFromJsonAsync<ProductDto>();
        var fb = await (await Client.GetAsync($"/api/products/{b.Id}")).Content.ReadFromJsonAsync<ProductDto>();
        var fc = await (await Client.GetAsync($"/api/products/{c.Id}")).Content.ReadFromJsonAsync<ProductDto>();
        Assert.That(fa!.Name, Is.EqualTo("Товар А"));
        Assert.That(fb!.Name, Is.EqualTo("Товар Б"));
        Assert.That(fc!.Name, Is.EqualTo("Товар В"));

        for (var i = 0; i < 4; i++)
        {
            var extra = await CreateProductAsync($"Доп {i}", 500m + i * 50m);
            await Client.GetAsync($"/api/products/{extra.Id}");
        }
        await Client.GetAsync("/api/products");
    }

    /// <summary>
    /// Создаёт товар, обновляет через PUT, проверяет GET, удаляет — полный HTTP-цикл.
    /// </summary>
    [Test]
    public async Task CreateUpdateDelete_VerifyEachStep_AllPersist()
    {
        var created = await CreateProductAsync("Монитор", 20_000m);

        var putResp = await Client.PutAsJsonAsync($"/api/products/{created.Id}",
            new UpdateProductRequest { Name = "Монитор 4K", Description = "UHD", Price = 25_000m });
        Assert.That(putResp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var updated = await putResp.Content.ReadFromJsonAsync<ProductDto>();
        Assert.That(updated!.Name, Is.EqualTo("Монитор 4K"));

        var getResp = await Client.GetAsync($"/api/products/{created.Id}");
        var fetched = await getResp.Content.ReadFromJsonAsync<ProductDto>();
        Assert.That(fetched!.Name, Is.EqualTo("Монитор 4K"));

        var delResp = await Client.DeleteAsync($"/api/products/{created.Id}");
        Assert.That(delResp.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That((await Client.GetAsync($"/api/products/{created.Id}")).StatusCode,
            Is.EqualTo(HttpStatusCode.NotFound));

        for (var i = 0; i < 4; i++)
        {
            var extra = await CreateProductAsync($"Доп {i}", 1_000m + i * 100m);
            await Client.PutAsJsonAsync($"/api/products/{extra.Id}",
                new UpdateProductRequest { Name = $"Доп {i} v2", Price = 1_100m + i * 100m });
            await Client.GetAsync($"/api/products/{extra.Id}");
        }
        await Client.GetAsync("/api/products");
    }

    // --- helpers ---

    /// <summary>
    /// Создаёт товар через API и возвращает его DTO.
    /// </summary>
    /// <param name="name">Название товара.</param>
    /// <param name="price">Цена товара.</param>
    /// <param name="ct">Токен отмены операции.</param>
    private async Task<ProductDto> CreateProductAsync(string name, decimal price, CancellationToken ct = default)
    {
        var response = await Client.PostAsJsonAsync("/api/products",
            new CreateProductRequest { Name = name, Price = price }, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductDto>(ct))!;
    }
}
```

- [ ] **Step 2: Проверить build и discovery**

```bash
dotnet build tests/FastIntegrationTests.Tests.NUnit.IntegreSQL
dotnet test tests/FastIntegrationTests.Tests.NUnit.IntegreSQL --list-tests --no-build 2>&1 | grep -c "FastIntegrationTests.Tests.NUnit.IntegreSQL\."
```

Ожидаемое: build успешен; счётчик = 24 (12 service + 12 api).

- [ ] **Step 3: Коммит**

```bash
git add tests/FastIntegrationTests.Tests.NUnit.IntegreSQL/Products/ProductsApiTests.cs
git commit -m "$(cat <<'EOF'
feat: ProductsApiTests на NUnit — порт 12 HTTP-тестов

[Fact] → [Test], Assert.Equal/NotNull/Empty/True переведены на
constraint-model (Assert.That ... Is.EqualTo, Is.Not.Null, Is.Empty,
Is.GreaterThan). Хелпер CreateProductAsync перенесён без изменений —
он не использует ассерты xUnit, только EnsureSuccessStatusCode.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 7: Обновить `CLAUDE.md` и финальная проверка

**Files:**
- Modify: `CLAUDE.md`

- [ ] **Step 1: Добавить раздел про NUnit-проект в CLAUDE.md**

В `CLAUDE.md` найти заголовок `## Архитектура` и блок описаний проектов (строки начинаются с `- **Tests.IntegreSQL** ...`, `- **Tests.Respawn** ...`, `- **Tests.Testcontainers** ...`).

После строки, описывающей `Tests.Testcontainers` (последняя `- **Tests.Testcontainers** ...`), добавить новый абзац:

```markdown
- **Tests.NUnit.IntegreSQL** (`tests/FastIntegrationTests.Tests.NUnit.IntegreSQL/`) — учебный мини-проект для команд на NUnit. Содержит `ProductServiceTests` и `ProductsApiTests` (~24 теста), демонстрирующие маппинг xUnit-инфраструктуры IntegreSQL на NUnit: `IAsyncLifetime` → `[SetUp]` / `[TearDown]`, constraint-model ассерты (`Assert.That(value, Is.EqualTo(...))`), `[Parallelizable]` + `LevelOfParallelism` на уровне assembly. В BenchmarkRunner и в PowerShell-скрипты не включён — это пример, а не альтернативный стек. Общая IntegreSQL-инфраструктура (`IntegresSqlContainerManager`, `IntegresSqlDefaults`, `IntegresSqlState`) живёт в `Tests.Shared/Infrastructure/IntegreSQL/` и переиспользуется обоими проектами.
```

Также: в описании `- **Tests.IntegreSQL**` упомянуто `IntegreSQL/IntegresSqlContainerManager` и `IntegresSqlDefaults` + `IntegresSqlState`. После рефакторинга эти типы переехали в Shared. Поправить эту строку:

Было (фрагмент):
```
Инфраструктура: `AppServiceTestBase`, `ComponentTestBase`, `IntegreSQL/IntegresSqlContainerManager` + `IntegresSqlDefaults` + `IntegresSqlState`.
```

Стало:
```
Инфраструктура: `AppServiceTestBase`, `ComponentTestBase` (общий `IntegresSqlContainerManager` + `IntegresSqlDefaults` + `IntegresSqlState` живут в `Tests.Shared`).
```

- [ ] **Step 2: Финальная проверка — собирается всё solution и оба IntegreSQL-проекта видят свои тесты**

```bash
dotnet build FastIntegrationTests.slnx
dotnet test tests/FastIntegrationTests.Tests.IntegreSQL --list-tests --no-build 2>&1 | grep -c "FastIntegrationTests.Tests.IntegreSQL\."
dotnet test tests/FastIntegrationTests.Tests.NUnit.IntegreSQL --list-tests --no-build 2>&1 | grep -c "FastIntegrationTests.Tests.NUnit.IntegreSQL\."
```

Ожидаемое:
- solution build — успех, 0 errors;
- xUnit-проект: счётчик ≥ 195 (без изменений относительно состояния до начала плана);
- NUnit-проект: счётчик = 24.

- [ ] **Step 3: Финальный коммит**

```bash
git add CLAUDE.md
git commit -m "$(cat <<'EOF'
docs: CLAUDE.md — описание Tests.NUnit.IntegreSQL и переезд общей инфры в Shared

Добавлен раздел про новый NUnit-проект (учебный пример, не часть
бенчмарка). Описание Tests.IntegreSQL обновлено: общие
IntegresSqlContainerManager/Defaults/State теперь в Tests.Shared.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Self-review (после написания плана)

- **Покрытие spec'а:** все разделы spec'а отображены в задачах (структура → Tasks 2–6, рефакторинг Shared → Task 1, базовые классы → Tasks 3–4, тестовые классы → Tasks 5–6, параллелизм → Task 2 step 3, документация → Task 7, верификация → Task 7 step 2, scope → не делается, что отражено в Task 1 step 5 с убранным дубликатом и в Task 7 без PowerShell/Benchmark). Готово.
- **Placeholder scan:** код во всех шагах конкретный, без «similar to», «add here», «and other tests». Готово.
- **Type consistency:** имя поля `_sut` в `ProductServiceTests` (Task 5) и упоминание в комментариях согласованы. Метод `CreateProductAsync` в `ProductsApiTests` (Task 6) совпадает с xUnit-версией один в один. Имена `BaseSetUp`/`BaseTearDown` в `AppServiceTestBase` и `ComponentTestBase` (Tasks 3–4) одинаковы — NUnit допускает одинаковые имена в разных классах. Готово.
