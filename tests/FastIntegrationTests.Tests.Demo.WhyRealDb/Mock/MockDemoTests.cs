namespace FastIntegrationTests.Tests.Demo.WhyRealDb.Mock;

/// <summary>
/// Демонстрация: подмена репозитория моком (Moq) убирает БД целиком.
/// У мока нет состояния между вызовами и он не выполняет ни одного запроса — поэтому
/// «проверки», построенные на моках, могут быть фиктивными. Каждый тест намеренно КРАСНЫЙ:
/// мок настроен наивно (как сделал бы разработчик), а ассерт требует исход, который
/// гарантировала бы реальная БД.
/// Зелёные эквиваленты — в tests/FastIntegrationTests.Tests.IntegreSQL/.
/// </summary>
public class MockDemoTests
{
    /// <summary>
    /// (1) Что проверяем: после Update значение видно при последующем GetById (read-after-write).
    /// (2) Postgres: Update пишет строку, GetById читает её же — изменение видно. Зелёный.
    /// (3) Что не так у мока: у мока нет общего состояния между вызовами. GetById застаблен
    ///     возвращать исходные данные и ничего не знает о произошедшем Update.
    /// (4) Почему красный: GetById возвращает старое имя, ассерт нового имени падает.
    /// (5) Зелёный эквивалент: Products/ProductServiceTests.UpdateAsync_*.
    /// </summary>
    [Fact]
    [Trait("Category", "Demo")]
    public async Task Mock_HasNoState_ReadAfterWriteIsLost()
    {
        var repo = new Mock<IProductRepository>();
        // Наивный стаб: GetById всегда возвращает СВЕЖИЙ исходный объект (как «состояние в БД»).
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new Product { Id = 1, Name = "Старое имя", Description = "", Price = 10m });
        // UpdateAsync — пустой стаб (по умолчанию ничего не делает): «изменение» некуда сохранять.

        var sut = new ProductService(repo.Object);

        await sut.UpdateAsync(1, new UpdateProductRequest { Name = "Новое имя", Description = "", Price = 20m });
        var read = await sut.GetByIdAsync(1);

        // Postgres вернул бы "Новое имя"; мок не сохранил изменение → красный.
        Assert.Equal("Новое имя", read.Name);
    }

    /// <summary>
    /// (1) Что проверяем: OrderService.GetById отдаёт заказ ВМЕСТЕ с позициями.
    /// (2) Postgres: репозиторий делает .Include(o => o.Items), позиции приходят. Зелёный.
    /// (3) Что не так у мока: мок возвращает ту форму, которую задал разработчик. Здесь он
    ///     застаблен так, как повёл бы себя реальный репозиторий, потерявший .Include — с
    ///     пустыми Items. Мок не выполняет JOIN, поэтому форму запроса он не проверяет.
    /// (4) Почему красный: Items пустой, ассерт наличия позиций падает.
    /// (5) Зелёный эквивалент: Orders/OrderServiceTests.GetByIdAsync_*.
    /// Урок: мок = ТВОЁ представление о запросе, а не сам запрос. Регрессию .Include мок не ловит.
    /// </summary>
    [Fact]
    [Trait("Category", "Demo")]
    public async Task Mock_DoesNotExecuteInclude_ItemsContractDrifts()
    {
        var orderRepo = new Mock<IOrderRepository>();
        var productRepo = new Mock<IProductRepository>();
        // Застаблено как репозиторий, забывший .Include: заказ без позиций.
        orderRepo.Setup(r => r.GetByIdWithItemsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Order
            {
                Id = 1,
                Status = OrderStatus.New,
                TotalAmount = 100m,
                Items = new List<OrderItem>(), // .Include «потерялся»
            });

        var sut = new OrderService(orderRepo.Object, productRepo.Object);

        var dto = await sut.GetByIdAsync(1);

        // Реальный .Include вернул бы позиции → красный.
        Assert.NotEmpty(dto.Items);
    }

    /// <summary>
    /// (1) Что проверяем: список товаров приходит отсортированным по имени (контракт ORDER BY).
    /// (2) Postgres: репозиторий выполняет ORDER BY Name, порядок гарантирован. Зелёный.
    /// (3) Что не так у мока: мок возвращает заранее заданный список КАК ЕСТЬ. Он не выполняет
    ///     ORDER BY, поэтому баг в сортировке репозитория через мок не виден.
    /// (4) Почему красный: первым в списке идёт "Банан", ассерт "Апельсин" первым падает.
    /// (5) Зелёный эквивалент: Products/ProductServiceTests.GetAllAsync_*.
    /// </summary>
    [Fact]
    [Trait("Category", "Demo")]
    public async Task Mock_DoesNotExecuteOrderBy_SortContractIsFake()
    {
        var repo = new Mock<IProductRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>
            {
                new() { Id = 1, Name = "Банан", Description = "", Price = 1m },
                new() { Id = 2, Name = "Апельсин", Description = "", Price = 2m },
            });

        var sut = new ProductService(repo.Object);

        var all = await sut.GetAllAsync();

        // ORDER BY Name дал бы "Апельсин" первым; мок не сортирует → красный.
        Assert.Equal("Апельсин", all[0].Name);
    }

    /// <summary>
    /// (1) Что проверяем: после Create товар получает присвоенный БД идентификатор (Id > 0).
    /// (2) Postgres: INSERT присваивает identity/serial, AddAsync возвращает сущность с Id > 0. Зелёный.
    /// (3) Что не так у мока: AddAsync застаблен возвращать переданный объект как есть. БД нет —
    ///     identity никто не присваивает, Id остаётся 0.
    /// (4) Почему красный: dto.Id == 0, ассерт Id > 0 падает.
    /// (5) Зелёный эквивалент: Products/ProductServiceTests.CreateAsync_*AssignedId.
    /// </summary>
    [Fact]
    [Trait("Category", "Demo")]
    public async Task Mock_HasNoIdentity_CreatedEntityHasZeroId()
    {
        var repo = new Mock<IProductRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product p, CancellationToken _) => p); // Id остаётся 0

        var sut = new ProductService(repo.Object);

        var dto = await sut.CreateAsync(new CreateProductRequest { Name = "Мышь", Description = "", Price = 100m });

        // БД присвоила бы Id > 0; мок вернул объект с Id == 0 → красный.
        Assert.True(dto.Id > 0);
    }

    /// <summary>
    /// (1) Что проверяем: нельзя создать двух покупателей с одинаковым email (уникальность).
    /// (2) Postgres: UNIQUE-индекс на Email отвергает второй INSERT; сервис ловит это раньше через
    ///     ExistsByEmailAsync, который видит реальные строки. Зелёный (бросает DuplicateValueException).
    /// (3) Что не так у мока: у мока нет данных. ExistsByEmailAsync наивно застаблен в false (как
    ///     если бы разработчик «не думал про дубли»), поэтому сервис пропускает обоих.
    /// (4) Почему красный: второй CreateAsync НЕ бросает DuplicateValueException, Assert.ThrowsAsync падает.
    /// (5) Зелёный эквивалент: Customers/CustomerServiceTests.CreateAsync_*DuplicateEmail.
    /// Примечание: стоит вместо «атомарности» из спеки — в текущем коде нет сервиса с двумя
    /// последовательными записями, на котором можно показать откат. Урок тот же: у фейка нет БД-инвариантов.
    /// </summary>
    [Fact]
    [Trait("Category", "Demo")]
    public async Task Mock_HasNoState_DuplicateEmailSlipsThrough()
    {
        var repo = new Mock<ICustomerRepository>();
        repo.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // мок не знает про уже созданных покупателей
        repo.Setup(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer c, CancellationToken _) => c);

        var sut = new CustomerService(repo.Object);

        await sut.CreateAsync(new CreateCustomerRequest { Name = "A", Email = "dup@example.com" });

        // Второй покупатель с тем же email должен быть отвергнут; мок не хранит состояние → красный.
        await Assert.ThrowsAsync<DuplicateValueException>(() =>
            sut.CreateAsync(new CreateCustomerRequest { Name = "B", Email = "dup@example.com" }));
    }
}
