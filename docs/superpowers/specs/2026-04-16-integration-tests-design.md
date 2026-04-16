# Спецификация: интеграционные тесты с реальной базой данных

**Дата:** 2026-04-16  
**Проект:** FastIntegrationTests  
**Статус:** Утверждён

---

## Контекст

Проект реализует REST API магазина с двумя агрегатами: `Product` (CRUD) и `Order` (жизненный цикл со статусами). Архитектура трёхслойная: Application / Infrastructure / WebApi. Поддерживаются два провайдера БД: PostgreSQL и MSSQL. Тестов на момент создания спецификации нет.

Цель: написать полный набор интеграционных тестов, которые работают с реальной базой данных через Testcontainers, покрывают оба уровня (HTTP и сервисный), запускаются параллельно и полностью изолированы друг от друга.

---

## Архитектурные решения

### Database-per-test

Каждый тест получает собственную базу данных с уникальным именем `test_{guid}`. База создаётся в `InitializeAsync`, миграции применяются через EF Core (`MigrateAsync`), база удаляется в `DisposeAsync`. Один Testcontainers-контейнер живёт на всю тест-сессию и удаляется по её завершении (через `DisposeAsync` или Ryuk при аварийном завершении процесса).

**Почему не Respawn:** Respawn не совместим с параллельными тестами на одной БД — reset одного теста уничтожает данные другого.  
**Почему не schema-per-test:** схемы PostgreSQL-специфичны, для MSSQL паттерн не работает.

### Параллелизм

`MaxParallelThreads = 4` в `xunit.runner.json`. При степени параллелизма 4 одновременно живут максимум 4 базы данных внутри одного контейнера.

### Определение провайдера

`ContainerFixture` читает `DatabaseProvider` из `appsettings.Development.json` и поднимает либо `PostgreSqlContainer`, либо `MsSqlContainer`. Тесты не знают о конкретном провайдере.

---

## Структура тест-проекта

```
tests/FastIntegrationTests.Tests/
├── Infrastructure/
│   ├── Fixtures/
│   │   └── ContainerFixture.cs           ← IAsyncLifetime, один контейнер на сессию
│   ├── Factories/
│   │   └── TestDbFactory.cs              ← создаёт DbContext с уникальной БД
│   ├── Base/
│   │   ├── ServiceTestBase.cs            ← базовый класс для сервисных тестов
│   │   └── ApiTestBase.cs                ← базовый класс для HTTP-тестов
│   └── WebApp/
│       └── TestWebApplicationFactory.cs  ← WebApplicationFactory с подменой DbContext
├── Products/
│   ├── ProductServiceTests.cs
│   └── ProductsApiTests.cs
├── Orders/
│   ├── OrderServiceTests.cs
│   └── OrdersApiTests.cs
├── xunit.runner.json
└── FastIntegrationTests.Tests.csproj
```

---

## Ключевые компоненты инфраструктуры

### ContainerFixture

```csharp
// [CollectionDefinition("Database")] → все тест-классы подключаются к одному контейнеру
public class ContainerFixture : IAsyncLifetime
{
    // Читает DatabaseProvider из appsettings.Development.json
    // Поднимает PostgreSqlContainer или MsSqlContainer
    // Предоставляет свойство ConnectionString (без имени БД)
}
```

### TestDbFactory

```csharp
public class TestDbFactory
{
    // Принимает ContainerFixture
    // CreateAsync() → генерирует имя test_{guid}, создаёт БД, применяет миграции
    // Возвращает (ShopDbContext context, string connectionString)
}
```

### ServiceTestBase

```csharp
[Collection("Database")]
public abstract class ServiceTestBase : IAsyncLifetime
{
    // InitializeAsync: TestDbFactory.CreateAsync() → ShopDbContext
    // Создаёт ProductRepository, OrderRepository, ProductService, OrderService
    // DisposeAsync: context.Database.EnsureDeletedAsync()
}
```

### ApiTestBase

```csharp
[Collection("Database")]
public abstract class ApiTestBase : IAsyncLifetime
{
    // InitializeAsync: TestDbFactory.CreateAsync() → connectionString
    // Создаёт TestWebApplicationFactory с этим connectionString
    // Предоставляет HttpClient
    // DisposeAsync: удаляет БД, останавливает WebApplicationFactory
}
```

### TestWebApplicationFactory

```csharp
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    // ConfigureTestServices: заменяет регистрацию ShopDbContext
    // Использует тот же провайдер (PostgreSQL/MSSQL), но с тестовой БД
}
```

---

## Полный список тестов (52 теста)

### Products — сервисный уровень (11 тестов)

| # | Метод | Сценарий |
|---|-------|----------|
| 1 | `GetAllAsync` | Возвращает пустой список когда товаров нет |
| 2 | `GetAllAsync` | Возвращает все товары |
| 3 | `GetByIdAsync` | Возвращает товар по существующему ID |
| 4 | `GetByIdAsync` | Бросает `NotFoundException` для несуществующего ID |
| 5 | `CreateAsync` | Сохраняет товар и возвращает DTO с присвоенным ID |
| 6 | `CreateAsync` | `CreatedAt` устанавливается автоматически |
| 7 | `UpdateAsync` | Обновляет поля товара в БД |
| 8 | `UpdateAsync` | Бросает `NotFoundException` для несуществующего ID |
| 9 | `DeleteAsync` | Удаляет товар из БД |
| 10 | `DeleteAsync` | Бросает `NotFoundException` для несуществующего ID |
| 11 | `DeleteAsync` | Нельзя удалить товар с позициями в заказе (FK Restrict) |

### Products — HTTP-уровень (10 тестов)

| # | Endpoint | Сценарий |
|---|----------|----------|
| 12 | `GET /api/products` | 200 + пустой массив |
| 13 | `GET /api/products` | 200 + список товаров |
| 14 | `GET /api/products/{id}` | 200 + товар |
| 15 | `GET /api/products/{id}` | 404 для несуществующего |
| 16 | `POST /api/products` | 201 + Location header + тело с ID |
| 17 | `PUT /api/products/{id}` | 200 + обновлённые поля |
| 18 | `PUT /api/products/{id}` | 404 для несуществующего |
| 19 | `DELETE /api/products/{id}` | 204 |
| 20 | `DELETE /api/products/{id}` | 404 для несуществующего |
| 21 | `POST → GET` | Создать товар, получить по ID — данные совпадают |

### Orders — сервисный уровень (16 тестов)

| # | Метод | Сценарий |
|---|-------|----------|
| 22 | `GetAllAsync` | Возвращает пустой список |
| 23 | `GetAllAsync` | Возвращает все заказы |
| 24 | `GetByIdAsync` | Возвращает заказ с позициями (`Include` работает) |
| 25 | `GetByIdAsync` | Бросает `NotFoundException` |
| 26 | `CreateAsync` | Создаёт заказ с позициями, `TotalAmount` рассчитан верно |
| 27 | `CreateAsync` | `UnitPrice` берётся из текущей цены товара |
| 28 | `CreateAsync` | Бросает `NotFoundException` для несуществующего `ProductId` |
| 29 | `CreateAsync` | Статус нового заказа — `New` |
| 30 | `ConfirmAsync` | `New → Confirmed` |
| 31 | `ShipAsync` | `Confirmed → Shipped` |
| 32 | `CompleteAsync` | `Shipped → Completed` |
| 33 | `CancelAsync` | `New → Cancelled` |
| 34 | `CancelAsync` | `Confirmed → Cancelled` |
| 35 | `ConfirmAsync` | `Completed → Confirmed` бросает `InvalidOrderStatusTransitionException` |
| 36 | `CancelAsync` | `Shipped → Cancelled` бросает `InvalidOrderStatusTransitionException` |
| 37 | Цепочка | `Create → Confirm → Ship → Complete` — статус корректен на каждом шаге |

### Orders — HTTP-уровень (15 тестов)

| # | Endpoint | Сценарий |
|---|----------|----------|
| 38 | `GET /api/orders` | 200 + пустой массив |
| 39 | `GET /api/orders` | 200 + список заказов |
| 40 | `GET /api/orders/{id}` | 200 + заказ с позициями |
| 41 | `GET /api/orders/{id}` | 404 |
| 42 | `POST /api/orders` | 201 + `TotalAmount` рассчитан |
| 43 | `POST /api/orders` | 404 если товар не существует |
| 44 | `POST /{id}/confirm` | 200 + статус `Confirmed` |
| 45 | `POST /{id}/ship` | 200 + статус `Shipped` |
| 46 | `POST /{id}/complete` | 200 + статус `Completed` |
| 47 | `POST /{id}/cancel` | 200 + статус `Cancelled` |
| 48 | `POST /{id}/confirm` | 404 для несуществующего заказа |
| 49 | `POST /{id}/ship` | 400 при недопустимом переходе (`New → Shipped`) |
| 50 | `POST /{id}/cancel` | 400 при недопустимом переходе (`Shipped → Cancelled`) |
| 51 | `POST → GET` | Создать заказ, получить по ID — позиции совпадают |
| 52 | `POST → confirm → ship → complete → GET` | Полный жизненный цикл заказа |

---

## Зависимости тест-проекта

```xml
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
<PackageReference Include="Testcontainers.PostgreSql" />
<PackageReference Include="Testcontainers.MsSql" />
<PackageReference Include="xunit" />
<PackageReference Include="xunit.runner.visualstudio" />
<PackageReference Include="Microsoft.NET.Test.Sdk" />
```

---

## Жизненный цикл контейнера и БД

```
dotnet test
│
├── ContainerFixture.InitializeAsync()     → 1 контейнер запущен
│
├── Test_1.InitializeAsync()  → БД test_aaaa создана, миграции применены
├── Test_2.InitializeAsync()  → БД test_bbbb создана  ← параллельно (worker 2)
├── Test_3.InitializeAsync()  → БД test_cccc создана  ← параллельно (worker 3)
├── Test_4.InitializeAsync()  → БД test_dddd создана  ← параллельно (worker 4)
│
├── Test_1.DisposeAsync()     → БД test_aaaa удалена
├── Test_5.InitializeAsync()  → БД test_eeee создана  ← занял освободившийся слот
│   ...
│
└── ContainerFixture.DisposeAsync()        → контейнер остановлен и удалён (Ryuk — fallback)
```

---

## Ограничения и известные решения

- **Миграции PostgreSQL-специфичны** (Npgsql-аннотации). При тестировании с MSSQL провайдером `MigrateAsync` завершится ошибкой — это известное ограничение проекта, зафиксированное в `CLAUDE.md`. Решение: пересоздать миграции под MSSQL при необходимости.
- **`Program.cs` должен быть доступен** для `WebApplicationFactory`. Добавляем `partial class Program {}` в конец `Program.cs`.
