# Дизайн: Миграция тестов на IntegreSQL + упрощение инфраструктуры

## Контекст

Проект содержит две тест-инфраструктуры: Testcontainers-напрямую и IntegreSQL. Цель:
- Перевести основные тест-классы на IntegreSQL (быстрее)
- Упростить Testcontainers-инфраструктуру (убрать MSSQL, оставить как baseline)
- Исправить дизайн `AppServiceTestBase` — убрать захардкоженные сервисы, отдать создание SUT тест-классам

## 1. Удаление MSSQL

Убрать полностью из проекта:

| Файл | Что меняем |
|---|---|
| `ContainerFixture.cs` | Убрать `MsSqlBuilder`, `Testcontainers.MsSql`, ветку MSSQL |
| `TestWebApplicationFactory.cs` | Убрать ветку `UseSqlServer` в `ConfigureTestServices` |
| `Program.cs` | Убрать регистрацию MSSQL DbContext |
| `Infrastructure/Extensions/ServiceCollectionExtensions.cs` | Убрать MSSQL-ветку |
| `docker-compose.yml` | Убрать сервис `mssql` |
| `appsettings.Development.json.example` | Убрать MSSQL строку подключения и упоминание провайдера |
| `CLAUDE.md` | Убрать команды и упоминания MSSQL |

После удаления `ContainerFixture` знает только PostgreSQL — поле `Provider` и выбор провайдера уходят.

## 2. Новый `AppServiceTestBase`

**Проблема текущего дизайна:** базовый класс создаёт `ProductService` и `OrderService` для всех наследников — не расширяемо. Новый сервис потребует изменения базового класса.

**Новый дизайн (Вариант Б — не-generic):**

```csharp
public abstract class AppServiceTestBase : IAsyncLifetime
{
    private string _connectionString = null!;
    private NpgsqlDatabaseInitializer _initializer = null!;

    /// <summary>Контекст тестовой БД. Доступен после InitializeAsync.</summary>
    protected ShopDbContext Context { get; private set; } = null!;

    public virtual async Task InitializeAsync()
    {
        var state = await IntegresSqlContainerManager.GetStateAsync();
        _initializer = state.Initializer;
        _connectionString = await _initializer.CreateDatabaseGetConnectionString<ShopDbContext>(
            IntegresSqlDefaults.SeedingOptions);
        var options = new DbContextOptionsBuilder<ShopDbContext>()
            .UseNpgsql(_connectionString).Options;
        Context = new ShopDbContext(options);
    }

    public virtual async Task DisposeAsync()
    {
        await Context.DisposeAsync();
        await using var conn = new NpgsqlConnection(_connectionString);
        NpgsqlConnection.ClearPool(conn);
        await _initializer.RemoveDatabase(_connectionString);
    }
}
```

Каждый тест-класс переопределяет `InitializeAsync`, вызывает `await base.InitializeAsync()`, затем создаёт свой SUT:

```csharp
public class ProductServiceTests : AppServiceTestBase
{
    private IProductService Sut = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        Sut = new ProductService(new ProductRepository(Context));
    }
}

public class OrderServiceTests : AppServiceTestBase
{
    private IOrderService Sut = null!;
    private IProductService _products = null!;  // helper для тест-данных

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        var productRepo = new ProductRepository(Context);
        _products = new ProductService(productRepo);
        Sut = new OrderService(new OrderRepository(Context), productRepo);
    }
}
```

`Context` остаётся `protected` — тест-класс может обратиться к БД напрямую для сложных assertions минуя сервис.

## 3. Миграция тест-классов на IntegreSQL

| Файл | Было | Станет |
|---|---|---|
| `ProductServiceTests.cs` | `[Collection("ProductsService")]` + `ServiceTestBase` | `AppServiceTestBase`, `private IProductService Sut` |
| `ProductsApiTests.cs` | `[Collection("ProductsApi")]` + `ApiTestBase` | `ComponentTestBase` |
| `OrderServiceTests.cs` | `ServiceTestBase` (сломан — нет `[Collection]`) | `AppServiceTestBase`, `private IOrderService Sut` |
| `OrdersApiTests.cs` | уже `ComponentTestBase` | без изменений |

Тела тест-методов не меняются — только заголовок класса и `InitializeAsync`.

В тест-методах: `ProductService` → `Sut`, `OrderService` → `Sut`, `_products` для helper в OrderServiceTests.

## 4. Testcontainers-инфраструктура после упрощения

Остаётся как baseline для сравнения производительности. После удаления MSSQL:

- `ContainerFixture` — только PostgreSQL, убираем поле `Provider` и выбор провайдера
- `ServiceTestBase` — убираем зависимость от `Provider`, строка подключения напрямую из `ContainerFixture`
- `ApiTestBase` — аналогично
- `TestDbFactory` — убираем `Provider`-логику, только PostgreSQL

Smoke-тесты (`ProductServiceIntegreTests`, `ProductsApiIntegreTests`) — без изменений.

## 5. Что удаляем

- `CollectionDefinitions.cs` — все четыре коллекции уходят: основные тест-классы мигрируют на IntegreSQL без `[Collection]`.

## 6. Параллелизм

Сохраняется текущая конфигурация `xunit.runner.json` (`maxParallelThreads = 4`). После миграции все четыре основных класса (`ProductServiceTests`, `ProductsApiTests`, `OrderServiceTests`, `OrdersApiTests`) работают без `[Collection]` — каждый в своей неявной коллекции, выполняются параллельно.

Smoke-тесты (`ProductServiceIntegreTests`, `ProductsApiIntegreTests`) — также без коллекций, тоже параллельно.

## Файлы не затронутые изменениями

- `IntegresSqlContainerManager.cs` — без изменений
- `IntegresSqlState.cs` — без изменений
- `IntegresSqlDefaults.cs` — без изменений
- `ComponentTestBase.cs` — без изменений
- `TestWebApplicationFactory.cs` — только удаление MSSQL-ветки
- `GlobalUsings.cs` — без изменений
