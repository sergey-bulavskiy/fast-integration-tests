using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FastIntegrationTests.Tests.Demo.WhyRealDb.InMemory;

/// <summary>
/// Базовый класс для демо-тестов на EF Core InMemory.
/// Поднимает настоящий <see cref="ShopDbContext"/> на in-memory провайдере и строит
/// схему через <see cref="DatabaseFacade.EnsureCreated"/> — то есть из EF-модели
/// (конфигураций <c>IEntityTypeConfiguration</c>), БЕЗ raw-SQL миграций.
/// </summary>
public abstract class InMemoryDemoBase
{
    /// <summary>
    /// Создаёт свежий контекст на изолированной in-memory БД (уникальное имя на вызов).
    /// </summary>
    /// <remarks>
    /// <see cref="InMemoryEventId.TransactionIgnoredWarning"/> переведён в Ignore намеренно:
    /// по умолчанию InMemory БРОСАЕТ исключение при вызове <c>BeginTransaction</c>. Чтобы
    /// продемонстрировать, что транзакция на InMemory — это no-op (а не ошибка), мы
    /// подавляем предупреждение и получаем «пустую» транзакцию, чей Rollback ничего не делает.
    /// </remarks>
    protected ShopDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ShopDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var context = new ShopDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
