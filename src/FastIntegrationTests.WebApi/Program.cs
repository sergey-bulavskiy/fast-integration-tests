using FastIntegrationTests.WebApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Регистрируем сервисы бизнес-логики
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();

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

if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddPostgresql(connStr);
else if (provider.Equals("MSSQL", StringComparison.OrdinalIgnoreCase))
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

app.UseHttpsRedirection();
app.UseExceptionHandler();
app.MapControllers();
app.Run();
