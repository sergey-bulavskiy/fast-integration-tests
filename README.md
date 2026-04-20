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
# Запустить все 57 тестов
dotnet test tests/FastIntegrationTests.Tests

# С подробным выводом (видны имена тестов и SQL-запросы)
dotnet test tests/FastIntegrationTests.Tests --verbosity normal

# Запустить тесты отдельного класса
dotnet test tests/FastIntegrationTests.Tests --filter "FullyQualifiedName~ProductServiceTests"
dotnet test tests/FastIntegrationTests.Tests --filter "FullyQualifiedName~ProductsApiTests"
dotnet test tests/FastIntegrationTests.Tests --filter "FullyQualifiedName~OrderServiceTests"
dotnet test tests/FastIntegrationTests.Tests --filter "FullyQualifiedName~OrdersApiTests"
```

### Что тестируется

| Класс | Уровень | Тестов | Инфраструктура |
|-------|---------|--------|----------------|
| `ProductServiceTests` | Сервисный | 11 | IntegreSQL |
| `ProductsApiTests` | HTTP | 10 | IntegreSQL |
| `OrderServiceTests` | Сервисный | 16 | IntegreSQL |
| `OrdersApiTests` | HTTP | 16 | IntegreSQL |

### Как устроена изоляция

- Миграции применяются один раз как шаблонная БД (IntegreSQL).
- Каждый тест получает клон шаблона (~5 мс) вместо полного поднятия БД.
- Клон возвращается в пул в `DisposeAsync`.
- Тест-классы выполняются параллельно (`maxParallelThreads = 4`).

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
    │   ├── Base/         # AppServiceTestBase, ComponentTestBase, ServiceTestBase, ApiTestBase
    │   └── WebApp/       # TestWebApplicationFactory
    ├── Products/         # ProductServiceTests, ProductsApiTests
    └── Orders/           # OrderServiceTests, OrdersApiTests
```
