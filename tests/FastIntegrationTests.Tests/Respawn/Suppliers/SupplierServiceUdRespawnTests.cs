namespace FastIntegrationTests.Tests.Respawn.Suppliers;

/// <summary>
/// Тесты сервисного уровня: Create (ошибки), Update, Delete, Activate, Deactivate для SupplierService.
/// Каждый тест сбрасывает данные через Respawn (~1 мс), схема сохраняется.
/// </summary>
public class SupplierServiceUdRespawnTests : RespawnServiceTestBase
{
    private ISupplierService Sut = null!;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="SupplierServiceUdRespawnTests"/>.
    /// </summary>
    /// <param name="fixture">Фикстура с контейнером и Respawner.</param>
    public SupplierServiceUdRespawnTests(RespawnFixture fixture) : base(fixture) { }

    /// <inheritdoc/>
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        Sut = new SupplierService(new SupplierRepository(Context));
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task CreateAsync_WhenDuplicateEmail_ThrowsDuplicateValueException(int _)
    {
        await Sut.CreateAsync(new CreateSupplierRequest { Name = "ООО Альфа", ContactEmail = "dup@vendor.com", Country = "Россия" });

        await Assert.ThrowsAsync<DuplicateValueException>(
            () => Sut.CreateAsync(new CreateSupplierRequest { Name = "Другой", ContactEmail = "dup@vendor.com", Country = "Беларусь" }));
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task UpdateAsync_UpdatesFields(int _)
    {
        var created = await Sut.CreateAsync(new CreateSupplierRequest { Name = "Старое", ContactEmail = "old@vendor.com", Country = "Россия" });

        var updated = await Sut.UpdateAsync(created.Id, new UpdateSupplierRequest { Name = "Новое", ContactEmail = "new@vendor.com", Country = "Казахстан" });

        Assert.Equal("Новое", updated.Name);
        Assert.Equal("new@vendor.com", updated.ContactEmail);
        Assert.Equal("Казахстан", updated.Country);

        var fetched = await Sut.GetByIdAsync(created.Id);
        Assert.Equal("Новое", fetched.Name);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task UpdateAsync_WhenNotFound_ThrowsNotFoundException(int _)
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => Sut.UpdateAsync(Guid.NewGuid(), new UpdateSupplierRequest { Name = "Любое", ContactEmail = "any@vendor.com", Country = "Россия" }));
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task DeleteAsync_RemovesSupplier(int _)
    {
        var created = await Sut.CreateAsync(new CreateSupplierRequest { Name = "Удаляемый", ContactEmail = "del@vendor.com", Country = "Россия" });

        await Sut.DeleteAsync(created.Id);

        await Assert.ThrowsAsync<NotFoundException>(() => Sut.GetByIdAsync(created.Id));
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task ActivateAsync_ActivatesSupplier(int _)
    {
        var created = await Sut.CreateAsync(new CreateSupplierRequest { Name = "Неактивный", ContactEmail = "inactive@vendor.com", Country = "Россия" });
        await Sut.DeactivateAsync(created.Id);

        var activated = await Sut.ActivateAsync(created.Id);

        Assert.True(activated.IsActive);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task DeactivateAsync_DeactivatesSupplier(int _)
    {
        var created = await Sut.CreateAsync(new CreateSupplierRequest { Name = "Активный", ContactEmail = "active@vendor.com", Country = "Россия" });

        var deactivated = await Sut.DeactivateAsync(created.Id);

        Assert.False(deactivated.IsActive);
    }
}
