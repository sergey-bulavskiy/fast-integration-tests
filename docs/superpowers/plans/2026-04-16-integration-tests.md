# Integration Tests Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Написать 52 интеграционных теста с реальной БД (Testcontainers) на двух уровнях — HTTP и сервисном — с полной изоляцией и степенью параллелизма 4.

**Architecture:** Database-per-test: каждый тест создаёт уникальную базу (`test_{guid}`) внутри одного Testcontainers-контейнера, применяет миграции EF Core и удаляет базу после завершения. Четыре xUnit-коллекции (ProductsService, ProductsApi, OrdersService, OrdersApi) запускаются параллельно — по одному контейнеру на коллекцию. Инфраструктура скрыта в базовых классах `ServiceTestBase` / `ApiTestBase`; тесты не знают о контейнерах и базах данных.

**Tech Stack:** .NET 8, xUnit 2.9.x, Testcontainers.PostgreSql / Testcontainers.MsSql 4.4.x, Microsoft.AspNetCore.Mvc.Testing 8.0.x, EF Core 8.0.x.

---

## Карта файлов

```
tests/FastIntegrationTests.Tests/
├── FastIntegrationTests.Tests.csproj        ← Task 1: создать
├── appsettings.json                          ← Task 1: создать (DatabaseProvider)
├── xunit.runner.json                         ← Task 1: создать (MaxParallelThreads=4)
├── GlobalUsings.cs                           ← Task 6: создать
├── Infrastructure/
│   ├── Fixtures/
│   │   ├── ContainerFixture.cs               ← Task 3: создать
│   │   └── CollectionDefinitions.cs          ← Task 3: создать
│   ├── Factories/
│   │   └── TestDbFactory.cs                  ← Task 4: создать
│   ├── Base/
│   │   ├── ServiceTestBase.cs                ← Task 5: создать
│   │   └── ApiTestBase.cs                    ← Task 6: создать
│   └── WebApp/
│       └── TestWebApplicationFactory.cs      ← Task 6: создать
├── Products/
│   ├── ProductServiceTests.cs                ← Task 8: создать (11 тестов)
│   └── ProductsApiTests.cs                   ← Task 9: создать (10 тестов)
└── Orders/
    ├── OrderServiceTests.cs                  ← Task 10: создать (16 тестов)
    └── OrdersApiTests.cs                     ← Task 11: создать (15 тестов)

src/FastIntegrationTests.WebApi/Program.cs   ← Task 2: добавить partial class Program
```

---

## Task 1: Создать тест-проект

**Files:**
- Create: `tests/FastIntegrationTests.Tests/FastIntegrationTests.Tests.csproj`
- Create: `tests/FastIntegrationTests.Tests/appsettings.json`
- Create: `tests/FastIntegrationTests.Tests/xunit.runner.json`

- [ ] **Step 1: Создать директории**

```bash
mkdir -p tests/FastIntegrationTests.Tests/Infrastructure/Fixtures
mkdir -p tests/FastIntegrationTests.Tests/Infrastructure/Factories
mkdir -p tests/FastIntegrationTests.Tests/Infrastructure/Base
mkdir -p tests/FastIntegrationTests.Tests/Infrastructure/WebApp
mkdir -p tests/FastIntegrationTests.Tests/Products
mkdir -p tests/FastIntegrationTests.Tests/Orders
```

- [ ] **Step 2: Создать `FastIntegrationTests.Tests.csproj`**

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
    <PackageReference Include="Testcontainers.PostgreSql" Version="4.4.0" />
    <PackageReference Include="Testcontainers.MsSql" Version="4.4.0" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.15" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="8.0.1" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\FastIntegrationTests.Application\FastIntegrationTests.Application.csproj" />
    <ProjectReference Include="..\..\src\FastIntegrationTests.Infrastructure\FastIntegrationTests.Infrastructure.csproj" />
    <ProjectReference Include="..\..\src\FastIntegrationTests.WebApi\FastIntegrationTests.WebApi.csproj" />
  </ItemGroup>

  <ItemGroup>
    <None Update="appsettings.json">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </None>
    <None Update="xunit.runner.json">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </None>
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Создать `appsettings.json`**

Файл читается `ContainerFixture` для определения провайдера и типа контейнера.
Чтобы запустить тесты на MSSQL — поменяй `"PostgreSQL"` на `"MSSQL"`.

```json
{
  "DatabaseProvider": "PostgreSQL"
}
```

- [ ] **Step 4: Создать `xunit.runner.json`**

```json
{
  "$schema": "https://xunit.net/schema/current/xunit.runner.schema.json",
  "parallelizeAssembly": true,
  "parallelizeTestCollections": true,
  "maxParallelThreads": 4
}
```

- [ ] **Step 5: Восстановить пакеты**

```bash
dotnet restore tests/FastIntegrationTests.Tests
```

Ожидаемый вывод: `Restore completed` без ошибок.

- [ ] **Step 6: Зафиксировать**

```bash
git add tests/
git commit -m "feat: добавить тест-проект с зависимостями и конфигурацией xUnit"
```

---

## Task 2: Сделать Program доступным для WebApplicationFactory

**Files:**
- Modify: `src/FastIntegrationTests.WebApi/Program.cs` (добавить одну строку в конец)

`WebApplicationFactory<Program>` требует, чтобы класс `Program` был публичным. В .NET 8 с top-level statements он генерируется как internal. Фикс: объявить `public partial class Program {}`.

- [ ] **Step 1: Добавить в конец `Program.cs`**

```csharp
// Делает класс Program публичным для WebApplicationFactory в тестах
public partial class Program { }
```

- [ ] **Step 2: Проверить сборку**

```bash
dotnet build src/FastIntegrationTests.WebApi
```

Ожидаемый вывод: `Build succeeded`.

- [ ] **Step 3: Зафиксировать**

```bash
git add src/FastIntegrationTests.WebApi/Program.cs
git commit -m "feat: объявить partial class Program для WebApplicationFactory"
```

---

## Task 3: ContainerFixture и CollectionDefinitions

**Files:**
- Create: `tests/FastIntegrationTests.Tests/Infrastructure/Fixtures/ContainerFixture.cs`
- Create: `tests/FastIntegrationTests.Tests/Infrastructure/Fixtures/CollectionDefinitions.cs`

`ContainerFixture` — singleton на коллекцию тестов. Запускает один контейнер (PostgreSQL или MSSQL) при старте коллекции, останавливает при завершении. Предоставляет `ConnectionString` (без имени БД) и `Provider`.

- [ ] **Step 1: Создать `ContainerFixture.cs`**

```csharp
using DotNet.Testcontainers.Builders;
using Microsoft.Extensions.Configuration;
using Testcontainers.MsSql;
using Testcontainers.PostgreSql;

namespace FastIntegrationTests.Tests.Infrastructure.Fixtures;

/// <summary>
/// Запускает один Testcontainers-контейнер (PostgreSQL или MSSQL) на всю коллекцию тестов.
/// Тип контейнера определяется из appsettings.json тест-проекта (DatabaseProvider).
/// </summary>
public sealed class ContainerFixture : IAsyncLifetime
{
    private IAsyncDisposable _container = null!;

    /// <summary>Базовая строка подключения к контейнеру (без конкретной БД).</summary>
    public string ConnectionString { get; private set; } = null!;

    /// <summary>Имя провайдера: "PostgreSQL" или "MSSQL".</summary>
    public string Provider { get; private set; } = null!;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        Provider = config["DatabaseProvider"]
            ?? throw new InvalidOperationException(
                "DatabaseProvider не задан в appsettings.json тест-проекта.");

        if (Provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
        {
            var pg = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .Build();
            await pg.StartAsync();
            ConnectionString = pg.GetConnectionString();
            _container = pg;
        }
        else if (Provider.Equals("MSSQL", StringComparison.OrdinalIgnoreCase))
        {
            var mssql = new MsSqlBuilder()
                .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                .Build();
            await mssql.StartAsync();
            ConnectionString = mssql.GetConnectionString();
            _container = mssql;
        }
        else
        {
            throw new InvalidOperationException(
                $"Неизвестный провайдер '{Provider}'. Допустимые значения: PostgreSQL, MSSQL.");
        }
    }

    /// <inheritdoc />
    public async Task DisposeAsync() => await _container.DisposeAsync();
}
```

- [ ] **Step 2: Создать `CollectionDefinitions.cs`**

Четыре отдельные коллекции — четыре параллельных потока выполнения, каждый со своим контейнером.

```csharp
namespace FastIntegrationTests.Tests.Infrastructure.Fixtures;

/// <summary>Коллекция для сервисных тестов продуктов.</summary>
[CollectionDefinition("ProductsService")]
public class ProductsServiceCollection : ICollectionFixture<ContainerFixture> { }

/// <summary>Коллекция для HTTP-тестов продуктов.</summary>
[CollectionDefinition("ProductsApi")]
public class ProductsApiCollection : ICollectionFixture<ContainerFixture> { }

/// <summary>Коллекция для сервисных тестов заказов.</summary>
[CollectionDefinition("OrdersService")]
public class OrdersServiceCollection : ICollectionFixture<ContainerFixture> { }

/// <summary>Коллекция для HTTP-тестов заказов.</summary>
[CollectionDefinition("OrdersApi")]
public class OrdersApiCollection : ICollectionFixture<ContainerFixture> { }
```

- [ ] **Step 3: Зафиксировать**

```bash
git add tests/
git commit -m "feat: добавить ContainerFixture и определения коллекций"
```

---

## Task 4: TestDbFactory

**Files:**
- Create: `tests/FastIntegrationTests.Tests/Infrastructure/Factories/TestDbFactory.cs`

`TestDbFactory` создаёт уникальную БД внутри контейнера, применяет миграции EF Core и возвращает готовый `ShopDbContext`. Каждый тест получает полностью изолированную базу.

- [ ] **Step 1: Создать `TestDbFactory.cs`**

```csharp
using FastIntegrationTests.Infrastructure.Data;
using FastIntegrationTests.Tests.Infrastructure.Fixtures;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FastIntegrationTests.Tests.Infrastructure.Factories;

/// <summary>
/// Создаёт уникальную тестовую базу данных внутри контейнера и применяет миграции EF Core.
/// </summary>
public sealed class TestDbFactory
{
    private readonly ContainerFixture _fixture;

    /// <param name="fixture">Запущенный контейнер с СУБД.</param>
    public TestDbFactory(ContainerFixture fixture) => _fixture = fixture;

    /// <summary>
    /// Создаёт БД с именем <c>test_{guid}</c>, применяет миграции и возвращает контекст.
    /// </summary>
    /// <param name="ct">Токен отмены операции.</param>
    public async Task<ShopDbContext> CreateAsync(CancellationToken ct = default)
    {
        var dbName = $"test_{Guid.NewGuid():N}";

        DbContextOptions<ShopDbContext> options;

        if (_fixture.Provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
        {
            var csb = new NpgsqlConnectionStringBuilder(_fixture.ConnectionString)
            {
                Database = dbName
            };
            options = new DbContextOptionsBuilder<ShopDbContext>()
                .UseNpgsql(csb.ConnectionString)
                .Options;
        }
        else
        {
            var csb = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(_fixture.ConnectionString)
            {
                InitialCatalog = dbName
            };
            options = new DbContextOptionsBuilder<ShopDbContext>()
                .UseSqlServer(csb.ConnectionString)
                .Options;
        }

        var context = new ShopDbContext(options);
        await context.Database.MigrateAsync(ct);
        return context;
    }
}
```

- [ ] **Step 2: Зафиксировать**

```bash
git add tests/
git commit -m "feat: добавить TestDbFactory для database-per-test изоляции"
```

---

## Task 5: ServiceTestBase

**Files:**
- Create: `tests/FastIntegrationTests.Tests/Infrastructure/Base/ServiceTestBase.cs`

Базовый класс для сервисных тестов. В `InitializeAsync` создаёт свежую изолированную БД и собирает граф объектов (репозитории + сервисы) вручную. В `DisposeAsync` удаляет БД.

- [ ] **Step 1: Создать `ServiceTestBase.cs`**

```csharp
using FastIntegrationTests.Application.Interfaces;
using FastIntegrationTests.Application.Services;
using FastIntegrationTests.Infrastructure.Data;
using FastIntegrationTests.Infrastructure.Repositories;
using FastIntegrationTests.Tests.Infrastructure.Factories;
using FastIntegrationTests.Tests.Infrastructure.Fixtures;

namespace FastIntegrationTests.Tests.Infrastructure.Base;

/// <summary>
/// Базовый класс для интеграционных тестов сервисного уровня.
/// Создаёт изолированную БД на каждый тест, предоставляет готовые сервисы.
/// </summary>
public abstract class ServiceTestBase : IAsyncLifetime
{
    private readonly ContainerFixture _fixture;
    private ShopDbContext _context = null!;

    /// <summary>Сервис для работы с товарами.</summary>
    protected IProductService ProductService { get; private set; } = null!;

    /// <summary>Сервис для работы с заказами.</summary>
    protected IOrderService OrderService { get; private set; } = null!;

    /// <param name="fixture">Запущенный контейнер с СУБД.</param>
    protected ServiceTestBase(ContainerFixture fixture) => _fixture = fixture;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        var factory = new TestDbFactory(_fixture);
        _context = await factory.CreateAsync();

        var productRepo = new ProductRepository(_context);
        var orderRepo = new OrderRepository(_context);

        ProductService = new ProductService(productRepo);
        OrderService = new OrderService(orderRepo, productRepo);
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }
}
```

- [ ] **Step 2: Зафиксировать**

```bash
git add tests/
git commit -m "feat: добавить ServiceTestBase с database-per-test lifecycle"
```

---

## Task 6: TestWebApplicationFactory, ApiTestBase и GlobalUsings

**Files:**
- Create: `tests/FastIntegrationTests.Tests/Infrastructure/WebApp/TestWebApplicationFactory.cs`
- Create: `tests/FastIntegrationTests.Tests/Infrastructure/Base/ApiTestBase.cs`
- Create: `tests/FastIntegrationTests.Tests/GlobalUsings.cs`

`TestWebApplicationFactory` переопределяет конфигурацию приложения, подменяя строку подключения тестовой. `ApiTestBase` создаёт БД, затем поднимает `TestWebApplicationFactory` — приложение подключается к уже готовой тестовой БД.

- [ ] **Step 1: Создать `TestWebApplicationFactory.cs`**

```csharp
using FastIntegrationTests.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FastIntegrationTests.Tests.Infrastructure.WebApp;

/// <summary>
/// WebApplicationFactory с подменой строки подключения к БД.
/// Переопределяет DatabaseProvider и ConnectionStrings через ConfigureAppConfiguration,
/// а также заменяет регистрацию DbContext через ConfigureTestServices.
/// </summary>
public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _provider;
    private readonly string _connectionString;

    /// <param name="provider">Провайдер БД: "PostgreSQL" или "MSSQL".</param>
    /// <param name="connectionString">Строка подключения к тестовой БД.</param>
    public TestWebApplicationFactory(string provider, string connectionString)
    {
        _provider = provider;
        _connectionString = connectionString;
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Подменяем конфигурацию: Program.cs читает DatabaseProvider и ConnectionStrings из builder.Configuration
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DatabaseProvider"] = _provider,
                [$"ConnectionStrings:{_provider}"] = _connectionString,
            });
        });

        // Гарантируем правильный DbContext на случай, если Program.cs уже зарегистрировал другой
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ShopDbContext>>();
            services.RemoveAll<ShopDbContext>();

            if (_provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
                services.AddDbContext<ShopDbContext>(o => o.UseNpgsql(_connectionString));
            else
                services.AddDbContext<ShopDbContext>(o => o.UseSqlServer(_connectionString));
        });
    }
}
```

- [ ] **Step 2: Создать `ApiTestBase.cs`**

```csharp
using FastIntegrationTests.Infrastructure.Data;
using FastIntegrationTests.Tests.Infrastructure.Factories;
using FastIntegrationTests.Tests.Infrastructure.Fixtures;
using FastIntegrationTests.Tests.Infrastructure.WebApp;

namespace FastIntegrationTests.Tests.Infrastructure.Base;

/// <summary>
/// Базовый класс для интеграционных тестов HTTP-уровня.
/// Создаёт изолированную БД на каждый тест и поднимает TestWebApplicationFactory.
/// </summary>
public abstract class ApiTestBase : IAsyncLifetime
{
    private readonly ContainerFixture _fixture;
    private ShopDbContext _schemaContext = null!;
    private TestWebApplicationFactory _factory = null!;

    /// <summary>HTTP-клиент для обращений к тестируемому API.</summary>
    protected HttpClient Client { get; private set; } = null!;

    /// <param name="fixture">Запущенный контейнер с СУБД.</param>
    protected ApiTestBase(ContainerFixture fixture) => _fixture = fixture;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        // Создаём изолированную БД и применяем миграции
        var dbFactory = new TestDbFactory(_fixture);
        _schemaContext = await dbFactory.CreateAsync();

        // Передаём строку подключения к уже готовой БД в фабрику приложения
        var connectionString = _schemaContext.Database.GetConnectionString()!;
        _factory = new TestWebApplicationFactory(_fixture.Provider, connectionString);
        Client = _factory.CreateClient();
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        Client.Dispose();
        await _factory.DisposeAsync();
        await _schemaContext.Database.EnsureDeletedAsync();
        await _schemaContext.DisposeAsync();
    }
}
```

- [ ] **Step 3: Создать `GlobalUsings.cs`**

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
global using FastIntegrationTests.Tests.Infrastructure.Base;
global using FastIntegrationTests.Tests.Infrastructure.Factories;
global using FastIntegrationTests.Tests.Infrastructure.Fixtures;
global using FastIntegrationTests.Tests.Infrastructure.WebApp;
global using Microsoft.EntityFrameworkCore;
global using Xunit;
```

- [ ] **Step 4: Зафиксировать**

```bash
git add tests/
git commit -m "feat: добавить TestWebApplicationFactory, ApiTestBase и GlobalUsings"
```

---

## Task 7: Проверить сборку всего проекта

**Files:** нет изменений

- [ ] **Step 1: Собрать тест-проект**

```bash
dotnet build tests/FastIntegrationTests.Tests
```

Ожидаемый вывод: `Build succeeded. 0 Error(s)`.

Если есть ошибки — исправь их перед тем как переходить к написанию тестов. Типичные проблемы:
- `NpgsqlConnectionStringBuilder` не найден → добавь `using Npgsql;` в `TestDbFactory.cs`
- `SqlConnectionStringBuilder` не найден → добавь `using Microsoft.Data.SqlClient;`
- `WebApplicationFactory<Program>` — `Program` не найден → убедись, что Task 2 выполнен

---

## Task 8: ProductServiceTests (11 тестов)

**Files:**
- Create: `tests/FastIntegrationTests.Tests/Products/ProductServiceTests.cs`

- [ ] **Step 1: Создать `ProductServiceTests.cs`**

```csharp
namespace FastIntegrationTests.Tests.Products;

/// <summary>
/// Интеграционные тесты сервисного уровня для ProductService.
/// Каждый тест работает с изолированной базой данных.
/// </summary>
[Collection("ProductsService")]
public class ProductServiceTests : ServiceTestBase
{
    /// <param name="fixture">Контейнер PostgreSQL/MSSQL, общий для коллекции.</param>
    public ProductServiceTests(ContainerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task GetAllAsync_WhenNoProducts_ReturnsEmptyList()
    {
        var result = await ProductService.GetAllAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_WhenProductsExist_ReturnsAllProducts()
    {
        await ProductService.CreateAsync(new CreateProductRequest { Name = "Товар 1", Description = "Описание 1", Price = 100m });
        await ProductService.CreateAsync(new CreateProductRequest { Name = "Товар 2", Description = "Описание 2", Price = 200m });

        var result = await ProductService.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetByIdAsync_WhenProductExists_ReturnsProduct()
    {
        var created = await ProductService.CreateAsync(new CreateProductRequest { Name = "Ноутбук", Description = "Core i9", Price = 50_000m });

        var result = await ProductService.GetByIdAsync(created.Id);

        Assert.Equal(created.Id, result.Id);
        Assert.Equal("Ноутбук", result.Name);
        Assert.Equal("Core i9", result.Description);
        Assert.Equal(50_000m, result.Price);
    }

    [Fact]
    public async Task GetByIdAsync_WhenProductNotFound_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => ProductService.GetByIdAsync(999));
    }

    [Fact]
    public async Task CreateAsync_PersistsProductAndReturnsWithAssignedId()
    {
        var request = new CreateProductRequest { Name = "Мышь", Description = "Беспроводная", Price = 2_500m };

        var result = await ProductService.CreateAsync(request);

        Assert.True(result.Id > 0);
        Assert.Equal("Мышь", result.Name);
        Assert.Equal("Беспроводная", result.Description);
        Assert.Equal(2_500m, result.Price);
    }

    [Fact]
    public async Task CreateAsync_SetsCreatedAtAutomatically()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var result = await ProductService.CreateAsync(new CreateProductRequest { Name = "Клавиатура", Price = 3_000m });

        var after = DateTime.UtcNow.AddSeconds(1);
        Assert.InRange(result.CreatedAt, before, after);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesProductFieldsInDatabase()
    {
        var created = await ProductService.CreateAsync(new CreateProductRequest { Name = "Старое название", Price = 1_000m });
        var updateRequest = new UpdateProductRequest { Name = "Новое название", Description = "Новое описание", Price = 1_500m };

        var updated = await ProductService.UpdateAsync(created.Id, updateRequest);

        Assert.Equal("Новое название", updated.Name);
        Assert.Equal("Новое описание", updated.Description);
        Assert.Equal(1_500m, updated.Price);

        // Проверяем сохранение в БД через повторный запрос
        var fetched = await ProductService.GetByIdAsync(created.Id);
        Assert.Equal("Новое название", fetched.Name);
        Assert.Equal(1_500m, fetched.Price);
    }

    [Fact]
    public async Task UpdateAsync_WhenProductNotFound_ThrowsNotFoundException()
    {
        var request = new UpdateProductRequest { Name = "Название", Description = string.Empty, Price = 100m };

        await Assert.ThrowsAsync<NotFoundException>(() => ProductService.UpdateAsync(999, request));
    }

    [Fact]
    public async Task DeleteAsync_RemovesProductFromDatabase()
    {
        var created = await ProductService.CreateAsync(new CreateProductRequest { Name = "Временный товар", Price = 500m });

        await ProductService.DeleteAsync(created.Id);

        await Assert.ThrowsAsync<NotFoundException>(() => ProductService.GetByIdAsync(created.Id));
    }

    [Fact]
    public async Task DeleteAsync_WhenProductNotFound_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => ProductService.DeleteAsync(999));
    }

    [Fact]
    public async Task DeleteAsync_WhenProductHasOrderItems_ThrowsDbUpdateException()
    {
        // Создаём товар и заказ с этим товаром
        var product = await ProductService.CreateAsync(new CreateProductRequest { Name = "Товар в заказе", Price = 1_000m });
        await OrderService.CreateAsync(new CreateOrderRequest
        {
            Items = new List<OrderItemRequest> { new() { ProductId = product.Id, Quantity = 1 } }
        });

        // FK Restrict: нельзя удалить товар, на который ссылаются позиции заказа
        await Assert.ThrowsAsync<DbUpdateException>(() => ProductService.DeleteAsync(product.Id));
    }
}
```

- [ ] **Step 2: Запустить тесты коллекции ProductsService**

```bash
dotnet test tests/FastIntegrationTests.Tests --filter "Collection=ProductsService" --verbosity normal
```

Ожидаемый вывод: `Passed! - Failed: 0, Passed: 11, Skipped: 0`.

При первом запуске Testcontainers скачает образ PostgreSQL (~80 МБ) — это нормально.

- [ ] **Step 3: Зафиксировать**

```bash
git add tests/FastIntegrationTests.Tests/Products/ProductServiceTests.cs
git commit -m "test: добавить 11 сервисных тестов для ProductService"
```

---

## Task 9: ProductsApiTests (10 тестов)

**Files:**
- Create: `tests/FastIntegrationTests.Tests/Products/ProductsApiTests.cs`

- [ ] **Step 1: Создать `ProductsApiTests.cs`**

```csharp
namespace FastIntegrationTests.Tests.Products;

/// <summary>
/// Интеграционные тесты HTTP-уровня для ProductsController.
/// Каждый тест работает с изолированной базой данных через реальный HTTP-клиент.
/// </summary>
[Collection("ProductsApi")]
public class ProductsApiTests : ApiTestBase
{
    /// <param name="fixture">Контейнер PostgreSQL/MSSQL, общий для коллекции.</param>
    public ProductsApiTests(ContainerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task GetAll_WhenNoProducts_Returns200WithEmptyArray()
    {
        var response = await Client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>();
        Assert.Empty(products!);
    }

    [Fact]
    public async Task GetAll_WhenProductsExist_Returns200WithProducts()
    {
        await CreateProductAsync("Товар 1", 100m);
        await CreateProductAsync("Товар 2", 200m);

        var response = await Client.GetAsync("/api/products");
        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, products!.Count);
    }

    [Fact]
    public async Task GetById_WhenProductExists_Returns200WithProduct()
    {
        var created = await CreateProductAsync("Ноутбук", 50_000m);

        var response = await Client.GetAsync($"/api/products/{created.Id}");
        var product = await response.Content.ReadFromJsonAsync<ProductDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(created.Id, product!.Id);
        Assert.Equal("Ноутбук", product.Name);
    }

    [Fact]
    public async Task GetById_WhenProductNotFound_Returns404()
    {
        var response = await Client.GetAsync("/api/products/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_ValidRequest_Returns201WithLocationHeaderAndId()
    {
        var request = new CreateProductRequest { Name = "Монитор", Description = "4K", Price = 25_000m };

        var response = await Client.PostAsJsonAsync("/api/products", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        Assert.True(product!.Id > 0);
        Assert.Equal("Монитор", product.Name);
    }

    [Fact]
    public async Task Update_WhenProductExists_Returns200WithUpdatedFields()
    {
        var created = await CreateProductAsync("Старое", 100m);
        var updateRequest = new UpdateProductRequest { Name = "Новое", Description = "Обновлено", Price = 200m };

        var response = await Client.PutAsJsonAsync($"/api/products/{created.Id}", updateRequest);
        var updated = await response.Content.ReadFromJsonAsync<ProductDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Новое", updated!.Name);
        Assert.Equal("Обновлено", updated.Description);
        Assert.Equal(200m, updated.Price);
    }

    [Fact]
    public async Task Update_WhenProductNotFound_Returns404()
    {
        var request = new UpdateProductRequest { Name = "Название", Description = string.Empty, Price = 100m };

        var response = await Client.PutAsJsonAsync("/api/products/999", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WhenProductExists_Returns204()
    {
        var created = await CreateProductAsync("Удаляемый", 100m);

        var response = await Client.DeleteAsync($"/api/products/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WhenProductNotFound_Returns404()
    {
        var response = await Client.DeleteAsync("/api/products/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateThenGetById_DataMatchesExactly()
    {
        var createRequest = new CreateProductRequest { Name = "Системный блок", Description = "Core i9", Price = 80_000m };
        var createResponse = await Client.PostAsJsonAsync("/api/products", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductDto>();

        var getResponse = await Client.GetAsync($"/api/products/{created!.Id}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<ProductDto>();

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.Equal(created.Id, fetched!.Id);
        Assert.Equal("Системный блок", fetched.Name);
        Assert.Equal("Core i9", fetched.Description);
        Assert.Equal(80_000m, fetched.Price);
    }

    // --- helpers ---

    private async Task<ProductDto> CreateProductAsync(string name, decimal price)
    {
        var response = await Client.PostAsJsonAsync("/api/products",
            new CreateProductRequest { Name = name, Price = price });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductDto>())!;
    }
}
```

- [ ] **Step 2: Запустить тесты коллекции ProductsApi**

```bash
dotnet test tests/FastIntegrationTests.Tests --filter "Collection=ProductsApi" --verbosity normal
```

Ожидаемый вывод: `Passed! - Failed: 0, Passed: 10, Skipped: 0`.

- [ ] **Step 3: Зафиксировать**

```bash
git add tests/FastIntegrationTests.Tests/Products/ProductsApiTests.cs
git commit -m "test: добавить 10 HTTP-тестов для ProductsController"
```

---

## Task 10: OrderServiceTests (16 тестов)

**Files:**
- Create: `tests/FastIntegrationTests.Tests/Orders/OrderServiceTests.cs`

- [ ] **Step 1: Создать `OrderServiceTests.cs`**

```csharp
namespace FastIntegrationTests.Tests.Orders;

/// <summary>
/// Интеграционные тесты сервисного уровня для OrderService.
/// Проверяют CRUD, расчёт суммы, фиксацию цены и все переходы статусов.
/// </summary>
[Collection("OrdersService")]
public class OrderServiceTests : ServiceTestBase
{
    /// <param name="fixture">Контейнер PostgreSQL/MSSQL, общий для коллекции.</param>
    public OrderServiceTests(ContainerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task GetAllAsync_WhenNoOrders_ReturnsEmptyList()
    {
        var result = await OrderService.GetAllAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_WhenOrdersExist_ReturnsAllOrders()
    {
        var product = await ProductService.CreateAsync(new CreateProductRequest { Name = "Товар", Price = 100m });
        await OrderService.CreateAsync(new CreateOrderRequest
        {
            Items = new List<OrderItemRequest> { new() { ProductId = product.Id, Quantity = 1 } }
        });
        await OrderService.CreateAsync(new CreateOrderRequest
        {
            Items = new List<OrderItemRequest> { new() { ProductId = product.Id, Quantity = 2 } }
        });

        var result = await OrderService.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetByIdAsync_WhenOrderExists_ReturnsOrderWithItems()
    {
        var product = await ProductService.CreateAsync(new CreateProductRequest { Name = "Товар", Price = 500m });
        var created = await OrderService.CreateAsync(new CreateOrderRequest
        {
            Items = new List<OrderItemRequest> { new() { ProductId = product.Id, Quantity = 3 } }
        });

        var result = await OrderService.GetByIdAsync(created.Id);

        Assert.Equal(created.Id, result.Id);
        Assert.Single(result.Items);
        Assert.Equal(product.Id, result.Items[0].ProductId);
        Assert.Equal(3, result.Items[0].Quantity);
    }

    [Fact]
    public async Task GetByIdAsync_WhenOrderNotFound_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => OrderService.GetByIdAsync(999));
    }

    [Fact]
    public async Task CreateAsync_CalculatesTotalAmountCorrectly()
    {
        var product1 = await ProductService.CreateAsync(new CreateProductRequest { Name = "Товар 1", Price = 100m });
        var product2 = await ProductService.CreateAsync(new CreateProductRequest { Name = "Товар 2", Price = 200m });

        var order = await OrderService.CreateAsync(new CreateOrderRequest
        {
            Items = new List<OrderItemRequest>
            {
                new() { ProductId = product1.Id, Quantity = 2 }, // 2 * 100 = 200
                new() { ProductId = product2.Id, Quantity = 3 }, // 3 * 200 = 600
            }
        });

        Assert.Equal(800m, order.TotalAmount); // 200 + 600
    }

    [Fact]
    public async Task CreateAsync_SetsUnitPriceFromCurrentProductPrice()
    {
        var product = await ProductService.CreateAsync(new CreateProductRequest { Name = "Товар", Price = 999m });

        var order = await OrderService.CreateAsync(new CreateOrderRequest
        {
            Items = new List<OrderItemRequest> { new() { ProductId = product.Id, Quantity = 1 } }
        });

        Assert.Equal(999m, order.Items[0].UnitPrice);
    }

    [Fact]
    public async Task CreateAsync_WhenProductNotFound_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => OrderService.CreateAsync(new CreateOrderRequest
        {
            Items = new List<OrderItemRequest> { new() { ProductId = 999, Quantity = 1 } }
        }));
    }

    [Fact]
    public async Task CreateAsync_NewOrderHasStatusNew()
    {
        var product = await ProductService.CreateAsync(new CreateProductRequest { Name = "Товар", Price = 100m });

        var order = await OrderService.CreateAsync(new CreateOrderRequest
        {
            Items = new List<OrderItemRequest> { new() { ProductId = product.Id, Quantity = 1 } }
        });

        Assert.Equal(OrderStatus.New, order.Status);
    }

    [Fact]
    public async Task ConfirmAsync_ChangesStatusFromNewToConfirmed()
    {
        var order = await CreateOrderAsync();

        var confirmed = await OrderService.ConfirmAsync(order.Id);

        Assert.Equal(OrderStatus.Confirmed, confirmed.Status);
    }

    [Fact]
    public async Task ShipAsync_ChangesStatusFromConfirmedToShipped()
    {
        var order = await CreateOrderAsync();
        await OrderService.ConfirmAsync(order.Id);

        var shipped = await OrderService.ShipAsync(order.Id);

        Assert.Equal(OrderStatus.Shipped, shipped.Status);
    }

    [Fact]
    public async Task CompleteAsync_ChangesStatusFromShippedToCompleted()
    {
        var order = await CreateOrderAsync();
        await OrderService.ConfirmAsync(order.Id);
        await OrderService.ShipAsync(order.Id);

        var completed = await OrderService.CompleteAsync(order.Id);

        Assert.Equal(OrderStatus.Completed, completed.Status);
    }

    [Fact]
    public async Task CancelAsync_ChangesStatusFromNewToCancelled()
    {
        var order = await CreateOrderAsync();

        var cancelled = await OrderService.CancelAsync(order.Id);

        Assert.Equal(OrderStatus.Cancelled, cancelled.Status);
    }

    [Fact]
    public async Task CancelAsync_ChangesStatusFromConfirmedToCancelled()
    {
        var order = await CreateOrderAsync();
        await OrderService.ConfirmAsync(order.Id);

        var cancelled = await OrderService.CancelAsync(order.Id);

        Assert.Equal(OrderStatus.Cancelled, cancelled.Status);
    }

    [Fact]
    public async Task ConfirmAsync_WhenOrderIsCompleted_ThrowsInvalidOrderStatusTransitionException()
    {
        var order = await CreateOrderAsync();
        await OrderService.ConfirmAsync(order.Id);
        await OrderService.ShipAsync(order.Id);
        await OrderService.CompleteAsync(order.Id);

        await Assert.ThrowsAsync<InvalidOrderStatusTransitionException>(
            () => OrderService.ConfirmAsync(order.Id));
    }

    [Fact]
    public async Task CancelAsync_WhenOrderIsShipped_ThrowsInvalidOrderStatusTransitionException()
    {
        var order = await CreateOrderAsync();
        await OrderService.ConfirmAsync(order.Id);
        await OrderService.ShipAsync(order.Id);

        await Assert.ThrowsAsync<InvalidOrderStatusTransitionException>(
            () => OrderService.CancelAsync(order.Id));
    }

    [Fact]
    public async Task FullLifecycle_CreateConfirmShipComplete_StatusCorrectAtEachStep()
    {
        var order = await CreateOrderAsync();
        Assert.Equal(OrderStatus.New, order.Status);

        var confirmed = await OrderService.ConfirmAsync(order.Id);
        Assert.Equal(OrderStatus.Confirmed, confirmed.Status);

        var shipped = await OrderService.ShipAsync(order.Id);
        Assert.Equal(OrderStatus.Shipped, shipped.Status);

        var completed = await OrderService.CompleteAsync(order.Id);
        Assert.Equal(OrderStatus.Completed, completed.Status);

        // Проверяем финальный статус через повторный запрос
        var fetched = await OrderService.GetByIdAsync(order.Id);
        Assert.Equal(OrderStatus.Completed, fetched.Status);
    }

    // --- helpers ---

    private async Task<OrderDto> CreateOrderAsync()
    {
        var product = await ProductService.CreateAsync(new CreateProductRequest { Name = "Товар", Price = 100m });
        return await OrderService.CreateAsync(new CreateOrderRequest
        {
            Items = new List<OrderItemRequest> { new() { ProductId = product.Id, Quantity = 1 } }
        });
    }
}
```

- [ ] **Step 2: Запустить тесты коллекции OrdersService**

```bash
dotnet test tests/FastIntegrationTests.Tests --filter "Collection=OrdersService" --verbosity normal
```

Ожидаемый вывод: `Passed! - Failed: 0, Passed: 16, Skipped: 0`.

- [ ] **Step 3: Зафиксировать**

```bash
git add tests/FastIntegrationTests.Tests/Orders/OrderServiceTests.cs
git commit -m "test: добавить 16 сервисных тестов для OrderService"
```

---

## Task 11: OrdersApiTests (15 тестов)

**Files:**
- Create: `tests/FastIntegrationTests.Tests/Orders/OrdersApiTests.cs`

- [ ] **Step 1: Создать `OrdersApiTests.cs`**

```csharp
namespace FastIntegrationTests.Tests.Orders;

/// <summary>
/// Интеграционные тесты HTTP-уровня для OrdersController.
/// Проверяют HTTP-статусы, тела ответов и полный жизненный цикл заказа.
/// </summary>
[Collection("OrdersApi")]
public class OrdersApiTests : ApiTestBase
{
    /// <param name="fixture">Контейнер PostgreSQL/MSSQL, общий для коллекции.</param>
    public OrdersApiTests(ContainerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task GetAll_WhenNoOrders_Returns200WithEmptyArray()
    {
        var response = await Client.GetAsync("/api/orders");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var orders = await response.Content.ReadFromJsonAsync<List<OrderDto>>();
        Assert.Empty(orders!);
    }

    [Fact]
    public async Task GetAll_WhenOrdersExist_Returns200WithOrders()
    {
        await CreateOrderWithProductAsync();
        await CreateOrderWithProductAsync();

        var response = await Client.GetAsync("/api/orders");
        var orders = await response.Content.ReadFromJsonAsync<List<OrderDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, orders!.Count);
    }

    [Fact]
    public async Task GetById_WhenOrderExists_Returns200WithItems()
    {
        var created = await CreateOrderWithProductAsync();

        var response = await Client.GetAsync($"/api/orders/{created.Id}");
        var order = await response.Content.ReadFromJsonAsync<OrderDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(created.Id, order!.Id);
        Assert.Single(order.Items);
    }

    [Fact]
    public async Task GetById_WhenOrderNotFound_Returns404()
    {
        var response = await Client.GetAsync("/api/orders/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_ValidRequest_Returns201WithCalculatedTotalAmount()
    {
        var product = await CreateProductAsync("Процессор", 15_000m);
        var request = new CreateOrderRequest
        {
            Items = new List<OrderItemRequest> { new() { ProductId = product.Id, Quantity = 2 } }
        };

        var response = await Client.PostAsJsonAsync("/api/orders", request);
        var order = await response.Content.ReadFromJsonAsync<OrderDto>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.True(order!.Id > 0);
        Assert.Equal(30_000m, order.TotalAmount); // 2 * 15000
    }

    [Fact]
    public async Task Create_WhenProductNotFound_Returns404()
    {
        var request = new CreateOrderRequest
        {
            Items = new List<OrderItemRequest> { new() { ProductId = 999, Quantity = 1 } }
        };

        var response = await Client.PostAsJsonAsync("/api/orders", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Confirm_WhenOrderIsNew_Returns200WithConfirmedStatus()
    {
        var order = await CreateOrderWithProductAsync();

        var response = await Client.PostAsync($"/api/orders/{order.Id}/confirm", null);
        var confirmed = await response.Content.ReadFromJsonAsync<OrderDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(OrderStatus.Confirmed, confirmed!.Status);
    }

    [Fact]
    public async Task Ship_WhenOrderIsConfirmed_Returns200WithShippedStatus()
    {
        var order = await CreateOrderWithProductAsync();
        await Client.PostAsync($"/api/orders/{order.Id}/confirm", null);

        var response = await Client.PostAsync($"/api/orders/{order.Id}/ship", null);
        var shipped = await response.Content.ReadFromJsonAsync<OrderDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(OrderStatus.Shipped, shipped!.Status);
    }

    [Fact]
    public async Task Complete_WhenOrderIsShipped_Returns200WithCompletedStatus()
    {
        var order = await CreateOrderWithProductAsync();
        await Client.PostAsync($"/api/orders/{order.Id}/confirm", null);
        await Client.PostAsync($"/api/orders/{order.Id}/ship", null);

        var response = await Client.PostAsync($"/api/orders/{order.Id}/complete", null);
        var completed = await response.Content.ReadFromJsonAsync<OrderDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(OrderStatus.Completed, completed!.Status);
    }

    [Fact]
    public async Task Cancel_WhenOrderIsNew_Returns200WithCancelledStatus()
    {
        var order = await CreateOrderWithProductAsync();

        var response = await Client.PostAsync($"/api/orders/{order.Id}/cancel", null);
        var cancelled = await response.Content.ReadFromJsonAsync<OrderDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(OrderStatus.Cancelled, cancelled!.Status);
    }

    [Fact]
    public async Task Cancel_WhenOrderIsConfirmed_Returns200WithCancelledStatus()
    {
        var order = await CreateOrderWithProductAsync();
        await Client.PostAsync($"/api/orders/{order.Id}/confirm", null);

        var response = await Client.PostAsync($"/api/orders/{order.Id}/cancel", null);
        var cancelled = await response.Content.ReadFromJsonAsync<OrderDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(OrderStatus.Cancelled, cancelled!.Status);
    }

    [Fact]
    public async Task Confirm_WhenOrderNotFound_Returns404()
    {
        var response = await Client.PostAsync("/api/orders/999/confirm", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Ship_WhenOrderIsNew_InvalidTransition_Returns400()
    {
        var order = await CreateOrderWithProductAsync();

        // New → Shipped недопустимо: пропущен Confirmed
        var response = await Client.PostAsync($"/api/orders/{order.Id}/ship", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_WhenOrderIsShipped_InvalidTransition_Returns400()
    {
        var order = await CreateOrderWithProductAsync();
        await Client.PostAsync($"/api/orders/{order.Id}/confirm", null);
        await Client.PostAsync($"/api/orders/{order.Id}/ship", null);

        // Shipped → Cancelled недопустимо
        var response = await Client.PostAsync($"/api/orders/{order.Id}/cancel", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateThenGetById_OrderItemsMatchRequest()
    {
        var product = await CreateProductAsync("Видеокарта", 40_000m);
        var createRequest = new CreateOrderRequest
        {
            Items = new List<OrderItemRequest> { new() { ProductId = product.Id, Quantity = 2 } }
        };

        var createResponse = await Client.PostAsJsonAsync("/api/orders", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<OrderDto>();

        var getResponse = await Client.GetAsync($"/api/orders/{created!.Id}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<OrderDto>();

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.Single(fetched!.Items);
        Assert.Equal(product.Id, fetched.Items[0].ProductId);
        Assert.Equal(2, fetched.Items[0].Quantity);
        Assert.Equal(40_000m, fetched.Items[0].UnitPrice);
    }

    [Fact]
    public async Task FullLifecycle_CreateConfirmShipCompleteGetById_StatusIsCompleted()
    {
        var order = await CreateOrderWithProductAsync();

        await Client.PostAsync($"/api/orders/{order.Id}/confirm", null);
        await Client.PostAsync($"/api/orders/{order.Id}/ship", null);
        await Client.PostAsync($"/api/orders/{order.Id}/complete", null);

        var response = await Client.GetAsync($"/api/orders/{order.Id}");
        var completed = await response.Content.ReadFromJsonAsync<OrderDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(OrderStatus.Completed, completed!.Status);
    }

    // --- helpers ---

    private async Task<ProductDto> CreateProductAsync(string name, decimal price)
    {
        var response = await Client.PostAsJsonAsync("/api/products",
            new CreateProductRequest { Name = name, Price = price });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductDto>())!;
    }

    private async Task<OrderDto> CreateOrderWithProductAsync()
    {
        var product = await CreateProductAsync("Товар", 100m);
        var response = await Client.PostAsJsonAsync("/api/orders", new CreateOrderRequest
        {
            Items = new List<OrderItemRequest> { new() { ProductId = product.Id, Quantity = 1 } }
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OrderDto>())!;
    }
}
```

- [ ] **Step 2: Запустить тесты коллекции OrdersApi**

```bash
dotnet test tests/FastIntegrationTests.Tests --filter "Collection=OrdersApi" --verbosity normal
```

Ожидаемый вывод: `Passed! - Failed: 0, Passed: 15, Skipped: 0`.

- [ ] **Step 3: Запустить все 52 теста параллельно**

```bash
dotnet test tests/FastIntegrationTests.Tests --verbosity normal
```

Ожидаемый вывод: `Passed! - Failed: 0, Passed: 52, Skipped: 0`.

- [ ] **Step 4: Зафиксировать**

```bash
git add tests/FastIntegrationTests.Tests/Orders/OrdersApiTests.cs
git commit -m "test: добавить 15 HTTP-тестов для OrdersController"
```

---

## Самопроверка плана

**Покрытие спецификации:**
- ✅ Все 52 теста из спецификации реализованы (Tasks 8–11)
- ✅ Database-per-test через `TestDbFactory` (Task 4)
- ✅ Параллелизм 4 через 4 коллекции + `maxParallelThreads` (Tasks 1, 3)
- ✅ HTTP-уровень через `TestWebApplicationFactory` + `ApiTestBase` (Task 6)
- ✅ Сервисный уровень через `ServiceTestBase` (Task 5)
- ✅ FK-ограничение (тест 11 в ProductServiceTests)
- ✅ Все переходы статусов заказа (тесты 30–37 в OrderServiceTests)
- ✅ Коммиты после каждого логического шага

**Примечание о `GetAllAsync` для заказов:** `OrderRepository.GetAllAsync` не делает `.Include(o => o.Items)`, поэтому `OrderDto.Items` будет пустым списком при вызове `GET /api/orders`. Это ожидаемое поведение — тесты `GetAll` проверяют только количество заказов, не позиции.
