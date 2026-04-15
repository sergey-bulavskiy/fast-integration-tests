# Shop Service Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Построить трёхслойный .NET 8 сервис интернет-магазина (WebApi / Application / Infrastructure) на EF Core с поддержкой PostgreSQL и MSSQL.

**Architecture:** WebApi содержит контроллеры и DI-конфигурацию. Application содержит доменные сущности, DTO, интерфейсы и сервисы бизнес-логики без зависимости от EF Core. Infrastructure реализует репозитории через EF Core и предоставляет extension-методы для регистрации провайдера БД. Переключение PostgreSQL ↔ MSSQL — через `DatabaseProvider` в конфиге.

**Tech Stack:** .NET 8, ASP.NET Core 8, EF Core 8, Npgsql.EntityFrameworkCore.PostgreSQL 8, Microsoft.EntityFrameworkCore.SqlServer 8, Swashbuckle.AspNetCore.

---

## Файловая структура

```
FastIntegrationTests.sln
├── src/
│   ├── FastIntegrationTests.WebApi/
│   │   ├── FastIntegrationTests.WebApi.csproj
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   ├── appsettings.Development.json
│   │   ├── GlobalUsings.cs
│   │   ├── Controllers/
│   │   │   ├── ProductsController.cs
│   │   │   └── OrdersController.cs
│   │   └── Middleware/
│   │       └── GlobalExceptionHandler.cs
│   ├── FastIntegrationTests.Application/
│   │   ├── FastIntegrationTests.Application.csproj
│   │   ├── GlobalUsings.cs
│   │   ├── Entities/
│   │   │   ├── Product.cs
│   │   │   ├── Order.cs
│   │   │   └── OrderItem.cs
│   │   ├── Enums/
│   │   │   └── OrderStatus.cs
│   │   ├── DTOs/
│   │   │   ├── ProductDto.cs
│   │   │   ├── CreateProductRequest.cs
│   │   │   ├── UpdateProductRequest.cs
│   │   │   ├── OrderDto.cs
│   │   │   ├── OrderItemDto.cs
│   │   │   └── CreateOrderRequest.cs
│   │   ├── Interfaces/
│   │   │   ├── IProductRepository.cs
│   │   │   └── IOrderRepository.cs
│   │   ├── Exceptions/
│   │   │   ├── NotFoundException.cs
│   │   │   └── InvalidOrderStatusTransitionException.cs
│   │   └── Services/
│   │       ├── ProductService.cs
│   │       └── OrderService.cs
│   └── FastIntegrationTests.Infrastructure/
│       ├── FastIntegrationTests.Infrastructure.csproj
│       ├── GlobalUsings.cs
│       ├── DesignTimeDbContextFactory.cs
│       ├── Data/
│       │   ├── ShopDbContext.cs
│       │   └── Configurations/
│       │       ├── ProductConfiguration.cs
│       │       ├── OrderConfiguration.cs
│       │       └── OrderItemConfiguration.cs
│       ├── Repositories/
│       │   ├── ProductRepository.cs
│       │   └── OrderRepository.cs
│       └── Extensions/
│           └── ServiceCollectionExtensions.cs
└── docker-compose.yml
```

---

### Task 1: Создание solution и трёх проектов

**Files:**
- Create: `FastIntegrationTests.sln`
- Create: `src/FastIntegrationTests.WebApi/FastIntegrationTests.WebApi.csproj`
- Create: `src/FastIntegrationTests.Application/FastIntegrationTests.Application.csproj`
- Create: `src/FastIntegrationTests.Infrastructure/FastIntegrationTests.Infrastructure.csproj`

- [ ] **Шаг 1: Создать solution и проекты**

```bash
cd /c/source/misc/fast-integration-tests
dotnet new sln -n FastIntegrationTests
dotnet new webapi --use-controllers -n FastIntegrationTests.WebApi -o src/FastIntegrationTests.WebApi --framework net8.0
dotnet new classlib -n FastIntegrationTests.Application -o src/FastIntegrationTests.Application --framework net8.0
dotnet new classlib -n FastIntegrationTests.Infrastructure -o src/FastIntegrationTests.Infrastructure --framework net8.0
```

- [ ] **Шаг 2: Добавить проекты в solution**

```bash
dotnet sln add src/FastIntegrationTests.WebApi/FastIntegrationTests.WebApi.csproj
dotnet sln add src/FastIntegrationTests.Application/FastIntegrationTests.Application.csproj
dotnet sln add src/FastIntegrationTests.Infrastructure/FastIntegrationTests.Infrastructure.csproj
```

- [ ] **Шаг 3: Добавить ссылки между проектами**

```bash
dotnet add src/FastIntegrationTests.WebApi/FastIntegrationTests.WebApi.csproj reference \
  src/FastIntegrationTests.Application/FastIntegrationTests.Application.csproj \
  src/FastIntegrationTests.Infrastructure/FastIntegrationTests.Infrastructure.csproj

dotnet add src/FastIntegrationTests.Infrastructure/FastIntegrationTests.Infrastructure.csproj reference \
  src/FastIntegrationTests.Application/FastIntegrationTests.Application.csproj
```

- [ ] **Шаг 4: Добавить NuGet-пакеты в Infrastructure**

```bash
dotnet add src/FastIntegrationTests.Infrastructure/FastIntegrationTests.Infrastructure.csproj package Microsoft.EntityFrameworkCore
dotnet add src/FastIntegrationTests.Infrastructure/FastIntegrationTests.Infrastructure.csproj package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add src/FastIntegrationTests.Infrastructure/FastIntegrationTests.Infrastructure.csproj package Microsoft.EntityFrameworkCore.SqlServer
dotnet add src/FastIntegrationTests.Infrastructure/FastIntegrationTests.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Design
```

- [ ] **Шаг 5: Добавить NuGet-пакет в WebApi (для команд миграций)**

```bash
dotnet add src/FastIntegrationTests.WebApi/FastIntegrationTests.WebApi.csproj package Microsoft.EntityFrameworkCore.Design
```

- [ ] **Шаг 6: Удалить сгенерированный шаблонный код**

```bash
rm src/FastIntegrationTests.WebApi/Controllers/WeatherForecastController.cs
rm src/FastIntegrationTests.WebApi/WeatherForecast.cs
rm src/FastIntegrationTests.Application/Class1.cs
rm src/FastIntegrationTests.Infrastructure/Class1.cs
```

- [ ] **Шаг 7: Проверить сборку**

```bash
dotnet build
```

Ожидаемый вывод: `Build succeeded.`

- [ ] **Шаг 8: Коммит**

```bash
git add .
git commit -m "chore: инициализация solution с тремя проектами и зависимостями"
```

---

### Task 2: Сущности и перечисления (Application)

**Files:**
- Create: `src/FastIntegrationTests.Application/GlobalUsings.cs`
- Create: `src/FastIntegrationTests.Application/Enums/OrderStatus.cs`
- Create: `src/FastIntegrationTests.Application/Entities/Product.cs`
- Create: `src/FastIntegrationTests.Application/Entities/Order.cs`
- Create: `src/FastIntegrationTests.Application/Entities/OrderItem.cs`

- [ ] **Шаг 1: Создать глобальные using-директивы**

`src/FastIntegrationTests.Application/GlobalUsings.cs`:
```csharp
global using FastIntegrationTests.Application.DTOs;
global using FastIntegrationTests.Application.Entities;
global using FastIntegrationTests.Application.Enums;
global using FastIntegrationTests.Application.Exceptions;
global using FastIntegrationTests.Application.Interfaces;
```

- [ ] **Шаг 2: Создать перечисление статусов заказа**

`src/FastIntegrationTests.Application/Enums/OrderStatus.cs`:
```csharp
namespace FastIntegrationTests.Application.Enums;

/// <summary>
/// Статус заказа. Определяет текущее состояние заказа в жизненном цикле.
/// </summary>
public enum OrderStatus
{
    /// <summary>Заказ создан, ожидает подтверждения.</summary>
    New = 0,

    /// <summary>Заказ подтверждён менеджером.</summary>
    Confirmed = 1,

    /// <summary>Заказ передан в доставку.</summary>
    Shipped = 2,

    /// <summary>Заказ доставлен покупателю. Финальный статус.</summary>
    Completed = 3,

    /// <summary>Заказ отменён. Финальный статус.</summary>
    Cancelled = 4,
}
```

- [ ] **Шаг 3: Создать сущность Product**

`src/FastIntegrationTests.Application/Entities/Product.cs`:
```csharp
namespace FastIntegrationTests.Application.Entities;

/// <summary>
/// Товар в каталоге магазина.
/// </summary>
public class Product
{
    /// <summary>Уникальный идентификатор товара.</summary>
    public int Id { get; set; }

    /// <summary>Название товара.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Описание товара.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Текущая цена товара.</summary>
    public decimal Price { get; set; }

    /// <summary>Дата и время создания записи (UTC).</summary>
    public DateTime CreatedAt { get; set; }
}
```

- [ ] **Шаг 4: Создать сущность Order**

`src/FastIntegrationTests.Application/Entities/Order.cs`:
```csharp
namespace FastIntegrationTests.Application.Entities;

/// <summary>
/// Заказ покупателя.
/// </summary>
public class Order
{
    /// <summary>Уникальный идентификатор заказа.</summary>
    public int Id { get; set; }

    /// <summary>Дата и время создания заказа (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Текущий статус заказа.</summary>
    public OrderStatus Status { get; set; }

    /// <summary>
    /// Итоговая сумма заказа.
    /// Рассчитывается в момент создания заказа и не меняется при изменении цен товаров.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Позиции заказа.</summary>
    public List<OrderItem> Items { get; set; } = new();
}
```

- [ ] **Шаг 5: Создать сущность OrderItem**

`src/FastIntegrationTests.Application/Entities/OrderItem.cs`:
```csharp
namespace FastIntegrationTests.Application.Entities;

/// <summary>
/// Позиция заказа — конкретный товар с зафиксированной ценой и количеством.
/// </summary>
public class OrderItem
{
    /// <summary>Уникальный идентификатор позиции.</summary>
    public int Id { get; set; }

    /// <summary>Идентификатор заказа.</summary>
    public int OrderId { get; set; }

    /// <summary>Навигационное свойство — заказ.</summary>
    public Order Order { get; set; } = null!;

    /// <summary>Идентификатор товара.</summary>
    public int ProductId { get; set; }

    /// <summary>Навигационное свойство — товар.</summary>
    public Product Product { get; set; } = null!;

    /// <summary>Количество единиц товара.</summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Цена товара на момент оформления заказа.
    /// Фиксируется при создании заказа и не изменяется при последующем изменении цены товара.
    /// </summary>
    public decimal UnitPrice { get; set; }
}
```

- [ ] **Шаг 6: Проверить сборку**

```bash
dotnet build src/FastIntegrationTests.Application
```

Ожидаемый вывод: `Build succeeded.`

- [ ] **Шаг 7: Коммит**

```bash
git add src/FastIntegrationTests.Application/
git commit -m "feat: доменные сущности и перечисление OrderStatus (Application)"
```

---

### Task 3: DTO (Application)

**Files:**
- Create: `src/FastIntegrationTests.Application/DTOs/ProductDto.cs`
- Create: `src/FastIntegrationTests.Application/DTOs/CreateProductRequest.cs`
- Create: `src/FastIntegrationTests.Application/DTOs/UpdateProductRequest.cs`
- Create: `src/FastIntegrationTests.Application/DTOs/OrderDto.cs`
- Create: `src/FastIntegrationTests.Application/DTOs/OrderItemDto.cs`
- Create: `src/FastIntegrationTests.Application/DTOs/CreateOrderRequest.cs`

- [ ] **Шаг 1: ProductDto**

`src/FastIntegrationTests.Application/DTOs/ProductDto.cs`:
```csharp
namespace FastIntegrationTests.Application.DTOs;

/// <summary>
/// Данные товара, возвращаемые клиенту.
/// </summary>
public class ProductDto
{
    /// <summary>Идентификатор товара.</summary>
    public int Id { get; set; }

    /// <summary>Название товара.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Описание товара.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Текущая цена товара.</summary>
    public decimal Price { get; set; }

    /// <summary>Дата и время создания.</summary>
    public DateTime CreatedAt { get; set; }
}
```

- [ ] **Шаг 2: CreateProductRequest**

`src/FastIntegrationTests.Application/DTOs/CreateProductRequest.cs`:
```csharp
namespace FastIntegrationTests.Application.DTOs;

/// <summary>
/// Запрос на создание нового товара.
/// </summary>
public class CreateProductRequest
{
    /// <summary>Название товара.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Описание товара.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Цена товара.</summary>
    public decimal Price { get; set; }
}
```

- [ ] **Шаг 3: UpdateProductRequest**

`src/FastIntegrationTests.Application/DTOs/UpdateProductRequest.cs`:
```csharp
namespace FastIntegrationTests.Application.DTOs;

/// <summary>
/// Запрос на обновление существующего товара.
/// </summary>
public class UpdateProductRequest
{
    /// <summary>Новое название товара.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Новое описание товара.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Новая цена товара.</summary>
    public decimal Price { get; set; }
}
```

- [ ] **Шаг 4: OrderItemDto**

`src/FastIntegrationTests.Application/DTOs/OrderItemDto.cs`:
```csharp
namespace FastIntegrationTests.Application.DTOs;

/// <summary>
/// Данные позиции заказа, возвращаемые клиенту.
/// </summary>
public class OrderItemDto
{
    /// <summary>Идентификатор позиции.</summary>
    public int Id { get; set; }

    /// <summary>Идентификатор товара.</summary>
    public int ProductId { get; set; }

    /// <summary>Количество единиц товара.</summary>
    public int Quantity { get; set; }

    /// <summary>Цена товара на момент оформления заказа.</summary>
    public decimal UnitPrice { get; set; }
}
```

- [ ] **Шаг 5: OrderDto**

`src/FastIntegrationTests.Application/DTOs/OrderDto.cs`:
```csharp
namespace FastIntegrationTests.Application.DTOs;

/// <summary>
/// Данные заказа, возвращаемые клиенту.
/// </summary>
public class OrderDto
{
    /// <summary>Идентификатор заказа.</summary>
    public int Id { get; set; }

    /// <summary>Дата и время создания заказа.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Текущий статус заказа.</summary>
    public OrderStatus Status { get; set; }

    /// <summary>Итоговая сумма заказа.</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Позиции заказа.</summary>
    public List<OrderItemDto> Items { get; set; } = new();
}
```

- [ ] **Шаг 6: CreateOrderRequest**

`src/FastIntegrationTests.Application/DTOs/CreateOrderRequest.cs`:
```csharp
namespace FastIntegrationTests.Application.DTOs;

/// <summary>
/// Запрос на создание нового заказа.
/// </summary>
public class CreateOrderRequest
{
    /// <summary>Список позиций заказа.</summary>
    public List<OrderItemRequest> Items { get; set; } = new();
}

/// <summary>
/// Позиция в запросе на создание заказа.
/// </summary>
public class OrderItemRequest
{
    /// <summary>Идентификатор товара.</summary>
    public int ProductId { get; set; }

    /// <summary>Количество единиц товара.</summary>
    public int Quantity { get; set; }
}
```

- [ ] **Шаг 7: Проверить сборку**

```bash
dotnet build src/FastIntegrationTests.Application
```

Ожидаемый вывод: `Build succeeded.`

- [ ] **Шаг 8: Коммит**

```bash
git add src/FastIntegrationTests.Application/DTOs/
git commit -m "feat: DTO для товаров и заказов (Application)"
```

---

### Task 4: Интерфейсы репозиториев и доменные исключения (Application)

**Files:**
- Create: `src/FastIntegrationTests.Application/Interfaces/IProductRepository.cs`
- Create: `src/FastIntegrationTests.Application/Interfaces/IOrderRepository.cs`
- Create: `src/FastIntegrationTests.Application/Exceptions/NotFoundException.cs`
- Create: `src/FastIntegrationTests.Application/Exceptions/InvalidOrderStatusTransitionException.cs`

- [ ] **Шаг 1: IProductRepository**

`src/FastIntegrationTests.Application/Interfaces/IProductRepository.cs`:
```csharp
namespace FastIntegrationTests.Application.Interfaces;

/// <summary>
/// Репозиторий для работы с товарами.
/// </summary>
public interface IProductRepository
{
    /// <summary>Возвращает все товары.</summary>
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Возвращает товар по идентификатору или <c>null</c>.</summary>
    Task<Product?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>Добавляет новый товар и возвращает его с присвоенным Id.</summary>
    Task<Product> AddAsync(Product product, CancellationToken ct = default);

    /// <summary>Обновляет существующий товар.</summary>
    Task UpdateAsync(Product product, CancellationToken ct = default);

    /// <summary>Удаляет товар.</summary>
    Task DeleteAsync(Product product, CancellationToken ct = default);
}
```

- [ ] **Шаг 2: IOrderRepository**

`src/FastIntegrationTests.Application/Interfaces/IOrderRepository.cs`:
```csharp
namespace FastIntegrationTests.Application.Interfaces;

/// <summary>
/// Репозиторий для работы с заказами.
/// </summary>
public interface IOrderRepository
{
    /// <summary>Возвращает все заказы (без позиций).</summary>
    Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Возвращает заказ вместе с позициями или <c>null</c>.</summary>
    Task<Order?> GetByIdWithItemsAsync(int id, CancellationToken ct = default);

    /// <summary>Добавляет новый заказ и возвращает его с присвоенным Id.</summary>
    Task<Order> AddAsync(Order order, CancellationToken ct = default);

    /// <summary>Обновляет существующий заказ.</summary>
    Task UpdateAsync(Order order, CancellationToken ct = default);
}
```

- [ ] **Шаг 3: NotFoundException**

`src/FastIntegrationTests.Application/Exceptions/NotFoundException.cs`:
```csharp
namespace FastIntegrationTests.Application.Exceptions;

/// <summary>
/// Выбрасывается, когда запрашиваемая сущность не найдена в базе данных.
/// </summary>
public class NotFoundException : Exception
{
    /// <summary>
    /// Создаёт новый экземпляр <see cref="NotFoundException"/>.
    /// </summary>
    /// <param name="entityName">Название типа сущности.</param>
    /// <param name="id">Идентификатор сущности.</param>
    public NotFoundException(string entityName, object id)
        : base($"{entityName} с идентификатором '{id}' не найден.")
    {
    }
}
```

- [ ] **Шаг 4: InvalidOrderStatusTransitionException**

`src/FastIntegrationTests.Application/Exceptions/InvalidOrderStatusTransitionException.cs`:
```csharp
namespace FastIntegrationTests.Application.Exceptions;

/// <summary>
/// Выбрасывается при попытке выполнить недопустимый переход статуса заказа.
/// </summary>
public class InvalidOrderStatusTransitionException : Exception
{
    /// <summary>
    /// Создаёт новый экземпляр <see cref="InvalidOrderStatusTransitionException"/>.
    /// </summary>
    /// <param name="currentStatus">Текущий статус заказа.</param>
    /// <param name="targetStatus">Запрашиваемый целевой статус.</param>
    public InvalidOrderStatusTransitionException(OrderStatus currentStatus, OrderStatus targetStatus)
        : base($"Переход из статуса '{currentStatus}' в статус '{targetStatus}' недопустим.")
    {
    }
}
```

- [ ] **Шаг 5: Проверить сборку**

```bash
dotnet build src/FastIntegrationTests.Application
```

Ожидаемый вывод: `Build succeeded.`

- [ ] **Шаг 6: Коммит**

```bash
git add src/FastIntegrationTests.Application/Interfaces/ src/FastIntegrationTests.Application/Exceptions/
git commit -m "feat: интерфейсы репозиториев и доменные исключения (Application)"
```

---

### Task 5: ProductService (Application)

**Files:**
- Create: `src/FastIntegrationTests.Application/Services/ProductService.cs`

- [ ] **Шаг 1: Создать ProductService**

`src/FastIntegrationTests.Application/Services/ProductService.cs`:
```csharp
namespace FastIntegrationTests.Application.Services;

/// <summary>
/// Сервис для управления товарами каталога.
/// </summary>
public class ProductService
{
    private readonly IProductRepository _repository;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="ProductService"/>.
    /// </summary>
    /// <param name="repository">Репозиторий товаров.</param>
    public ProductService(IProductRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Возвращает список всех товаров.
    /// </summary>
    public async Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken ct = default)
    {
        var products = await _repository.GetAllAsync(ct);
        return products.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Возвращает товар по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор товара.</param>
    /// <exception cref="NotFoundException">Если товар не найден.</exception>
    public async Task<ProductDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var product = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Product), id);
        return MapToDto(product);
    }

    /// <summary>
    /// Создаёт новый товар.
    /// </summary>
    /// <param name="request">Данные нового товара.</param>
    public async Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            CreatedAt = DateTime.UtcNow,
        };
        var created = await _repository.AddAsync(product, ct);
        return MapToDto(created);
    }

    /// <summary>
    /// Обновляет существующий товар.
    /// </summary>
    /// <param name="id">Идентификатор товара.</param>
    /// <param name="request">Новые данные товара.</param>
    /// <exception cref="NotFoundException">Если товар не найден.</exception>
    public async Task<ProductDto> UpdateAsync(int id, UpdateProductRequest request, CancellationToken ct = default)
    {
        var product = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Product), id);

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;

        await _repository.UpdateAsync(product, ct);
        return MapToDto(product);
    }

    /// <summary>
    /// Удаляет товар по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор товара.</param>
    /// <exception cref="NotFoundException">Если товар не найден.</exception>
    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var product = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Product), id);
        await _repository.DeleteAsync(product, ct);
    }

    private static ProductDto MapToDto(Product p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Description = p.Description,
        Price = p.Price,
        CreatedAt = p.CreatedAt,
    };
}
```

- [ ] **Шаг 2: Проверить сборку**

```bash
dotnet build src/FastIntegrationTests.Application
```

Ожидаемый вывод: `Build succeeded.`

- [ ] **Шаг 3: Коммит**

```bash
git add src/FastIntegrationTests.Application/Services/ProductService.cs
git commit -m "feat: ProductService с CRUD-операциями (Application)"
```

---

### Task 6: OrderService (Application)

**Files:**
- Create: `src/FastIntegrationTests.Application/Services/OrderService.cs`

- [ ] **Шаг 1: Создать OrderService**

`src/FastIntegrationTests.Application/Services/OrderService.cs`:
```csharp
namespace FastIntegrationTests.Application.Services;

/// <summary>
/// Сервис для управления заказами и их жизненным циклом.
/// </summary>
public class OrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="OrderService"/>.
    /// </summary>
    /// <param name="orderRepository">Репозиторий заказов.</param>
    /// <param name="productRepository">Репозиторий товаров.</param>
    public OrderService(IOrderRepository orderRepository, IProductRepository productRepository)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
    }

    /// <summary>
    /// Возвращает список всех заказов.
    /// </summary>
    public async Task<IReadOnlyList<OrderDto>> GetAllAsync(CancellationToken ct = default)
    {
        var orders = await _orderRepository.GetAllAsync(ct);
        return orders.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Возвращает заказ вместе с позициями по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор заказа.</param>
    /// <exception cref="NotFoundException">Если заказ не найден.</exception>
    public async Task<OrderDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var order = await _orderRepository.GetByIdWithItemsAsync(id, ct)
            ?? throw new NotFoundException(nameof(Order), id);
        return MapToDto(order);
    }

    /// <summary>
    /// Создаёт новый заказ. Фиксирует цены товаров и рассчитывает итоговую сумму.
    /// </summary>
    /// <param name="request">Данные нового заказа с позициями.</param>
    /// <exception cref="NotFoundException">Если один из указанных товаров не найден.</exception>
    public async Task<OrderDto> CreateAsync(CreateOrderRequest request, CancellationToken ct = default)
    {
        var order = new Order
        {
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.New,
            Items = new List<OrderItem>(),
        };

        foreach (var itemRequest in request.Items)
        {
            var product = await _productRepository.GetByIdAsync(itemRequest.ProductId, ct)
                ?? throw new NotFoundException(nameof(Product), itemRequest.ProductId);

            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                Quantity = itemRequest.Quantity,
                UnitPrice = product.Price,
            });
        }

        order.TotalAmount = order.Items.Sum(i => i.UnitPrice * i.Quantity);

        var created = await _orderRepository.AddAsync(order, ct);
        return MapToDto(created);
    }

    /// <summary>Подтверждает заказ (New → Confirmed).</summary>
    /// <exception cref="NotFoundException">Если заказ не найден.</exception>
    /// <exception cref="InvalidOrderStatusTransitionException">Если переход недопустим.</exception>
    public Task<OrderDto> ConfirmAsync(int id, CancellationToken ct = default)
        => ChangeStatusAsync(id, OrderStatus.Confirmed, ct);

    /// <summary>Передаёт заказ в доставку (Confirmed → Shipped).</summary>
    /// <exception cref="NotFoundException">Если заказ не найден.</exception>
    /// <exception cref="InvalidOrderStatusTransitionException">Если переход недопустим.</exception>
    public Task<OrderDto> ShipAsync(int id, CancellationToken ct = default)
        => ChangeStatusAsync(id, OrderStatus.Shipped, ct);

    /// <summary>Завершает заказ (Shipped → Completed).</summary>
    /// <exception cref="NotFoundException">Если заказ не найден.</exception>
    /// <exception cref="InvalidOrderStatusTransitionException">Если переход недопустим.</exception>
    public Task<OrderDto> CompleteAsync(int id, CancellationToken ct = default)
        => ChangeStatusAsync(id, OrderStatus.Completed, ct);

    /// <summary>Отменяет заказ (New/Confirmed → Cancelled).</summary>
    /// <exception cref="NotFoundException">Если заказ не найден.</exception>
    /// <exception cref="InvalidOrderStatusTransitionException">Если переход недопустим.</exception>
    public Task<OrderDto> CancelAsync(int id, CancellationToken ct = default)
        => ChangeStatusAsync(id, OrderStatus.Cancelled, ct);

    private async Task<OrderDto> ChangeStatusAsync(int id, OrderStatus targetStatus, CancellationToken ct)
    {
        var order = await _orderRepository.GetByIdWithItemsAsync(id, ct)
            ?? throw new NotFoundException(nameof(Order), id);

        ValidateStatusTransition(order.Status, targetStatus);
        order.Status = targetStatus;
        await _orderRepository.UpdateAsync(order, ct);
        return MapToDto(order);
    }

    /// <summary>
    /// Проверяет допустимость перехода статуса заказа.
    /// Допустимые переходы: New→Confirmed, New→Cancelled, Confirmed→Shipped,
    /// Confirmed→Cancelled, Shipped→Completed.
    /// </summary>
    private static void ValidateStatusTransition(OrderStatus current, OrderStatus target)
    {
        var allowed = current switch
        {
            OrderStatus.New => new[] { OrderStatus.Confirmed, OrderStatus.Cancelled },
            OrderStatus.Confirmed => new[] { OrderStatus.Shipped, OrderStatus.Cancelled },
            OrderStatus.Shipped => new[] { OrderStatus.Completed },
            _ => Array.Empty<OrderStatus>(),
        };

        if (!allowed.Contains(target))
            throw new InvalidOrderStatusTransitionException(current, target);
    }

    private static OrderDto MapToDto(Order o) => new()
    {
        Id = o.Id,
        CreatedAt = o.CreatedAt,
        Status = o.Status,
        TotalAmount = o.TotalAmount,
        Items = o.Items.Select(i => new OrderItemDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
        }).ToList(),
    };
}
```

- [ ] **Шаг 2: Проверить сборку**

```bash
dotnet build src/FastIntegrationTests.Application
```

Ожидаемый вывод: `Build succeeded.`

- [ ] **Шаг 3: Коммит**

```bash
git add src/FastIntegrationTests.Application/Services/OrderService.cs
git commit -m "feat: OrderService с жизненным циклом статусов заказа (Application)"
```

---

### Task 7: ShopDbContext и конфигурации EF Core (Infrastructure)

**Files:**
- Create: `src/FastIntegrationTests.Infrastructure/GlobalUsings.cs`
- Create: `src/FastIntegrationTests.Infrastructure/Data/ShopDbContext.cs`
- Create: `src/FastIntegrationTests.Infrastructure/Data/Configurations/ProductConfiguration.cs`
- Create: `src/FastIntegrationTests.Infrastructure/Data/Configurations/OrderConfiguration.cs`
- Create: `src/FastIntegrationTests.Infrastructure/Data/Configurations/OrderItemConfiguration.cs`
- Create: `src/FastIntegrationTests.Infrastructure/DesignTimeDbContextFactory.cs`

- [ ] **Шаг 1: Создать глобальные using-директивы**

`src/FastIntegrationTests.Infrastructure/GlobalUsings.cs`:
```csharp
global using FastIntegrationTests.Application.Entities;
global using FastIntegrationTests.Application.Interfaces;
global using FastIntegrationTests.Infrastructure.Data;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.EntityFrameworkCore.Metadata.Builders;
global using Microsoft.Extensions.DependencyInjection;
```

- [ ] **Шаг 2: Создать ShopDbContext**

`src/FastIntegrationTests.Infrastructure/Data/ShopDbContext.cs`:
```csharp
namespace FastIntegrationTests.Infrastructure.Data;

/// <summary>
/// Контекст базы данных магазина.
/// Содержит DbSet для всех доменных сущностей и применяет конфигурации EF Core из текущей сборки.
/// </summary>
public class ShopDbContext : DbContext
{
    /// <summary>
    /// Создаёт новый экземпляр <see cref="ShopDbContext"/>.
    /// </summary>
    /// <param name="options">Параметры контекста, включая провайдер и строку подключения.</param>
    public ShopDbContext(DbContextOptions<ShopDbContext> options) : base(options)
    {
    }

    /// <summary>Товары каталога.</summary>
    public DbSet<Product> Products => Set<Product>();

    /// <summary>Заказы.</summary>
    public DbSet<Order> Orders => Set<Order>();

    /// <summary>Позиции заказов.</summary>
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Применяем все конфигурации IEntityTypeConfiguration из текущей сборки
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ShopDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
```

- [ ] **Шаг 3: Конфигурация Product**

`src/FastIntegrationTests.Infrastructure/Data/Configurations/ProductConfiguration.cs`:
```csharp
namespace FastIntegrationTests.Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация таблицы товаров для EF Core.
/// </summary>
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Description).HasMaxLength(1000);
        // decimal(18,2) совместим с PostgreSQL (numeric) и MSSQL (decimal)
        builder.Property(p => p.Price).HasColumnType("decimal(18,2)");
    }
}
```

- [ ] **Шаг 4: Конфигурация Order**

`src/FastIntegrationTests.Infrastructure/Data/Configurations/OrderConfiguration.cs`:
```csharp
namespace FastIntegrationTests.Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация таблицы заказов для EF Core.
/// </summary>
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Status).IsRequired();
        builder.Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");

        builder.HasMany(o => o.Items)
               .WithOne(i => i.Order)
               .HasForeignKey(i => i.OrderId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
```

- [ ] **Шаг 5: Конфигурация OrderItem**

`src/FastIntegrationTests.Infrastructure/Data/Configurations/OrderItemConfiguration.cs`:
```csharp
namespace FastIntegrationTests.Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация таблицы позиций заказов для EF Core.
/// </summary>
public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.UnitPrice).HasColumnType("decimal(18,2)");

        // Restrict: удаление товара запрещено, если на него есть ссылки в заказах
        builder.HasOne(i => i.Product)
               .WithMany()
               .HasForeignKey(i => i.ProductId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
```

- [ ] **Шаг 6: DesignTimeDbContextFactory (для команд dotnet ef)**

`src/FastIntegrationTests.Infrastructure/DesignTimeDbContextFactory.cs`:
```csharp
using Microsoft.EntityFrameworkCore.Design;

namespace FastIntegrationTests.Infrastructure;

/// <summary>
/// Фабрика DbContext для инструментов EF Core в design-time (миграции, scaffolding).
/// Используется только командами dotnet ef — не влияет на production-конфигурацию.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ShopDbContext>
{
    /// <inheritdoc/>
    public ShopDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ShopDbContext>()
            .UseNpgsql("Host=localhost;Database=shop;Username=postgres;Password=postgres")
            .Options;
        return new ShopDbContext(options);
    }
}
```

- [ ] **Шаг 7: Проверить сборку**

```bash
dotnet build src/FastIntegrationTests.Infrastructure
```

Ожидаемый вывод: `Build succeeded.`

- [ ] **Шаг 8: Коммит**

```bash
git add src/FastIntegrationTests.Infrastructure/
git commit -m "feat: ShopDbContext, EF Core конфигурации и DesignTimeDbContextFactory (Infrastructure)"
```

---

### Task 8: Репозитории (Infrastructure)

**Files:**
- Create: `src/FastIntegrationTests.Infrastructure/Repositories/ProductRepository.cs`
- Create: `src/FastIntegrationTests.Infrastructure/Repositories/OrderRepository.cs`

- [ ] **Шаг 1: ProductRepository**

`src/FastIntegrationTests.Infrastructure/Repositories/ProductRepository.cs`:
```csharp
namespace FastIntegrationTests.Infrastructure.Repositories;

/// <summary>
/// Реализация репозитория товаров на основе EF Core.
/// </summary>
public class ProductRepository : IProductRepository
{
    private readonly ShopDbContext _context;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="ProductRepository"/>.
    /// </summary>
    /// <param name="context">Контекст базы данных.</param>
    public ProductRepository(ShopDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default)
        => await _context.Products.AsNoTracking().ToListAsync(ct);

    /// <inheritdoc/>
    public async Task<Product?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _context.Products.FirstOrDefaultAsync(p => p.Id == id, ct);

    /// <inheritdoc/>
    public async Task<Product> AddAsync(Product product, CancellationToken ct = default)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync(ct);
        return product;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Product product, CancellationToken ct = default)
    {
        _context.Products.Update(product);
        await _context.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Product product, CancellationToken ct = default)
    {
        _context.Products.Remove(product);
        await _context.SaveChangesAsync(ct);
    }
}
```

- [ ] **Шаг 2: OrderRepository**

`src/FastIntegrationTests.Infrastructure/Repositories/OrderRepository.cs`:
```csharp
namespace FastIntegrationTests.Infrastructure.Repositories;

/// <summary>
/// Реализация репозитория заказов на основе EF Core.
/// </summary>
public class OrderRepository : IOrderRepository
{
    private readonly ShopDbContext _context;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="OrderRepository"/>.
    /// </summary>
    /// <param name="context">Контекст базы данных.</param>
    public OrderRepository(ShopDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken ct = default)
        => await _context.Orders.AsNoTracking().ToListAsync(ct);

    /// <inheritdoc/>
    public async Task<Order?> GetByIdWithItemsAsync(int id, CancellationToken ct = default)
        => await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    /// <inheritdoc/>
    public async Task<Order> AddAsync(Order order, CancellationToken ct = default)
    {
        _context.Orders.Add(order);
        await _context.SaveChangesAsync(ct);
        return order;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Order order, CancellationToken ct = default)
    {
        _context.Orders.Update(order);
        await _context.SaveChangesAsync(ct);
    }
}
```

- [ ] **Шаг 3: Проверить сборку**

```bash
dotnet build src/FastIntegrationTests.Infrastructure
```

Ожидаемый вывод: `Build succeeded.`

- [ ] **Шаг 4: Коммит**

```bash
git add src/FastIntegrationTests.Infrastructure/Repositories/
git commit -m "feat: репозитории ProductRepository и OrderRepository (Infrastructure)"
```

---

### Task 9: Extension-методы регистрации DI (Infrastructure)

**Files:**
- Create: `src/FastIntegrationTests.Infrastructure/Extensions/ServiceCollectionExtensions.cs`

- [ ] **Шаг 1: Создать ServiceCollectionExtensions**

`src/FastIntegrationTests.Infrastructure/Extensions/ServiceCollectionExtensions.cs`:
```csharp
using FastIntegrationTests.Infrastructure.Repositories;

namespace FastIntegrationTests.Infrastructure.Extensions;

/// <summary>
/// Методы расширения для регистрации зависимостей Infrastructure в контейнере DI.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует <see cref="ShopDbContext"/> с провайдером PostgreSQL
    /// и все репозитории Infrastructure.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <param name="connectionString">Строка подключения к PostgreSQL.</param>
    public static IServiceCollection AddPostgresql(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<ShopDbContext>(options =>
            options.UseNpgsql(connectionString));
        return services.AddRepositories();
    }

    /// <summary>
    /// Регистрирует <see cref="ShopDbContext"/> с провайдером Microsoft SQL Server
    /// и все репозитории Infrastructure.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <param name="connectionString">Строка подключения к MSSQL.</param>
    public static IServiceCollection AddMssql(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<ShopDbContext>(options =>
            options.UseSqlServer(connectionString));
        return services.AddRepositories();
    }

    /// <summary>
    /// Регистрирует репозитории из Infrastructure.
    /// </summary>
    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        return services;
    }
}
```

- [ ] **Шаг 2: Проверить сборку**

```bash
dotnet build src/FastIntegrationTests.Infrastructure
```

Ожидаемый вывод: `Build succeeded.`

- [ ] **Шаг 3: Коммит**

```bash
git add src/FastIntegrationTests.Infrastructure/Extensions/
git commit -m "feat: extension-методы AddPostgresql/AddMssql для регистрации DI (Infrastructure)"
```

---

### Task 10: Program.cs, конфигурация и глобальная обработка ошибок (WebApi)

**Files:**
- Modify: `src/FastIntegrationTests.WebApi/Program.cs`
- Modify: `src/FastIntegrationTests.WebApi/appsettings.json`
- Create: `src/FastIntegrationTests.WebApi/appsettings.Development.json`
- Create: `src/FastIntegrationTests.WebApi/GlobalUsings.cs`
- Create: `src/FastIntegrationTests.WebApi/Middleware/GlobalExceptionHandler.cs`

- [ ] **Шаг 1: Создать GlobalUsings.cs**

`src/FastIntegrationTests.WebApi/GlobalUsings.cs`:
```csharp
global using FastIntegrationTests.Application.DTOs;
global using FastIntegrationTests.Application.Exceptions;
global using FastIntegrationTests.Application.Services;
global using FastIntegrationTests.Infrastructure.Extensions;
global using Microsoft.AspNetCore.Mvc;
```

- [ ] **Шаг 2: Создать GlobalExceptionHandler**

`src/FastIntegrationTests.WebApi/Middleware/GlobalExceptionHandler.cs`:
```csharp
using Microsoft.AspNetCore.Diagnostics;

namespace FastIntegrationTests.WebApi.Middleware;

/// <summary>
/// Глобальный обработчик исключений.
/// Преобразует доменные исключения в HTTP-ответы с соответствующими статус-кодами.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    /// <inheritdoc/>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken ct)
    {
        (int statusCode, string message) = exception switch
        {
            NotFoundException ex => (StatusCodes.Status404NotFound, ex.Message),
            InvalidOrderStatusTransitionException ex => (StatusCodes.Status400BadRequest, ex.Message),
            _ => (0, string.Empty),
        };

        if (statusCode == 0)
            return false;

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(new { error = message }, ct);
        return true;
    }
}
```

- [ ] **Шаг 3: Заменить Program.cs**

`src/FastIntegrationTests.WebApi/Program.cs`:
```csharp
using FastIntegrationTests.WebApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Регистрируем сервисы бизнес-логики
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<OrderService>();

// Регистрируем глобальный обработчик исключений
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Учебный проект: предполагаем, что все миграции используют только EF Core Fluent API
// без raw SQL, поэтому один набор миграций совместим с обоими провайдерами.
// В production-проекте при наличии raw SQL миграции пришлось бы разделять по провайдерам.
var provider = builder.Configuration["DatabaseProvider"]
    ?? throw new InvalidOperationException("Конфигурация 'DatabaseProvider' не задана.");
var connStr = builder.Configuration.GetConnectionString(provider)
    ?? throw new InvalidOperationException($"Строка подключения '{provider}' не задана.");

if (provider == "PostgreSQL")
    builder.Services.AddPostgresql(connStr);
else if (provider == "MSSQL")
    builder.Services.AddMssql(connStr);
else
    throw new InvalidOperationException(
        $"Неизвестный провайдер БД: '{provider}'. Допустимые значения: 'PostgreSQL', 'MSSQL'.");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.MapControllers();
app.Run();
```

- [ ] **Шаг 4: Заменить appsettings.json (только заглушки без реальных паролей)**

`src/FastIntegrationTests.WebApi/appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "DatabaseProvider": "PostgreSQL",
  "ConnectionStrings": {
    "PostgreSQL": "",
    "MSSQL": ""
  }
}
```

- [ ] **Шаг 5: Создать appsettings.Development.json с реальными строками подключения**

`src/FastIntegrationTests.WebApi/appsettings.Development.json`:
```json
{
  "DatabaseProvider": "PostgreSQL",
  "ConnectionStrings": {
    "PostgreSQL": "Host=localhost;Database=shop;Username=postgres;Password=postgres",
    "MSSQL": "Server=localhost,1433;Database=shop;User Id=sa;Password=Strong!Pass1;TrustServerCertificate=true"
  }
}
```

- [ ] **Шаг 6: Проверить сборку**

```bash
dotnet build
```

Ожидаемый вывод: `Build succeeded.`

- [ ] **Шаг 7: Коммит**

```bash
git add src/FastIntegrationTests.WebApi/
git commit -m "feat: Program.cs, конфигурация и GlobalExceptionHandler (WebApi)"
```

---

### Task 11: ProductsController (WebApi)

**Files:**
- Create: `src/FastIntegrationTests.WebApi/Controllers/ProductsController.cs`

- [ ] **Шаг 1: Создать ProductsController**

`src/FastIntegrationTests.WebApi/Controllers/ProductsController.cs`:
```csharp
namespace FastIntegrationTests.WebApi.Controllers;

/// <summary>
/// Контроллер для управления товарами каталога.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ProductService _productService;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="ProductsController"/>.
    /// </summary>
    /// <param name="productService">Сервис управления товарами.</param>
    public ProductsController(ProductService productService)
    {
        _productService = productService;
    }

    /// <summary>
    /// Возвращает список всех товаров.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetAll(CancellationToken ct)
        => Ok(await _productService.GetAllAsync(ct));

    /// <summary>
    /// Возвращает товар по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор товара.</param>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> GetById(int id, CancellationToken ct)
        => Ok(await _productService.GetByIdAsync(id, ct));

    /// <summary>
    /// Создаёт новый товар.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(CreateProductRequest request, CancellationToken ct)
    {
        var created = await _productService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Обновляет существующий товар.
    /// </summary>
    /// <param name="id">Идентификатор товара.</param>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductDto>> Update(
        int id,
        UpdateProductRequest request,
        CancellationToken ct)
        => Ok(await _productService.UpdateAsync(id, request, ct));

    /// <summary>
    /// Удаляет товар по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор товара.</param>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _productService.DeleteAsync(id, ct);
        return NoContent();
    }
}
```

- [ ] **Шаг 2: Проверить сборку**

```bash
dotnet build
```

Ожидаемый вывод: `Build succeeded.`

- [ ] **Шаг 3: Коммит**

```bash
git add src/FastIntegrationTests.WebApi/Controllers/ProductsController.cs
git commit -m "feat: ProductsController с CRUD-эндпоинтами (WebApi)"
```

---

### Task 12: OrdersController (WebApi)

**Files:**
- Create: `src/FastIntegrationTests.WebApi/Controllers/OrdersController.cs`

- [ ] **Шаг 1: Создать OrdersController**

`src/FastIntegrationTests.WebApi/Controllers/OrdersController.cs`:
```csharp
namespace FastIntegrationTests.WebApi.Controllers;

/// <summary>
/// Контроллер для управления заказами и их жизненным циклом.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrderService _orderService;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="OrdersController"/>.
    /// </summary>
    /// <param name="orderService">Сервис управления заказами.</param>
    public OrdersController(OrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>
    /// Возвращает список всех заказов.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetAll(CancellationToken ct)
        => Ok(await _orderService.GetAllAsync(ct));

    /// <summary>
    /// Возвращает заказ с позициями по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор заказа.</param>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDto>> GetById(int id, CancellationToken ct)
        => Ok(await _orderService.GetByIdAsync(id, ct));

    /// <summary>
    /// Создаёт новый заказ.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderRequest request, CancellationToken ct)
    {
        var created = await _orderService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Подтверждает заказ (New → Confirmed).
    /// </summary>
    /// <param name="id">Идентификатор заказа.</param>
    [HttpPost("{id:int}/confirm")]
    public async Task<ActionResult<OrderDto>> Confirm(int id, CancellationToken ct)
        => Ok(await _orderService.ConfirmAsync(id, ct));

    /// <summary>
    /// Передаёт заказ в доставку (Confirmed → Shipped).
    /// </summary>
    /// <param name="id">Идентификатор заказа.</param>
    [HttpPost("{id:int}/ship")]
    public async Task<ActionResult<OrderDto>> Ship(int id, CancellationToken ct)
        => Ok(await _orderService.ShipAsync(id, ct));

    /// <summary>
    /// Завершает заказ (Shipped → Completed).
    /// </summary>
    /// <param name="id">Идентификатор заказа.</param>
    [HttpPost("{id:int}/complete")]
    public async Task<ActionResult<OrderDto>> Complete(int id, CancellationToken ct)
        => Ok(await _orderService.CompleteAsync(id, ct));

    /// <summary>
    /// Отменяет заказ (New/Confirmed → Cancelled).
    /// </summary>
    /// <param name="id">Идентификатор заказа.</param>
    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<OrderDto>> Cancel(int id, CancellationToken ct)
        => Ok(await _orderService.CancelAsync(id, ct));
}
```

- [ ] **Шаг 2: Проверить сборку**

```bash
dotnet build
```

Ожидаемый вывод: `Build succeeded.`

- [ ] **Шаг 3: Коммит**

```bash
git add src/FastIntegrationTests.WebApi/Controllers/OrdersController.cs
git commit -m "feat: OrdersController с эндпоинтами жизненного цикла заказа (WebApi)"
```

---

### Task 13: docker-compose, первая миграция и запуск сервиса

**Files:**
- Create: `docker-compose.yml`

- [ ] **Шаг 1: Создать docker-compose.yml**

`docker-compose.yml` (в корне репозитория):
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

- [ ] **Шаг 2: Запустить PostgreSQL**

```bash
docker-compose up postgres -d
```

Ожидаемый вывод: `Started` или `Running`.

- [ ] **Шаг 3: Проверить, что dotnet-ef установлен**

```bash
dotnet ef --version
```

Если инструмент не установлен:
```bash
dotnet tool install --global dotnet-ef
```

- [ ] **Шаг 4: Создать первую миграцию**

```bash
dotnet ef migrations add InitialCreate \
  --project src/FastIntegrationTests.Infrastructure \
  --startup-project src/FastIntegrationTests.WebApi \
  --output-dir Migrations
```

Ожидаемый вывод: `Done. To undo this action, use 'ef migrations remove'`

- [ ] **Шаг 5: Применить миграцию к базе данных**

```bash
dotnet ef database update \
  --project src/FastIntegrationTests.Infrastructure \
  --startup-project src/FastIntegrationTests.WebApi
```

Ожидаемый вывод: `Done.`

- [ ] **Шаг 6: Запустить сервис и проверить Swagger**

```bash
dotnet run --project src/FastIntegrationTests.WebApi
```

Открыть в браузере: `https://localhost:<port>/swagger`

Проверить что видны эндпоинты `/api/products` и `/api/orders`.

- [ ] **Шаг 7: Коммит**

```bash
git add docker-compose.yml src/FastIntegrationTests.Infrastructure/Migrations/
git commit -m "feat: docker-compose и первая миграция EF Core InitialCreate"
```

---

### Task 14: Обновить CLAUDE.md

**Files:**
- Modify: `CLAUDE.md`

- [ ] **Шаг 1: Заменить CLAUDE.md финальным содержимым**

`CLAUDE.md`:
```markdown
# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Команды разработки

```bash
# Сборка
dotnet build

# Запуск сервиса
dotnet run --project src/FastIntegrationTests.WebApi

# Запустить PostgreSQL локально
docker-compose up postgres -d

# Запустить MSSQL локально
docker-compose up mssql -d

# Добавить новую миграцию
dotnet ef migrations add <НазваниеМиграции> \
  --project src/FastIntegrationTests.Infrastructure \
  --startup-project src/FastIntegrationTests.WebApi \
  --output-dir Migrations

# Применить миграции
dotnet ef database update \
  --project src/FastIntegrationTests.Infrastructure \
  --startup-project src/FastIntegrationTests.WebApi
```

## Архитектура

Трёхслойная архитектура:

- **Application** — доменные сущности (`Entities/`), DTO (`DTOs/`), интерфейсы репозиториев (`Interfaces/`), сервисы бизнес-логики (`Services/`), доменные исключения (`Exceptions/`). Не зависит от EF Core и конкретной СУБД.
- **Infrastructure** — реализация репозиториев через EF Core (`Repositories/`), `ShopDbContext` с конфигурациями (`Data/`), extension-методы регистрации DI (`Extensions/ServiceCollectionExtensions.cs`).
- **WebApi** — контроллеры (`Controllers/`), `Program.cs` с DI-конфигурацией, глобальная обработка ошибок (`Middleware/GlobalExceptionHandler.cs`).

## Переключение провайдера БД

В `appsettings.Development.json` изменить `"DatabaseProvider"` на `"PostgreSQL"` или `"MSSQL"`. Строки подключения хранятся там же. Оба docker-сервиса объявлены в `docker-compose.yml`.

## Соглашения

- Документация и комментарии на русском языке.
- Все публичные классы и методы — с XML-документацией (`/// <summary>`).
- Коммит после каждого логического шага.
```

- [ ] **Шаг 2: Коммит**

```bash
git add CLAUDE.md
git commit -m "docs: обновлён CLAUDE.md с командами и описанием архитектуры"
```
