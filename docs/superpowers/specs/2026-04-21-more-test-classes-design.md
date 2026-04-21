# Дизайн: расширение тест-классов для равномерного бенчмарка

**Дата:** 2026-04-21
**Цель:** устранить перекос в бенчмарке — при db-per-class (Respawn) сейчас только 4 базы на прогон, тогда как IntegreSQL/Testcontainers создают базу на каждый тест. Добавляем 5 новых AppService'ов и дробим все тест-классы на CR/UD, чтобы получить 28 классов на подход.

---

## 1. Доменный слой (Application)

### Новые сущности

| Сущность | Ключевые поля |
|---|---|
| `Category` | `Id` (Guid), `Name` (string, unique), `Description?` (string), `CreatedAt` |
| `Customer` | `Id` (Guid), `Name` (string), `Email` (string, unique), `Phone?` (string), `Status` (CustomerStatus), `CreatedAt` |
| `Supplier` | `Id` (Guid), `Name` (string), `ContactEmail` (string), `Country` (string), `IsActive` (bool), `CreatedAt` |
| `Review` | `Id` (Guid), `Title` (string), `Body` (string), `Rating` (int, 1–5), `Status` (ReviewStatus), `CreatedAt` |
| `Discount` | `Id` (Guid), `Code` (string, unique), `DiscountPercent` (int, 1–100), `IsActive` (bool), `ExpiresAt?` (DateTime), `CreatedAt` |

Новые сущности не имеют FK-связей с существующими таблицами.

### Новые enum'ы

```csharp
CustomerStatus { Active = 0, Inactive = 1, Banned = 2 }
ReviewStatus   { Pending = 0, Approved = 1, Rejected = 2 }
```

### Новые исключения

| Класс | Когда бросается |
|---|---|
| `DuplicateValueException` | нарушение уникальности (email, code, name) |
| `InvalidRatingException` | рейтинг Review вне диапазона 1–5 |
| `InvalidDiscountPercentException` | процент Discount вне диапазона 1–100 |

Следуют паттерну существующих `NotFoundException` / `InvalidOrderStatusTransitionException`.

### DTO

Для каждой сущности: `{Entity}Dto`, `Create{Entity}Request`, `Update{Entity}Request` (где применимо).
`Review` не имеет `UpdateReviewRequest` — у сущности нет PUT-эндпоинта, только действия approve/reject.

### Интерфейсы

**Репозитории** — CRUD по образцу `IProductRepository` / `IOrderRepository`.

**Сервисы:**

```
ICategoryService  → GetAllAsync, GetByIdAsync, CreateAsync, UpdateAsync, DeleteAsync
ICustomerService  → GetAllAsync, GetByIdAsync, CreateAsync, UpdateAsync, DeleteAsync,
                    BanAsync, ActivateAsync, DeactivateAsync
ISupplierService  → GetAllAsync, GetByIdAsync, CreateAsync, UpdateAsync, DeleteAsync,
                    ActivateAsync, DeactivateAsync
IReviewService    → GetAllAsync, GetByIdAsync, CreateAsync, DeleteAsync,
                    ApproveAsync, RejectAsync
IDiscountService  → GetAllAsync, GetByIdAsync, CreateAsync, UpdateAsync, DeleteAsync,
                    ActivateAsync, DeactivateAsync
```

### Бизнес-логика

- **Customer**: переходы Active↔Inactive, любой→Banned; нельзя забанить уже забаненного (`InvalidOrderStatusTransitionException`-паттерн)
- **Review**: Pending→Approved или Pending→Rejected; статус Approved и Rejected финальные — любые дальнейшие переходы бросают исключение
- **Discount**: валидация `DiscountPercent` в диапазоне 1–100 при создании/обновлении → `InvalidDiscountPercentException`
- **Category**: проверка уникальности `Name` → `DuplicateValueException`
- **Supplier**: проверка уникальности `ContactEmail` → `DuplicateValueException`

---

## 2. Infrastructure + WebApi

### EF Core конфигурации

Один `IEntityTypeConfiguration<T>` файл на сущность:

- `Category.Name` — уникальный индекс
- `Customer.Email` — уникальный индекс
- `Discount.Code` — уникальный индекс
- `Review.Rating` — check constraint `Rating BETWEEN 1 AND 5`
- `Discount.DiscountPercent` — check constraint `DiscountPercent BETWEEN 1 AND 100`

### Миграция

Одна миграция для всех пяти новых таблиц.

### Контроллеры

| Контроллер | Эндпоинты |
|---|---|
| `CategoriesController` | GET /categories, GET /{id}, POST, PUT /{id}, DELETE /{id} |
| `CustomersController` | GET /customers, GET /{id}, POST, PUT /{id}, DELETE /{id}, POST /{id}/ban, POST /{id}/activate, POST /{id}/deactivate |
| `SuppliersController` | GET /suppliers, GET /{id}, POST, PUT /{id}, DELETE /{id}, POST /{id}/activate, POST /{id}/deactivate |
| `ReviewsController` | GET /reviews, GET /{id}, POST, DELETE /{id}, POST /{id}/approve, POST /{id}/reject |
| `DiscountsController` | GET /discounts, GET /{id}, POST, PUT /{id}, DELETE /{id}, POST /{id}/activate, POST /{id}/deactivate |

Все action-эндпоинты возвращают `204 No Content` при успехе, `404` при не найденной сущности, `409 Conflict` при нарушении уникальности, `422 Unprocessable Entity` при невалидном переходе/значении.

---

## 3. Тестовая структура

### Итог

**28 тест-классов на подход** (было 4), всего **84 новых файла**.

Каждый entity → 4 класса:

| Суффикс | Содержит |
|---|---|
| `*ServiceCrTests` | GetAll, GetById, Create (включая дубли и ошибки валидации) |
| `*ServiceUdTests` | Update, Delete + бизнес-логика (статусы, approve/reject, activate) |
| `*ApiCrTests` | GET all, GET /{id}, POST + error cases |
| `*ApiUdTests` | PUT /{id}, DELETE /{id} + action-эндпоинты |

Для **Orders** `UdTests` содержит статусные переходы (Confirm/Ship/Complete/Cancel) вместо Update.

### Структура директорий

```
tests/FastIntegrationTests.Tests/
  IntegreSQL/
    Products/
      ProductServiceCrTests.cs
      ProductServiceUdTests.cs
      ProductsApiCrTests.cs
      ProductsApiUdTests.cs
    Orders/
      OrderServiceCrTests.cs
      OrderServiceUdTests.cs
      OrdersApiCrTests.cs
      OrdersApiUdTests.cs
    Categories/
      CategoryServiceCrTests.cs, CategoryServiceUdTests.cs
      CategoriesApiCrTests.cs, CategoriesApiUdTests.cs
    Customers/
      CustomerServiceCrTests.cs, CustomerServiceUdTests.cs
      CustomersApiCrTests.cs, CustomersApiUdTests.cs
    Suppliers/
      SupplierServiceCrTests.cs, SupplierServiceUdTests.cs
      SuppliersApiCrTests.cs, SuppliersApiUdTests.cs
    Reviews/
      ReviewServiceCrTests.cs, ReviewServiceUdTests.cs
      ReviewsApiCrTests.cs, ReviewsApiUdTests.cs
    Discounts/
      DiscountServiceCrTests.cs, DiscountServiceUdTests.cs
      DiscountsApiCrTests.cs, DiscountsApiUdTests.cs
  Respawn/        (зеркально)
  Testcontainers/ (зеркально)
```

### Ориентировочное число тестов на класс

| Сущность | CR (service) | UD (service) | CR (api) | UD (api) |
|---|---|---|---|---|
| Product | 5 | 6 | 5 | 7 |
| Order | 5 | 10 | 5 | 13 |
| Category | 4 | 4 | 4 | 5 |
| Customer | 4 | 6 | 4 | 7 |
| Supplier | 4 | 5 | 4 | 6 |
| Review | 4 | 5 | 4 | 6 |
| Discount | 4 | 5 | 4 | 6 |

### Эффект на Respawn

Respawn создаёт одну базу данных на тест-класс. При 28 классах:
- **Было:** 4 базы → миграции применяются 4 раза
- **Станет:** 28 баз → миграции применяются 28 раз

Это делает бенчмарк честным: Respawn несёт реальный overhead пропорционально числу классов.

---

## 4. Артефакты для обновления

Помимо новых файлов, необходимо обновить следующие существующие артефакты:

### Тесты Products/Orders → разбиение на CR/UD
Существующие файлы (`ProductServiceTests.cs`, `OrdersApiTests.cs` и т.д.) **разбиваются**, а не удаляются:
- `ProductServiceTests.cs` → `ProductServiceCrTests.cs` + `ProductServiceUdTests.cs`
- `ProductsApiTests.cs` → `ProductsApiCrTests.cs` + `ProductsApiUdTests.cs`
- `OrderServiceTests.cs` → `OrderServiceCrTests.cs` + `OrderServiceUdTests.cs`
- `OrdersApiTests.cs` → `OrdersApiCrTests.cs` + `OrdersApiUdTests.cs`

Аналогично для Respawn и Testcontainers вариантов (12 существующих файлов → 24 файла).

### CLAUDE.md
- Обновить количество тест-классов в описании трёх подходов
- Убрать пункт "Больше AppService'ов и тест-классов" из раздела "Идеи для развития"

### PowerShell-скрипты (`run-integresql.ps1`, `run-respawn.ps1`, `run-testcontainers.ps1`)
Проверить, нет ли хардкодных фильтров по именам классов — если есть, обновить.

### BenchmarkRunner (`tools/BenchmarkRunner/Program.cs`)
Убедиться, что фильтры запуска тестов (`--filter`) используют namespace-паттерны (`Tests.IntegreSQL` и т.д.), а не конкретные имена классов.

---

## 5. Совместимость с фейковыми миграциями

Фейковые миграции (генерируемые `MigrationManager`) создают таблицы `benchmark_ref_NNN` и `benchmark_lookup_NNN`. Конфликтов с новыми сущностями (Categories, Customers, Suppliers, Reviews, Discounts) нет — имена таблиц не пересекаются.

**Порядок применения миграций:**
1. Реальные миграции (timestamp ~2024–2026) — включая новую миграцию для 5 сущностей
2. Фейковые миграции (timestamp 29990101...) — всегда после реальных

Новая миграция имеет стандартный timestamp (~2026), что гарантирует правильный порядок относительно фейковых (2999).

**Designer.cs фейковых миграций** содержит пустой `BuildTargetModel` — намеренно, т.к. фейковые миграции не изменяют EF-модель. Новые сущности не нарушают эту логику: их Designer.cs генерируется обычным образом (`dotnet ef migrations add`).

---

## Что НЕ входит в скоуп

- FK-связи новых сущностей с Products/Orders
- Пагинация, фильтрация, сортировка
- Аутентификация/авторизация
