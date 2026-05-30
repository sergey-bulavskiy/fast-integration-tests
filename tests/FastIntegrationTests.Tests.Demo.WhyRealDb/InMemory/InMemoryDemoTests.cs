namespace FastIntegrationTests.Tests.Demo.WhyRealDb.InMemory;

/// <summary>
/// Демонстрация: EF Core InMemory — это словарь объектов, а не реляционная БД.
/// UNIQUE, FK, delete-behavior, транзакции он не enforce'ит, raw SQL не выполняет, а
/// нетранслируемые предикаты молча считает в памяти. Каждый тест намеренно КРАСНЫЙ.
/// Зелёные эквиваленты — в tests/FastIntegrationTests.Tests.IntegreSQL/.
/// </summary>
public class InMemoryDemoTests : InMemoryDemoBase
{
    /// <summary>
    /// (1) Что проверяем: нельзя сохранить двух покупателей с одинаковым email.
    /// (2) Postgres: UNIQUE-индекс на Email (HasIndex().IsUnique()) отвергает второй INSERT → DbUpdateException.
    /// (3) Что не так у InMemory: уникальные индексы не enforce'ятся — оба объекта сохраняются.
    /// (4) Почему красный: SaveChanges не бросает, Assert.Throws&lt;DbUpdateException&gt; не дожидается исключения.
    /// (5) Зелёный эквивалент: Customers/CustomerServiceTests (дубликат email).
    /// </summary>
    [Fact]
    [Trait("Category", "Demo")]
    public void InMemory_DoesNotEnforce_UniqueEmail()
    {
        using var context = CreateContext();
        context.Customers.Add(new Customer { Id = Guid.NewGuid(), Name = "A", Email = "dup@example.com", Status = CustomerStatus.Active, CreatedAt = DateTime.UtcNow });
        context.Customers.Add(new Customer { Id = Guid.NewGuid(), Name = "B", Email = "dup@example.com", Status = CustomerStatus.Active, CreatedAt = DateTime.UtcNow });

        // Postgres: нарушение UNIQUE → InMemory не проверяет → красный.
        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }

    /// <summary>
    /// (1) Что проверяем: нельзя создать позицию заказа со ссылкой на несуществующий товар.
    /// (2) Postgres: FK OrderItem.ProductId → Products(Id) отвергает «призрачный» ProductId → DbUpdateException.
    /// (3) Что не так у InMemory: внешние ключи не enforce'ятся — позиция с ProductId=999999 сохраняется.
    /// (4) Почему красный: SaveChanges не бросает, Assert.Throws не дожидается исключения.
    /// (5) Зелёный эквивалент: Orders/OrderServiceTests (создание заказа с несуществующим товаром).
    /// </summary>
    [Fact]
    [Trait("Category", "Demo")]
    public void InMemory_DoesNotEnforce_ForeignKey_OrderItemWithGhostProduct()
    {
        using var context = CreateContext();
        var order = new Order
        {
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.New,
            TotalAmount = 10m,
            Items = { new OrderItem { ProductId = 999999, Quantity = 1, UnitPrice = 10m } },
        };
        context.Orders.Add(order);

        // Postgres: FK violation → InMemory не проверяет → красный.
        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }

    /// <summary>
    /// (1) Что проверяем: товар, на который ссылается позиция заказа, нельзя удалить (Restrict).
    /// (2) Postgres: OrderItem→Product настроен OnDelete(Restrict) → удаление товара со ссылками → DbUpdateException.
    /// (3) Что не так у InMemory: delete-behavior не enforce'ится — товар удаляется молча.
    /// (4) Почему красный: SaveChanges не бросает, Assert.Throws не дожидается исключения.
    /// (5) Зелёный эквивалент: Products/ProductServiceTests (удаление товара со ссылками).
    /// Примечание 1: удаляем через context.Remove, а не через ProductRepository.DeleteAsync —
    /// тот использует ExecuteDeleteAsync, не поддерживаемый InMemory (бросил бы не то исключение).
    /// Примечание 2: данные сохраняются и загружаются в разных контекстах — иначе ChangeTracker
    /// InMemory бросает InvalidOperationException на Remove из-за отслеживания связанных сущностей
    /// (это артефакт ChangeTracker, не Restrict, и произошло бы до SaveChanges в неправильном месте).
    /// </summary>
    [Fact]
    [Trait("Category", "Demo")]
    public void InMemory_DoesNotEnforce_RestrictOnDelete()
    {
        // Shared in-memory database so two contexts see the same data.
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ShopDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        int productId;
        using (var setupCtx = new ShopDbContext(options))
        {
            setupCtx.Database.EnsureCreated();
            var product = new Product { Name = "Товар", Description = "", Price = 10m, CreatedAt = DateTime.UtcNow };
            setupCtx.Products.Add(product);
            setupCtx.SaveChanges();
            productId = product.Id;

            setupCtx.Orders.Add(new Order
            {
                CreatedAt = DateTime.UtcNow,
                Status = OrderStatus.New,
                TotalAmount = 10m,
                Items = { new OrderItem { ProductId = productId, Quantity = 1, UnitPrice = 10m } },
            });
            setupCtx.SaveChanges();
        }

        // Fresh context: ChangeTracker не знает об OrderItem, поэтому Remove не вызовет
        // каскадную проверку ChangeTracker. Postgres отклонил бы SaveChanges с DbUpdateException
        // из-за Restrict — InMemory молча удалит.
        using var deleteCtx = new ShopDbContext(options);
        var toDelete = deleteCtx.Products.Find(productId)!;
        deleteCtx.Products.Remove(toDelete);

        // Postgres: Restrict не даст удалить → InMemory удаляет молча → красный.
        Assert.Throws<DbUpdateException>(() => deleteCtx.SaveChanges());
    }

    /// <summary>
    /// (1) Что проверяем: изменения внутри транзакции откатываются при Rollback.
    /// (2) Postgres: BeginTransaction + SaveChanges + Rollback → строки нет.
    /// (3) Что не так у InMemory: транзакции — no-op. SaveChanges пишет сразу, Rollback ничего не откатывает.
    ///     (См. InMemoryDemoBase: TransactionIgnoredWarning переведён в Ignore, иначе BeginTransaction бросил бы.)
    /// (4) Почему красный: после Rollback покупатель на месте, ассерт count == 0 падает.
    /// (5) Зелёный эквивалент: любой тест с транзакционным откатом в Tests.IntegreSQL.
    /// </summary>
    [Fact]
    [Trait("Category", "Demo")]
    public void InMemory_DoesNotRollback_Transaction()
    {
        using var context = CreateContext();
        using (var tx = context.Database.BeginTransaction())
        {
            context.Customers.Add(new Customer { Id = Guid.NewGuid(), Name = "A", Email = "tx@example.com", Status = CustomerStatus.Active, CreatedAt = DateTime.UtcNow });
            context.SaveChanges();
            tx.Rollback();
        }

        context.ChangeTracker.Clear();
        var count = context.Customers.Count();

        // Postgres: после Rollback строки нет → InMemory сохранил → красный.
        Assert.Equal(0, count);
    }

    /// <summary>
    /// (1) Что проверяем: регистронезависимый поиск через raw SQL (ILIKE).
    /// (2) Postgres: FromSqlRaw с ILIKE выполняется, "Apple" находится по "%apple%". Зелёный.
    /// (3) Что не так у InMemory: FromSqlRaw не поддерживается — провайдер не реляционный.
    /// (4) Почему красный: .ToList() бросает InvalidOperationException ещё до ассерта → тест падает.
    /// (5) Зелёный эквивалент: тесты поиска товаров в Tests.IntegreSQL.
    /// </summary>
    [Fact]
    [Trait("Category", "Demo")]
    public void InMemory_DoesNotSupport_RawSql_Ilike()
    {
        using var context = CreateContext();
        context.Products.Add(new Product { Name = "Apple", Description = "", Price = 1m, CreatedAt = DateTime.UtcNow });
        context.SaveChanges();

        // Postgres выполнил бы ILIKE; InMemory бросает на FromSqlRaw → красный.
        var found = context.Products
            .FromSqlRaw("SELECT * FROM \"Products\" WHERE \"Name\" ILIKE '%apple%'")
            .ToList();

        Assert.Single(found);
    }

    /// <summary>
    /// (1) Что проверяем: нетранслируемый предикат (вызов локального метода в Where) на реальной БД
    ///     отвергается с «could not be translated».
    /// (2) Postgres (реляционный провайдер): такой Where бросает InvalidOperationException на ToList().
    /// (3) Что не так у InMemory: он молча считает предикат в памяти (client-eval) и возвращает результат —
    ///     опасная иллюзия «запрос работает».
    /// (4) Почему красный: InMemory НЕ бросает, Assert.Throws&lt;InvalidOperationException&gt; не дожидается исключения.
    /// (5) Зелёный эквивалент: на реальной БД этот запрос упал бы — здесь демонстрируем расхождение.
    /// Урок: InMemory молча выполняет запрос, который настоящая БД отвергает.
    /// </summary>
    [Fact]
    [Trait("Category", "Demo")]
    public void InMemory_SilentlyRuns_NonTranslatablePredicate()
    {
        using var context = CreateContext();
        context.Products.Add(new Product { Name = "X", Description = "", Price = 150m, CreatedAt = DateTime.UtcNow });
        context.SaveChanges();

        // Реляционный провайдер бросил бы «could not be translated»; InMemory считает в памяти → красный.
        Assert.Throws<InvalidOperationException>(() =>
            context.Products.Where(p => IsExpensive(p.Price)).ToList());
    }

    // Локальный метод нельзя транслировать в SQL — реляционный провайдер бросит, InMemory выполнит client-side.
    private static bool IsExpensive(decimal price) => price > 100m;
}
