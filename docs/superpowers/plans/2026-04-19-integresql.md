# IntegreSQL Integration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Добавить два базовых класса `AppServiceTestBase` и `ComponentTestBase`, которые используют IntegreSQL (шаблон + клонирование) вместо `MigrateAsync()` на каждый тест — рядом с существующим Testcontainers-подходом.

**Architecture:** Статический `IntegresSqlContainerManager` запускает Docker-сеть + PostgreSQL + IntegreSQL-сервер через Testcontainers один раз на весь процесс. `AppServiceTestBase` и `ComponentTestBase` наследуют от него без `[Collection]` и без `ContainerFixture` — каждый тест-класс становится неявной xUnit-коллекцией и выполняется параллельно.

**Tech Stack:** MccSoft.IntegreSql.EF 0.12.2 (NuGet), Testcontainers 4.4.0 (уже в проекте), xUnit 2.9.3, PostgreSQL 16-alpine, IntegreSQL ghcr.io/allaboutapps/integresql:latest

---

## File Map

| Файл | Действие | Ответственность |
|---|---|---|
| `tests/.../FastIntegrationTests.Tests.csproj` | изменить | добавить NuGet-пакет |
| `tests/.../Infrastructure/IntegreSQL/IntegresSqlState.cs` | создать | DTO: хранит `NpgsqlDatabaseInitializer` |
| `tests/.../Infrastructure/IntegreSQL/IntegresSqlContainerManager.cs` | создать | запуск контейнеров, создание инициализатора |
| `tests/.../Infrastructure/Base/AppServiceTestBase.cs` | создать | базовый класс сервисных тестов через IntegreSQL |
| `tests/.../Infrastructure/Base/ComponentTestBase.cs` | создать | базовый класс HTTP-тестов через IntegreSQL |
| `tests/.../Products/ProductServiceIntegreTests.cs` | создать | дымовые сервисные тесты |
| `tests/.../Products/ProductsApiIntegreTests.cs` | создать | дымовые HTTP-тесты |

---

## Task 1: Добавить NuGet-пакет MccSoft.IntegreSql.EF

**Files:**
- Modify: `tests/FastIntegrationTests.Tests/FastIntegrationTests.Tests.csproj`

- [ ] **Шаг 1: Добавить PackageReference**

Открыть `tests/FastIntegrationTests.Tests/FastIntegrationTests.Tests.csproj` и добавить в блок `<ItemGroup>` с пакетами:

```xml
<PackageReference Include="MccSoft.IntegreSql.EF" Version="0.12.2" />
```

Полный блок пакетов станет:

```xml
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
  <PackageReference Include="Testcontainers.PostgreSql" Version="4.4.0" />
  <PackageReference Include="Testcontainers.MsSql" Version="4.4.0" />
  <PackageReference Include="MccSoft.IntegreSql.EF" Version="0.12.2" />
  <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.15" />
  <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="8.0.1" />
</ItemGroup>
```

- [ ] **Шаг 2: Восстановить пакеты и проверить сборку**

```bash
dotnet build tests/FastIntegrationTests.Tests
```

Ожидаемый результат: `Build succeeded` без ошибок.

- [ ] **Шаг 3: Закоммитить**

```bash
git add tests/FastIntegrationTests.Tests/FastIntegrationTests.Tests.csproj
git commit -m "feat: добавить пакет MccSoft.IntegreSql.EF"
```

---

## Task 2: Создать IntegresSqlState и IntegresSqlContainerManager

**Files:**
- Create: `tests/FastIntegrationTests.Tests/Infrastructure/IntegreSQL/IntegresSqlState.cs`
- Create: `tests/FastIntegrationTests.Tests/Infrastructure/IntegreSQL/IntegresSqlContainerManager.cs`

- [ ] **Шаг 1: Создать IntegresSqlState**

Создать файл `tests/FastIntegrationTests.Tests/Infrastructure/IntegreSQL/IntegresSqlState.cs`:

```csharp
using MccSoft.IntegreSql.EF;

namespace FastIntegrationTests.Tests.Infrastructure.IntegreSQL;

/// <summary>
/// Готовое состояние IntegreSQL-инфраструктуры для получения тестовых БД.
/// </summary>
public sealed class IntegresSqlState
{
    /// <summary>Инициализатор баз данных через IntegreSQL.</summary>
    public NpgsqlDatabaseInitializer Initializer { get; }

    /// <summary>
    /// Создаёт новый экземпляр <see cref="IntegresSqlState"/>.
    /// </summary>
    /// <param name="initializer">Настроенный инициализатор БД.</param>
    public IntegresSqlState(NpgsqlDatabaseInitializer initializer)
    {
        Initializer = initializer;
    }
}
```

- [ ] **Шаг 2: Создать IntegresSqlContainerManager**

Создать файл `tests/FastIntegrationTests.Tests/Infrastructure/IntegreSQL/IntegresSqlContainerManager.cs`:

```csharp
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using MccSoft.IntegreSql.EF;
using MccSoft.IntegreSql.EF.DatabaseInitialization;
using Testcontainers.PostgreSql;

namespace FastIntegrationTests.Tests.Infrastructure.IntegreSQL;

/// <summary>
/// Запускает PostgreSQL и IntegreSQL-сервер в Docker-контейнерах один раз на весь процесс.
/// Все тест-классы, использующие IntegreSQL, разделяют одну пару контейнеров.
/// </summary>
public static class IntegresSqlContainerManager
{
    private static readonly Lazy<Task<IntegresSqlState>> _state =
        new(() => InitializeAsync(), LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// Возвращает готовое состояние IntegreSQL.
    /// При первом вызове запускает контейнеры — последующие вызовы возвращают кешированный результат.
    /// </summary>
    public static Task<IntegresSqlState> GetStateAsync() => _state.Value;

    private static async Task<IntegresSqlState> InitializeAsync()
    {
        var network = new NetworkBuilder().Build();
        await network.CreateAsync();

        var pgContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithNetwork(network)
            .WithNetworkAliases("postgres")
            .Build();
        await pgContainer.StartAsync();

        var integreSqlContainer = new ContainerBuilder()
            .WithImage("ghcr.io/allaboutapps/integresql:latest")
            .WithNetwork(network)
            .WithEnvironment("PGHOST", "postgres")
            .WithEnvironment("PGUSER", "postgres")
            .WithEnvironment("PGPASSWORD", "postgres")
            .WithEnvironment("PGPORT", "5432")
            .WithPortBinding(5000, true)
            .WaitingFor(Wait.ForHttp("/api/v1/ready").ForPort(5000))
            .Build();
        await integreSqlContainer.StartAsync();

        var initializer = new NpgsqlDatabaseInitializer(
            integreSqlUri: new Uri(
                $"http://localhost:{integreSqlContainer.GetMappedPublicPort(5000)}/api/v1/"),
            connectionStringOverride: new ConnectionStringOverride
            {
                Host = "localhost",
                Port = pgContainer.GetMappedPublicPort(5432)
            }
        );

        return new IntegresSqlState(initializer);
    }
}
```

- [ ] **Шаг 3: Проверить сборку**

```bash
dotnet build tests/FastIntegrationTests.Tests
```

Ожидаемый результат: `Build succeeded`.

Если `DotNet.Testcontainers.Configurations` не найден — заменить на `DotNet.Testcontainers.Builders` (все типы могут быть в одном пространстве имён). Если `Wait.ForHttp` не компилируется — заменить на `Wait.ForListeningPort()`.

- [ ] **Шаг 4: Закоммитить**

```bash
git add tests/FastIntegrationTests.Tests/Infrastructure/IntegreSQL/
git commit -m "feat: добавить IntegresSqlContainerManager и IntegresSqlState"
```

---

## Task 3: Дымовые сервисные тесты + AppServiceTestBase (TDD)

**Files:**
- Create: `tests/FastIntegrationTests.Tests/Products/ProductServiceIntegreTests.cs`
- Create: `tests/FastIntegrationTests.Tests/Infrastructure/Base/AppServiceTestBase.cs`

- [ ] **Шаг 1: Написать дымовые тесты (не компилируются)**

Создать `tests/FastIntegrationTests.Tests/Products/ProductServiceIntegreTests.cs`:

```csharp
namespace FastIntegrationTests.Tests.Products;

/// <summary>
/// Дымовые тесты сервисного уровня через IntegreSQL.
/// Проверяют работу шаблонного клонирования БД.
/// </summary>
public class ProductServiceIntegreTests : AppServiceTestBase
{
    /// <summary>
    /// GetAllAsync при пустой базе возвращает пустой список.
    /// </summary>
    [Fact]
    public async Task GetAllAsync_WhenNoProducts_ReturnsEmptyList()
    {
        var result = await ProductService.GetAllAsync();

        Assert.Empty(result);
    }

    /// <summary>
    /// CreateAsync сохраняет товар и возвращает его с присвоенным Id.
    /// </summary>
    [Fact]
    public async Task CreateAsync_PersistsProductAndReturnsWithAssignedId()
    {
        var request = new CreateProductRequest
        {
            Name = "Ноутбук",
            Description = "Core i9",
            Price = 50_000m
        };

        var result = await ProductService.CreateAsync(request);

        Assert.True(result.Id > 0);
        Assert.Equal("Ноутбук", result.Name);
        Assert.Equal("Core i9", result.Description);
        Assert.Equal(50_000m, result.Price);
    }
}
```

- [ ] **Шаг 2: Убедиться что тест не компилируется**

```bash
dotnet build tests/FastIntegrationTests.Tests
```

Ожидаемый результат: ошибка `CS0246: The type or namespace name 'AppServiceTestBase' could not be found`.

- [ ] **Шаг 3: Реализовать AppServiceTestBase**

Создать `tests/FastIntegrationTests.Tests/Infrastructure/Base/AppServiceTestBase.cs`:

```csharp
using FastIntegrationTests.Tests.Infrastructure.IntegreSQL;
using MccSoft.IntegreSql.EF.DatabaseInitialization;

namespace FastIntegrationTests.Tests.Infrastructure.Base;

/// <summary>
/// Базовый класс для сервисных интеграционных тестов через IntegreSQL.
/// Миграции выполняются один раз как шаблон; каждый тест получает клон (~5 мс).
/// Не требует [Collection] — каждый наследующий класс выполняется в своей неявной коллекции.
/// </summary>
public abstract class AppServiceTestBase : IAsyncLifetime
{
    private static readonly DatabaseSeedingOptions<ShopDbContext> SeedingOptions =
        new(
            Name: "shop-default",
            SeedingFunction: async ctx => await ctx.Database.MigrateAsync(),
            DisableEnsureCreated: true,
            DbContextFactory: opts => new ShopDbContext(opts)
        );

    private string _connectionString = null!;
    private ShopDbContext _context = null!;

    /// <summary>Сервис для работы с товарами.</summary>
    protected IProductService ProductService { get; private set; } = null!;

    /// <summary>Сервис для работы с заказами.</summary>
    protected IOrderService OrderService { get; private set; } = null!;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        var state = await IntegresSqlContainerManager.GetStateAsync();
        _connectionString = await state.Initializer.CreateDatabaseGetConnectionString<ShopDbContext>(
            SeedingOptions);

        var options = new DbContextOptionsBuilder<ShopDbContext>()
            .UseNpgsql(_connectionString)
            .Options;
        _context = new ShopDbContext(options);

        var productRepo = new ProductRepository(_context);
        var orderRepo = new OrderRepository(_context);
        ProductService = new ProductService(productRepo);
        OrderService = new OrderService(orderRepo, productRepo);
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        var state = await IntegresSqlContainerManager.GetStateAsync();
        await state.Initializer.RemoveDatabase(_connectionString);
    }
}
```

- [ ] **Шаг 4: Проверить сборку**

```bash
dotnet build tests/FastIntegrationTests.Tests
```

Ожидаемый результат: `Build succeeded`.

- [ ] **Шаг 5: Запустить дымовые сервисные тесты**

```bash
dotnet test tests/FastIntegrationTests.Tests --filter "FullyQualifiedName~ProductServiceIntegreTests" --verbosity normal
```

Ожидаемый результат: оба теста `Passed`. Первый запуск — дольше (запуск контейнеров + создание шаблона). Повторный — быстрее (шаблон уже есть).

- [ ] **Шаг 6: Закоммитить**

```bash
git add tests/FastIntegrationTests.Tests/Infrastructure/Base/AppServiceTestBase.cs
git add tests/FastIntegrationTests.Tests/Products/ProductServiceIntegreTests.cs
git commit -m "feat: добавить AppServiceTestBase и дымовые сервисные тесты через IntegreSQL"
```

---

## Task 4: Дымовые HTTP-тесты + ComponentTestBase (TDD)

**Files:**
- Create: `tests/FastIntegrationTests.Tests/Products/ProductsApiIntegreTests.cs`
- Create: `tests/FastIntegrationTests.Tests/Infrastructure/Base/ComponentTestBase.cs`

- [ ] **Шаг 1: Написать дымовые HTTP-тесты (не компилируются)**

Создать `tests/FastIntegrationTests.Tests/Products/ProductsApiIntegreTests.cs`:

```csharp
namespace FastIntegrationTests.Tests.Products;

/// <summary>
/// Дымовые HTTP-тесты через IntegreSQL.
/// Проверяют работу ComponentTestBase с реальным HTTP-клиентом.
/// </summary>
public class ProductsApiIntegreTests : ComponentTestBase
{
    /// <summary>
    /// GET /api/products при пустой базе возвращает 200 и пустой массив.
    /// </summary>
    [Fact]
    public async Task GetAll_WhenNoProducts_Returns200WithEmptyArray()
    {
        var response = await Client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>();
        Assert.Empty(products!);
    }

    /// <summary>
    /// POST /api/products с валидными данными возвращает 201 и созданный товар.
    /// </summary>
    [Fact]
    public async Task Create_ValidRequest_Returns201WithProduct()
    {
        var request = new CreateProductRequest
        {
            Name = "Монитор",
            Description = "4K",
            Price = 25_000m
        };

        var response = await Client.PostAsJsonAsync("/api/products", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        Assert.True(product!.Id > 0);
        Assert.Equal("Монитор", product.Name);
    }
}
```

- [ ] **Шаг 2: Убедиться что тест не компилируется**

```bash
dotnet build tests/FastIntegrationTests.Tests
```

Ожидаемый результат: ошибка `CS0246: The type or namespace name 'ComponentTestBase' could not be found`.

- [ ] **Шаг 3: Реализовать ComponentTestBase**

Создать `tests/FastIntegrationTests.Tests/Infrastructure/Base/ComponentTestBase.cs`:

```csharp
using FastIntegrationTests.Tests.Infrastructure.IntegreSQL;
using MccSoft.IntegreSql.EF.DatabaseInitialization;

namespace FastIntegrationTests.Tests.Infrastructure.Base;

/// <summary>
/// Базовый класс для HTTP-интеграционных тестов через IntegreSQL.
/// Миграции выполняются один раз как шаблон; каждый тест получает клон (~5 мс) и отдельный TestServer.
/// Не требует [Collection] — каждый наследующий класс выполняется в своей неявной коллекции.
/// </summary>
public abstract class ComponentTestBase : IAsyncLifetime
{
    private static readonly DatabaseSeedingOptions<ShopDbContext> SeedingOptions =
        new(
            Name: "shop-default",
            SeedingFunction: async ctx => await ctx.Database.MigrateAsync(),
            DisableEnsureCreated: true,
            DbContextFactory: opts => new ShopDbContext(opts)
        );

    private string _connectionString = null!;
    private ShopDbContext _schemaContext = null!;
    private TestWebApplicationFactory _factory = null!;

    /// <summary>HTTP-клиент для обращений к тестируемому API.</summary>
    protected HttpClient Client { get; private set; } = null!;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        var state = await IntegresSqlContainerManager.GetStateAsync();
        _connectionString = await state.Initializer.CreateDatabaseGetConnectionString<ShopDbContext>(
            SeedingOptions);

        var options = new DbContextOptionsBuilder<ShopDbContext>()
            .UseNpgsql(_connectionString)
            .Options;
        _schemaContext = new ShopDbContext(options);

        _factory = new TestWebApplicationFactory("PostgreSQL", _connectionString);
        Client = _factory.CreateClient();
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        Client?.Dispose();
        if (_factory is not null) await _factory.DisposeAsync();
        await _schemaContext.DisposeAsync();
        var state = await IntegresSqlContainerManager.GetStateAsync();
        await state.Initializer.RemoveDatabase(_connectionString);
    }
}
```

- [ ] **Шаг 4: Проверить сборку**

```bash
dotnet build tests/FastIntegrationTests.Tests
```

Ожидаемый результат: `Build succeeded`.

- [ ] **Шаг 5: Запустить дымовые HTTP-тесты**

```bash
dotnet test tests/FastIntegrationTests.Tests --filter "FullyQualifiedName~ProductsApiIntegreTests" --verbosity normal
```

Ожидаемый результат: оба теста `Passed`.

- [ ] **Шаг 6: Закоммитить**

```bash
git add tests/FastIntegrationTests.Tests/Infrastructure/Base/ComponentTestBase.cs
git add tests/FastIntegrationTests.Tests/Products/ProductsApiIntegreTests.cs
git commit -m "feat: добавить ComponentTestBase и дымовые HTTP-тесты через IntegreSQL"
```

---

## Task 5: Запустить полный набор тестов

- [ ] **Шаг 1: Запустить все тесты**

```bash
dotnet test tests/FastIntegrationTests.Tests --verbosity normal
```

Ожидаемый результат: все тесты `Passed`. Всего должно быть 53 существующих + 4 новых = 57 тестов.

- [ ] **Шаг 2: Сравнить производительность**

Запустить только Testcontainers-тесты:

```bash
dotnet test tests/FastIntegrationTests.Tests --filter "FullyQualifiedName~ProductServiceTests|FullyQualifiedName~OrderServiceTests" --verbosity normal
```

Запустить только IntegreSQL-тесты:

```bash
dotnet test tests/FastIntegrationTests.Tests --filter "FullyQualifiedName~IntegreTests" --verbosity normal
```

Обратить внимание на время выполнения в выводе: разница будет в overhead'е на тест (Testcontainers ~300 мс на `MigrateAsync` vs IntegreSQL ~5 мс на клон).

- [ ] **Шаг 3: Закоммитить если были правки**

```bash
git add -A
git commit -m "test: запустить и верифицировать все тесты включая IntegreSQL"
```
