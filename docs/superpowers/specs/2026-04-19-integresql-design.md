# IntegreSQL — дизайн интеграции

## Цель

Добавить второе семейство базовых классов для интеграционных тестов, использующее IntegreSQL вместо Testcontainers-миграций. Выбор режима — через наследование, без изменений конфигурации.

## Мотивация

Текущий Testcontainers-подход прогоняет `MigrateAsync()` на каждый тест (~200–500 мс). IntegreSQL создаёт шаблонную БД один раз, а каждый тест получает мгновенный клон через `CREATE DATABASE ... TEMPLATE` (~5 мс). Цель — сравнить скорость обоих подходов при одинаковом уровне изоляции.

## Два семейства базовых классов

### Существующее (не меняется)

```
[Collection("...")] + ContainerFixture (Testcontainers)
  ServiceTestBase   ← сервисные тесты
  ApiTestBase       ← HTTP-тесты
```

Каждый тест-класс объявляет `[Collection("...")]`, получает инжектированный `ContainerFixture`, который запускает PostgreSQL- или MSSQL-контейнер на коллекцию. `TestDbFactory` создаёт `test_{guid}` и прогоняет `MigrateAsync()`.

### Новое (IntegreSQL)

```
AppServiceTestBase  ← сервисные тесты через IntegreSQL
ComponentTestBase   ← HTTP-тесты через IntegreSQL
```

Без `[Collection]`, без `ContainerFixture`. Каждый тест-класс = неявная xUnit-коллекция → выполняются параллельно, `maxParallelThreads = 4` ограничивает concurrency.

## Компоненты

### IntegresSqlContainerManager

Статический класс с `Lazy<Task<IntegresSqlState>>`. Инициализируется один раз на весь процесс при первом обращении из любого теста.

**Последовательность запуска:**
1. `NetworkBuilder` → создать Docker-сеть
2. `PostgreSqlBuilder.WithNetwork(network).WithNetworkAliases("postgres")` → запустить PostgreSQL
3. `ContainerBuilder` с образом `ghcr.io/allaboutapps/integresql:latest`:
   - `WithNetwork(network)`
   - `PGHOST=postgres`, `PGUSER=postgres`, `PGPASSWORD=postgres`, `PGPORT=5432`
   - `WaitingFor(Wait.ForHttp("/api/v1/ready").ForPort(5000))`
4. Создать `NpgsqlDatabaseInitializer` с:
   - URL IntegreSQL: `http://localhost:{mappedPort5000}`
   - `NpgsqlConnectionStringBuilder` с `Host=localhost`, `Port={mappedPort5432}` — чтобы переопределить внутренний Docker-адрес на внешний

**Возвращает** `IntegresSqlState` с готовым `NpgsqlDatabaseInitializer`.

### AppServiceTestBase

```csharp
public abstract class AppServiceTestBase : IAsyncLifetime
{
    private string _connectionString = null!;
    private ShopDbContext _context = null!;

    protected IProductService ProductService { get; private set; } = null!;
    protected IOrderService OrderService { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var state = await IntegresSqlContainerManager.GetStateAsync();
        _connectionString = state.Initializer.CreateDatabaseGetConnectionString<ShopDbContext>(
            new DatabaseSeedingOptions<ShopDbContext>(
                Name: "default",
                SeedingFunction: ctx => ctx.Database.MigrateAsync()
            )
        );
        var options = state.Initializer.GetDbContextOptionsBuilder<ShopDbContext>(_connectionString).Options;
        _context = new ShopDbContext(options);
        ProductService = new ProductService(new ProductRepository(_context));
        OrderService = new OrderService(new OrderRepository(_context), new ProductRepository(_context));
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        var state = await IntegresSqlContainerManager.GetStateAsync();
        state.Initializer.RemoveDatabase(_connectionString);
    }
}
```

### ComponentTestBase

Аналог `ApiTestBase`: получает строку подключения через `NpgsqlDatabaseInitializer`, передаёт её в `TestWebApplicationFactory("PostgreSQL", connectionString)`, создаёт `HttpClient`. В `DisposeAsync` — утилизирует фабрику и возвращает БД.

### Хеш шаблона

`NpgsqlDatabaseInitializer` из `MccSoft.IntegreSql.EF` вычисляет хеш автоматически на основе имени последней миграции + имени сборки `ShopDbContext`. При изменении миграций хеш меняется → создаётся новый шаблон.

## Параллелизм

| Режим | Параллелизм | Изоляция |
|---|---|---|
| Testcontainers | 4 коллекции × 1 контейнер | БД `test_{guid}` на тест |
| IntegreSQL | каждый класс = implicit collection | клон шаблона на тест |

В обоих случаях: 4 потока одновременно, одна БД на тест.

## Новые файлы

```
tests/FastIntegrationTests.Tests/
  Infrastructure/
    IntegreSQL/
      IntegresSqlContainerManager.cs   ← статик, запуск контейнеров
      IntegresSqlState.cs              ← DTO: initializer + cleanup
    Base/
      AppServiceTestBase.cs            ← новый базовый класс (сервисный)
      ComponentTestBase.cs             ← новый базовый класс (HTTP)
```

## Изменения в существующих файлах

- `FastIntegrationTests.Tests.csproj` — добавить:
  - `MccSoft.IntegreSql.EF` (NuGet)
  - `Testcontainers` базовый пакет (для `ContainerBuilder`, `NetworkBuilder`)

Всё остальное — `ContainerFixture`, `CollectionDefinitions`, `ServiceTestBase`, `ApiTestBase`, `TestDbFactory`, `appsettings.json` — **не меняется**.

## Сравнение скоростей

После реализации: запустить одни и те же тесты дважды — унаследованные от `ServiceTestBase` (Testcontainers) и от `AppServiceTestBase` (IntegreSQL) — и сравнить `dotnet test` wall-clock time.
