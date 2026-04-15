# Дизайн: .NET-сервис интернет-магазина для изучения интеграционных тестов

**Дата:** 2026-04-15  
**Статус:** Утверждён

---

## Цель

Создать учебный .NET-сервис на базе EF Core и ASP.NET Core, моделирующий минимальный интернет-магазин. Сервис служит основой для написания интеграционных тестов, работающих с реальной базой данных.

---

## Структура проекта

```
FastIntegrationTests.sln
├── src/
│   ├── FastIntegrationTests.WebApi/          # Контроллеры, DI, Program.cs
│   ├── FastIntegrationTests.Application/     # Сервисы, DTO, интерфейсы, доменные исключения
│   └── FastIntegrationTests.Infrastructure/  # DbContext, конфигурации EF, миграции, extension-методы
└── docker-compose.yml
```

**Зависимости между проектами:**

```
WebApi → Application (интерфейсы сервисов, DTO)
WebApi → Infrastructure (регистрация в DI)
Infrastructure → Application (реализует интерфейсы репозиториев/сервисов)
```

`Application` не ссылается на EF Core и не знает о конкретной БД — только доменные объекты и интерфейсы. `Infrastructure` реализует эти интерфейсы через EF Core. `WebApi` связывает всё через DI в `Program.cs`.

---

## Доменная модель

### Сущности

**Product**
- `Id` (int)
- `Name` (string)
- `Description` (string)
- `Price` (decimal)
- `CreatedAt` (DateTime)

**Order**
- `Id` (int)
- `CreatedAt` (DateTime)
- `Status` (enum: `New`, `Confirmed`, `Shipped`, `Completed`, `Cancelled`)
- `TotalAmount` (decimal) — вычисляется при создании заказа
- `Items` (коллекция `OrderItem`)

**OrderItem**
- `Id` (int)
- `OrderId` (int)
- `ProductId` (int)
- `Quantity` (int)
- `UnitPrice` (decimal) — фиксируется в момент создания заказа, не меняется при изменении цены товара

### Правила переходов статусов заказа

```
New       → Confirmed, Cancelled
Confirmed → Shipped,   Cancelled
Shipped   → Completed
Completed → (финальный статус, переходы запрещены)
Cancelled → (финальный статус, переходы запрещены)
```

Попытка недопустимого перехода бросает `InvalidOrderStatusTransitionException` (доменное исключение в `Application`). `WebApi` перехватывает его и возвращает `400 Bad Request`.

---

## Бизнес-логика

### ProductService (Application)

| Метод | Описание |
|---|---|
| `GetAllAsync()` | Список всех товаров |
| `GetByIdAsync(id)` | Товар по Id, `NotFoundException` если не найден |
| `CreateAsync(dto)` | Создать товар |
| `UpdateAsync(id, dto)` | Обновить товар |
| `DeleteAsync(id)` | Удалить товар |

### OrderService (Application)

| Метод | Описание |
|---|---|
| `GetAllAsync()` | Список заказов |
| `GetByIdAsync(id)` | Заказ с позициями, `NotFoundException` если не найден |
| `CreateAsync(dto)` | Создать заказ: загрузить товары, зафиксировать `UnitPrice`, подсчитать `TotalAmount` |
| `ConfirmAsync(id)` | Перевести в `Confirmed` |
| `ShipAsync(id)` | Перевести в `Shipped` |
| `CompleteAsync(id)` | Перевести в `Completed` |
| `CancelAsync(id)` | Перевести в `Cancelled` |

---

## API Endpoints

### ProductsController — `/api/products`

| Метод | Маршрут | Статус успеха |
|---|---|---|
| GET | `/api/products` | 200 |
| GET | `/api/products/{id}` | 200 / 404 |
| POST | `/api/products` | 201 |
| PUT | `/api/products/{id}` | 200 / 404 |
| DELETE | `/api/products/{id}` | 204 / 404 |

### OrdersController — `/api/orders`

| Метод | Маршрут | Статус успеха |
|---|---|---|
| GET | `/api/orders` | 200 |
| GET | `/api/orders/{id}` | 200 / 404 |
| POST | `/api/orders` | 201 |
| POST | `/api/orders/{id}/confirm` | 200 / 404 / 400 |
| POST | `/api/orders/{id}/ship` | 200 / 404 / 400 |
| POST | `/api/orders/{id}/complete` | 200 / 404 / 400 |
| POST | `/api/orders/{id}/cancel` | 200 / 404 / 400 |

Переходы статусов реализованы как отдельные endpoints (а не PATCH), чтобы явно выражать намерение и упростить тестирование.

---

## Переключение провайдера БД

### Extension-методы в Infrastructure

```csharp
// Infrastructure/Extensions/ServiceCollectionExtensions.cs
services.AddPostgresql(connectionString);
services.AddMssql(connectionString);
```

Каждый метод регистрирует `ShopDbContext` с нужным провайдером (Npgsql / Microsoft.SqlServer).

### Конфигурация

**`appsettings.json`** — заглушки без реальных значений:
```json
{
  "DatabaseProvider": "PostgreSQL",
  "ConnectionStrings": {
    "PostgreSQL": "",
    "MSSQL": ""
  }
}
```

**`appsettings.Development.json`** — реальные строки подключения к docker-контейнерам:
```json
{
  "DatabaseProvider": "PostgreSQL",
  "ConnectionStrings": {
    "PostgreSQL": "Host=localhost;Database=shop;Username=postgres;Password=postgres",
    "MSSQL": "Server=localhost,1433;Database=shop;User Id=sa;Password=Strong!Pass1;TrustServerCertificate=true"
  }
}
```

### Выбор провайдера в Program.cs

```csharp
// Учебный проект: предполагаем, что все миграции используют только EF Core Fluent API
// без raw SQL, поэтому один набор миграций совместим с обоими провайдерами.
// В production-проекте при наличии raw SQL миграции пришлось бы разделять.
var provider = builder.Configuration["DatabaseProvider"];
var connStr = builder.Configuration.GetConnectionString(provider!);

if (provider == "PostgreSQL")
    builder.Services.AddPostgresql(connStr!);
else
    builder.Services.AddMssql(connStr!);
```

### Миграции

Одна папка `Infrastructure/Migrations/` — миграции EF Core используют только Fluent API без raw SQL, поэтому они совместимы с обоими провайдерами.

---

## docker-compose

```yaml
services:
  postgres:
    image: postgres:16
    environment:
      POSTGRES_DB: shop
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
    ports:
      - "5432:5432"

  mssql:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      SA_PASSWORD: "Strong!Pass1"
      ACCEPT_EULA: "Y"
    ports:
      - "1433:1433"
```

Оба сервиса объявлены — запускается нужный через `docker-compose up postgres` или `docker-compose up mssql`.

---

## Соглашения по коду

- Документация и комментарии — на русском языке
- Все публичные классы, методы и свойства — с XML-документацией (`/// <summary>`)
- Коммит после каждого логического шага реализации

---

## Вне scope

- Аутентификация и авторизация
- Пагинация и сортировка
- Логирование и мониторинг
- Тесты (будут добавлены отдельно в следующей итерации)
