namespace FastIntegrationTests.Application.Services;

/// <summary>
/// Сервис для управления товарами каталога.
/// </summary>
public class ProductService : IProductService
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
    /// <param name="ct">Токен отмены операции.</param>
    public async Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken ct = default)
    {
        var products = await _repository.GetAllAsync(ct);
        return products.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Возвращает товар по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор товара.</param>
    /// <param name="ct">Токен отмены операции.</param>
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
    /// <param name="ct">Токен отмены операции.</param>
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
    /// <param name="ct">Токен отмены операции.</param>
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
    /// <param name="ct">Токен отмены операции.</param>
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
