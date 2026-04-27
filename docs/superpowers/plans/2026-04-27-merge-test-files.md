# Объединение тест-файлов Cr+Ud по уровню Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Объединить 4 тест-файла на сущность (ServiceCr, ServiceUd, ApiCr, ApiUd) в 2 (ServiceTests, ApiTests) во всех трёх тест-проектах.

**Architecture:** Tasks 1–3 независимы (разные проекты) и выполняются параллельно. Внутри каждого таска все 7 сущностей тоже независимы и обрабатываются параллельно. Task 4 выполняется после завершения 1–3.

**Tech Stack:** C# / xUnit / .NET 8

---

## Алгоритм слияния (применяется ко всем 21 группам)

Для каждой пары файлов `*CrTests.cs` + `*UdTests.cs`:

1. **Namespace и class-declaration** — из Cr-файла, summary объединить: вместо `GetAll, GetById, Create` / `Update, Delete` написать `GetAll, GetById, Create, Update, Delete`.
2. **Приватные поля** — из **Ud**-файла (он — надмножество Cr; у Ud могут быть доп. зависимости вроде `IOrderService _orders`).
3. **Конструктор** (только Respawn и Testcontainers) — из Cr-файла (в Cr и Ud конструктор идентичен).
4. **`InitializeAsync`** — из **Ud**-файла (надмножество: инициализирует все сервисы).
5. **Тест-методы** — сначала все из Cr, затем уникальные из Ud. **Не дублировать** методы `CreateMultiple_GetAll_GetByIdEach_ReturnsConsistentData` и `CreateUpdateDelete_VerifyEachStep_AllPersist` — они присутствуют в обоих файлах, оставить только копию из Cr.
6. **Старый Ud-файл удалить**, Cr-файл переписать объединённым содержимым (и переименовать класс: убрать суффикс `Cr`).

---

## Таблица файлов — IntegreSQL

| Сущность | Cr-файл (источник+цель) | Ud-файл (источник, удалить) | Новый класс Service | Новый класс API |
|---|---|---|---|---|
| Categories | `Categories/CategoryServiceCrTests.cs` → `CategoryServiceTests.cs` | `CategoryServiceUdTests.cs` | `CategoryServiceTests` | `CategoriesApiTests` |
| Customers | `Customers/CustomerServiceCrTests.cs` → `CustomerServiceTests.cs` | `CustomerServiceUdTests.cs` | `CustomerServiceTests` | `CustomersApiTests` |
| Discounts | `Discounts/DiscountServiceCrTests.cs` → `DiscountServiceTests.cs` | `DiscountServiceUdTests.cs` | `DiscountServiceTests` | `DiscountsApiTests` |
| Orders | `Orders/OrderServiceCrTests.cs` → `OrderServiceTests.cs` | `OrderServiceUdTests.cs` | `OrderServiceTests` | `OrdersApiTests` |
| Products | `Products/ProductServiceCrTests.cs` → `ProductServiceTests.cs` | `ProductServiceUdTests.cs` | `ProductServiceTests` | `ProductsApiTests` |
| Reviews | `Reviews/ReviewServiceCrTests.cs` → `ReviewServiceTests.cs` | `ReviewServiceUdTests.cs` | `ReviewServiceTests` | `ReviewsApiTests` |
| Suppliers | `Suppliers/SupplierServiceCrTests.cs` → `SupplierServiceTests.cs` | `SupplierServiceUdTests.cs` | `SupplierServiceTests` | `SuppliersApiTests` |

API-файлы: `CategoriesApiCrTests.cs` → `CategoriesApiTests.cs`, `CategoriesApiUdTests.cs` удалить, и так для каждой сущности.

## Таблица файлов — Respawn

| Сущность | Service Cr (источник+цель) | Service Ud (удалить) | API Cr (источник+цель) | API Ud (удалить) |
|---|---|---|---|---|
| Categories | `CategoryServiceCrRespawnTests.cs` → `CategoryServiceRespawnTests.cs` | `CategoryServiceUdRespawnTests.cs` | `CategoriesApiCrRespawnTests.cs` → `CategoriesApiRespawnTests.cs` | `CategoriesApiUdRespawnTests.cs` |
| Customers | `CustomerServiceCrRespawnTests.cs` → `CustomerServiceRespawnTests.cs` | `CustomerServiceUdRespawnTests.cs` | `CustomersApiCrRespawnTests.cs` → `CustomersApiRespawnTests.cs` | `CustomersApiUdRespawnTests.cs` |
| Discounts | `DiscountServiceCrRespawnTests.cs` → `DiscountServiceRespawnTests.cs` | `DiscountServiceUdRespawnTests.cs` | `DiscountsApiCrRespawnTests.cs` → `DiscountsApiRespawnTests.cs` | `DiscountsApiUdRespawnTests.cs` |
| Orders | `OrderServiceCrRespawnTests.cs` → `OrderServiceRespawnTests.cs` | `OrderServiceUdRespawnTests.cs` | `OrdersApiCrRespawnTests.cs` → `OrdersApiRespawnTests.cs` | `OrdersApiUdRespawnTests.cs` |
| Products | `ProductServiceCrRespawnTests.cs` → `ProductServiceRespawnTests.cs` | `ProductServiceUdRespawnTests.cs` | `ProductsApiCrRespawnTests.cs` → `ProductsApiRespawnTests.cs` | `ProductsApiUdRespawnTests.cs` |
| Reviews | `ReviewServiceCrRespawnTests.cs` → `ReviewServiceRespawnTests.cs` | `ReviewServiceUdRespawnTests.cs` | `ReviewsApiCrRespawnTests.cs` → `ReviewsApiRespawnTests.cs` | `ReviewsApiUdRespawnTests.cs` |
| Suppliers | `SupplierServiceCrRespawnTests.cs` → `SupplierServiceRespawnTests.cs` | `SupplierServiceUdRespawnTests.cs` | `SuppliersApiCrRespawnTests.cs` → `SuppliersApiRespawnTests.cs` | `SuppliersApiUdRespawnTests.cs` |

Все файлы находятся в `tests/FastIntegrationTests.Tests.Respawn/{Entity}/`.

## Таблица файлов — Testcontainers

| Сущность | Service Cr (источник+цель) | Service Ud (удалить) | API Cr (источник+цель) | API Ud (удалить) |
|---|---|---|---|---|
| Categories | `CategoryServiceCrContainerTests.cs` → `CategoryServiceContainerTests.cs` | `CategoryServiceUdContainerTests.cs` | `CategoriesApiCrContainerTests.cs` → `CategoriesApiContainerTests.cs` | `CategoriesApiUdContainerTests.cs` |
| Customers | `CustomerServiceCrContainerTests.cs` → `CustomerServiceContainerTests.cs` | `CustomerServiceUdContainerTests.cs` | `CustomersApiCrContainerTests.cs` → `CustomersApiContainerTests.cs` | `CustomersApiUdContainerTests.cs` |
| Discounts | `DiscountServiceCrContainerTests.cs` → `DiscountServiceContainerTests.cs` | `DiscountServiceUdContainerTests.cs` | `DiscountsApiCrContainerTests.cs` → `DiscountsApiContainerTests.cs` | `DiscountsApiUdContainerTests.cs` |
| Orders | `OrderServiceCrContainerTests.cs` → `OrderServiceContainerTests.cs` | `OrderServiceUdContainerTests.cs` | `OrdersApiCrContainerTests.cs` → `OrdersApiContainerTests.cs` | `OrdersApiUdContainerTests.cs` |
| Products | `ProductServiceCrContainerTests.cs` → `ProductServiceContainerTests.cs` | `ProductServiceUdContainerTests.cs` | `ProductsApiCrContainerTests.cs` → `ProductsApiContainerTests.cs` | `ProductsApiUdContainerTests.cs` |
| Reviews | `ReviewServiceCrContainerTests.cs` → `ReviewServiceContainerTests.cs` | `ReviewServiceUdContainerTests.cs` | `ReviewsApiCrContainerTests.cs` → `ReviewsApiContainerTests.cs` | `ReviewsApiUdContainerTests.cs` |
| Suppliers | `SupplierServiceCrContainerTests.cs` → `SupplierServiceContainerTests.cs` | `SupplierServiceUdContainerTests.cs` | `SuppliersApiCrContainerTests.cs` → `SuppliersApiContainerTests.cs` | `SuppliersApiUdContainerTests.cs` |

Все файлы находятся в `tests/FastIntegrationTests.Tests.Testcontainers/{Entity}/`.

---

## Эталонный пример: ProductServiceTests.cs (IntegreSQL)

Результат слияния `ProductServiceCrTests.cs` + `ProductServiceUdTests.cs`:

```csharp
namespace FastIntegrationTests.Tests.IntegreSQL.Products;

/// <summary>
/// Тесты сервисного уровня: GetAll, GetById, Create, Update, Delete для ProductService.
/// Каждый тест получает изолированный клон БД через IntegreSQL (~5 мс).
/// </summary>
public class ProductServiceTests : AppServiceTestBase
{
    private IProductService Sut = null!;
    private IOrderService _orders = null!;

    /// <inheritdoc/>
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        var productRepo = new ProductRepository(Context);
        var orderRepo = new OrderRepository(Context);
        Sut = new ProductService(productRepo);
        _orders = new OrderService(orderRepo, productRepo);
    }

    [Fact]
    public async Task GetAllAsync_WhenNoProducts_ReturnsEmptyList()
    {
        var result = await Sut.GetAllAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_WhenProductsExist_ReturnsAllProducts()
    {
        await Sut.CreateAsync(new CreateProductRequest { Name = "Товар 1", Description = "Описание 1", Price = 100m });
        await Sut.CreateAsync(new CreateProductRequest { Name = "Товар 2", Description = "Описание 2", Price = 200m });

        var result = await Sut.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetByIdAsync_WhenProductExists_ReturnsProduct()
    {
        var created = await Sut.CreateAsync(new CreateProductRequest { Name = "Ноутбук", Description = "Core i9", Price = 50_000m });

        var result = await Sut.GetByIdAsync(created.Id);

        Assert.Equal(created.Id, result.Id);
        Assert.Equal("Ноутбук", result.Name);
        Assert.Equal("Core i9", result.Description);
        Assert.Equal(50_000m, result.Price);
    }

    [Fact]
    public async Task GetByIdAsync_WhenProductNotFound_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Sut.GetByIdAsync(999));
    }

    [Fact]
    public async Task CreateAsync_PersistsProductAndReturnsWithAssignedId()
    {
        var request = new CreateProductRequest { Name = "Мышь", Description = "Беспроводная", Price = 2_500m };

        var result = await Sut.CreateAsync(request);

        Assert.True(result.Id > 0);
        Assert.Equal("Мышь", result.Name);
        Assert.Equal("Беспроводная", result.Description);
        Assert.Equal(2_500m, result.Price);
    }

    [Fact]
    public async Task CreateAsync_SetsCreatedAtAutomatically()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var result = await Sut.CreateAsync(new CreateProductRequest { Name = "Клавиатура", Price = 3_000m });

        var after = DateTime.UtcNow.AddSeconds(1);
        Assert.InRange(result.CreatedAt, before, after);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesProductFieldsInDatabase()
    {
        var created = await Sut.CreateAsync(new CreateProductRequest { Name = "Старое название", Price = 1_000m });
        var updateRequest = new UpdateProductRequest { Name = "Новое название", Description = "Новое описание", Price = 1_500m };

        var updated = await Sut.UpdateAsync(created.Id, updateRequest);

        Assert.Equal("Новое название", updated.Name);
        Assert.Equal("Новое описание", updated.Description);
        Assert.Equal(1_500m, updated.Price);

        var fetched = await Sut.GetByIdAsync(created.Id);
        Assert.Equal("Новое название", fetched.Name);
        Assert.Equal(1_500m, fetched.Price);
    }

    [Fact]
    public async Task UpdateAsync_WhenProductNotFound_ThrowsNotFoundException()
    {
        var request = new UpdateProductRequest { Name = "Название", Description = string.Empty, Price = 100m };

        await Assert.ThrowsAsync<NotFoundException>(() => Sut.UpdateAsync(999, request));
    }

    [Fact]
    public async Task DeleteAsync_RemovesProductFromDatabase()
    {
        var created = await Sut.CreateAsync(new CreateProductRequest { Name = "Временный товар", Price = 500m });

        await Sut.DeleteAsync(created.Id);

        await Assert.ThrowsAsync<NotFoundException>(() => Sut.GetByIdAsync(created.Id));
    }

    [Fact]
    public async Task DeleteAsync_WhenProductNotFound_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Sut.DeleteAsync(999));
    }

    [Fact]
    public async Task DeleteAsync_WhenProductHasOrderItems_ThrowsDbUpdateException()
    {
        var product = await Sut.CreateAsync(new CreateProductRequest { Name = "Товар в заказе", Price = 1_000m });
        await _orders.CreateAsync(new CreateOrderRequest
        {
            Items = new List<OrderItemRequest> { new() { ProductId = product.Id, Quantity = 1 } }
        });

        // FK Restrict: нельзя удалить товар, на который ссылаются позиции заказа
        await Assert.ThrowsAsync<DbUpdateException>(() => Sut.DeleteAsync(product.Id));
    }

    /// <summary>
    /// Создаёт несколько товаров, читает через GetAll и GetById — проверяет согласованность данных.
    /// </summary>
    [Fact]
    public async Task CreateMultiple_GetAll_GetByIdEach_ReturnsConsistentData()
    {
        var a = await Sut.CreateAsync(new CreateProductRequest { Name = "Товар А", Price = 100m });
        var b = await Sut.CreateAsync(new CreateProductRequest { Name = "Товар Б", Price = 200m });
        var c = await Sut.CreateAsync(new CreateProductRequest { Name = "Товар В", Price = 300m });

        var all = await Sut.GetAllAsync();
        Assert.Equal(3, all.Count);
        Assert.Equal("Товар А", (await Sut.GetByIdAsync(a.Id)).Name);
        Assert.Equal("Товар Б", (await Sut.GetByIdAsync(b.Id)).Name);
        Assert.Equal("Товар В", (await Sut.GetByIdAsync(c.Id)).Name);

        // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
        for (var i = 0; i < 4; i++)
        {
            var extra = await Sut.CreateAsync(new CreateProductRequest { Name = $"Доп {i}", Price = 500m + i * 50m });
            await Sut.GetByIdAsync(extra.Id);
        }
        await Sut.GetAllAsync();
    }

    /// <summary>
    /// Создаёт товар, обновляет поля, проверяет персистентность, удаляет — полный цикл записи.
    /// </summary>
    [Fact]
    public async Task CreateUpdateDelete_VerifyEachStep_AllPersist()
    {
        var created = await Sut.CreateAsync(new CreateProductRequest { Name = "Монитор", Price = 20_000m });
        var updated = await Sut.UpdateAsync(created.Id, new UpdateProductRequest { Name = "Монитор 4K", Description = "UHD", Price = 25_000m });
        Assert.Equal("Монитор 4K", updated.Name);
        Assert.Equal(25_000m, updated.Price);

        var fetched = await Sut.GetByIdAsync(created.Id);
        Assert.Equal("Монитор 4K", fetched.Name);

        await Sut.DeleteAsync(created.Id);
        await Assert.ThrowsAsync<NotFoundException>(() => Sut.GetByIdAsync(created.Id));

        // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
        for (var i = 0; i < 4; i++)
        {
            var extra = await Sut.CreateAsync(new CreateProductRequest { Name = $"Доп {i}", Price = 1_000m + i * 100m });
            await Sut.UpdateAsync(extra.Id, new UpdateProductRequest { Name = $"Доп {i} v2", Price = 1_100m + i * 100m });
            await Sut.GetByIdAsync(extra.Id);
        }
        await Sut.GetAllAsync();
    }
}
```

---

### Task 1: IntegreSQL — слияние всех 7 сущностей

**Проект:** `tests/FastIntegrationTests.Tests.IntegreSQL/`

Для каждой из 7 сущностей выполнить параллельно одинаковые шаги. Показаны пути для Products — для остальных сущностей использовать таблицу выше.

**Files:**
- Modify (Source Cr → merged result): `tests/FastIntegrationTests.Tests.IntegreSQL/Products/ProductServiceCrTests.cs` → переписать как `ProductServiceTests.cs`
- Delete: `tests/FastIntegrationTests.Tests.IntegreSQL/Products/ProductServiceUdTests.cs`
- Modify (Source Cr → merged result): `tests/FastIntegrationTests.Tests.IntegreSQL/Products/ProductsApiCrTests.cs` → переписать как `ProductsApiTests.cs`
- Delete: `tests/FastIntegrationTests.Tests.IntegreSQL/Products/ProductsApiUdTests.cs`
- То же для: Categories, Customers, Discounts, Orders, Reviews, Suppliers

- [ ] **Шаг 1.1: Прочитать оба Service-файла для каждой сущности**

Для каждой сущности прочитать Cr и Ud файлы. Алгоритм слияния описан в разделе выше. Для Products — использовать эталонный пример из этого плана.

Особенности IntegreSQL:
- Нет конструктора с fixture (базовый класс `AppServiceTestBase : IAsyncLifetime`)
- Namespace: `FastIntegrationTests.Tests.IntegreSQL.{Entity}`
- Поля и `InitializeAsync` берутся из Ud-файла

- [ ] **Шаг 1.2: Перезаписать Cr-файлы объединённым содержимым**

Перезаписать каждый Cr-файл результатом слияния:
- `ProductServiceCrTests.cs` → содержимое `ProductServiceTests` (класс переименован, Cr убран из имени)
- `ProductsApiCrTests.cs` → содержимое `ProductsApiTests`
- Аналогично для Categories, Customers, Discounts, Orders, Reviews, Suppliers

- [ ] **Шаг 1.3: Переименовать перезаписанные файлы**

```bash
cd tests/FastIntegrationTests.Tests.IntegreSQL
mv Products/ProductServiceCrTests.cs Products/ProductServiceTests.cs
mv Products/ProductsApiCrTests.cs Products/ProductsApiTests.cs
mv Categories/CategoryServiceCrTests.cs Categories/CategoryServiceTests.cs
mv Categories/CategoriesApiCrTests.cs Categories/CategoriesApiTests.cs
mv Customers/CustomerServiceCrTests.cs Customers/CustomerServiceTests.cs
mv Customers/CustomersApiCrTests.cs Customers/CustomersApiTests.cs
mv Discounts/DiscountServiceCrTests.cs Discounts/DiscountServiceTests.cs
mv Discounts/DiscountsApiCrTests.cs Discounts/DiscountsApiTests.cs
mv Orders/OrderServiceCrTests.cs Orders/OrderServiceTests.cs
mv Orders/OrdersApiCrTests.cs Orders/OrdersApiTests.cs
mv Reviews/ReviewServiceCrTests.cs Reviews/ReviewServiceTests.cs
mv Reviews/ReviewsApiCrTests.cs Reviews/ReviewsApiTests.cs
mv Suppliers/SupplierServiceCrTests.cs Suppliers/SupplierServiceTests.cs
mv Suppliers/SuppliersApiCrTests.cs Suppliers/SuppliersApiTests.cs
```

- [ ] **Шаг 1.4: Удалить Ud-файлы**

```bash
cd tests/FastIntegrationTests.Tests.IntegreSQL
rm Products/ProductServiceUdTests.cs Products/ProductsApiUdTests.cs
rm Categories/CategoryServiceUdTests.cs Categories/CategoriesApiUdTests.cs
rm Customers/CustomerServiceUdTests.cs Customers/CustomersApiUdTests.cs
rm Discounts/DiscountServiceUdTests.cs Discounts/DiscountsApiUdTests.cs
rm Orders/OrderServiceUdTests.cs Orders/OrdersApiUdTests.cs
rm Reviews/ReviewServiceUdTests.cs Reviews/ReviewsApiUdTests.cs
rm Suppliers/SupplierServiceUdTests.cs Suppliers/SuppliersApiUdTests.cs
```

- [ ] **Шаг 1.5: Проверить сборку проекта**

```bash
dotnet build tests/FastIntegrationTests.Tests.IntegreSQL
```

Ожидается: `Build succeeded` с 0 ошибками.

- [ ] **Шаг 1.6: Проверить количество тест-методов**

```bash
dotnet test tests/FastIntegrationTests.Tests.IntegreSQL --list-tests 2>/dev/null | grep -c "::"
```

Ожидается: то же число, что и до слияния (тест-методы не добавлялись и не удалялись — только переструктурированы).

- [ ] **Шаг 1.7: Коммит**

```bash
git add tests/FastIntegrationTests.Tests.IntegreSQL/
git commit -m "refactor: объединить Cr+Ud тест-файлы в Tests — проект IntegreSQL"
```

---

### Task 2: Respawn — слияние всех 7 сущностей

**Проект:** `tests/FastIntegrationTests.Tests.Respawn/`

Алгоритм идентичен Task 1, с тремя отличиями:
1. Суффикс `Respawn` сохраняется в именах классов и файлов (например, `ProductServiceRespawnTests`).
2. Классы принимают fixture в конструкторе — брать из Cr-файла (в Cr и Ud он идентичен):
   - Service: `public ProductServiceRespawnTests(RespawnFixture fixture) : base(fixture) { }`
   - API: `public ProductsApiRespawnTests(RespawnApiFixture fixture) : base(fixture) { }`
3. XML-doc на конструктор сохраняется.

**Files:**
- Modify + rename: все `*CrRespawnTests.cs` → `*RespawnTests.cs` (убрать `Cr` из имени)
- Delete: все `*UdRespawnTests.cs`

Список файлов — в таблице Respawn выше.

- [ ] **Шаг 2.1: Прочитать оба файла для каждой сущности**

Алгоритм слияния тот же. Базовый класс `RespawnServiceTestBase` требует конструктора с `RespawnFixture`. Поля и `InitializeAsync` — из Ud-файла.

- [ ] **Шаг 2.2: Перезаписать Cr-файлы объединённым содержимым**

Перезаписать каждый Cr-файл объединённым классом с новым именем (без `Cr`).

- [ ] **Шаг 2.3: Переименовать перезаписанные файлы**

```bash
cd tests/FastIntegrationTests.Tests.Respawn
mv Products/ProductServiceCrRespawnTests.cs Products/ProductServiceRespawnTests.cs
mv Products/ProductsApiCrRespawnTests.cs Products/ProductsApiRespawnTests.cs
mv Categories/CategoryServiceCrRespawnTests.cs Categories/CategoryServiceRespawnTests.cs
mv Categories/CategoriesApiCrRespawnTests.cs Categories/CategoriesApiRespawnTests.cs
mv Customers/CustomerServiceCrRespawnTests.cs Customers/CustomerServiceRespawnTests.cs
mv Customers/CustomersApiCrRespawnTests.cs Customers/CustomersApiRespawnTests.cs
mv Discounts/DiscountServiceCrRespawnTests.cs Discounts/DiscountServiceRespawnTests.cs
mv Discounts/DiscountsApiCrRespawnTests.cs Discounts/DiscountsApiRespawnTests.cs
mv Orders/OrderServiceCrRespawnTests.cs Orders/OrderServiceRespawnTests.cs
mv Orders/OrdersApiCrRespawnTests.cs Orders/OrdersApiRespawnTests.cs
mv Reviews/ReviewServiceCrRespawnTests.cs Reviews/ReviewServiceRespawnTests.cs
mv Reviews/ReviewsApiCrRespawnTests.cs Reviews/ReviewsApiRespawnTests.cs
mv Suppliers/SupplierServiceCrRespawnTests.cs Suppliers/SupplierServiceRespawnTests.cs
mv Suppliers/SuppliersApiCrRespawnTests.cs Suppliers/SuppliersApiRespawnTests.cs
```

- [ ] **Шаг 2.4: Удалить Ud-файлы**

```bash
cd tests/FastIntegrationTests.Tests.Respawn
rm Products/ProductServiceUdRespawnTests.cs Products/ProductsApiUdRespawnTests.cs
rm Categories/CategoryServiceUdRespawnTests.cs Categories/CategoriesApiUdRespawnTests.cs
rm Customers/CustomerServiceUdRespawnTests.cs Customers/CustomersApiUdRespawnTests.cs
rm Discounts/DiscountServiceUdRespawnTests.cs Discounts/DiscountsApiUdRespawnTests.cs
rm Orders/OrderServiceUdRespawnTests.cs Orders/OrdersApiUdRespawnTests.cs
rm Reviews/ReviewServiceUdRespawnTests.cs Reviews/ReviewsApiUdRespawnTests.cs
rm Suppliers/SupplierServiceUdRespawnTests.cs Suppliers/SuppliersApiUdRespawnTests.cs
```

- [ ] **Шаг 2.5: Проверить сборку проекта**

```bash
dotnet build tests/FastIntegrationTests.Tests.Respawn
```

Ожидается: `Build succeeded` с 0 ошибками.

- [ ] **Шаг 2.6: Коммит**

```bash
git add tests/FastIntegrationTests.Tests.Respawn/
git commit -m "refactor: объединить Cr+Ud тест-файлы в Tests — проект Respawn"
```

---

### Task 3: Testcontainers — слияние всех 7 сущностей

**Проект:** `tests/FastIntegrationTests.Tests.Testcontainers/`

Алгоритм идентичен Task 1, с тремя отличиями:
1. Суффикс `Container` сохраняется в именах классов и файлов (например, `ProductServiceContainerTests`).
2. Конструктор с `ContainerFixture` берётся из Cr-файла:
   - Service: `public ProductServiceContainerTests(ContainerFixture fixture) : base(fixture) { }`
   - API: `public ProductsApiContainerTests(ContainerFixture fixture) : base(fixture) { }`
3. В Testcontainers сервис доступен через property базового класса (`private IProductService Sut => ProductService;`), а не через поле с `InitializeAsync`. Из Ud-файла брать его вариант полей/properties — он является надмножеством.

**Files:**
- Modify + rename: все `*CrContainerTests.cs` → `*ContainerTests.cs` (убрать `Cr`)
- Delete: все `*UdContainerTests.cs`

Список файлов — в таблице Testcontainers выше.

- [ ] **Шаг 3.1: Прочитать оба файла для каждой сущности**

Алгоритм слияния тот же. Важно: в Testcontainers поля — это properties (`private IProductService Sut => ProductService;`), а не null!-поля с инициализацией. Ud-файл является надмножеством Cr по набору properties.

- [ ] **Шаг 3.2: Перезаписать Cr-файлы объединённым содержимым**

- [ ] **Шаг 3.3: Переименовать перезаписанные файлы**

```bash
cd tests/FastIntegrationTests.Tests.Testcontainers
mv Products/ProductServiceCrContainerTests.cs Products/ProductServiceContainerTests.cs
mv Products/ProductsApiCrContainerTests.cs Products/ProductsApiContainerTests.cs
mv Categories/CategoryServiceCrContainerTests.cs Categories/CategoryServiceContainerTests.cs
mv Categories/CategoriesApiCrContainerTests.cs Categories/CategoriesApiContainerTests.cs
mv Customers/CustomerServiceCrContainerTests.cs Customers/CustomerServiceContainerTests.cs
mv Customers/CustomersApiCrContainerTests.cs Customers/CustomersApiContainerTests.cs
mv Discounts/DiscountServiceCrContainerTests.cs Discounts/DiscountServiceContainerTests.cs
mv Discounts/DiscountsApiCrContainerTests.cs Discounts/DiscountsApiContainerTests.cs
mv Orders/OrderServiceCrContainerTests.cs Orders/OrderServiceContainerTests.cs
mv Orders/OrdersApiCrContainerTests.cs Orders/OrdersApiContainerTests.cs
mv Reviews/ReviewServiceCrContainerTests.cs Reviews/ReviewServiceContainerTests.cs
mv Reviews/ReviewsApiCrContainerTests.cs Reviews/ReviewsApiContainerTests.cs
mv Suppliers/SupplierServiceCrContainerTests.cs Suppliers/SupplierServiceContainerTests.cs
mv Suppliers/SuppliersApiCrContainerTests.cs Suppliers/SuppliersApiContainerTests.cs
```

- [ ] **Шаг 3.4: Удалить Ud-файлы**

```bash
cd tests/FastIntegrationTests.Tests.Testcontainers
rm Products/ProductServiceUdContainerTests.cs Products/ProductsApiUdContainerTests.cs
rm Categories/CategoryServiceUdContainerTests.cs Categories/CategoriesApiUdContainerTests.cs
rm Customers/CustomerServiceUdContainerTests.cs Customers/CustomersApiUdContainerTests.cs
rm Discounts/DiscountServiceUdContainerTests.cs Discounts/DiscountsApiUdContainerTests.cs
rm Orders/OrderServiceUdContainerTests.cs Orders/OrdersApiUdContainerTests.cs
rm Reviews/ReviewServiceUdContainerTests.cs Reviews/ReviewsApiUdContainerTests.cs
rm Suppliers/SupplierServiceUdContainerTests.cs Suppliers/SuppliersApiUdContainerTests.cs
```

- [ ] **Шаг 3.5: Проверить сборку проекта**

```bash
dotnet build tests/FastIntegrationTests.Tests.Testcontainers
```

Ожидается: `Build succeeded` с 0 ошибками.

- [ ] **Шаг 3.6: Коммит**

```bash
git add tests/FastIntegrationTests.Tests.Testcontainers/
git commit -m "refactor: объединить Cr+Ud тест-файлы в Tests — проект Testcontainers"
```

---

### Task 4: Удалить BenchmarkScaleClasses.cs + финальная верификация

Выполняется после завершения Tasks 1–3.

**Files:**
- Delete: `tests/FastIntegrationTests.Tests.IntegreSQL/BenchmarkScaleClasses.cs`
- Delete: `tests/FastIntegrationTests.Tests.Respawn/BenchmarkScaleClasses.cs`
- Delete: `tests/FastIntegrationTests.Tests.Testcontainers/BenchmarkScaleClasses.cs`

- [ ] **Шаг 4.1: Удалить устаревшие BenchmarkScaleClasses.cs**

Файлы содержат наследников старых классов (`CategoriesApiCrTests_2`, `CategoriesApiUdTests_3` и т.д.). После слияния базовые классы переименованы, файлы компилироваться не будут. BenchmarkRunner пересоздаст их с новыми именами при следующем запуске.

```bash
rm tests/FastIntegrationTests.Tests.IntegreSQL/BenchmarkScaleClasses.cs
rm tests/FastIntegrationTests.Tests.Respawn/BenchmarkScaleClasses.cs
rm tests/FastIntegrationTests.Tests.Testcontainers/BenchmarkScaleClasses.cs
```

- [ ] **Шаг 4.2: Финальная сборка всего решения**

```bash
dotnet build
```

Ожидается: `Build succeeded` с 0 ошибками.

- [ ] **Шаг 4.3: Проверить итоговое число тест-методов**

```bash
dotnet test tests/FastIntegrationTests.Tests.IntegreSQL --list-tests 2>/dev/null | grep -c "::"
```

Ожидается: то же число, что было до рефакторинга (методы не добавлялись и не удалялись).

- [ ] **Шаг 4.4: Коммит**

```bash
git add tests/
git commit -m "refactor: удалить BenchmarkScaleClasses.cs — будет пересоздан BenchmarkRunner с новыми именами классов"
```
