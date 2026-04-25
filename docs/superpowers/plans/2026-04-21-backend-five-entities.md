# Backend: 5 новых сущностей — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Добавить 5 новых сущностей (Category, Customer, Supplier, Review, Discount) на всех трёх слоях — Application, Infrastructure, WebApi.

**Architecture:** Трёхслойная чистая архитектура. Application не зависит от EF Core. Новые сущности используют `Guid Id` (не `int`). Uniqueness-проверки — в сервисе перед записью.

**Tech Stack:** .NET 9, EF Core + Npgsql, ASP.NET Core minimal API pattern (controllers), xUnit

---

## File Map

### Создать (Application)
- `src/FastIntegrationTests.Application/Enums/CustomerStatus.cs`
- `src/FastIntegrationTests.Application/Enums/ReviewStatus.cs`
- `src/FastIntegrationTests.Application/Exceptions/DuplicateValueException.cs`
- `src/FastIntegrationTests.Application/Exceptions/InvalidRatingException.cs`
- `src/FastIntegrationTests.Application/Exceptions/InvalidDiscountPercentException.cs`
- `src/FastIntegrationTests.Application/Exceptions/InvalidStatusTransitionException.cs`
- `src/FastIntegrationTests.Application/Entities/Category.cs`
- `src/FastIntegrationTests.Application/Entities/Customer.cs`
- `src/FastIntegrationTests.Application/Entities/Supplier.cs`
- `src/FastIntegrationTests.Application/Entities/Review.cs`
- `src/FastIntegrationTests.Application/Entities/Discount.cs`
- `src/FastIntegrationTests.Application/DTOs/CategoryDto.cs`
- `src/FastIntegrationTests.Application/DTOs/CreateCategoryRequest.cs`
- `src/FastIntegrationTests.Application/DTOs/UpdateCategoryRequest.cs`
- `src/FastIntegrationTests.Application/DTOs/CustomerDto.cs`
- `src/FastIntegrationTests.Application/DTOs/CreateCustomerRequest.cs`
- `src/FastIntegrationTests.Application/DTOs/UpdateCustomerRequest.cs`
- `src/FastIntegrationTests.Application/DTOs/SupplierDto.cs`
- `src/FastIntegrationTests.Application/DTOs/CreateSupplierRequest.cs`
- `src/FastIntegrationTests.Application/DTOs/UpdateSupplierRequest.cs`
- `src/FastIntegrationTests.Application/DTOs/ReviewDto.cs`
- `src/FastIntegrationTests.Application/DTOs/CreateReviewRequest.cs`
- `src/FastIntegrationTests.Application/DTOs/DiscountDto.cs`
- `src/FastIntegrationTests.Application/DTOs/CreateDiscountRequest.cs`
- `src/FastIntegrationTests.Application/DTOs/UpdateDiscountRequest.cs`
- `src/FastIntegrationTests.Application/Interfaces/ICategoryRepository.cs`
- `src/FastIntegrationTests.Application/Interfaces/ICategoryService.cs`
- `src/FastIntegrationTests.Application/Interfaces/ICustomerRepository.cs`
- `src/FastIntegrationTests.Application/Interfaces/ICustomerService.cs`
- `src/FastIntegrationTests.Application/Interfaces/ISupplierRepository.cs`
- `src/FastIntegrationTests.Application/Interfaces/ISupplierService.cs`
- `src/FastIntegrationTests.Application/Interfaces/IReviewRepository.cs`
- `src/FastIntegrationTests.Application/Interfaces/IReviewService.cs`
- `src/FastIntegrationTests.Application/Interfaces/IDiscountRepository.cs`
- `src/FastIntegrationTests.Application/Interfaces/IDiscountService.cs`
- `src/FastIntegrationTests.Application/Services/CategoryService.cs`
- `src/FastIntegrationTests.Application/Services/CustomerService.cs`
- `src/FastIntegrationTests.Application/Services/SupplierService.cs`
- `src/FastIntegrationTests.Application/Services/ReviewService.cs`
- `src/FastIntegrationTests.Application/Services/DiscountService.cs`

### Создать (Infrastructure)
- `src/FastIntegrationTests.Infrastructure/Data/Configurations/CategoryConfiguration.cs`
- `src/FastIntegrationTests.Infrastructure/Data/Configurations/CustomerConfiguration.cs`
- `src/FastIntegrationTests.Infrastructure/Data/Configurations/SupplierConfiguration.cs`
- `src/FastIntegrationTests.Infrastructure/Data/Configurations/ReviewConfiguration.cs`
- `src/FastIntegrationTests.Infrastructure/Data/Configurations/DiscountConfiguration.cs`
- `src/FastIntegrationTests.Infrastructure/Repositories/CategoryRepository.cs`
- `src/FastIntegrationTests.Infrastructure/Repositories/CustomerRepository.cs`
- `src/FastIntegrationTests.Infrastructure/Repositories/SupplierRepository.cs`
- `src/FastIntegrationTests.Infrastructure/Repositories/ReviewRepository.cs`
- `src/FastIntegrationTests.Infrastructure/Repositories/DiscountRepository.cs`

### Изменить (Infrastructure)
- `src/FastIntegrationTests.Infrastructure/Data/ShopDbContext.cs` — добавить 5 DbSet
- `src/FastIntegrationTests.Infrastructure/Extensions/ServiceCollectionExtensions.cs` — 5 новых репозиториев

### Создать (WebApi)
- `src/FastIntegrationTests.WebApi/Controllers/CategoriesController.cs`
- `src/FastIntegrationTests.WebApi/Controllers/CustomersController.cs`
- `src/FastIntegrationTests.WebApi/Controllers/SuppliersController.cs`
- `src/FastIntegrationTests.WebApi/Controllers/ReviewsController.cs`
- `src/FastIntegrationTests.WebApi/Controllers/DiscountsController.cs`

### Изменить (WebApi)
- `src/FastIntegrationTests.WebApi/Middleware/GlobalExceptionHandler.cs` — 4 новых типа
- `src/FastIntegrationTests.WebApi/Program.cs` — 5 новых сервисов

---

## Task 1: Enums

**Files:** Create `Enums/CustomerStatus.cs`, `Enums/ReviewStatus.cs`

- [ ] Создать `src/FastIntegrationTests.Application/Enums/CustomerStatus.cs`:

```csharp
namespace FastIntegrationTests.Application.Enums;

/// <summary>Статус покупателя.</summary>
public enum CustomerStatus
{
    /// <summary>Активен.</summary>
    Active = 0,

    /// <summary>Неактивен.</summary>
    Inactive = 1,

    /// <summary>Заблокирован.</summary>
    Banned = 2,
}
```

- [ ] Создать `src/FastIntegrationTests.Application/Enums/ReviewStatus.cs`:

```csharp
namespace FastIntegrationTests.Application.Enums;

/// <summary>Статус отзыва.</summary>
public enum ReviewStatus
{
    /// <summary>На проверке.</summary>
    Pending = 0,

    /// <summary>Одобрен.</summary>
    Approved = 1,

    /// <summary>Отклонён.</summary>
    Rejected = 2,
}
```

- [ ] Commit: `git add src/FastIntegrationTests.Application/Enums/ && git commit -m "feat: CustomerStatus, ReviewStatus enums"`

---

## Task 2: Exceptions

**Files:** Create 4 exception classes in `Exceptions/`

- [ ] Создать `DuplicateValueException.cs`:

```csharp
namespace FastIntegrationTests.Application.Exceptions;

/// <summary>
/// Выбрасывается при нарушении ограничения уникальности поля сущности.
/// </summary>
public class DuplicateValueException : Exception
{
    /// <summary>
    /// Создаёт новый экземпляр <see cref="DuplicateValueException"/>.
    /// </summary>
    /// <param name="entityName">Название типа сущности.</param>
    /// <param name="fieldName">Название поля с нарушенной уникальностью.</param>
    /// <param name="value">Повторяющееся значение.</param>
    public DuplicateValueException(string entityName, string fieldName, string value)
        : base($"{entityName} с {fieldName} '{value}' уже существует.")
    {
    }
}
```

- [ ] Создать `InvalidRatingException.cs`:

```csharp
namespace FastIntegrationTests.Application.Exceptions;

/// <summary>
/// Выбрасывается при указании рейтинга отзыва вне диапазона 1–5.
/// </summary>
public class InvalidRatingException : Exception
{
    /// <summary>
    /// Создаёт новый экземпляр <see cref="InvalidRatingException"/>.
    /// </summary>
    /// <param name="rating">Недопустимый рейтинг.</param>
    public InvalidRatingException(int rating)
        : base($"Рейтинг '{rating}' недопустим. Допустимы значения от 1 до 5.")
    {
    }
}
```

- [ ] Создать `InvalidDiscountPercentException.cs`:

```csharp
namespace FastIntegrationTests.Application.Exceptions;

/// <summary>
/// Выбрасывается при указании процента скидки вне диапазона 1–100.
/// </summary>
public class InvalidDiscountPercentException : Exception
{
    /// <summary>
    /// Создаёт новый экземпляр <see cref="InvalidDiscountPercentException"/>.
    /// </summary>
    /// <param name="percent">Недопустимый процент.</param>
    public InvalidDiscountPercentException(int percent)
        : base($"Процент скидки '{percent}' недопустим. Допустимы значения от 1 до 100.")
    {
    }
}
```

- [ ] Создать `InvalidStatusTransitionException.cs`:

```csharp
namespace FastIntegrationTests.Application.Exceptions;

/// <summary>
/// Выбрасывается при попытке выполнить недопустимый переход статуса
/// для Customer, Review или Supplier.
/// </summary>
public class InvalidStatusTransitionException : Exception
{
    /// <summary>
    /// Создаёт новый экземпляр <see cref="InvalidStatusTransitionException"/>.
    /// </summary>
    /// <param name="currentStatus">Текущий статус.</param>
    /// <param name="targetStatus">Запрашиваемый статус.</param>
    public InvalidStatusTransitionException(Enum currentStatus, Enum targetStatus)
        : base($"Переход из статуса '{currentStatus}' в статус '{targetStatus}' недопустим.")
    {
    }
}
```

- [ ] Commit: `git add src/FastIntegrationTests.Application/Exceptions/ && git commit -m "feat: DuplicateValueException, InvalidRatingException, InvalidDiscountPercentException, InvalidStatusTransitionException"`

---

## Task 3: Category — Domain

**Files:** Entity, DTOs, interfaces, service

- [ ] Создать `Entities/Category.cs`:

```csharp
namespace FastIntegrationTests.Application.Entities;

/// <summary>Категория товаров.</summary>
public class Category
{
    /// <summary>Уникальный идентификатор.</summary>
    public Guid Id { get; set; }

    /// <summary>Название категории (уникально).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Описание категории.</summary>
    public string? Description { get; set; }

    /// <summary>Дата и время создания (UTC).</summary>
    public DateTime CreatedAt { get; set; }
}
```

- [ ] Создать `DTOs/CategoryDto.cs`:

```csharp
namespace FastIntegrationTests.Application.DTOs;

/// <summary>Данные категории, возвращаемые клиенту.</summary>
public class CategoryDto
{
    /// <summary>Идентификатор категории.</summary>
    public Guid Id { get; set; }

    /// <summary>Название категории.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Описание категории.</summary>
    public string? Description { get; set; }

    /// <summary>Дата и время создания.</summary>
    public DateTime CreatedAt { get; set; }
}
```

- [ ] Создать `DTOs/CreateCategoryRequest.cs`:

```csharp
namespace FastIntegrationTests.Application.DTOs;

/// <summary>Запрос на создание категории.</summary>
public class CreateCategoryRequest
{
    /// <summary>Название категории.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Описание категории.</summary>
    public string? Description { get; set; }
}
```

- [ ] Создать `DTOs/UpdateCategoryRequest.cs`:

```csharp
namespace FastIntegrationTests.Application.DTOs;

/// <summary>Запрос на обновление категории.</summary>
public class UpdateCategoryRequest
{
    /// <summary>Новое название категории.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Новое описание категории.</summary>
    public string? Description { get; set; }
}
```

- [ ] Создать `Interfaces/ICategoryRepository.cs`:

```csharp
namespace FastIntegrationTests.Application.Interfaces;

/// <summary>Репозиторий категорий.</summary>
public interface ICategoryRepository
{
    /// <summary>Возвращает все категории.</summary>
    /// <param name="ct">Токен отмены операции.</param>
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Возвращает категорию по идентификатору или null.</summary>
    /// <param name="id">Идентификатор категории.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Проверяет, существует ли категория с указанным именем.</summary>
    /// <param name="name">Название для проверки.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);

    /// <summary>Добавляет новую категорию и возвращает её.</summary>
    /// <param name="category">Сущность категории.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<Category> AddAsync(Category category, CancellationToken ct = default);

    /// <summary>Обновляет существующую категорию.</summary>
    /// <param name="category">Сущность категории.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task UpdateAsync(Category category, CancellationToken ct = default);

    /// <summary>Удаляет категорию.</summary>
    /// <param name="category">Сущность категории.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task DeleteAsync(Category category, CancellationToken ct = default);
}
```

- [ ] Создать `Interfaces/ICategoryService.cs`:

```csharp
namespace FastIntegrationTests.Application.Interfaces;

/// <summary>Сервис для управления категориями товаров.</summary>
public interface ICategoryService
{
    /// <summary>Возвращает список всех категорий.</summary>
    /// <param name="ct">Токен отмены операции.</param>
    Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Возвращает категорию по идентификатору.</summary>
    /// <param name="id">Идентификатор категории.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<CategoryDto> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Создаёт новую категорию.</summary>
    /// <param name="request">Данные новой категории.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default);

    /// <summary>Обновляет существующую категорию.</summary>
    /// <param name="id">Идентификатор категории.</param>
    /// <param name="request">Новые данные.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<CategoryDto> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken ct = default);

    /// <summary>Удаляет категорию.</summary>
    /// <param name="id">Идентификатор категории.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
```

- [ ] Создать `Services/CategoryService.cs`:

```csharp
namespace FastIntegrationTests.Application.Services;

/// <summary>Сервис для управления категориями товаров.</summary>
public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _repository;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="CategoryService"/>.
    /// </summary>
    /// <param name="repository">Репозиторий категорий.</param>
    public CategoryService(ICategoryRepository repository) => _repository = repository;

    /// <summary>Возвращает список всех категорий.</summary>
    /// <param name="ct">Токен отмены операции.</param>
    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct = default)
    {
        var items = await _repository.GetAllAsync(ct);
        return items.Select(MapToDto).ToList();
    }

    /// <summary>Возвращает категорию по идентификатору.</summary>
    /// <param name="id">Идентификатор категории.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="NotFoundException">Если категория не найдена.</exception>
    public async Task<CategoryDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Category), id);
        return MapToDto(item);
    }

    /// <summary>Создаёт новую категорию.</summary>
    /// <param name="request">Данные новой категории.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="DuplicateValueException">Если категория с таким именем уже существует.</exception>
    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default)
    {
        if (await _repository.ExistsByNameAsync(request.Name, ct))
            throw new DuplicateValueException(nameof(Category), nameof(Category.Name), request.Name);

        var item = new Category
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
        };
        var created = await _repository.AddAsync(item, ct);
        return MapToDto(created);
    }

    /// <summary>Обновляет существующую категорию.</summary>
    /// <param name="id">Идентификатор категории.</param>
    /// <param name="request">Новые данные.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="NotFoundException">Если категория не найдена.</exception>
    /// <exception cref="DuplicateValueException">Если новое имя уже занято другой категорией.</exception>
    public async Task<CategoryDto> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Category), id);

        if (item.Name != request.Name && await _repository.ExistsByNameAsync(request.Name, ct))
            throw new DuplicateValueException(nameof(Category), nameof(Category.Name), request.Name);

        item.Name = request.Name;
        item.Description = request.Description;
        await _repository.UpdateAsync(item, ct);
        return MapToDto(item);
    }

    /// <summary>Удаляет категорию.</summary>
    /// <param name="id">Идентификатор категории.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="NotFoundException">Если категория не найдена.</exception>
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Category), id);
        await _repository.DeleteAsync(item, ct);
    }

    private static CategoryDto MapToDto(Category c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Description = c.Description,
        CreatedAt = c.CreatedAt,
    };
}
```

- [ ] Commit: `git add src/FastIntegrationTests.Application/ && git commit -m "feat: Category domain — entity, DTOs, repository/service interfaces, service"`

---

## Task 4: Customer — Domain

- [ ] Создать `Entities/Customer.cs`:

```csharp
namespace FastIntegrationTests.Application.Entities;

/// <summary>Покупатель.</summary>
public class Customer
{
    /// <summary>Уникальный идентификатор.</summary>
    public Guid Id { get; set; }

    /// <summary>Имя покупателя.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Электронная почта (уникальна).</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Номер телефона.</summary>
    public string? Phone { get; set; }

    /// <summary>Статус покупателя.</summary>
    public CustomerStatus Status { get; set; }

    /// <summary>Дата и время создания (UTC).</summary>
    public DateTime CreatedAt { get; set; }
}
```

- [ ] Создать `DTOs/CustomerDto.cs`:

```csharp
namespace FastIntegrationTests.Application.DTOs;

/// <summary>Данные покупателя, возвращаемые клиенту.</summary>
public class CustomerDto
{
    /// <summary>Идентификатор покупателя.</summary>
    public Guid Id { get; set; }

    /// <summary>Имя покупателя.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Электронная почта.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Номер телефона.</summary>
    public string? Phone { get; set; }

    /// <summary>Статус покупателя.</summary>
    public CustomerStatus Status { get; set; }

    /// <summary>Дата и время создания.</summary>
    public DateTime CreatedAt { get; set; }
}
```

- [ ] Создать `DTOs/CreateCustomerRequest.cs`:

```csharp
namespace FastIntegrationTests.Application.DTOs;

/// <summary>Запрос на создание покупателя.</summary>
public class CreateCustomerRequest
{
    /// <summary>Имя покупателя.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Электронная почта.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Номер телефона.</summary>
    public string? Phone { get; set; }
}
```

- [ ] Создать `DTOs/UpdateCustomerRequest.cs`:

```csharp
namespace FastIntegrationTests.Application.DTOs;

/// <summary>Запрос на обновление данных покупателя.</summary>
public class UpdateCustomerRequest
{
    /// <summary>Новое имя покупателя.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Новая электронная почта.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Новый номер телефона.</summary>
    public string? Phone { get; set; }
}
```

- [ ] Создать `Interfaces/ICustomerRepository.cs`:

```csharp
namespace FastIntegrationTests.Application.Interfaces;

/// <summary>Репозиторий покупателей.</summary>
public interface ICustomerRepository
{
    /// <summary>Возвращает всех покупателей.</summary>
    /// <param name="ct">Токен отмены операции.</param>
    Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Возвращает покупателя по идентификатору или null.</summary>
    /// <param name="id">Идентификатор покупателя.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Проверяет, существует ли покупатель с указанным email.</summary>
    /// <param name="email">Email для проверки.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>Добавляет нового покупателя и возвращает его.</summary>
    /// <param name="customer">Сущность покупателя.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<Customer> AddAsync(Customer customer, CancellationToken ct = default);

    /// <summary>Обновляет существующего покупателя.</summary>
    /// <param name="customer">Сущность покупателя.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task UpdateAsync(Customer customer, CancellationToken ct = default);

    /// <summary>Удаляет покупателя.</summary>
    /// <param name="customer">Сущность покупателя.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task DeleteAsync(Customer customer, CancellationToken ct = default);
}
```

- [ ] Создать `Interfaces/ICustomerService.cs`:

```csharp
namespace FastIntegrationTests.Application.Interfaces;

/// <summary>Сервис для управления покупателями.</summary>
public interface ICustomerService
{
    /// <summary>Возвращает список всех покупателей.</summary>
    /// <param name="ct">Токен отмены операции.</param>
    Task<IReadOnlyList<CustomerDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Возвращает покупателя по идентификатору.</summary>
    /// <param name="id">Идентификатор покупателя.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<CustomerDto> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Создаёт нового покупателя.</summary>
    /// <param name="request">Данные нового покупателя.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken ct = default);

    /// <summary>Обновляет существующего покупателя.</summary>
    /// <param name="id">Идентификатор покупателя.</param>
    /// <param name="request">Новые данные.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<CustomerDto> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken ct = default);

    /// <summary>Удаляет покупателя.</summary>
    /// <param name="id">Идентификатор покупателя.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Блокирует покупателя.</summary>
    /// <param name="id">Идентификатор покупателя.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<CustomerDto> BanAsync(Guid id, CancellationToken ct = default);

    /// <summary>Активирует покупателя.</summary>
    /// <param name="id">Идентификатор покупателя.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<CustomerDto> ActivateAsync(Guid id, CancellationToken ct = default);

    /// <summary>Деактивирует покупателя.</summary>
    /// <param name="id">Идентификатор покупателя.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<CustomerDto> DeactivateAsync(Guid id, CancellationToken ct = default);
}
```

- [ ] Создать `Services/CustomerService.cs`:

```csharp
namespace FastIntegrationTests.Application.Services;

/// <summary>Сервис для управления покупателями.</summary>
public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _repository;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="CustomerService"/>.
    /// </summary>
    /// <param name="repository">Репозиторий покупателей.</param>
    public CustomerService(ICustomerRepository repository) => _repository = repository;

    /// <summary>Возвращает список всех покупателей.</summary>
    /// <param name="ct">Токен отмены операции.</param>
    public async Task<IReadOnlyList<CustomerDto>> GetAllAsync(CancellationToken ct = default)
    {
        var items = await _repository.GetAllAsync(ct);
        return items.Select(MapToDto).ToList();
    }

    /// <summary>Возвращает покупателя по идентификатору.</summary>
    /// <param name="id">Идентификатор покупателя.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="NotFoundException">Если покупатель не найден.</exception>
    public async Task<CustomerDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Customer), id);
        return MapToDto(item);
    }

    /// <summary>Создаёт нового покупателя.</summary>
    /// <param name="request">Данные нового покупателя.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="DuplicateValueException">Если покупатель с таким email уже существует.</exception>
    public async Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken ct = default)
    {
        if (await _repository.ExistsByEmailAsync(request.Email, ct))
            throw new DuplicateValueException(nameof(Customer), nameof(Customer.Email), request.Email);

        var item = new Customer
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            Status = CustomerStatus.Active,
            CreatedAt = DateTime.UtcNow,
        };
        var created = await _repository.AddAsync(item, ct);
        return MapToDto(created);
    }

    /// <summary>Обновляет существующего покупателя.</summary>
    /// <param name="id">Идентификатор покупателя.</param>
    /// <param name="request">Новые данные.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="NotFoundException">Если покупатель не найден.</exception>
    /// <exception cref="DuplicateValueException">Если новый email уже занят.</exception>
    public async Task<CustomerDto> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Customer), id);

        if (item.Email != request.Email && await _repository.ExistsByEmailAsync(request.Email, ct))
            throw new DuplicateValueException(nameof(Customer), nameof(Customer.Email), request.Email);

        item.Name = request.Name;
        item.Email = request.Email;
        item.Phone = request.Phone;
        await _repository.UpdateAsync(item, ct);
        return MapToDto(item);
    }

    /// <summary>Удаляет покупателя.</summary>
    /// <param name="id">Идентификатор покупателя.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="NotFoundException">Если покупатель не найден.</exception>
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Customer), id);
        await _repository.DeleteAsync(item, ct);
    }

    /// <summary>Блокирует покупателя.</summary>
    /// <param name="id">Идентификатор покупателя.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="NotFoundException">Если покупатель не найден.</exception>
    /// <exception cref="InvalidStatusTransitionException">Если покупатель уже заблокирован.</exception>
    public async Task<CustomerDto> BanAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Customer), id);

        if (item.Status == CustomerStatus.Banned)
            throw new InvalidStatusTransitionException(item.Status, CustomerStatus.Banned);

        item.Status = CustomerStatus.Banned;
        await _repository.UpdateAsync(item, ct);
        return MapToDto(item);
    }

    /// <summary>Активирует покупателя.</summary>
    /// <param name="id">Идентификатор покупателя.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="NotFoundException">Если покупатель не найден.</exception>
    public async Task<CustomerDto> ActivateAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Customer), id);

        item.Status = CustomerStatus.Active;
        await _repository.UpdateAsync(item, ct);
        return MapToDto(item);
    }

    /// <summary>Деактивирует покупателя.</summary>
    /// <param name="id">Идентификатор покупателя.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="NotFoundException">Если покупатель не найден.</exception>
    public async Task<CustomerDto> DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Customer), id);

        item.Status = CustomerStatus.Inactive;
        await _repository.UpdateAsync(item, ct);
        return MapToDto(item);
    }

    private static CustomerDto MapToDto(Customer c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Email = c.Email,
        Phone = c.Phone,
        Status = c.Status,
        CreatedAt = c.CreatedAt,
    };
}
```

- [ ] Commit: `git add src/FastIntegrationTests.Application/ && git commit -m "feat: Customer domain — entity, DTOs, repository/service interfaces, service"`

---

## Task 5: Supplier — Domain

- [ ] Создать `Entities/Supplier.cs`:

```csharp
namespace FastIntegrationTests.Application.Entities;

/// <summary>Поставщик.</summary>
public class Supplier
{
    /// <summary>Уникальный идентификатор.</summary>
    public Guid Id { get; set; }

    /// <summary>Название поставщика.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Контактный email (уникален).</summary>
    public string ContactEmail { get; set; } = string.Empty;

    /// <summary>Страна поставщика.</summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>Активен ли поставщик.</summary>
    public bool IsActive { get; set; }

    /// <summary>Дата и время создания (UTC).</summary>
    public DateTime CreatedAt { get; set; }
}
```

- [ ] Создать `DTOs/SupplierDto.cs`:

```csharp
namespace FastIntegrationTests.Application.DTOs;

/// <summary>Данные поставщика, возвращаемые клиенту.</summary>
public class SupplierDto
{
    /// <summary>Идентификатор поставщика.</summary>
    public Guid Id { get; set; }

    /// <summary>Название поставщика.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Контактный email.</summary>
    public string ContactEmail { get; set; } = string.Empty;

    /// <summary>Страна поставщика.</summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>Активен ли поставщик.</summary>
    public bool IsActive { get; set; }

    /// <summary>Дата и время создания.</summary>
    public DateTime CreatedAt { get; set; }
}
```

- [ ] Создать `DTOs/CreateSupplierRequest.cs`:

```csharp
namespace FastIntegrationTests.Application.DTOs;

/// <summary>Запрос на создание поставщика.</summary>
public class CreateSupplierRequest
{
    /// <summary>Название поставщика.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Контактный email.</summary>
    public string ContactEmail { get; set; } = string.Empty;

    /// <summary>Страна поставщика.</summary>
    public string Country { get; set; } = string.Empty;
}
```

- [ ] Создать `DTOs/UpdateSupplierRequest.cs`:

```csharp
namespace FastIntegrationTests.Application.DTOs;

/// <summary>Запрос на обновление поставщика.</summary>
public class UpdateSupplierRequest
{
    /// <summary>Новое название поставщика.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Новый контактный email.</summary>
    public string ContactEmail { get; set; } = string.Empty;

    /// <summary>Новая страна поставщика.</summary>
    public string Country { get; set; } = string.Empty;
}
```

- [ ] Создать `Interfaces/ISupplierRepository.cs`:

```csharp
namespace FastIntegrationTests.Application.Interfaces;

/// <summary>Репозиторий поставщиков.</summary>
public interface ISupplierRepository
{
    /// <summary>Возвращает всех поставщиков.</summary>
    /// <param name="ct">Токен отмены операции.</param>
    Task<IReadOnlyList<Supplier>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Возвращает поставщика по идентификатору или null.</summary>
    /// <param name="id">Идентификатор поставщика.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<Supplier?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Проверяет, существует ли поставщик с указанным contactEmail.</summary>
    /// <param name="email">Email для проверки.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>Добавляет нового поставщика и возвращает его.</summary>
    /// <param name="supplier">Сущность поставщика.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<Supplier> AddAsync(Supplier supplier, CancellationToken ct = default);

    /// <summary>Обновляет существующего поставщика.</summary>
    /// <param name="supplier">Сущность поставщика.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task UpdateAsync(Supplier supplier, CancellationToken ct = default);

    /// <summary>Удаляет поставщика.</summary>
    /// <param name="supplier">Сущность поставщика.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task DeleteAsync(Supplier supplier, CancellationToken ct = default);
}
```

- [ ] Создать `Interfaces/ISupplierService.cs`:

```csharp
namespace FastIntegrationTests.Application.Interfaces;

/// <summary>Сервис для управления поставщиками.</summary>
public interface ISupplierService
{
    /// <summary>Возвращает список всех поставщиков.</summary>
    /// <param name="ct">Токен отмены операции.</param>
    Task<IReadOnlyList<SupplierDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Возвращает поставщика по идентификатору.</summary>
    /// <param name="id">Идентификатор поставщика.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<SupplierDto> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Создаёт нового поставщика.</summary>
    /// <param name="request">Данные нового поставщика.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<SupplierDto> CreateAsync(CreateSupplierRequest request, CancellationToken ct = default);

    /// <summary>Обновляет существующего поставщика.</summary>
    /// <param name="id">Идентификатор поставщика.</param>
    /// <param name="request">Новые данные.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<SupplierDto> UpdateAsync(Guid id, UpdateSupplierRequest request, CancellationToken ct = default);

    /// <summary>Удаляет поставщика.</summary>
    /// <param name="id">Идентификатор поставщика.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Активирует поставщика.</summary>
    /// <param name="id">Идентификатор поставщика.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<SupplierDto> ActivateAsync(Guid id, CancellationToken ct = default);

    /// <summary>Деактивирует поставщика.</summary>
    /// <param name="id">Идентификатор поставщика.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<SupplierDto> DeactivateAsync(Guid id, CancellationToken ct = default);
}
```

- [ ] Создать `Services/SupplierService.cs`:

```csharp
namespace FastIntegrationTests.Application.Services;

/// <summary>Сервис для управления поставщиками.</summary>
public class SupplierService : ISupplierService
{
    private readonly ISupplierRepository _repository;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="SupplierService"/>.
    /// </summary>
    /// <param name="repository">Репозиторий поставщиков.</param>
    public SupplierService(ISupplierRepository repository) => _repository = repository;

    /// <summary>Возвращает список всех поставщиков.</summary>
    /// <param name="ct">Токен отмены операции.</param>
    public async Task<IReadOnlyList<SupplierDto>> GetAllAsync(CancellationToken ct = default)
    {
        var items = await _repository.GetAllAsync(ct);
        return items.Select(MapToDto).ToList();
    }

    /// <summary>Возвращает поставщика по идентификатору.</summary>
    /// <param name="id">Идентификатор поставщика.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="NotFoundException">Если поставщик не найден.</exception>
    public async Task<SupplierDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Supplier), id);
        return MapToDto(item);
    }

    /// <summary>Создаёт нового поставщика.</summary>
    /// <param name="request">Данные нового поставщика.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="DuplicateValueException">Если поставщик с таким email уже существует.</exception>
    public async Task<SupplierDto> CreateAsync(CreateSupplierRequest request, CancellationToken ct = default)
    {
        if (await _repository.ExistsByEmailAsync(request.ContactEmail, ct))
            throw new DuplicateValueException(nameof(Supplier), nameof(Supplier.ContactEmail), request.ContactEmail);

        var item = new Supplier
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            ContactEmail = request.ContactEmail,
            Country = request.Country,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        var created = await _repository.AddAsync(item, ct);
        return MapToDto(created);
    }

    /// <summary>Обновляет существующего поставщика.</summary>
    /// <param name="id">Идентификатор поставщика.</param>
    /// <param name="request">Новые данные.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="NotFoundException">Если поставщик не найден.</exception>
    /// <exception cref="DuplicateValueException">Если новый email уже занят.</exception>
    public async Task<SupplierDto> UpdateAsync(Guid id, UpdateSupplierRequest request, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Supplier), id);

        if (item.ContactEmail != request.ContactEmail && await _repository.ExistsByEmailAsync(request.ContactEmail, ct))
            throw new DuplicateValueException(nameof(Supplier), nameof(Supplier.ContactEmail), request.ContactEmail);

        item.Name = request.Name;
        item.ContactEmail = request.ContactEmail;
        item.Country = request.Country;
        await _repository.UpdateAsync(item, ct);
        return MapToDto(item);
    }

    /// <summary>Удаляет поставщика.</summary>
    /// <param name="id">Идентификатор поставщика.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="NotFoundException">Если поставщик не найден.</exception>
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Supplier), id);
        await _repository.DeleteAsync(item, ct);
    }

    /// <summary>Активирует поставщика.</summary>
    /// <param name="id">Идентификатор поставщика.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="NotFoundException">Если поставщик не найден.</exception>
    public async Task<SupplierDto> ActivateAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Supplier), id);
        item.IsActive = true;
        await _repository.UpdateAsync(item, ct);
        return MapToDto(item);
    }

    /// <summary>Деактивирует поставщика.</summary>
    /// <param name="id">Идентификатор поставщика.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="NotFoundException">Если поставщик не найден.</exception>
    public async Task<SupplierDto> DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Supplier), id);
        item.IsActive = false;
        await _repository.UpdateAsync(item, ct);
        return MapToDto(item);
    }

    private static SupplierDto MapToDto(Supplier s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        ContactEmail = s.ContactEmail,
        Country = s.Country,
        IsActive = s.IsActive,
        CreatedAt = s.CreatedAt,
    };
}
```

- [ ] Commit: `git add src/FastIntegrationTests.Application/ && git commit -m "feat: Supplier domain — entity, DTOs, repository/service interfaces, service"`

---

## Task 6: Review — Domain

- [ ] Создать `Entities/Review.cs`:

```csharp
namespace FastIntegrationTests.Application.Entities;

/// <summary>Отзыв.</summary>
public class Review
{
    /// <summary>Уникальный идентификатор.</summary>
    public Guid Id { get; set; }

    /// <summary>Заголовок отзыва.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Текст отзыва.</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>Рейтинг (1–5).</summary>
    public int Rating { get; set; }

    /// <summary>Статус отзыва.</summary>
    public ReviewStatus Status { get; set; }

    /// <summary>Дата и время создания (UTC).</summary>
    public DateTime CreatedAt { get; set; }
}
```

- [ ] Создать `DTOs/ReviewDto.cs`:

```csharp
namespace FastIntegrationTests.Application.DTOs;

/// <summary>Данные отзыва, возвращаемые клиенту.</summary>
public class ReviewDto
{
    /// <summary>Идентификатор отзыва.</summary>
    public Guid Id { get; set; }

    /// <summary>Заголовок отзыва.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Текст отзыва.</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>Рейтинг (1–5).</summary>
    public int Rating { get; set; }

    /// <summary>Статус отзыва.</summary>
    public ReviewStatus Status { get; set; }

    /// <summary>Дата и время создания.</summary>
    public DateTime CreatedAt { get; set; }
}
```

- [ ] Создать `DTOs/CreateReviewRequest.cs`:

```csharp
namespace FastIntegrationTests.Application.DTOs;

/// <summary>Запрос на создание отзыва.</summary>
public class CreateReviewRequest
{
    /// <summary>Заголовок отзыва.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Текст отзыва.</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>Рейтинг (1–5).</summary>
    public int Rating { get; set; }
}
```

- [ ] Создать `Interfaces/IReviewRepository.cs`:

```csharp
namespace FastIntegrationTests.Application.Interfaces;

/// <summary>Репозиторий отзывов.</summary>
public interface IReviewRepository
{
    /// <summary>Возвращает все отзывы.</summary>
    /// <param name="ct">Токен отмены операции.</param>
    Task<IReadOnlyList<Review>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Возвращает отзыв по идентификатору или null.</summary>
    /// <param name="id">Идентификатор отзыва.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<Review?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Добавляет новый отзыв и возвращает его.</summary>
    /// <param name="review">Сущность отзыва.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<Review> AddAsync(Review review, CancellationToken ct = default);

    /// <summary>Обновляет существующий отзыв.</summary>
    /// <param name="review">Сущность отзыва.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task UpdateAsync(Review review, CancellationToken ct = default);

    /// <summary>Удаляет отзыв.</summary>
    /// <param name="review">Сущность отзыва.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task DeleteAsync(Review review, CancellationToken ct = default);
}
```

- [ ] Создать `Interfaces/IReviewService.cs`:

```csharp
namespace FastIntegrationTests.Application.Interfaces;

/// <summary>Сервис для управления отзывами.</summary>
public interface IReviewService
{
    /// <summary>Возвращает список всех отзывов.</summary>
    /// <param name="ct">Токен отмены операции.</param>
    Task<IReadOnlyList<ReviewDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Возвращает отзыв по идентификатору.</summary>
    /// <param name="id">Идентификатор отзыва.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<ReviewDto> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Создаёт новый отзыв.</summary>
    /// <param name="request">Данные нового отзыва.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<ReviewDto> CreateAsync(CreateReviewRequest request, CancellationToken ct = default);

    /// <summary>Удаляет отзыв.</summary>
    /// <param name="id">Идентификатор отзыва.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Одобряет отзыв (Pending → Approved).</summary>
    /// <param name="id">Идентификатор отзыва.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<ReviewDto> ApproveAsync(Guid id, CancellationToken ct = default);

    /// <summary>Отклоняет отзыв (Pending → Rejected).</summary>
    /// <param name="id">Идентификатор отзыва.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<ReviewDto> RejectAsync(Guid id, CancellationToken ct = default);
}
```

- [ ] Создать `Services/ReviewService.cs`:

```csharp
namespace FastIntegrationTests.Application.Services;

/// <summary>Сервис для управления отзывами.</summary>
public class ReviewService : IReviewService
{
    private readonly IReviewRepository _repository;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="ReviewService"/>.
    /// </summary>
    /// <param name="repository">Репозиторий отзывов.</param>
    public ReviewService(IReviewRepository repository) => _repository = repository;

    /// <summary>Возвращает список всех отзывов.</summary>
    /// <param name="ct">Токен отмены операции.</param>
    public async Task<IReadOnlyList<ReviewDto>> GetAllAsync(CancellationToken ct = default)
    {
        var items = await _repository.GetAllAsync(ct);
        return items.Select(MapToDto).ToList();
    }

    /// <summary>Возвращает отзыв по идентификатору.</summary>
    /// <param name="id">Идентификатор отзыва.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="NotFoundException">Если отзыв не найден.</exception>
    public async Task<ReviewDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Review), id);
        return MapToDto(item);
    }

    /// <summary>Создаёт новый отзыв.</summary>
    /// <param name="request">Данные нового отзыва.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="InvalidRatingException">Если рейтинг вне диапазона 1–5.</exception>
    public async Task<ReviewDto> CreateAsync(CreateReviewRequest request, CancellationToken ct = default)
    {
        if (request.Rating < 1 || request.Rating > 5)
            throw new InvalidRatingException(request.Rating);

        var item = new Review
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Body = request.Body,
            Rating = request.Rating,
            Status = ReviewStatus.Pending,
            CreatedAt = DateTime.UtcNow,
        };
        var created = await _repository.AddAsync(item, ct);
        return MapToDto(created);
    }

    /// <summary>Удаляет отзыв.</summary>
    /// <param name="id">Идентификатор отзыва.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="NotFoundException">Если отзыв не найден.</exception>
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Review), id);
        await _repository.DeleteAsync(item, ct);
    }

    /// <summary>Одобряет отзыв (Pending → Approved).</summary>
    /// <param name="id">Идентификатор отзыва.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="NotFoundException">Если отзыв не найден.</exception>
    /// <exception cref="InvalidStatusTransitionException">Если статус не Pending.</exception>
    public async Task<ReviewDto> ApproveAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Review), id);

        if (item.Status != ReviewStatus.Pending)
            throw new InvalidStatusTransitionException(item.Status, ReviewStatus.Approved);

        item.Status = ReviewStatus.Approved;
        await _repository.UpdateAsync(item, ct);
        return MapToDto(item);
    }

    /// <summary>Отклоняет отзыв (Pending → Rejected).</summary>
    /// <param name="id">Идентификатор отзыва.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="NotFoundException">Если отзыв не найден.</exception>
    /// <exception cref="InvalidStatusTransitionException">Если статус не Pending.</exception>
    public async Task<ReviewDto> RejectAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Review), id);

        if (item.Status != ReviewStatus.Pending)
            throw new InvalidStatusTransitionException(item.Status, ReviewStatus.Rejected);

        item.Status = ReviewStatus.Rejected;
        await _repository.UpdateAsync(item, ct);
        return MapToDto(item);
    }

    private static ReviewDto MapToDto(Review r) => new()
    {
        Id = r.Id,
        Title = r.Title,
        Body = r.Body,
        Rating = r.Rating,
        Status = r.Status,
        CreatedAt = r.CreatedAt,
    };
}
```

- [ ] Commit: `git add src/FastIntegrationTests.Application/ && git commit -m "feat: Review domain — entity, DTOs, repository/service interfaces, service"`

---

## Task 7: Discount — Domain

- [ ] Создать `Entities/Discount.cs`:

```csharp
namespace FastIntegrationTests.Application.Entities;

/// <summary>Скидка.</summary>
public class Discount
{
    /// <summary>Уникальный идентификатор.</summary>
    public Guid Id { get; set; }

    /// <summary>Код скидки (уникален).</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Процент скидки (1–100).</summary>
    public int DiscountPercent { get; set; }

    /// <summary>Активна ли скидка.</summary>
    public bool IsActive { get; set; }

    /// <summary>Дата истечения скидки (UTC, опционально).</summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>Дата и время создания (UTC).</summary>
    public DateTime CreatedAt { get; set; }
}
```

- [ ] Создать `DTOs/DiscountDto.cs`:

```csharp
namespace FastIntegrationTests.Application.DTOs;

/// <summary>Данные скидки, возвращаемые клиенту.</summary>
public class DiscountDto
{
    /// <summary>Идентификатор скидки.</summary>
    public Guid Id { get; set; }

    /// <summary>Код скидки.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Процент скидки (1–100).</summary>
    public int DiscountPercent { get; set; }

    /// <summary>Активна ли скидка.</summary>
    public bool IsActive { get; set; }

    /// <summary>Дата истечения скидки (UTC).</summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>Дата и время создания.</summary>
    public DateTime CreatedAt { get; set; }
}
```

- [ ] Создать `DTOs/CreateDiscountRequest.cs`:

```csharp
namespace FastIntegrationTests.Application.DTOs;

/// <summary>Запрос на создание скидки.</summary>
public class CreateDiscountRequest
{
    /// <summary>Код скидки.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Процент скидки (1–100).</summary>
    public int DiscountPercent { get; set; }

    /// <summary>Дата истечения скидки (UTC, опционально).</summary>
    public DateTime? ExpiresAt { get; set; }
}
```

- [ ] Создать `DTOs/UpdateDiscountRequest.cs`:

```csharp
namespace FastIntegrationTests.Application.DTOs;

/// <summary>Запрос на обновление скидки.</summary>
public class UpdateDiscountRequest
{
    /// <summary>Новый код скидки.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Новый процент скидки (1–100).</summary>
    public int DiscountPercent { get; set; }

    /// <summary>Новая дата истечения скидки (UTC, опционально).</summary>
    public DateTime? ExpiresAt { get; set; }
}
```

- [ ] Создать `Interfaces/IDiscountRepository.cs`:

```csharp
namespace FastIntegrationTests.Application.Interfaces;

/// <summary>Репозиторий скидок.</summary>
public interface IDiscountRepository
{
    /// <summary>Возвращает все скидки.</summary>
    /// <param name="ct">Токен отмены операции.</param>
    Task<IReadOnlyList<Discount>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Возвращает скидку по идентификатору или null.</summary>
    /// <param name="id">Идентификатор скидки.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<Discount?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Проверяет, существует ли скидка с указанным кодом.</summary>
    /// <param name="code">Код для проверки.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<bool> ExistsByCodeAsync(string code, CancellationToken ct = default);

    /// <summary>Добавляет новую скидку и возвращает её.</summary>
    /// <param name="discount">Сущность скидки.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<Discount> AddAsync(Discount discount, CancellationToken ct = default);

    /// <summary>Обновляет существующую скидку.</summary>
    /// <param name="discount">Сущность скидки.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task UpdateAsync(Discount discount, CancellationToken ct = default);

    /// <summary>Удаляет скидку.</summary>
    /// <param name="discount">Сущность скидки.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task DeleteAsync(Discount discount, CancellationToken ct = default);
}
```

- [ ] Создать `Interfaces/IDiscountService.cs`:

```csharp
namespace FastIntegrationTests.Application.Interfaces;

/// <summary>Сервис для управления скидками.</summary>
public interface IDiscountService
{
    /// <summary>Возвращает список всех скидок.</summary>
    /// <param name="ct">Токен отмены операции.</param>
    Task<IReadOnlyList<DiscountDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Возвращает скидку по идентификатору.</summary>
    /// <param name="id">Идентификатор скидки.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<DiscountDto> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Создаёт новую скидку.</summary>
    /// <param name="request">Данные новой скидки.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<DiscountDto> CreateAsync(CreateDiscountRequest request, CancellationToken ct = default);

    /// <summary>Обновляет существующую скидку.</summary>
    /// <param name="id">Идентификатор скидки.</param>
    /// <param name="request">Новые данные.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<DiscountDto> UpdateAsync(Guid id, UpdateDiscountRequest request, CancellationToken ct = default);

    /// <summary>Удаляет скидку.</summary>
    /// <param name="id">Идентификатор скидки.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Активирует скидку.</summary>
    /// <param name="id">Идентификатор скидки.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<DiscountDto> ActivateAsync(Guid id, CancellationToken ct = default);

    /// <summary>Деактивирует скидку.</summary>
    /// <param name="id">Идентификатор скидки.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<DiscountDto> DeactivateAsync(Guid id, CancellationToken ct = default);
}
```

- [ ] Создать `Services/DiscountService.cs`:

```csharp
namespace FastIntegrationTests.Application.Services;

/// <summary>Сервис для управления скидками.</summary>
public class DiscountService : IDiscountService
{
    private readonly IDiscountRepository _repository;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="DiscountService"/>.
    /// </summary>
    /// <param name="repository">Репозиторий скидок.</param>
    public DiscountService(IDiscountRepository repository) => _repository = repository;

    /// <summary>Возвращает список всех скидок.</summary>
    /// <param name="ct">Токен отмены операции.</param>
    public async Task<IReadOnlyList<DiscountDto>> GetAllAsync(CancellationToken ct = default)
    {
        var items = await _repository.GetAllAsync(ct);
        return items.Select(MapToDto).ToList();
    }

    /// <summary>Возвращает скидку по идентификатору.</summary>
    /// <param name="id">Идентификатор скидки.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="NotFoundException">Если скидка не найдена.</exception>
    public async Task<DiscountDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Discount), id);
        return MapToDto(item);
    }

    /// <summary>Создаёт новую скидку.</summary>
    /// <param name="request">Данные новой скидки.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="InvalidDiscountPercentException">Если процент вне диапазона 1–100.</exception>
    /// <exception cref="DuplicateValueException">Если скидка с таким кодом уже существует.</exception>
    public async Task<DiscountDto> CreateAsync(CreateDiscountRequest request, CancellationToken ct = default)
    {
        if (request.DiscountPercent < 1 || request.DiscountPercent > 100)
            throw new InvalidDiscountPercentException(request.DiscountPercent);

        if (await _repository.ExistsByCodeAsync(request.Code, ct))
            throw new DuplicateValueException(nameof(Discount), nameof(Discount.Code), request.Code);

        var item = new Discount
        {
            Id = Guid.NewGuid(),
            Code = request.Code,
            DiscountPercent = request.DiscountPercent,
            IsActive = false,
            ExpiresAt = request.ExpiresAt,
            CreatedAt = DateTime.UtcNow,
        };
        var created = await _repository.AddAsync(item, ct);
        return MapToDto(created);
    }

    /// <summary>Обновляет существующую скидку.</summary>
    /// <param name="id">Идентификатор скидки.</param>
    /// <param name="request">Новые данные.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="NotFoundException">Если скидка не найдена.</exception>
    /// <exception cref="InvalidDiscountPercentException">Если процент вне диапазона 1–100.</exception>
    /// <exception cref="DuplicateValueException">Если новый код уже занят.</exception>
    public async Task<DiscountDto> UpdateAsync(Guid id, UpdateDiscountRequest request, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Discount), id);

        if (request.DiscountPercent < 1 || request.DiscountPercent > 100)
            throw new InvalidDiscountPercentException(request.DiscountPercent);

        if (item.Code != request.Code && await _repository.ExistsByCodeAsync(request.Code, ct))
            throw new DuplicateValueException(nameof(Discount), nameof(Discount.Code), request.Code);

        item.Code = request.Code;
        item.DiscountPercent = request.DiscountPercent;
        item.ExpiresAt = request.ExpiresAt;
        await _repository.UpdateAsync(item, ct);
        return MapToDto(item);
    }

    /// <summary>Удаляет скидку.</summary>
    /// <param name="id">Идентификатор скидки.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="NotFoundException">Если скидка не найдена.</exception>
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Discount), id);
        await _repository.DeleteAsync(item, ct);
    }

    /// <summary>Активирует скидку.</summary>
    /// <param name="id">Идентификатор скидки.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="NotFoundException">Если скидка не найдена.</exception>
    public async Task<DiscountDto> ActivateAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Discount), id);
        item.IsActive = true;
        await _repository.UpdateAsync(item, ct);
        return MapToDto(item);
    }

    /// <summary>Деактивирует скидку.</summary>
    /// <param name="id">Идентификатор скидки.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="NotFoundException">Если скидка не найдена.</exception>
    public async Task<DiscountDto> DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Discount), id);
        item.IsActive = false;
        await _repository.UpdateAsync(item, ct);
        return MapToDto(item);
    }

    private static DiscountDto MapToDto(Discount d) => new()
    {
        Id = d.Id,
        Code = d.Code,
        DiscountPercent = d.DiscountPercent,
        IsActive = d.IsActive,
        ExpiresAt = d.ExpiresAt,
        CreatedAt = d.CreatedAt,
    };
}
```

- [ ] Commit: `git add src/FastIntegrationTests.Application/ && git commit -m "feat: Discount domain — entity, DTOs, repository/service interfaces, service"`

---

## Task 8: Infrastructure — EF Configurations

Файлы в `src/FastIntegrationTests.Infrastructure/Data/Configurations/`.

- [ ] Создать `CategoryConfiguration.cs`:

```csharp
namespace FastIntegrationTests.Infrastructure.Data.Configurations;

/// <summary>Конфигурация EF Core для сущности <see cref="Category"/>.</summary>
public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Description).HasMaxLength(1000);
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.HasIndex(c => c.Name).IsUnique();
    }
}
```

- [ ] Создать `CustomerConfiguration.cs`:

```csharp
namespace FastIntegrationTests.Infrastructure.Data.Configurations;

/// <summary>Конфигурация EF Core для сущности <see cref="Customer"/>.</summary>
public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Email).IsRequired().HasMaxLength(320);
        builder.Property(c => c.Phone).HasMaxLength(50);
        builder.Property(c => c.Status).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.HasIndex(c => c.Email).IsUnique();
    }
}
```

- [ ] Создать `SupplierConfiguration.cs`:

```csharp
namespace FastIntegrationTests.Infrastructure.Data.Configurations;

/// <summary>Конфигурация EF Core для сущности <see cref="Supplier"/>.</summary>
public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
        builder.Property(s => s.ContactEmail).IsRequired().HasMaxLength(320);
        builder.Property(s => s.Country).IsRequired().HasMaxLength(100);
        builder.Property(s => s.IsActive).IsRequired();
        builder.Property(s => s.CreatedAt).IsRequired();
        builder.HasIndex(s => s.ContactEmail).IsUnique();
    }
}
```

- [ ] Создать `ReviewConfiguration.cs`:

```csharp
namespace FastIntegrationTests.Infrastructure.Data.Configurations;

/// <summary>Конфигурация EF Core для сущности <see cref="Review"/>.</summary>
public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Title).IsRequired().HasMaxLength(200);
        builder.Property(r => r.Body).IsRequired().HasMaxLength(4000);
        builder.Property(r => r.Rating).IsRequired();
        builder.Property(r => r.Status).IsRequired();
        builder.Property(r => r.CreatedAt).IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_Reviews_Rating", "\"Rating\" BETWEEN 1 AND 5"));
    }
}
```

- [ ] Создать `DiscountConfiguration.cs`:

```csharp
namespace FastIntegrationTests.Infrastructure.Data.Configurations;

/// <summary>Конфигурация EF Core для сущности <see cref="Discount"/>.</summary>
public class DiscountConfiguration : IEntityTypeConfiguration<Discount>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Discount> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Code).IsRequired().HasMaxLength(100);
        builder.Property(d => d.DiscountPercent).IsRequired();
        builder.Property(d => d.IsActive).IsRequired();
        builder.Property(d => d.CreatedAt).IsRequired();
        builder.HasIndex(d => d.Code).IsUnique();
        builder.ToTable(t => t.HasCheckConstraint("CK_Discounts_DiscountPercent", "\"DiscountPercent\" BETWEEN 1 AND 100"));
    }
}
```

- [ ] Commit: `git add src/FastIntegrationTests.Infrastructure/Data/Configurations/ && git commit -m "feat: EF Core конфигурации для 5 новых сущностей"`

---

## Task 9: Infrastructure — Repositories

Файлы в `src/FastIntegrationTests.Infrastructure/Repositories/`. Паттерн: см. `ProductRepository.cs`.

- [ ] Создать `CategoryRepository.cs`:

```csharp
namespace FastIntegrationTests.Infrastructure.Repositories;

/// <summary>Репозиторий категорий на основе EF Core.</summary>
public class CategoryRepository : ICategoryRepository
{
    private readonly ShopDbContext _context;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="CategoryRepository"/>.
    /// </summary>
    /// <param name="context">Контекст базы данных.</param>
    public CategoryRepository(ShopDbContext context) => _context = context;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default)
        => await _context.Categories.AsNoTracking().ToListAsync(ct);

    /// <inheritdoc/>
    public async Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);

    /// <inheritdoc/>
    public async Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default)
        => await _context.Categories.AnyAsync(c => c.Name == name, ct);

    /// <inheritdoc/>
    public async Task<Category> AddAsync(Category category, CancellationToken ct = default)
    {
        _context.Categories.Add(category);
        await _context.SaveChangesAsync(ct);
        return category;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Category category, CancellationToken ct = default)
    {
        _context.Categories.Update(category);
        await _context.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Category category, CancellationToken ct = default)
    {
        await _context.Categories
            .Where(c => c.Id == category.Id)
            .ExecuteDeleteAsync(ct);
    }
}
```

- [ ] Создать `CustomerRepository.cs`:

```csharp
namespace FastIntegrationTests.Infrastructure.Repositories;

/// <summary>Репозиторий покупателей на основе EF Core.</summary>
public class CustomerRepository : ICustomerRepository
{
    private readonly ShopDbContext _context;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="CustomerRepository"/>.
    /// </summary>
    /// <param name="context">Контекст базы данных.</param>
    public CustomerRepository(ShopDbContext context) => _context = context;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken ct = default)
        => await _context.Customers.AsNoTracking().ToListAsync(ct);

    /// <inheritdoc/>
    public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);

    /// <inheritdoc/>
    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
        => await _context.Customers.AnyAsync(c => c.Email == email, ct);

    /// <inheritdoc/>
    public async Task<Customer> AddAsync(Customer customer, CancellationToken ct = default)
    {
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync(ct);
        return customer;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Customer customer, CancellationToken ct = default)
    {
        _context.Customers.Update(customer);
        await _context.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Customer customer, CancellationToken ct = default)
    {
        await _context.Customers
            .Where(c => c.Id == customer.Id)
            .ExecuteDeleteAsync(ct);
    }
}
```

- [ ] Создать `SupplierRepository.cs`:

```csharp
namespace FastIntegrationTests.Infrastructure.Repositories;

/// <summary>Репозиторий поставщиков на основе EF Core.</summary>
public class SupplierRepository : ISupplierRepository
{
    private readonly ShopDbContext _context;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="SupplierRepository"/>.
    /// </summary>
    /// <param name="context">Контекст базы данных.</param>
    public SupplierRepository(ShopDbContext context) => _context = context;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Supplier>> GetAllAsync(CancellationToken ct = default)
        => await _context.Suppliers.AsNoTracking().ToListAsync(ct);

    /// <inheritdoc/>
    public async Task<Supplier?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == id, ct);

    /// <inheritdoc/>
    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
        => await _context.Suppliers.AnyAsync(s => s.ContactEmail == email, ct);

    /// <inheritdoc/>
    public async Task<Supplier> AddAsync(Supplier supplier, CancellationToken ct = default)
    {
        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync(ct);
        return supplier;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Supplier supplier, CancellationToken ct = default)
    {
        _context.Suppliers.Update(supplier);
        await _context.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Supplier supplier, CancellationToken ct = default)
    {
        await _context.Suppliers
            .Where(s => s.Id == supplier.Id)
            .ExecuteDeleteAsync(ct);
    }
}
```

- [ ] Создать `ReviewRepository.cs`:

```csharp
namespace FastIntegrationTests.Infrastructure.Repositories;

/// <summary>Репозиторий отзывов на основе EF Core.</summary>
public class ReviewRepository : IReviewRepository
{
    private readonly ShopDbContext _context;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="ReviewRepository"/>.
    /// </summary>
    /// <param name="context">Контекст базы данных.</param>
    public ReviewRepository(ShopDbContext context) => _context = context;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Review>> GetAllAsync(CancellationToken ct = default)
        => await _context.Reviews.AsNoTracking().ToListAsync(ct);

    /// <inheritdoc/>
    public async Task<Review?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Reviews.FirstOrDefaultAsync(r => r.Id == id, ct);

    /// <inheritdoc/>
    public async Task<Review> AddAsync(Review review, CancellationToken ct = default)
    {
        _context.Reviews.Add(review);
        await _context.SaveChangesAsync(ct);
        return review;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Review review, CancellationToken ct = default)
    {
        _context.Reviews.Update(review);
        await _context.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Review review, CancellationToken ct = default)
    {
        await _context.Reviews
            .Where(r => r.Id == review.Id)
            .ExecuteDeleteAsync(ct);
    }
}
```

- [ ] Создать `DiscountRepository.cs`:

```csharp
namespace FastIntegrationTests.Infrastructure.Repositories;

/// <summary>Репозиторий скидок на основе EF Core.</summary>
public class DiscountRepository : IDiscountRepository
{
    private readonly ShopDbContext _context;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="DiscountRepository"/>.
    /// </summary>
    /// <param name="context">Контекст базы данных.</param>
    public DiscountRepository(ShopDbContext context) => _context = context;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Discount>> GetAllAsync(CancellationToken ct = default)
        => await _context.Discounts.AsNoTracking().ToListAsync(ct);

    /// <inheritdoc/>
    public async Task<Discount?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Discounts.FirstOrDefaultAsync(d => d.Id == id, ct);

    /// <inheritdoc/>
    public async Task<bool> ExistsByCodeAsync(string code, CancellationToken ct = default)
        => await _context.Discounts.AnyAsync(d => d.Code == code, ct);

    /// <inheritdoc/>
    public async Task<Discount> AddAsync(Discount discount, CancellationToken ct = default)
    {
        _context.Discounts.Add(discount);
        await _context.SaveChangesAsync(ct);
        return discount;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Discount discount, CancellationToken ct = default)
    {
        _context.Discounts.Update(discount);
        await _context.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Discount discount, CancellationToken ct = default)
    {
        await _context.Discounts
            .Where(d => d.Id == discount.Id)
            .ExecuteDeleteAsync(ct);
    }
}
```

- [ ] Commit: `git add src/FastIntegrationTests.Infrastructure/Repositories/ && git commit -m "feat: 5 новых репозиториев (Category, Customer, Supplier, Review, Discount)"`

---

## Task 10: Infrastructure — DbContext + DI

- [ ] Обновить `src/FastIntegrationTests.Infrastructure/Data/ShopDbContext.cs` — добавить 5 DbSet после `OrderItems`:

```csharp
    /// <summary>Категории товаров.</summary>
    public DbSet<Category> Categories => Set<Category>();

    /// <summary>Покупатели.</summary>
    public DbSet<Customer> Customers => Set<Customer>();

    /// <summary>Поставщики.</summary>
    public DbSet<Supplier> Suppliers => Set<Supplier>();

    /// <summary>Отзывы.</summary>
    public DbSet<Review> Reviews => Set<Review>();

    /// <summary>Скидки.</summary>
    public DbSet<Discount> Discounts => Set<Discount>();
```

- [ ] Обновить `src/FastIntegrationTests.Infrastructure/Extensions/ServiceCollectionExtensions.cs` — в метод `AddRepositories` добавить 5 строк:

```csharp
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IDiscountRepository, DiscountRepository>();
```

- [ ] Commit: `git add src/FastIntegrationTests.Infrastructure/ && git commit -m "feat: обновить DbContext и DI для 5 новых сущностей"`

---

## Task 11: Migration

- [ ] Запустить из корня репозитория:

```bash
dotnet ef migrations add AddCategoryCustomerSupplierReviewDiscount \
  --project src/FastIntegrationTests.Infrastructure \
  --startup-project src/FastIntegrationTests.WebApi \
  --output-dir Migrations
```

Ожидаемый результат: создан файл миграции `Migrations/YYYYMMDDHHMMSS_AddCategoryCustomerSupplierReviewDiscount.cs`

- [ ] Проверить содержимое миграции: `Up()` должен создать 5 таблиц (`Categories`, `Customers`, `Suppliers`, `Reviews`, `Discounts`) с соответствующими индексами и check-constraint'ами.

- [ ] Commit: `git add src/FastIntegrationTests.Infrastructure/Migrations/ && git commit -m "feat: миграция AddCategoryCustomerSupplierReviewDiscount"`

---

## Task 12: WebApi — CategoriesController

- [ ] Создать `src/FastIntegrationTests.WebApi/Controllers/CategoriesController.cs`:

```csharp
namespace FastIntegrationTests.WebApi.Controllers;

/// <summary>
/// Контроллер для управления категориями товаров.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="CategoriesController"/>.
    /// </summary>
    /// <param name="categoryService">Сервис категорий.</param>
    public CategoriesController(ICategoryService categoryService)
        => _categoryService = categoryService;

    /// <summary>GET /api/categories — возвращает все категории.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetAll(CancellationToken ct)
        => Ok(await _categoryService.GetAllAsync(ct));

    /// <summary>GET /api/categories/{id} — возвращает категорию по идентификатору.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CategoryDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await _categoryService.GetByIdAsync(id, ct));

    /// <summary>POST /api/categories — создаёт новую категорию.</summary>
    [HttpPost]
    public async Task<ActionResult<CategoryDto>> Create(CreateCategoryRequest request, CancellationToken ct)
    {
        var created = await _categoryService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>PUT /api/categories/{id} — обновляет категорию.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CategoryDto>> Update(Guid id, UpdateCategoryRequest request, CancellationToken ct)
        => Ok(await _categoryService.UpdateAsync(id, request, ct));

    /// <summary>DELETE /api/categories/{id} — удаляет категорию.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _categoryService.DeleteAsync(id, ct);
        return NoContent();
    }
}
```

- [ ] Commit: `git add src/FastIntegrationTests.WebApi/Controllers/CategoriesController.cs && git commit -m "feat: CategoriesController"`

---

## Task 13: WebApi — CustomersController

- [ ] Создать `src/FastIntegrationTests.WebApi/Controllers/CustomersController.cs`:

```csharp
namespace FastIntegrationTests.WebApi.Controllers;

/// <summary>
/// Контроллер для управления покупателями.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="CustomersController"/>.
    /// </summary>
    /// <param name="customerService">Сервис покупателей.</param>
    public CustomersController(ICustomerService customerService)
        => _customerService = customerService;

    /// <summary>GET /api/customers — возвращает всех покупателей.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CustomerDto>>> GetAll(CancellationToken ct)
        => Ok(await _customerService.GetAllAsync(ct));

    /// <summary>GET /api/customers/{id} — возвращает покупателя по идентификатору.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await _customerService.GetByIdAsync(id, ct));

    /// <summary>POST /api/customers — создаёт нового покупателя.</summary>
    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create(CreateCustomerRequest request, CancellationToken ct)
    {
        var created = await _customerService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>PUT /api/customers/{id} — обновляет данные покупателя.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CustomerDto>> Update(Guid id, UpdateCustomerRequest request, CancellationToken ct)
        => Ok(await _customerService.UpdateAsync(id, request, ct));

    /// <summary>DELETE /api/customers/{id} — удаляет покупателя.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _customerService.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>POST /api/customers/{id}/ban — блокирует покупателя.</summary>
    [HttpPost("{id:guid}/ban")]
    public async Task<IActionResult> Ban(Guid id, CancellationToken ct)
    {
        await _customerService.BanAsync(id, ct);
        return NoContent();
    }

    /// <summary>POST /api/customers/{id}/activate — активирует покупателя.</summary>
    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        await _customerService.ActivateAsync(id, ct);
        return NoContent();
    }

    /// <summary>POST /api/customers/{id}/deactivate — деактивирует покупателя.</summary>
    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await _customerService.DeactivateAsync(id, ct);
        return NoContent();
    }
}
```

- [ ] Commit: `git add src/FastIntegrationTests.WebApi/Controllers/CustomersController.cs && git commit -m "feat: CustomersController"`

---

## Task 14: WebApi — SuppliersController

- [ ] Создать `src/FastIntegrationTests.WebApi/Controllers/SuppliersController.cs`:

```csharp
namespace FastIntegrationTests.WebApi.Controllers;

/// <summary>
/// Контроллер для управления поставщиками.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SuppliersController : ControllerBase
{
    private readonly ISupplierService _supplierService;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="SuppliersController"/>.
    /// </summary>
    /// <param name="supplierService">Сервис поставщиков.</param>
    public SuppliersController(ISupplierService supplierService)
        => _supplierService = supplierService;

    /// <summary>GET /api/suppliers — возвращает всех поставщиков.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SupplierDto>>> GetAll(CancellationToken ct)
        => Ok(await _supplierService.GetAllAsync(ct));

    /// <summary>GET /api/suppliers/{id} — возвращает поставщика по идентификатору.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SupplierDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await _supplierService.GetByIdAsync(id, ct));

    /// <summary>POST /api/suppliers — создаёт нового поставщика.</summary>
    [HttpPost]
    public async Task<ActionResult<SupplierDto>> Create(CreateSupplierRequest request, CancellationToken ct)
    {
        var created = await _supplierService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>PUT /api/suppliers/{id} — обновляет данные поставщика.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SupplierDto>> Update(Guid id, UpdateSupplierRequest request, CancellationToken ct)
        => Ok(await _supplierService.UpdateAsync(id, request, ct));

    /// <summary>DELETE /api/suppliers/{id} — удаляет поставщика.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _supplierService.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>POST /api/suppliers/{id}/activate — активирует поставщика.</summary>
    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        await _supplierService.ActivateAsync(id, ct);
        return NoContent();
    }

    /// <summary>POST /api/suppliers/{id}/deactivate — деактивирует поставщика.</summary>
    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await _supplierService.DeactivateAsync(id, ct);
        return NoContent();
    }
}
```

- [ ] Commit: `git add src/FastIntegrationTests.WebApi/Controllers/SuppliersController.cs && git commit -m "feat: SuppliersController"`

---

## Task 15: WebApi — ReviewsController

- [ ] Создать `src/FastIntegrationTests.WebApi/Controllers/ReviewsController.cs`:

```csharp
namespace FastIntegrationTests.WebApi.Controllers;

/// <summary>
/// Контроллер для управления отзывами.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="ReviewsController"/>.
    /// </summary>
    /// <param name="reviewService">Сервис отзывов.</param>
    public ReviewsController(IReviewService reviewService)
        => _reviewService = reviewService;

    /// <summary>GET /api/reviews — возвращает все отзывы.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ReviewDto>>> GetAll(CancellationToken ct)
        => Ok(await _reviewService.GetAllAsync(ct));

    /// <summary>GET /api/reviews/{id} — возвращает отзыв по идентификатору.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReviewDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await _reviewService.GetByIdAsync(id, ct));

    /// <summary>POST /api/reviews — создаёт новый отзыв.</summary>
    [HttpPost]
    public async Task<ActionResult<ReviewDto>> Create(CreateReviewRequest request, CancellationToken ct)
    {
        var created = await _reviewService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>DELETE /api/reviews/{id} — удаляет отзыв.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _reviewService.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>POST /api/reviews/{id}/approve — одобряет отзыв.</summary>
    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        await _reviewService.ApproveAsync(id, ct);
        return NoContent();
    }

    /// <summary>POST /api/reviews/{id}/reject — отклоняет отзыв.</summary>
    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, CancellationToken ct)
    {
        await _reviewService.RejectAsync(id, ct);
        return NoContent();
    }
}
```

- [ ] Commit: `git add src/FastIntegrationTests.WebApi/Controllers/ReviewsController.cs && git commit -m "feat: ReviewsController"`

---

## Task 16: WebApi — DiscountsController

- [ ] Создать `src/FastIntegrationTests.WebApi/Controllers/DiscountsController.cs`:

```csharp
namespace FastIntegrationTests.WebApi.Controllers;

/// <summary>
/// Контроллер для управления скидками.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DiscountsController : ControllerBase
{
    private readonly IDiscountService _discountService;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="DiscountsController"/>.
    /// </summary>
    /// <param name="discountService">Сервис скидок.</param>
    public DiscountsController(IDiscountService discountService)
        => _discountService = discountService;

    /// <summary>GET /api/discounts — возвращает все скидки.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DiscountDto>>> GetAll(CancellationToken ct)
        => Ok(await _discountService.GetAllAsync(ct));

    /// <summary>GET /api/discounts/{id} — возвращает скидку по идентификатору.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DiscountDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await _discountService.GetByIdAsync(id, ct));

    /// <summary>POST /api/discounts — создаёт новую скидку.</summary>
    [HttpPost]
    public async Task<ActionResult<DiscountDto>> Create(CreateDiscountRequest request, CancellationToken ct)
    {
        var created = await _discountService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>PUT /api/discounts/{id} — обновляет скидку.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DiscountDto>> Update(Guid id, UpdateDiscountRequest request, CancellationToken ct)
        => Ok(await _discountService.UpdateAsync(id, request, ct));

    /// <summary>DELETE /api/discounts/{id} — удаляет скидку.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _discountService.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>POST /api/discounts/{id}/activate — активирует скидку.</summary>
    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        await _discountService.ActivateAsync(id, ct);
        return NoContent();
    }

    /// <summary>POST /api/discounts/{id}/deactivate — деактивирует скидку.</summary>
    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await _discountService.DeactivateAsync(id, ct);
        return NoContent();
    }
}
```

- [ ] Commit: `git add src/FastIntegrationTests.WebApi/Controllers/DiscountsController.cs && git commit -m "feat: DiscountsController"`

---

## Task 17: WebApi — GlobalExceptionHandler + Program.cs

**Modify:** `GlobalExceptionHandler.cs` — добавить 4 новых типа исключений в switch expression.

- [ ] В `src/FastIntegrationTests.WebApi/Middleware/GlobalExceptionHandler.cs` заменить switch expression:

```csharp
        (int statusCode, string detail) = exception switch
        {
            NotFoundException ex => (StatusCodes.Status404NotFound, ex.Message),
            InvalidOrderStatusTransitionException ex => (StatusCodes.Status400BadRequest, ex.Message),
            DuplicateValueException ex => (StatusCodes.Status409Conflict, ex.Message),
            InvalidRatingException ex => (StatusCodes.Status422UnprocessableEntity, ex.Message),
            InvalidDiscountPercentException ex => (StatusCodes.Status422UnprocessableEntity, ex.Message),
            InvalidStatusTransitionException ex => (StatusCodes.Status422UnprocessableEntity, ex.Message),
            _ => (0, string.Empty),
        };
```

**Modify:** `Program.cs` — добавить 5 новых сервисов после `AddScoped<IOrderService, OrderService>()`:

- [ ] В `src/FastIntegrationTests.WebApi/Program.cs` добавить строки:

```csharp
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IDiscountService, DiscountService>();
```

- [ ] Commit: `git add src/FastIntegrationTests.WebApi/ && git commit -m "feat: GlobalExceptionHandler + Program.cs — 5 новых сервисов и 4 новых типа исключений"`

---

## Task 18: Build Verification

- [ ] Запустить сборку:

```bash
dotnet build
```

Ожидаемый результат: `Build succeeded` без ошибок и предупреждений.

- [ ] Если есть ошибки компиляции — устранить. Типичные причины:
  - Пропущен `global using` для нового namespace (проверить `GlobalUsings.cs` в каждом проекте)
  - Опечатки в именах типов

---

## Self-Review

### Spec coverage check

| Требование спеки | Покрыто |
|---|---|
| Category (Name unique, CRUD) | Task 3, 8, 9, 12 |
| Customer (Email unique, Active/Inactive/Banned transitions) | Task 4, 8, 9, 13 |
| Supplier (ContactEmail unique, activate/deactivate) | Task 5, 8, 9, 14 |
| Review (Rating 1-5 validation, Pending→Approved/Rejected, no PUT) | Task 6, 8, 9, 15 |
| Discount (Code unique, percent 1-100, activate/deactivate) | Task 7, 8, 9, 16 |
| DuplicateValueException → 409 | Task 2, 17 |
| InvalidRatingException → 422 | Task 2, 17 |
| InvalidDiscountPercentException → 422 | Task 2, 17 |
| InvalidStatusTransitionException → 422 | Task 2, 17 |
| Review: нет UpdateReviewRequest, нет PUT | Task 6, 15 |
| Supplier: IsActive=true по умолчанию | Task 5 |
| Customer: Status=Active по умолчанию | Task 4 |
| Discount: IsActive=false по умолчанию | Task 7 |
| Одна миграция для 5 таблиц | Task 11 |
| Уникальные индексы в EF конфигурациях | Task 8 |
| Check constraints для Rating и DiscountPercent | Task 8 |

### Не входит в этот план
- Тесты (Plans 2 и 3)
- Обновление CLAUDE.md (Plan 3 или отдельный PR)
