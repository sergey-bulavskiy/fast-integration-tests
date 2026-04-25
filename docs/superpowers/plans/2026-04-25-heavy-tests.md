# Heavy Tests Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add 2 heavy integration tests per test class (168 total) to make the benchmark suite non-uniform — tests that do 15–25 SQL ops vs the current 1–3, with an explicit comment explaining artificial load inflation.

**Architecture:** Each class gets two test methods added at the end (before the closing `}`). Service-level tests use `Sut` (already defined in each class). API-level tests use `Client` and entity-specific helpers already present in each class. Test logic is identical across IntegreSQL / Respawn / Testcontainers for the same class type.

**Tech Stack:** xUnit, .NET 8, EF Core, PostgreSQL, IntegreSQL, Respawn, Testcontainers

---

## File Map

84 files modified, 2 methods added to each.

| Approach | Entity | Service Cr | Service Ud | API Cr | API Ud |
|---|---|---|---|---|---|
| IntegreSQL | Products | `Tests.IntegreSQL/Products/ProductServiceCrTests.cs` | `ProductServiceUdTests.cs` | `ProductsApiCrTests.cs` | `ProductsApiUdTests.cs` |
| IntegreSQL | Categories | `Categories/CategoryServiceCrTests.cs` | `CategoryServiceUdTests.cs` | `CategoriesApiCrTests.cs` | `CategoriesApiUdTests.cs` |
| IntegreSQL | Suppliers | `Suppliers/SupplierServiceCrTests.cs` | `SupplierServiceUdTests.cs` | `SuppliersApiCrTests.cs` | `SuppliersApiUdTests.cs` |
| IntegreSQL | Customers | `Customers/CustomerServiceCrTests.cs` | `CustomerServiceUdTests.cs` | `CustomersApiCrTests.cs` | `CustomersApiUdTests.cs` |
| IntegreSQL | Discounts | `Discounts/DiscountServiceCrTests.cs` | `DiscountServiceUdTests.cs` | `DiscountsApiCrTests.cs` | `DiscountsApiUdTests.cs` |
| IntegreSQL | Reviews | `Reviews/ReviewServiceCrTests.cs` | `ReviewServiceUdTests.cs` | `ReviewsApiCrTests.cs` | `ReviewsApiUdTests.cs` |
| IntegreSQL | Orders | `Orders/OrderServiceCrTests.cs` | `OrderServiceUdTests.cs` | `Orders/OrdersApiCrTests.cs` | `OrdersApiUdTests.cs` |
| Respawn | Products | `Tests.Respawn/Products/ProductServiceCrRespawnTests.cs` | `ProductServiceUdRespawnTests.cs` | `ProductsApiCrRespawnTests.cs` | `ProductsApiUdRespawnTests.cs` |
| Respawn | Categories | `Categories/CategoryServiceCrRespawnTests.cs` | … | … | … |
| Respawn | Suppliers | `Suppliers/SupplierServiceCrRespawnTests.cs` | … | … | … |
| Respawn | Customers | `Customers/CustomerServiceCrRespawnTests.cs` | … | … | … |
| Respawn | Discounts | `Discounts/DiscountServiceCrRespawnTests.cs` | … | … | … |
| Respawn | Reviews | `Reviews/ReviewServiceCrRespawnTests.cs` | … | … | … |
| Respawn | Orders | `Orders/OrderServiceCrRespawnTests.cs` | … | … | … |
| Testcontainers | Products | `Tests.Testcontainers/Products/ProductServiceCrContainerTests.cs` | … | … | … |
| Testcontainers | Categories | `Categories/CategoryServiceCrContainerTests.cs` | … | … | … |
| Testcontainers | Suppliers | `Suppliers/SupplierServiceCrContainerTests.cs` | … | … | … |
| Testcontainers | Customers | `Customers/CustomerServiceCrContainerTests.cs` | … | … | … |
| Testcontainers | Discounts | `Discounts/DiscountServiceCrContainerTests.cs` | … | … | … |
| Testcontainers | Reviews | `Reviews/ReviewServiceCrContainerTests.cs` | … | … | … |
| Testcontainers | Orders | `Orders/OrderServiceCrContainerTests.cs` | … | … | … |

All paths are under `tests/`. Respawn/Testcontainers follow the same `Cr/Ud + Api/Service` split with `Respawn`/`Container` suffixes.

---

## Infrastructure notes for the executing agent

**IntegreSQL service tests** (`AppServiceTestBase`): no constructor needed, `Sut` created in `InitializeAsync`.

**Respawn service tests** (`RespawnServiceTestBase`): class has constructor `(RespawnFixture fixture) : base(fixture)`. `Sut` created in `InitializeAsync`.

**Testcontainers service tests:**
- Products/Orders: inherit `ContainerServiceTestBase(ContainerFixture)`, `Sut => ProductService` / `Sut => OrderService` from base.
- All other entities: implement `IAsyncLifetime, IClassFixture<ContainerFixture>` directly, `Sut` created in `InitializeAsync`. Same `Sut` usage in test bodies.

**IntegreSQL API tests** (`ComponentTestBase`): no constructor. `Client` from base.

**Respawn API tests** (`RespawnApiTestBase`): constructor `(RespawnApiFixture fixture) : base(fixture)`. `Client` from base.

**Testcontainers API tests** (`ContainerApiTestBase`): constructor `(ContainerFixture fixture) : base(fixture)`. `Client` from base.

**XML-doc requirement** (project convention): every public/test method needs `/// <summary>` and `/// <param name="ct">Токен отмены операции.</param>` if it has a ct param. The `[Theory]` test methods have `int _` param, not `ct` — so only `<summary>` is needed.

---

## Task 1: Products

**Service H1** — add to all 6 service files (`*ProductServiceCr*` and `*ProductServiceUd*`):

```csharp
/// <summary>
/// Создаёт несколько товаров, читает через GetAll и GetById — проверяет согласованность данных.
/// </summary>
[Theory]
[MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
public async Task CreateMultiple_GetAll_GetByIdEach_ReturnsConsistentData(int _)
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
```

**Service H2** — add to all 6 service files:

```csharp
/// <summary>
/// Создаёт товар, обновляет поля, проверяет персистентность, удаляет — полный цикл записи.
/// </summary>
[Theory]
[MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
public async Task CreateUpdateDelete_VerifyEachStep_AllPersist(int _)
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
```

**API H1** — add to all 6 API files (`*ProductsApiCr*` and `*ProductsApiUd*`):

```csharp
/// <summary>
/// Создаёт несколько товаров через API, проверяет GetAll и GetById каждого.
/// </summary>
[Theory]
[MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
public async Task CreateMultiple_GetAll_GetByIdEach_ReturnsConsistentData(int _)
{
    var a = await CreateProductAsync("Товар А", 100m);
    var b = await CreateProductAsync("Товар Б", 200m);
    var c = await CreateProductAsync("Товар В", 300m);

    var all = await Client.GetAsync("/api/products");
    var list = await all.Content.ReadFromJsonAsync<List<ProductDto>>();
    Assert.Equal(3, list!.Count);

    var fa = await (await Client.GetAsync($"/api/products/{a.Id}")).Content.ReadFromJsonAsync<ProductDto>();
    var fb = await (await Client.GetAsync($"/api/products/{b.Id}")).Content.ReadFromJsonAsync<ProductDto>();
    var fc = await (await Client.GetAsync($"/api/products/{c.Id}")).Content.ReadFromJsonAsync<ProductDto>();
    Assert.Equal("Товар А", fa!.Name);
    Assert.Equal("Товар Б", fb!.Name);
    Assert.Equal("Товар В", fc!.Name);

    // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
    for (var i = 0; i < 4; i++)
    {
        var extra = await CreateProductAsync($"Доп {i}", 500m + i * 50m);
        await Client.GetAsync($"/api/products/{extra.Id}");
    }
    await Client.GetAsync("/api/products");
}
```

**API H2** — add to all 6 API files:

```csharp
/// <summary>
/// Создаёт товар, обновляет через PUT, проверяет GET, удаляет — полный HTTP-цикл.
/// </summary>
[Theory]
[MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
public async Task CreateUpdateDelete_VerifyEachStep_AllPersist(int _)
{
    var created = await CreateProductAsync("Монитор", 20_000m);

    var putResp = await Client.PutAsJsonAsync($"/api/products/{created.Id}",
        new UpdateProductRequest { Name = "Монитор 4K", Description = "UHD", Price = 25_000m });
    Assert.Equal(HttpStatusCode.OK, putResp.StatusCode);
    var updated = await putResp.Content.ReadFromJsonAsync<ProductDto>();
    Assert.Equal("Монитор 4K", updated!.Name);

    var getResp = await Client.GetAsync($"/api/products/{created.Id}");
    var fetched = await getResp.Content.ReadFromJsonAsync<ProductDto>();
    Assert.Equal("Монитор 4K", fetched!.Name);

    var delResp = await Client.DeleteAsync($"/api/products/{created.Id}");
    Assert.Equal(HttpStatusCode.NoContent, delResp.StatusCode);
    Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync($"/api/products/{created.Id}")).StatusCode);

    // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
    for (var i = 0; i < 4; i++)
    {
        var extra = await CreateProductAsync($"Доп {i}", 1_000m + i * 100m);
        await Client.PutAsJsonAsync($"/api/products/{extra.Id}",
            new UpdateProductRequest { Name = $"Доп {i} v2", Price = 1_100m + i * 100m });
        await Client.GetAsync($"/api/products/{extra.Id}");
    }
    await Client.GetAsync("/api/products");
}
```

### Steps

- [ ] **Добавить Service H1 и H2 в `ProductServiceCrTests.cs` (IntegreSQL)**
  Файл: `tests/FastIntegrationTests.Tests.IntegreSQL/Products/ProductServiceCrTests.cs`
  Добавить оба метода перед последней `}`.

- [ ] **Добавить Service H1 и H2 в `ProductServiceUdTests.cs` (IntegreSQL)**
  Файл: `tests/FastIntegrationTests.Tests.IntegreSQL/Products/ProductServiceUdTests.cs`
  Добавить оба метода перед последней `}`.

- [ ] **Добавить API H1 и H2 в `ProductsApiCrTests.cs` (IntegreSQL)**
  Файл: `tests/FastIntegrationTests.Tests.IntegreSQL/Products/ProductsApiCrTests.cs`
  Добавить перед `// --- helpers ---` или перед последней `}` если хелперов нет в этом классе.

- [ ] **Добавить API H1 и H2 в `ProductsApiUdTests.cs` (IntegreSQL)**
  Файл: `tests/FastIntegrationTests.Tests.IntegreSQL/Products/ProductsApiUdTests.cs`

- [ ] **Добавить Service H1 и H2 в оба `ProductService*RespawnTests.cs`**
  Файлы: `tests/FastIntegrationTests.Tests.Respawn/Products/ProductServiceCrRespawnTests.cs` и `ProductServiceUdRespawnTests.cs`

- [ ] **Добавить API H1 и H2 в оба `ProductsApi*RespawnTests.cs`**
  Файлы: `tests/FastIntegrationTests.Tests.Respawn/Products/ProductsApiCrRespawnTests.cs` и `ProductsApiUdRespawnTests.cs`

- [ ] **Добавить Service H1 и H2 в оба `ProductService*ContainerTests.cs`**
  Файлы: `tests/FastIntegrationTests.Tests.Testcontainers/Products/ProductServiceCrContainerTests.cs` и `ProductServiceUdContainerTests.cs`
  Примечание: Testcontainers Products service tests используют `IProductService Sut => ProductService;` (свойство из базового класса `ServiceTestBase`).

- [ ] **Добавить API H1 и H2 в оба `ProductsApi*ContainerTests.cs`**
  Файлы: `tests/FastIntegrationTests.Tests.Testcontainers/Products/ProductsApiCrContainerTests.cs` и `ProductsApiUdContainerTests.cs`

- [ ] **Запустить тесты Products**
  ```
  dotnet test tests/FastIntegrationTests.Tests.IntegreSQL --filter "FullyQualifiedName~Products"
  dotnet test tests/FastIntegrationTests.Tests.Respawn --filter "FullyQualifiedName~Products"
  dotnet test tests/FastIntegrationTests.Tests.Testcontainers --filter "FullyQualifiedName~Products"
  ```
  Ожидается: все PASS.

- [ ] **Commit**
  ```
  git add tests/
  git commit -m "test: тяжёлые тесты для Products"
  ```

---

## Task 2: Categories

Categories используют `Guid` ID и уникальное имя (`ExistsByName`). В параллельных создаёт используй уникальные имена.

**Service H1** — add to all 6 service files (`*CategoryServiceCr*`, `*CategoryServiceUd*`):

```csharp
/// <summary>
/// Создаёт несколько категорий, проверяет GetAll и GetById каждой.
/// </summary>
[Theory]
[MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
public async Task CreateMultiple_GetAll_GetByIdEach_ReturnsConsistentData(int _)
{
    var a = await Sut.CreateAsync(new CreateCategoryRequest { Name = "Электроника", Description = "Гаджеты" });
    var b = await Sut.CreateAsync(new CreateCategoryRequest { Name = "Одежда" });
    var c = await Sut.CreateAsync(new CreateCategoryRequest { Name = "Книги", Description = "Всё о книгах" });

    var all = await Sut.GetAllAsync();
    Assert.Equal(3, all.Count);
    Assert.Equal("Электроника", (await Sut.GetByIdAsync(a.Id)).Name);
    Assert.Equal("Одежда", (await Sut.GetByIdAsync(b.Id)).Name);
    Assert.Equal("Книги", (await Sut.GetByIdAsync(c.Id)).Name);

    // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
    for (var i = 0; i < 4; i++)
    {
        var extra = await Sut.CreateAsync(new CreateCategoryRequest { Name = $"Категория {i}" });
        await Sut.GetByIdAsync(extra.Id);
    }
    await Sut.GetAllAsync();
}
```

**Service H2** — add to all 6 service files:

```csharp
/// <summary>
/// Создаёт категорию, обновляет, проверяет персистентность, удаляет.
/// </summary>
[Theory]
[MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
public async Task CreateUpdateDelete_VerifyEachStep_AllPersist(int _)
{
    var created = await Sut.CreateAsync(new CreateCategoryRequest { Name = "Спорт", Description = "Инвентарь" });
    var updated = await Sut.UpdateAsync(created.Id, new UpdateCategoryRequest { Name = "Спорт и фитнес", Description = "Тренажёры и инвентарь" });
    Assert.Equal("Спорт и фитнес", updated.Name);

    var fetched = await Sut.GetByIdAsync(created.Id);
    Assert.Equal("Спорт и фитнес", fetched.Name);
    Assert.Equal("Тренажёры и инвентарь", fetched.Description);

    await Sut.DeleteAsync(created.Id);
    await Assert.ThrowsAsync<NotFoundException>(() => Sut.GetByIdAsync(created.Id));

    // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
    for (var i = 0; i < 4; i++)
    {
        var extra = await Sut.CreateAsync(new CreateCategoryRequest { Name = $"Доп кат {i}" });
        await Sut.UpdateAsync(extra.Id, new UpdateCategoryRequest { Name = $"Доп кат {i} v2" });
        await Sut.GetByIdAsync(extra.Id);
    }
    await Sut.GetAllAsync();
}
```

**API H1** — add to all 6 API files (`*CategoriesApiCr*`, `*CategoriesApiUd*`):

```csharp
/// <summary>
/// Создаёт несколько категорий через API, проверяет GetAll и GetById каждой.
/// </summary>
[Theory]
[MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
public async Task CreateMultiple_GetAll_GetByIdEach_ReturnsConsistentData(int _)
{
    var a = await CreateCategoryAsync("Электроника");
    var b = await CreateCategoryAsync("Одежда");
    var c = await CreateCategoryAsync("Книги");

    var all = await Client.GetAsync("/api/categories");
    var list = await all.Content.ReadFromJsonAsync<List<CategoryDto>>();
    Assert.Equal(3, list!.Count);

    var fa = await (await Client.GetAsync($"/api/categories/{a.Id}")).Content.ReadFromJsonAsync<CategoryDto>();
    var fb = await (await Client.GetAsync($"/api/categories/{b.Id}")).Content.ReadFromJsonAsync<CategoryDto>();
    var fc = await (await Client.GetAsync($"/api/categories/{c.Id}")).Content.ReadFromJsonAsync<CategoryDto>();
    Assert.Equal("Электроника", fa!.Name);
    Assert.Equal("Одежда", fb!.Name);
    Assert.Equal("Книги", fc!.Name);

    // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
    for (var i = 0; i < 4; i++)
    {
        var extra = await CreateCategoryAsync($"Категория {i}");
        await Client.GetAsync($"/api/categories/{extra.Id}");
    }
    await Client.GetAsync("/api/categories");
}
```

**API H2** — add to all 6 API files:

```csharp
/// <summary>
/// Создаёт категорию, обновляет через PUT, проверяет GET, удаляет — полный HTTP-цикл.
/// </summary>
[Theory]
[MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
public async Task CreateUpdateDelete_VerifyEachStep_AllPersist(int _)
{
    var created = await CreateCategoryAsync("Спорт");

    var putResp = await Client.PutAsJsonAsync($"/api/categories/{created.Id}",
        new UpdateCategoryRequest { Name = "Спорт и фитнес", Description = "Обновлено" });
    Assert.Equal(HttpStatusCode.OK, putResp.StatusCode);
    var updated = await putResp.Content.ReadFromJsonAsync<CategoryDto>();
    Assert.Equal("Спорт и фитнес", updated!.Name);

    var getResp = await Client.GetAsync($"/api/categories/{created.Id}");
    var fetched = await getResp.Content.ReadFromJsonAsync<CategoryDto>();
    Assert.Equal("Спорт и фитнес", fetched!.Name);

    var delResp = await Client.DeleteAsync($"/api/categories/{created.Id}");
    Assert.Equal(HttpStatusCode.NoContent, delResp.StatusCode);
    Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync($"/api/categories/{created.Id}")).StatusCode);

    // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
    for (var i = 0; i < 4; i++)
    {
        var extra = await CreateCategoryAsync($"Доп кат {i}");
        await Client.PutAsJsonAsync($"/api/categories/{extra.Id}",
            new UpdateCategoryRequest { Name = $"Доп кат {i} v2" });
        await Client.GetAsync($"/api/categories/{extra.Id}");
    }
    await Client.GetAsync("/api/categories");
}
```

### Steps

- [ ] Добавить Service H1 и H2 в `CategoryServiceCrTests.cs` и `CategoryServiceUdTests.cs` (IntegreSQL)
- [ ] Добавить API H1 и H2 в `CategoriesApiCrTests.cs` и `CategoriesApiUdTests.cs` (IntegreSQL)
- [ ] Добавить Service H1 и H2 в `CategoryServiceCrRespawnTests.cs` и `CategoryServiceUdRespawnTests.cs` (Respawn)
- [ ] Добавить API H1 и H2 в `CategoriesApiCrRespawnTests.cs` и `CategoriesApiUdRespawnTests.cs` (Respawn)
- [ ] Добавить Service H1 и H2 в `CategoryServiceCrContainerTests.cs` и `CategoryServiceUdContainerTests.cs` (Testcontainers)
  Примечание: эти классы реализуют `IAsyncLifetime, IClassFixture<ContainerFixture>` напрямую. `Sut` — поле класса, инициализируется в `InitializeAsync`. Тела методов идентичны.
- [ ] Добавить API H1 и H2 в `CategoriesApiCrContainerTests.cs` и `CategoriesApiUdContainerTests.cs` (Testcontainers)
- [ ] **Запустить тесты Categories**
  ```
  dotnet test tests/FastIntegrationTests.Tests.IntegreSQL --filter "FullyQualifiedName~Categor"
  dotnet test tests/FastIntegrationTests.Tests.Respawn --filter "FullyQualifiedName~Categor"
  dotnet test tests/FastIntegrationTests.Tests.Testcontainers --filter "FullyQualifiedName~Categor"
  ```
- [ ] **Commit**: `git add tests/ && git commit -m "test: тяжёлые тесты для Categories"`

---

## Task 3: Suppliers

Suppliers: Guid ID, уникальный `ContactEmail`, методы `ActivateAsync` / `DeactivateAsync`. CreateAsync проверяет уникальность email. В padding используй уникальные emails.

**Service H1**:

```csharp
/// <summary>
/// Создаёт несколько поставщиков, проверяет GetAll и GetById каждого.
/// </summary>
[Theory]
[MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
public async Task CreateMultiple_GetAll_GetByIdEach_ReturnsConsistentData(int _)
{
    var a = await Sut.CreateAsync(new CreateSupplierRequest { Name = "ООО Альфа", ContactEmail = "alpha@example.com", Country = "Россия" });
    var b = await Sut.CreateAsync(new CreateSupplierRequest { Name = "ИП Бета", ContactEmail = "beta@example.com", Country = "Беларусь" });
    var c = await Sut.CreateAsync(new CreateSupplierRequest { Name = "ЗАО Гамма", ContactEmail = "gamma@example.com", Country = "Казахстан" });

    var all = await Sut.GetAllAsync();
    Assert.Equal(3, all.Count);
    Assert.Equal("ООО Альфа", (await Sut.GetByIdAsync(a.Id)).Name);
    Assert.Equal("ИП Бета", (await Sut.GetByIdAsync(b.Id)).Name);
    Assert.Equal("ЗАО Гамма", (await Sut.GetByIdAsync(c.Id)).Name);

    // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
    for (var i = 0; i < 4; i++)
    {
        var extra = await Sut.CreateAsync(new CreateSupplierRequest { Name = $"Доп {i}", ContactEmail = $"extra{i}@example.com", Country = "РФ" });
        await Sut.GetByIdAsync(extra.Id);
    }
    await Sut.GetAllAsync();
}
```

**Service H2**:

```csharp
/// <summary>
/// Создаёт поставщика, обновляет поля, деактивирует, активирует — проверяет все переходы.
/// </summary>
[Theory]
[MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
public async Task CreateUpdateDeactivateActivate_AllPersist(int _)
{
    var created = await Sut.CreateAsync(new CreateSupplierRequest { Name = "ООО Старт", ContactEmail = "start@example.com", Country = "Россия" });
    Assert.True(created.IsActive);

    var updated = await Sut.UpdateAsync(created.Id, new UpdateSupplierRequest { Name = "ООО Финиш", ContactEmail = "start@example.com", Country = "Беларусь" });
    Assert.Equal("ООО Финиш", updated.Name);
    Assert.Equal("Беларусь", updated.Country);

    var deactivated = await Sut.DeactivateAsync(created.Id);
    Assert.False(deactivated.IsActive);

    var activated = await Sut.ActivateAsync(created.Id);
    Assert.True(activated.IsActive);

    var fetched = await Sut.GetByIdAsync(created.Id);
    Assert.Equal("ООО Финиш", fetched.Name);
    Assert.True(fetched.IsActive);

    // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
    for (var i = 0; i < 3; i++)
    {
        var extra = await Sut.CreateAsync(new CreateSupplierRequest { Name = $"Доп {i}", ContactEmail = $"pad{i}@example.com", Country = "РФ" });
        await Sut.DeactivateAsync(extra.Id);
        await Sut.GetByIdAsync(extra.Id);
    }
    await Sut.GetAllAsync();
}
```

**API H1**:

```csharp
/// <summary>
/// Создаёт несколько поставщиков через API, проверяет GetAll и GetById каждого.
/// </summary>
[Theory]
[MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
public async Task CreateMultiple_GetAll_GetByIdEach_ReturnsConsistentData(int _)
{
    var a = await CreateSupplierAsync("ООО Альфа", "alpha@example.com");
    var b = await CreateSupplierAsync("ИП Бета", "beta@example.com");
    var c = await CreateSupplierAsync("ЗАО Гамма", "gamma@example.com");

    var all = await Client.GetAsync("/api/suppliers");
    var list = await all.Content.ReadFromJsonAsync<List<SupplierDto>>();
    Assert.Equal(3, list!.Count);

    var fa = await (await Client.GetAsync($"/api/suppliers/{a.Id}")).Content.ReadFromJsonAsync<SupplierDto>();
    Assert.Equal("ООО Альфа", fa!.Name);

    // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
    for (var i = 0; i < 4; i++)
    {
        var extra = await CreateSupplierAsync($"Доп {i}", $"extra{i}@example.com");
        await Client.GetAsync($"/api/suppliers/{extra.Id}");
    }
    await Client.GetAsync("/api/suppliers");
}
```

**API H2**:

```csharp
/// <summary>
/// Создаёт поставщика, обновляет, деактивирует, активирует через API.
/// </summary>
[Theory]
[MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
public async Task CreateUpdateDeactivateActivate_AllPersist(int _)
{
    var created = await CreateSupplierAsync("ООО Старт", "start@example.com");

    var putResp = await Client.PutAsJsonAsync($"/api/suppliers/{created.Id}",
        new UpdateSupplierRequest { Name = "ООО Финиш", ContactEmail = "start@example.com", Country = "Беларусь" });
    Assert.Equal(HttpStatusCode.OK, putResp.StatusCode);

    var deact = await Client.PostAsync($"/api/suppliers/{created.Id}/deactivate", null);
    Assert.Equal(HttpStatusCode.NoContent, deact.StatusCode);

    var act = await Client.PostAsync($"/api/suppliers/{created.Id}/activate", null);
    Assert.Equal(HttpStatusCode.NoContent, act.StatusCode);

    var fetched = await (await Client.GetAsync($"/api/suppliers/{created.Id}")).Content.ReadFromJsonAsync<SupplierDto>();
    Assert.Equal("ООО Финиш", fetched!.Name);
    Assert.True(fetched.IsActive);

    // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
    for (var i = 0; i < 3; i++)
    {
        var extra = await CreateSupplierAsync($"Доп {i}", $"pad{i}@example.com");
        await Client.PostAsync($"/api/suppliers/{extra.Id}/deactivate", null);
        await Client.GetAsync($"/api/suppliers/{extra.Id}");
    }
    await Client.GetAsync("/api/suppliers");
}
```

Примечание: если в API-классах нет хелпера `CreateSupplierAsync`, добавь его по образцу хелперов из других классов (`CreateCategoryAsync`, `CreateProductAsync`):
```csharp
private async Task<SupplierDto> CreateSupplierAsync(string name, string email, CancellationToken ct = default)
{
    var resp = await Client.PostAsJsonAsync("/api/suppliers",
        new CreateSupplierRequest { Name = name, ContactEmail = email, Country = "Россия" }, ct);
    resp.EnsureSuccessStatusCode();
    return (await resp.Content.ReadFromJsonAsync<SupplierDto>(ct))!;
}
```

### Steps

- [ ] Добавить Service H1 и H2 в `SupplierServiceCrTests.cs` и `SupplierServiceUdTests.cs` (IntegreSQL)
- [ ] Добавить API H1 и H2 (+ хелпер если нужен) в `SuppliersApiCrTests.cs` и `SuppliersApiUdTests.cs` (IntegreSQL)
- [ ] Добавить Service H1 и H2 в `SupplierServiceCrRespawnTests.cs` и `SupplierServiceUdRespawnTests.cs` (Respawn)
- [ ] Добавить API H1 и H2 в `SuppliersApiCrRespawnTests.cs` и `SuppliersApiUdRespawnTests.cs` (Respawn)
- [ ] Добавить Service H1 и H2 в `SupplierServiceCrContainerTests.cs` и `SupplierServiceUdContainerTests.cs` (Testcontainers)
- [ ] Добавить API H1 и H2 в `SuppliersApiCrContainerTests.cs` и `SuppliersApiUdContainerTests.cs` (Testcontainers)
- [ ] **Запустить тесты Suppliers**
  ```
  dotnet test tests/FastIntegrationTests.Tests.IntegreSQL --filter "FullyQualifiedName~Supplier"
  dotnet test tests/FastIntegrationTests.Tests.Respawn --filter "FullyQualifiedName~Supplier"
  dotnet test tests/FastIntegrationTests.Tests.Testcontainers --filter "FullyQualifiedName~Supplier"
  ```
- [ ] **Commit**: `git add tests/ && git commit -m "test: тяжёлые тесты для Suppliers"`

---

## Task 4: Customers

Customers: Guid ID, уникальный `Email`, lifecycle: `BanAsync` / `ActivateAsync` / `DeactivateAsync`. CustomerStatus: Active, Banned, Inactive.

**Service H1**:

```csharp
/// <summary>
/// Создаёт несколько покупателей, проверяет GetAll и GetById каждого.
/// </summary>
[Theory]
[MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
public async Task CreateMultiple_GetAll_GetByIdEach_ReturnsConsistentData(int _)
{
    var a = await Sut.CreateAsync(new CreateCustomerRequest { Name = "Иван", Email = "ivan@example.com" });
    var b = await Sut.CreateAsync(new CreateCustomerRequest { Name = "Мария", Email = "maria@example.com" });
    var c = await Sut.CreateAsync(new CreateCustomerRequest { Name = "Пётр", Email = "peter@example.com" });

    var all = await Sut.GetAllAsync();
    Assert.Equal(3, all.Count);
    Assert.Equal("Иван", (await Sut.GetByIdAsync(a.Id)).Name);
    Assert.Equal("Мария", (await Sut.GetByIdAsync(b.Id)).Name);
    Assert.Equal("Пётр", (await Sut.GetByIdAsync(c.Id)).Name);

    // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
    for (var i = 0; i < 4; i++)
    {
        var extra = await Sut.CreateAsync(new CreateCustomerRequest { Name = $"Доп {i}", Email = $"extra{i}@example.com" });
        await Sut.GetByIdAsync(extra.Id);
    }
    await Sut.GetAllAsync();
}
```

**Service H2**:

```csharp
/// <summary>
/// Создаёт покупателя, выполняет Ban → Activate → Deactivate, проверяет статус после каждого.
/// </summary>
[Theory]
[MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
public async Task CreateBanActivateDeactivate_StatusTransitionsCorrect(int _)
{
    var created = await Sut.CreateAsync(new CreateCustomerRequest { Name = "Клиент", Email = "client@example.com" });
    Assert.Equal(CustomerStatus.Active, created.Status);

    var banned = await Sut.BanAsync(created.Id);
    Assert.Equal(CustomerStatus.Banned, banned.Status);

    var activated = await Sut.ActivateAsync(created.Id);
    Assert.Equal(CustomerStatus.Active, activated.Status);

    var deactivated = await Sut.DeactivateAsync(created.Id);
    Assert.Equal(CustomerStatus.Inactive, deactivated.Status);

    var fetched = await Sut.GetByIdAsync(created.Id);
    Assert.Equal(CustomerStatus.Inactive, fetched.Status);

    // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
    for (var i = 0; i < 3; i++)
    {
        var extra = await Sut.CreateAsync(new CreateCustomerRequest { Name = $"Доп {i}", Email = $"pad{i}@example.com" });
        await Sut.BanAsync(extra.Id);
        await Sut.GetByIdAsync(extra.Id);
    }
    await Sut.GetAllAsync();
}
```

**API H1**:

```csharp
/// <summary>
/// Создаёт несколько покупателей через API, проверяет GetAll и GetById каждого.
/// </summary>
[Theory]
[MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
public async Task CreateMultiple_GetAll_GetByIdEach_ReturnsConsistentData(int _)
{
    var a = await CreateCustomerAsync("Иван", "ivan@example.com");
    var b = await CreateCustomerAsync("Мария", "maria@example.com");
    var c = await CreateCustomerAsync("Пётр", "peter@example.com");

    var all = await Client.GetAsync("/api/customers");
    var list = await all.Content.ReadFromJsonAsync<List<CustomerDto>>();
    Assert.Equal(3, list!.Count);

    var fa = await (await Client.GetAsync($"/api/customers/{a.Id}")).Content.ReadFromJsonAsync<CustomerDto>();
    Assert.Equal("Иван", fa!.Name);
    Assert.Equal(CustomerStatus.Active, fa.Status);

    // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
    for (var i = 0; i < 4; i++)
    {
        var extra = await CreateCustomerAsync($"Доп {i}", $"extra{i}@example.com");
        await Client.GetAsync($"/api/customers/{extra.Id}");
    }
    await Client.GetAsync("/api/customers");
}
```

**API H2**:

```csharp
/// <summary>
/// Создаёт покупателя, выполняет ban → activate → deactivate через API.
/// </summary>
[Theory]
[MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
public async Task CreateBanActivateDeactivate_StatusTransitionsCorrect(int _)
{
    var created = await CreateCustomerAsync("Клиент", "client@example.com");

    Assert.Equal(HttpStatusCode.NoContent, (await Client.PostAsync($"/api/customers/{created.Id}/ban", null)).StatusCode);
    var banned = await (await Client.GetAsync($"/api/customers/{created.Id}")).Content.ReadFromJsonAsync<CustomerDto>();
    Assert.Equal(CustomerStatus.Banned, banned!.Status);

    Assert.Equal(HttpStatusCode.NoContent, (await Client.PostAsync($"/api/customers/{created.Id}/activate", null)).StatusCode);
    Assert.Equal(HttpStatusCode.NoContent, (await Client.PostAsync($"/api/customers/{created.Id}/deactivate", null)).StatusCode);

    var fetched = await (await Client.GetAsync($"/api/customers/{created.Id}")).Content.ReadFromJsonAsync<CustomerDto>();
    Assert.Equal(CustomerStatus.Inactive, fetched!.Status);

    // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
    for (var i = 0; i < 3; i++)
    {
        var extra = await CreateCustomerAsync($"Доп {i}", $"pad{i}@example.com");
        await Client.PostAsync($"/api/customers/{extra.Id}/ban", null);
        await Client.GetAsync($"/api/customers/{extra.Id}");
    }
    await Client.GetAsync("/api/customers");
}
```

### Steps

- [ ] Добавить Service H1 и H2 в `CustomerServiceCrTests.cs` и `CustomerServiceUdTests.cs` (IntegreSQL)
- [ ] Добавить API H1 и H2 в `CustomersApiCrTests.cs` и `CustomersApiUdTests.cs` (IntegreSQL)
- [ ] Добавить Service H1 и H2 в оба Respawn service файла Customers
- [ ] Добавить API H1 и H2 в оба Respawn API файла Customers
- [ ] Добавить Service H1 и H2 в оба Testcontainers service файла Customers
- [ ] Добавить API H1 и H2 в оба Testcontainers API файла Customers
- [ ] **Запустить тесты Customers**
  ```
  dotnet test tests/FastIntegrationTests.Tests.IntegreSQL --filter "FullyQualifiedName~Customer"
  dotnet test tests/FastIntegrationTests.Tests.Respawn --filter "FullyQualifiedName~Customer"
  dotnet test tests/FastIntegrationTests.Tests.Testcontainers --filter "FullyQualifiedName~Customer"
  ```
- [ ] **Commit**: `git add tests/ && git commit -m "test: тяжёлые тесты для Customers"`

---

## Task 5: Discounts

Discounts: Guid ID, уникальный `Code`, lifecycle: `ActivateAsync` / `DeactivateAsync`. По умолчанию `IsActive = false`.

**Service H1**:

```csharp
/// <summary>
/// Создаёт несколько скидок, проверяет GetAll и GetById каждой.
/// </summary>
[Theory]
[MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
public async Task CreateMultiple_GetAll_GetByIdEach_ReturnsConsistentData(int _)
{
    var a = await Sut.CreateAsync(new CreateDiscountRequest { Code = "SALE10", DiscountPercent = 10 });
    var b = await Sut.CreateAsync(new CreateDiscountRequest { Code = "SALE20", DiscountPercent = 20 });
    var c = await Sut.CreateAsync(new CreateDiscountRequest { Code = "SALE30", DiscountPercent = 30 });

    var all = await Sut.GetAllAsync();
    Assert.Equal(3, all.Count);
    Assert.Equal("SALE10", (await Sut.GetByIdAsync(a.Id)).Code);
    Assert.Equal("SALE20", (await Sut.GetByIdAsync(b.Id)).Code);
    Assert.Equal("SALE30", (await Sut.GetByIdAsync(c.Id)).Code);

    // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
    for (var i = 0; i < 4; i++)
    {
        var extra = await Sut.CreateAsync(new CreateDiscountRequest { Code = $"EXTRA{i:00}", DiscountPercent = 5 + i });
        await Sut.GetByIdAsync(extra.Id);
    }
    await Sut.GetAllAsync();
}
```

**Service H2**:

```csharp
/// <summary>
/// Создаёт скидку, активирует, деактивирует, обновляет — проверяет каждый шаг.
/// </summary>
[Theory]
[MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
public async Task CreateActivateDeactivateUpdate_AllPersist(int _)
{
    var created = await Sut.CreateAsync(new CreateDiscountRequest { Code = "START10", DiscountPercent = 10 });
    Assert.False(created.IsActive);

    var activated = await Sut.ActivateAsync(created.Id);
    Assert.True(activated.IsActive);

    var deactivated = await Sut.DeactivateAsync(created.Id);
    Assert.False(deactivated.IsActive);

    var updated = await Sut.UpdateAsync(created.Id, new UpdateDiscountRequest { Code = "FINISH25", DiscountPercent = 25 });
    Assert.Equal("FINISH25", updated.Code);
    Assert.Equal(25, updated.DiscountPercent);

    var fetched = await Sut.GetByIdAsync(created.Id);
    Assert.Equal("FINISH25", fetched.Code);
    Assert.False(fetched.IsActive);

    // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
    for (var i = 0; i < 3; i++)
    {
        var extra = await Sut.CreateAsync(new CreateDiscountRequest { Code = $"PAD{i:00}", DiscountPercent = 5 + i });
        await Sut.ActivateAsync(extra.Id);
        await Sut.GetByIdAsync(extra.Id);
    }
    await Sut.GetAllAsync();
}
```

**API H1**:

```csharp
/// <summary>
/// Создаёт несколько скидок через API, проверяет GetAll и GetById каждой.
/// </summary>
[Theory]
[MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
public async Task CreateMultiple_GetAll_GetByIdEach_ReturnsConsistentData(int _)
{
    var a = await CreateDiscountAsync("SALE10", 10);
    var b = await CreateDiscountAsync("SALE20", 20);
    var c = await CreateDiscountAsync("SALE30", 30);

    var all = await Client.GetAsync("/api/discounts");
    var list = await all.Content.ReadFromJsonAsync<List<DiscountDto>>();
    Assert.Equal(3, list!.Count);

    var fa = await (await Client.GetAsync($"/api/discounts/{a.Id}")).Content.ReadFromJsonAsync<DiscountDto>();
    Assert.Equal("SALE10", fa!.Code);

    // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
    for (var i = 0; i < 4; i++)
    {
        var extra = await CreateDiscountAsync($"EX{i:00}", 5 + i);
        await Client.GetAsync($"/api/discounts/{extra.Id}");
    }
    await Client.GetAsync("/api/discounts");
}
```

**API H2**:

```csharp
/// <summary>
/// Создаёт скидку, активирует, деактивирует, обновляет через API.
/// </summary>
[Theory]
[MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
public async Task CreateActivateDeactivateUpdate_AllPersist(int _)
{
    var created = await CreateDiscountAsync("START10", 10);
    Assert.False(created.IsActive);

    Assert.Equal(HttpStatusCode.NoContent, (await Client.PostAsync($"/api/discounts/{created.Id}/activate", null)).StatusCode);
    var activated = await (await Client.GetAsync($"/api/discounts/{created.Id}")).Content.ReadFromJsonAsync<DiscountDto>();
    Assert.True(activated!.IsActive);

    Assert.Equal(HttpStatusCode.NoContent, (await Client.PostAsync($"/api/discounts/{created.Id}/deactivate", null)).StatusCode);

    var putResp = await Client.PutAsJsonAsync($"/api/discounts/{created.Id}",
        new UpdateDiscountRequest { Code = "FINISH25", DiscountPercent = 25 });
    Assert.Equal(HttpStatusCode.OK, putResp.StatusCode);

    var fetched = await (await Client.GetAsync($"/api/discounts/{created.Id}")).Content.ReadFromJsonAsync<DiscountDto>();
    Assert.Equal("FINISH25", fetched!.Code);
    Assert.False(fetched.IsActive);

    // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
    for (var i = 0; i < 3; i++)
    {
        var extra = await CreateDiscountAsync($"PAD{i:00}", 5 + i);
        await Client.PostAsync($"/api/discounts/{extra.Id}/activate", null);
        await Client.GetAsync($"/api/discounts/{extra.Id}");
    }
    await Client.GetAsync("/api/discounts");
}
```

### Steps

- [ ] Добавить Service H1 и H2 в `DiscountServiceCrTests.cs` и `DiscountServiceUdTests.cs` (IntegreSQL)
- [ ] Добавить API H1 и H2 в `DiscountsApiCrTests.cs` и `DiscountsApiUdTests.cs` (IntegreSQL)
- [ ] Добавить Service H1 и H2 в оба Respawn service файла Discounts
- [ ] Добавить API H1 и H2 в оба Respawn API файла Discounts
- [ ] Добавить Service H1 и H2 в оба Testcontainers service файла Discounts
- [ ] Добавить API H1 и H2 в оба Testcontainers API файла Discounts
- [ ] **Запустить тесты Discounts**
  ```
  dotnet test tests/FastIntegrationTests.Tests.IntegreSQL --filter "FullyQualifiedName~Discount"
  dotnet test tests/FastIntegrationTests.Tests.Respawn --filter "FullyQualifiedName~Discount"
  dotnet test tests/FastIntegrationTests.Tests.Testcontainers --filter "FullyQualifiedName~Discount"
  ```
- [ ] **Commit**: `git add tests/ && git commit -m "test: тяжёлые тесты для Discounts"`

---

## Task 6: Reviews

Reviews: Guid ID, нет `UpdateAsync`. Lifecycle: `ApproveAsync` (Pending → Approved), `RejectAsync` (Pending → Rejected). Нет уникального ограничения.

**Service H1**:

```csharp
/// <summary>
/// Создаёт несколько отзывов, проверяет GetAll и GetById каждого.
/// </summary>
[Theory]
[MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
public async Task CreateMultiple_GetAll_GetByIdEach_ReturnsConsistentData(int _)
{
    var a = await Sut.CreateAsync(new CreateReviewRequest { Title = "Отлично", Body = "Всё понравилось", Rating = 5 });
    var b = await Sut.CreateAsync(new CreateReviewRequest { Title = "Хорошо", Body = "В целом ок", Rating = 4 });
    var c = await Sut.CreateAsync(new CreateReviewRequest { Title = "Средне", Body = "Бывало лучше", Rating = 3 });

    var all = await Sut.GetAllAsync();
    Assert.Equal(3, all.Count);
    Assert.Equal("Отлично", (await Sut.GetByIdAsync(a.Id)).Title);
    Assert.Equal("Хорошо", (await Sut.GetByIdAsync(b.Id)).Title);
    Assert.Equal("Средне", (await Sut.GetByIdAsync(c.Id)).Title);

    // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
    for (var i = 0; i < 4; i++)
    {
        var extra = await Sut.CreateAsync(new CreateReviewRequest { Title = $"Отзыв {i}", Body = "Текст", Rating = 3 + i % 3 });
        await Sut.GetByIdAsync(extra.Id);
    }
    await Sut.GetAllAsync();
}
```

**Service H2**:

```csharp
/// <summary>
/// Создаёт два отзыва, один одобряет, второй отклоняет, проверяет статусы, удаляет первый.
/// </summary>
[Theory]
[MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
public async Task CreateApproveReject_ThenDelete_LifecycleCorrect(int _)
{
    var toApprove = await Sut.CreateAsync(new CreateReviewRequest { Title = "Одобрить", Body = "Хороший отзыв", Rating = 5 });
    var toReject = await Sut.CreateAsync(new CreateReviewRequest { Title = "Отклонить", Body = "Плохой отзыв", Rating = 1 });

    var approved = await Sut.ApproveAsync(toApprove.Id);
    Assert.Equal(ReviewStatus.Approved, approved.Status);

    var rejected = await Sut.RejectAsync(toReject.Id);
    Assert.Equal(ReviewStatus.Rejected, rejected.Status);

    var fetchedApproved = await Sut.GetByIdAsync(toApprove.Id);
    Assert.Equal(ReviewStatus.Approved, fetchedApproved.Status);

    await Sut.DeleteAsync(toApprove.Id);
    await Assert.ThrowsAsync<NotFoundException>(() => Sut.GetByIdAsync(toApprove.Id));

    // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
    for (var i = 0; i < 4; i++)
    {
        var extra = await Sut.CreateAsync(new CreateReviewRequest { Title = $"Доп {i}", Body = "Текст", Rating = 4 });
        await Sut.ApproveAsync(extra.Id);
        await Sut.GetByIdAsync(extra.Id);
    }
    await Sut.GetAllAsync();
}
```

**API H1**:

```csharp
/// <summary>
/// Создаёт несколько отзывов через API, проверяет GetAll и GetById каждого.
/// </summary>
[Theory]
[MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
public async Task CreateMultiple_GetAll_GetByIdEach_ReturnsConsistentData(int _)
{
    var a = await CreateReviewAsync("Отлично", 5);
    var b = await CreateReviewAsync("Хорошо", 4);
    var c = await CreateReviewAsync("Средне", 3);

    var all = await Client.GetAsync("/api/reviews");
    var list = await all.Content.ReadFromJsonAsync<List<ReviewDto>>();
    Assert.Equal(3, list!.Count);

    var fa = await (await Client.GetAsync($"/api/reviews/{a.Id}")).Content.ReadFromJsonAsync<ReviewDto>();
    Assert.Equal("Отлично", fa!.Title);
    Assert.Equal(ReviewStatus.Pending, fa.Status);

    // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
    for (var i = 0; i < 4; i++)
    {
        var extra = await CreateReviewAsync($"Отзыв {i}", 3 + i % 3);
        await Client.GetAsync($"/api/reviews/{extra.Id}");
    }
    await Client.GetAsync("/api/reviews");
}
```

**API H2**:

```csharp
/// <summary>
/// Создаёт два отзыва, один одобряет, второй отклоняет через API, первый удаляет.
/// </summary>
[Theory]
[MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
public async Task CreateApproveReject_ThenDelete_LifecycleCorrect(int _)
{
    var toApprove = await CreateReviewAsync("Одобрить", 5);
    var toReject = await CreateReviewAsync("Отклонить", 1);

    Assert.Equal(HttpStatusCode.NoContent, (await Client.PostAsync($"/api/reviews/{toApprove.Id}/approve", null)).StatusCode);
    Assert.Equal(HttpStatusCode.NoContent, (await Client.PostAsync($"/api/reviews/{toReject.Id}/reject", null)).StatusCode);

    var approved = await (await Client.GetAsync($"/api/reviews/{toApprove.Id}")).Content.ReadFromJsonAsync<ReviewDto>();
    Assert.Equal(ReviewStatus.Approved, approved!.Status);

    Assert.Equal(HttpStatusCode.NoContent, (await Client.DeleteAsync($"/api/reviews/{toApprove.Id}")).StatusCode);
    Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync($"/api/reviews/{toApprove.Id}")).StatusCode);

    // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
    for (var i = 0; i < 4; i++)
    {
        var extra = await CreateReviewAsync($"Доп {i}", 4);
        await Client.PostAsync($"/api/reviews/{extra.Id}/approve", null);
        await Client.GetAsync($"/api/reviews/{extra.Id}");
    }
    await Client.GetAsync("/api/reviews");
}
```

Если в API-классах Reviews нет хелпера `CreateReviewAsync`, добавь:
```csharp
private async Task<ReviewDto> CreateReviewAsync(string title, int rating, CancellationToken ct = default)
{
    var resp = await Client.PostAsJsonAsync("/api/reviews",
        new CreateReviewRequest { Title = title, Body = "Текст отзыва", Rating = rating }, ct);
    resp.EnsureSuccessStatusCode();
    return (await resp.Content.ReadFromJsonAsync<ReviewDto>(ct))!;
}
```

### Steps

- [ ] Добавить Service H1 и H2 в `ReviewServiceCrTests.cs` и `ReviewServiceUdTests.cs` (IntegreSQL)
- [ ] Добавить API H1 и H2 (+ хелпер если нужен) в `ReviewsApiCrTests.cs` и `ReviewsApiUdTests.cs` (IntegreSQL)
- [ ] Добавить Service H1 и H2 в оба Respawn service файла Reviews
- [ ] Добавить API H1 и H2 в оба Respawn API файла Reviews
- [ ] Добавить Service H1 и H2 в оба Testcontainers service файла Reviews
- [ ] Добавить API H1 и H2 в оба Testcontainers API файла Reviews
- [ ] **Запустить тесты Reviews**
  ```
  dotnet test tests/FastIntegrationTests.Tests.IntegreSQL --filter "FullyQualifiedName~Review"
  dotnet test tests/FastIntegrationTests.Tests.Respawn --filter "FullyQualifiedName~Review"
  dotnet test tests/FastIntegrationTests.Tests.Testcontainers --filter "FullyQualifiedName~Review"
  ```
- [ ] **Commit**: `git add tests/ && git commit -m "test: тяжёлые тесты для Reviews"`

---

## Task 7: Orders

Orders: int ID. Service tests имеют `_products` (IProductService) и `Sut` (IOrderService), инициализируемые в `InitializeAsync`. API тесты уже имеют хелперы `CreateProductAsync` и `CreateOrderWithProductAsync`.

**Service H1** — для всех 6 service файлов Orders:

```csharp
/// <summary>
/// Создаёт заказ с тремя позициями разных товаров — проверяет итоговую сумму и состав.
/// </summary>
[Theory]
[MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
public async Task MultiItemOrder_TotalAmountAndItemsCorrect(int _)
{
    var p1 = await _products.CreateAsync(new CreateProductRequest { Name = "Телефон", Price = 30_000m });
    var p2 = await _products.CreateAsync(new CreateProductRequest { Name = "Чехол", Price = 500m });
    var p3 = await _products.CreateAsync(new CreateProductRequest { Name = "Зарядка", Price = 1_500m });

    var order = await Sut.CreateAsync(new CreateOrderRequest
    {
        Items = new List<OrderItemRequest>
        {
            new() { ProductId = p1.Id, Quantity = 1 },  // 30_000
            new() { ProductId = p2.Id, Quantity = 2 },  // 1_000
            new() { ProductId = p3.Id, Quantity = 1 },  // 1_500
        }
    });

    Assert.Equal(32_500m, order.TotalAmount);
    Assert.Equal(3, order.Items.Count);

    var fetched = await Sut.GetByIdAsync(order.Id);
    Assert.Equal(32_500m, fetched.TotalAmount);
    Assert.Equal(3, fetched.Items.Count);

    // benchmark: искусственное увеличение продолжительності теста и объёма работы с БД
    var extraProduct = await _products.CreateAsync(new CreateProductRequest { Name = "Доп товар", Price = 100m });
    for (var i = 0; i < 3; i++)
    {
        var extra = await Sut.CreateAsync(new CreateOrderRequest
        {
            Items = new List<OrderItemRequest> { new() { ProductId = extraProduct.Id, Quantity = 1 } }
        });
        await Sut.GetByIdAsync(extra.Id);
    }
    await Sut.GetAllAsync();
}
```

**Service H2** — для всех 6 service файлов Orders:

```csharp
/// <summary>
/// Создаёт заказ с тремя позициями, проводит полный lifecycle, проверяет итоговую сумму и статус.
/// </summary>
[Theory]
[MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
public async Task MultiItemLifecycle_FullPath_TotalAmountAndStatusCorrect(int _)
{
    var p1 = await _products.CreateAsync(new CreateProductRequest { Name = "Ноутбук", Price = 50_000m });
    var p2 = await _products.CreateAsync(new CreateProductRequest { Name = "Мышь", Price = 2_000m });
    var p3 = await _products.CreateAsync(new CreateProductRequest { Name = "Клавиатура", Price = 3_000m });

    var order = await Sut.CreateAsync(new CreateOrderRequest
    {
        Items = new List<OrderItemRequest>
        {
            new() { ProductId = p1.Id, Quantity = 1 },  // 50_000
            new() { ProductId = p2.Id, Quantity = 1 },  // 2_000
            new() { ProductId = p3.Id, Quantity = 2 },  // 6_000
        }
    });

    // 50_000 + 2_000 + 3_000 * 2 = 58_000
    Assert.Equal(58_000m, order.TotalAmount);
    Assert.Equal(OrderStatus.New, order.Status);

    await Sut.ConfirmAsync(order.Id);
    await Sut.ShipAsync(order.Id);
    var completed = await Sut.CompleteAsync(order.Id);
    Assert.Equal(OrderStatus.Completed, completed.Status);

    var fetched = await Sut.GetByIdAsync(order.Id);
    Assert.Equal(OrderStatus.Completed, fetched.Status);
    Assert.Equal(58_000m, fetched.TotalAmount);
    Assert.Equal(3, fetched.Items.Count);
}
```

**API H1** — для всех 6 API файлов Orders:

```csharp
/// <summary>
/// Создаёт заказ с тремя позициями через API — проверяет итоговую сумму и состав.
/// </summary>
[Theory]
[MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
public async Task MultiItemOrder_TotalAmountAndItemsCorrect(int _)
{
    var p1 = await CreateProductAsync("Телефон", 30_000m);
    var p2 = await CreateProductAsync("Чехол", 500m);
    var p3 = await CreateProductAsync("Зарядка", 1_500m);

    var createResp = await Client.PostAsJsonAsync("/api/orders", new CreateOrderRequest
    {
        Items = new List<OrderItemRequest>
        {
            new() { ProductId = p1.Id, Quantity = 1 },
            new() { ProductId = p2.Id, Quantity = 2 },
            new() { ProductId = p3.Id, Quantity = 1 },
        }
    });
    Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
    var order = await createResp.Content.ReadFromJsonAsync<OrderDto>();
    Assert.Equal(32_500m, order!.TotalAmount);
    Assert.Equal(3, order.Items.Count);

    var fetched = await (await Client.GetAsync($"/api/orders/{order.Id}")).Content.ReadFromJsonAsync<OrderDto>();
    Assert.Equal(32_500m, fetched!.TotalAmount);

    // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
    var extraProduct = await CreateProductAsync("Доп товар", 100m);
    for (var i = 0; i < 3; i++)
    {
        var extra = await Client.PostAsJsonAsync("/api/orders", new CreateOrderRequest
        {
            Items = new List<OrderItemRequest> { new() { ProductId = extraProduct.Id, Quantity = 1 } }
        });
        var extraOrder = await extra.Content.ReadFromJsonAsync<OrderDto>();
        await Client.GetAsync($"/api/orders/{extraOrder!.Id}");
    }
    await Client.GetAsync("/api/orders");
}
```

**API H2** — для всех 6 API файлов Orders:

```csharp
/// <summary>
/// Создаёт заказ с тремя позициями, проводит полный lifecycle через API.
/// </summary>
[Theory]
[MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
public async Task MultiItemLifecycle_FullPath_TotalAmountAndStatusCorrect(int _)
{
    var p1 = await CreateProductAsync("Ноутбук", 50_000m);
    var p2 = await CreateProductAsync("Мышь", 2_000m);
    var p3 = await CreateProductAsync("Клавиатура", 3_000m);

    var createResp = await Client.PostAsJsonAsync("/api/orders", new CreateOrderRequest
    {
        Items = new List<OrderItemRequest>
        {
            new() { ProductId = p1.Id, Quantity = 1 },
            new() { ProductId = p2.Id, Quantity = 1 },
            new() { ProductId = p3.Id, Quantity = 2 },
        }
    });
    var order = await createResp.Content.ReadFromJsonAsync<OrderDto>();
    Assert.Equal(58_000m, order!.TotalAmount);

    Assert.Equal(HttpStatusCode.OK, (await Client.PostAsync($"/api/orders/{order.Id}/confirm", null)).StatusCode);
    Assert.Equal(HttpStatusCode.OK, (await Client.PostAsync($"/api/orders/{order.Id}/ship", null)).StatusCode);
    var completedResp = await Client.PostAsync($"/api/orders/{order.Id}/complete", null);
    Assert.Equal(HttpStatusCode.OK, completedResp.StatusCode);
    var completed = await completedResp.Content.ReadFromJsonAsync<OrderDto>();
    Assert.Equal(OrderStatus.Completed, completed!.Status);

    var fetched = await (await Client.GetAsync($"/api/orders/{order.Id}")).Content.ReadFromJsonAsync<OrderDto>();
    Assert.Equal(OrderStatus.Completed, fetched!.Status);
    Assert.Equal(58_000m, fetched.TotalAmount);
    Assert.Equal(3, fetched.Items.Count);
}
```

Примечание для Testcontainers service Orders: эти классы наследуют `ContainerServiceTestBase` и используют `IOrderService Sut => OrderService;` и `IProductService _products => ProductService;`... Проверь есть ли `_products` в этих классах. Если нет — добавь `private IProductService _products => ProductService;` в класс перед методами.

### Steps

- [ ] Добавить Service H1 и H2 в `OrderServiceCrTests.cs` и `OrderServiceUdTests.cs` (IntegreSQL)
- [ ] Добавить API H1 и H2 в `OrdersApiCrTests.cs` и `OrdersApiUdTests.cs` (IntegreSQL)
- [ ] Добавить Service H1 и H2 в `OrderServiceCrRespawnTests.cs` и `OrderServiceUdRespawnTests.cs` (Respawn)
- [ ] Добавить API H1 и H2 в `OrdersApiCrRespawnTests.cs` и `OrdersApiUdRespawnTests.cs` (Respawn)
- [ ] Добавить Service H1 и H2 в `OrderServiceCrContainerTests.cs` и `OrderServiceUdContainerTests.cs` (Testcontainers)
  Проверить наличие `_products`. Если отсутствует: добавить `private IProductService _products => ProductService;` в класс.
- [ ] Добавить API H1 и H2 в `OrdersApiCrContainerTests.cs` и `OrdersApiUdContainerTests.cs` (Testcontainers)
- [ ] **Запустить тесты Orders**
  ```
  dotnet test tests/FastIntegrationTests.Tests.IntegreSQL --filter "FullyQualifiedName~Order"
  dotnet test tests/FastIntegrationTests.Tests.Respawn --filter "FullyQualifiedName~Order"
  dotnet test tests/FastIntegrationTests.Tests.Testcontainers --filter "FullyQualifiedName~Order"
  ```
- [ ] **Commit**: `git add tests/ && git commit -m "test: тяжёлые тесты для Orders"`
