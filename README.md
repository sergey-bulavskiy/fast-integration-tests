# FastIntegrationTests

Учебный проект: REST API магазина на .NET 8 с полным набором интеграционных тестов на реальной базе данных.

Демонстрирует паттерн **database-per-test** с Testcontainers: каждый тест получает изолированную базу данных, тесты выполняются параллельно, инфраструктура скрыта от тест-кода.

---

## Требования

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (для локальной разработки и запуска тестов)

---

## Быстрый старт

```bash
# 1. Восстановить инструменты EF CLI
dotnet tool restore

# 2. Поднять PostgreSQL
docker-compose up postgres -d

# 3. Создать appsettings.Development.json из шаблона
cp src/FastIntegrationTests.WebApi/appsettings.Development.json.example \
   src/FastIntegrationTests.WebApi/appsettings.Development.json

# 4. Заполнить строку подключения в appsettings.Development.json:
#    "PostgreSQL": "Host=localhost;Database=shop;Username=postgres;Password=postgres"

# 5. Применить миграции
dotnet ef database update \
  --project src/FastIntegrationTests.Infrastructure \
  --startup-project src/FastIntegrationTests.WebApi

# 6. Запустить сервис
dotnet run --project src/FastIntegrationTests.WebApi
```

После запуска Swagger UI доступен по адресу: `https://localhost:{port}/swagger`

---

## Переключение провайдера БД

В `appsettings.Development.json` установить `"DatabaseProvider"` в `"PostgreSQL"` или `"MSSQL"` и заполнить соответствующую строку подключения.

**Внимание:** текущие миграции сгенерированы под PostgreSQL. При переключении на MSSQL потребуется пересоздать миграции (известное ограничение учебного проекта).

```bash
# Поднять MSSQL
docker-compose up mssql -d
# Строка подключения для MSSQL:
# "Server=localhost,1433;Database=shop;User Id=sa;Password=Strong!Pass1;TrustServerCertificate=true"
```

---

## API

### Products

| Метод | Endpoint | Описание |
|-------|----------|----------|
| GET | `/api/products` | Список всех товаров |
| GET | `/api/products/{id}` | Товар по ID |
| POST | `/api/products` | Создать товар |
| PUT | `/api/products/{id}` | Обновить товар |
| DELETE | `/api/products/{id}` | Удалить товар |

### Orders

| Метод | Endpoint | Описание |
|-------|----------|----------|
| GET | `/api/orders` | Список всех заказов |
| GET | `/api/orders/{id}` | Заказ по ID (с позициями) |
| POST | `/api/orders` | Создать заказ |
| POST | `/api/orders/{id}/confirm` | Подтвердить заказ |
| POST | `/api/orders/{id}/ship` | Отправить заказ |
| POST | `/api/orders/{id}/complete` | Завершить заказ |
| POST | `/api/orders/{id}/cancel` | Отменить заказ |

#### Жизненный цикл заказа

```
New → Confirmed → Shipped → Completed
 └──────────────→ Cancelled
```

---

## Запуск тестов

Docker должен быть запущен. Testcontainers автоматически поднимает контейнер с базой данных — вручную ничего поднимать не нужно.

```bash
# Запустить все 53 теста
dotnet test tests/FastIntegrationTests.Tests

# С подробным выводом (видны имена тестов и SQL-запросы)
dotnet test tests/FastIntegrationTests.Tests --verbosity normal

# Запустить отдельную коллекцию
dotnet test tests/FastIntegrationTests.Tests --filter "Collection=ProductsService"
dotnet test tests/FastIntegrationTests.Tests --filter "Collection=ProductsApi"
dotnet test tests/FastIntegrationTests.Tests --filter "Collection=OrdersService"
dotnet test tests/FastIntegrationTests.Tests --filter "Collection=OrdersApi"
```

### Что тестируется

| Коллекция | Уровень | Тестов | Описание |
|-----------|---------|--------|----------|
| `ProductsService` | Сервисный | 11 | CRUD через `IProductService` |
| `ProductsApi` | HTTP | 10 | CRUD через `HttpClient` → `/api/products` |
| `OrdersService` | Сервисный | 16 | Создание, расчёт суммы, все переходы статусов |
| `OrdersApi` | HTTP | 16 | Все эндпоинты заказов, переходы статусов, полный цикл |

### Как устроена изоляция

- Каждый тест создаёт базу `test_{guid}` внутри Testcontainers-контейнера.
- Миграции применяются через EF Core (`MigrateAsync`) в `InitializeAsync`.
- База удаляется в `DisposeAsync`.
- Четыре коллекции выполняются параллельно: по одному контейнеру на коллекцию.

### Провайдер БД для тестов

Определяется в `tests/FastIntegrationTests.Tests/appsettings.json`:

```json
{ "DatabaseProvider": "PostgreSQL" }
```

Замените на `"MSSQL"` чтобы тесты запускались на Microsoft SQL Server.

---

## Структура проекта

```
src/
├── FastIntegrationTests.Application/    # Домен: сущности, DTO, сервисы, исключения
├── FastIntegrationTests.Infrastructure/ # EF Core: DbContext, репозитории, миграции
└── FastIntegrationTests.WebApi/         # ASP.NET Core: контроллеры, Program.cs

tests/
└── FastIntegrationTests.Tests/
    ├── Infrastructure/
    │   ├── Fixtures/     # ContainerFixture — Testcontainers lifecycle
    │   ├── Factories/    # TestDbFactory — создаёт изолированную БД на тест
    │   ├── Base/         # ServiceTestBase, ApiTestBase
    │   └── WebApp/       # TestWebApplicationFactory
    ├── Products/         # ProductServiceTests, ProductsApiTests
    └── Orders/           # OrderServiceTests, OrdersApiTests
```
