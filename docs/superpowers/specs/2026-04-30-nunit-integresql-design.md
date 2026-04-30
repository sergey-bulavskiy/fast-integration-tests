# NUnit-пример инфраструктуры IntegreSQL — Design Spec

**Дата:** 2026-04-30
**Цель:** Добавить мини-проект `FastIntegrationTests.Tests.NUnit.IntegreSQL` с примером того, как переносится IntegreSQL-инфраструктура с xUnit на NUnit. Назначение — учебное, для команд, которые сидят на NUnit и хотят увидеть рабочий шаблон.

---

## Контекст

Все три существующих подхода (IntegreSQL, Respawn, Testcontainers) реализованы только под xUnit. У NUnit принципиально другая модель жизненного цикла фикстур (`[SetUp]`/`[TearDown]` вместо `IAsyncLifetime`, один инстанс класса вместо инстанса на каждый тест) и параллелизма (`[Parallelizable]` + `LevelOfParallelism` вместо `xunit.runner.json`). Команды на NUnit сейчас не имеют готового примера, как ту же инфру повторить у себя.

Полное зеркалирование на 195 тестов не оправдано — цель не альтернативный стек, а демонстрация маппинга. Поэтому минимальный набор: одна сущность (Products), два уровня (Service + Api), ~24 теста.

---

## Раздел 1 — Структура нового проекта

```
tests/FastIntegrationTests.Tests.NUnit.IntegreSQL/
├── FastIntegrationTests.Tests.NUnit.IntegreSQL.csproj
├── GlobalUsings.cs
├── AssemblyInfo.cs
├── Infrastructure/Base/
│   ├── AppServiceTestBase.cs
│   └── ComponentTestBase.cs
└── Products/
    ├── ProductServiceTests.cs
    └── ProductsApiTests.cs
```

`csproj`:

- TargetFramework `net8.0`, Nullable + ImplicitUsings включены, `IsTestProject=true`.
- Пакеты: `NUnit` 4.x, `NUnit3TestAdapter` 4.x, `Microsoft.NET.Test.Sdk` 17.12.0, `coverlet.collector` 6.x.
- ProjectReference на `Application`, `Infrastructure`, `WebApi`, `Tests.Shared`.
- Никаких xUnit-пакетов, никакого `xunit.runner.json`.

`GlobalUsings.cs` повторяет xUnit-версию, но с заменой `Xunit` → `NUnit.Framework` и переносом IntegreSQL-инфраструктуры из `Tests.Shared`:

```csharp
global using System.Net;
global using System.Net.Http.Json;
global using FastIntegrationTests.Application.DTOs;
global using FastIntegrationTests.Application.Entities;
global using FastIntegrationTests.Application.Enums;
global using FastIntegrationTests.Application.Exceptions;
global using FastIntegrationTests.Application.Interfaces;
global using FastIntegrationTests.Application.Services;
global using FastIntegrationTests.Infrastructure.Data;
global using FastIntegrationTests.Infrastructure.Repositories;
global using FastIntegrationTests.Tests.Infrastructure;
global using FastIntegrationTests.Tests.Infrastructure.Base;
global using FastIntegrationTests.Tests.Infrastructure.WebApp;
global using Microsoft.EntityFrameworkCore;
global using NUnit.Framework;
```

---

## Раздел 2 — Рефакторинг `Tests.Shared`

Общая IntegreSQL-инфра фреймворк-агностична: `IntegresSqlContainerManager`, `IntegresSqlDefaults`, `IntegresSqlState` не используют ни одной строчки xUnit. Переезжают из `Tests.IntegreSQL/Infrastructure/IntegreSQL/` в `Tests.Shared/Infrastructure/IntegreSQL/` и становятся точкой переиспользования между xUnit- и NUnit-проектами.

`IntegresSqlDefaults` сейчас `internal` — становится `public`, чтобы наследники base-классов в обоих проектах могли использовать `IntegresSqlDefaults.SeedingOptions` (если потребуется в кастомном базовом классе). Прямого требования сейчас нет, но `internal` с переносом в чужую сборку перестаёт работать в NUnit-проекте при попытке расширить инфру.

Перенос NuGet-зависимостей в `Tests.Shared.csproj`:

- Добавить `MccSoft.IntegreSql.EF` 0.12.2 и `Testcontainers.PostgreSql` 4.4.0.
- В `Tests.IntegreSQL.csproj` эти же пакеты убрать — становятся транзитивными.

Чистка артефактов:

- Из `Tests.Shared.csproj` убрать `<PackageReference Include="xunit" />` — он там был для совместимости с прошлой структурой и сейчас не используется ни одним типом в Shared.
- Из `Tests.Shared/GlobalUsings.cs` убрать `global using Xunit;` по той же причине.

Эти две правки не влияют на работу xUnit-проекта (`Tests.IntegreSQL.csproj` сам тянет xUnit), но убирают мёртвую зависимость и делают Shared действительно фреймворк-агностичным.

---

## Раздел 3 — Базовые классы для NUnit

`AppServiceTestBase`:

```csharp
using FastIntegrationTests.Tests.Infrastructure.IntegreSQL;
using MccSoft.IntegreSql.EF;
using Npgsql;

namespace FastIntegrationTests.Tests.Infrastructure.Base;

/// <summary>
/// Базовый класс для сервисных интеграционных тестов через IntegreSQL на NUnit.
/// На каждый тест берётся новый клон шаблонной БД (~5 мс).
/// </summary>
public abstract class AppServiceTestBase
{
    private string _connectionString = null!;
    private NpgsqlDatabaseInitializer _initializer = null!;

    /// <summary>Контекст тестовой БД. Доступен после <see cref="BaseSetUp"/>.</summary>
    protected ShopDbContext Context { get; private set; } = null!;

    /// <summary>Запускает контейнеры (при первом вызове) и клонирует шаблон.</summary>
    [SetUp]
    public async Task BaseSetUp()
    {
        var state = await IntegresSqlContainerManager.GetStateAsync();
        _initializer = state.Initializer;
        _connectionString = await _initializer.CreateDatabaseGetConnectionString<ShopDbContext>(
            IntegresSqlDefaults.SeedingOptions);
        var options = new DbContextOptionsBuilder<ShopDbContext>()
            .UseNpgsql(_connectionString).Options;
        Context = new ShopDbContext(options);
    }

    /// <summary>Освобождает контекст и возвращает клонированную БД в пул.</summary>
    [TearDown]
    public async Task BaseTearDown()
    {
        await Context.DisposeAsync();
        await using var conn = new NpgsqlConnection(_connectionString);
        NpgsqlConnection.ClearPool(conn);
        await _initializer.RemoveDatabase(_connectionString);
    }
}
```

`ComponentTestBase` — аналогично, плюс создаёт `TestWebApplicationFactory` и `HttpClient` в `[SetUp]`, диспозит в `[TearDown]` (с тем же `try { ... } catch (NullReferenceException) { }` для `_factory.DisposeAsync()` — баг ASP.NET Core FileSystemWatcher остаётся актуальным).

В отличие от xUnit-версий: `BenchmarkLogger.Write("clone"|"reset", ms)` НЕ вызывается. NUnit-проект не участвует в бенчмарке, инструментирование лишнее.

**Поведение наследования в NUnit:** базовый `[SetUp]` вызывается перед производным, базовый `[TearDown]` — после. NUnit находит **все** `[SetUp]`-методы по иерархии через рефлексию и вызывает их в правильном порядке — никакого `override`/`virtual`. Производный класс просто объявляет свой `[SetUp]`-метод с другим именем для инициализации `Sut`-полей после того, как `Context` уже доступен.

---

## Раздел 4 — Тестовые классы

`Products/ProductServiceTests.cs` — порт `ProductServiceTests.cs` из xUnit-проекта 1:1 на 12 методов. Изменения по сравнению с оригиналом:

- Атрибут `[Fact]` → `[Test]`.
- Класс наследуется от `AppServiceTestBase`. Инициализация репозиториев/сервисов в `[SetUp]` с другим именем (например, `SetUpServices`):

  ```csharp
  [TestFixture]
  public class ProductServiceTests : AppServiceTestBase
  {
      private IProductService _sut = null!;
      private IOrderService _orders = null!;

      [SetUp]
      public void SetUpServices()
      {
          var productRepo = new ProductRepository(Context);
          var orderRepo = new OrderRepository(Context);
          _sut = new ProductService(productRepo);
          _orders = new OrderService(orderRepo, productRepo);
      }
      // ...
  }
  ```

- Ассерты в constraint model:

  | xUnit | NUnit |
  |---|---|
  | `Assert.Equal(a, b)` | `Assert.That(b, Is.EqualTo(a))` |
  | `Assert.Empty(c)` | `Assert.That(c, Is.Empty)` |
  | `Assert.True(x)` | `Assert.That(x, Is.True)` |
  | `Assert.InRange(v, lo, hi)` | `Assert.That(v, Is.InRange(lo, hi))` |
  | `Assert.ThrowsAsync<T>(() => ...)` | `Assert.ThatAsync(() => ..., Throws.TypeOf<T>())` |

`Products/ProductsApiTests.cs` — порт `ProductsApiTests.cs`, наследник `ComponentTestBase`. Хелпер `CreateProductAsync` (если есть в xUnit-версии) переносится как есть, его внутренние ассерты тоже переезжают на `Assert.That`.

---

## Раздел 5 — Параллелизм

`AssemblyInfo.cs`:

```csharp
using NUnit.Framework;

[assembly: Parallelizable(ParallelScope.Fixtures)]
[assembly: LevelOfParallelism(12)]

// Альтернатива: ParallelScope.All — параллелизм и внутри классов.
// IntegreSQL даёт изоляцию на уровне теста (каждый тест получает свой клон БД),
// поэтому ParallelScope.All валиден. Текущий выбор Fixtures соответствует
// поведению xUnit-версии (классы параллельно, тесты внутри — последовательно)
// и упрощает сравнение «один в один».
// [assembly: Parallelizable(ParallelScope.All)]
```

Закомментированная альтернатива оставлена намеренно как учебный момент: показывает, что NUnit-параллелизм управляется одним атрибутом, и переключение на тестовый параллелизм безопасно благодаря IntegreSQL.

---

## Раздел 6 — Документация

Обновляется только `CLAUDE.md`. В разделе про архитектуру/тестовые проекты добавляется блок:

> **Tests.NUnit.IntegreSQL** (`tests/FastIntegrationTests.Tests.NUnit.IntegreSQL/`) — учебный мини-проект для команд на NUnit. Содержит `ProductServiceTests` и `ProductsApiTests` (~24 теста), демонстрирующие маппинг xUnit-инфраструктуры IntegreSQL на NUnit:
>
> - `IAsyncLifetime` → `[SetUp]` / `[TearDown]`;
> - constraint-model ассерты (`Assert.That(value, Is.EqualTo(...))`);
> - `[Parallelizable]` + `LevelOfParallelism` на уровне assembly.
>
> Общая IntegreSQL-инфраструктура (`IntegresSqlContainerManager`, `IntegresSqlDefaults`, `IntegresSqlState`) живёт в `Tests.Shared/Infrastructure/IntegreSQL/` и переиспользуется между xUnit- и NUnit-проектами. В BenchmarkRunner и в PowerShell-скрипты NUnit-проект не включён — это пример, а не альтернативный стек.

`readme.md` не трогается — по решению пользователя.

Раздел «Идеи для развития бенчмарка» в `CLAUDE.md` (пункт «Инфраструктура для NUnit-тестов») остаётся как есть: текущий проект не закрывает идею полностью — нет ни NUnit-версий Respawn и Testcontainers, ни отдельной серии в бенчмарке.

---

## Раздел 7 — Верификация

В соответствии с пользовательским правилом «не запускать `dotnet test` (полный прогон), только build и `--list-tests`»:

1. `dotnet build FastIntegrationTests.slnx` — solution собирается, новый проект попадает в граф.
2. `dotnet test tests/FastIntegrationTests.Tests.NUnit.IntegreSQL --list-tests` — NUnit3TestAdapter находит фикстуры и тестовые методы (~24 шт.).
3. xUnit-проект продолжает собираться и `--list-tests` показывает прежнее количество тестов — рефакторинг Shared не сломал ничего.

Полный прогон тестов оставляется на усмотрение пользователя.

---

## Раздел 8 — За пределами scope

Намеренно не делается:

- PowerShell-скрипт `run-nunit-integresql.ps1` — пользователь явно отказался.
- Включение в BenchmarkRunner — то же самое, отдельная история «xUnit vs NUnit» в idea-list.
- NUnit-версии для Respawn и Testcontainers — отдельные мини-проекты, если потребуется. Этот spec покрывает только IntegreSQL.
- Полное зеркало 14 классов / 195 тестов — выходит за границу «учебного примера».
- Обновление `readme.md` — по решению пользователя.

---

## Открытые вопросы

Нет.
