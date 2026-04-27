# Дизайн: объединение тест-файлов по уровню

**Дата:** 2026-04-27

## Цель

Уменьшить дробление тест-файлов: вместо 4 файлов на сущность (ServiceCr, ServiceUd, ApiCr, ApiUd) — 2 файла (ServiceTests, ApiTests). Изменение охватывает все три тест-проекта: IntegreSQL, Respawn, Testcontainers.

## Масштаб

- 7 сущностей × 3 проекта = 21 группа
- Каждая группа: 4 файла → 2 файла
- Итого: 84 файла → 42 файла (42 файла удаляются, их содержимое вливается в оставшиеся)

## Структура файлов

### До

```
Products/
├── ProductServiceCrTests.cs   # класс ProductServiceCrTests
├── ProductServiceUdTests.cs   # класс ProductServiceUdTests
├── ProductsApiCrTests.cs      # класс ProductsApiCrTests
└── ProductsApiUdTests.cs      # класс ProductsApiUdTests
```

### После

```
Products/
├── ProductServiceTests.cs     # класс ProductServiceTests (Cr-методы, затем Ud-методы)
└── ProductsApiTests.cs        # класс ProductsApiTests (Cr-методы, затем Ud-методы)
```

## Правила слияния

1. Содержимое `*CrTests.cs` идёт первым, `*UdTests.cs` — следом (простая конкатенация).
2. Один объединённый класс без вложенных классов или регионов.
3. XML-документация класса берётся из Cr-файла; строка `/// <summary>` объединённого класса охватывает оба набора операций.
4. Дублирующийся `using`-блок убирается.
5. Старые Ud-файлы удаляются.

## Именование классов

Сохраняется существующий паттерн несимметрии (сервис — единственное число, API — множественное):

| Сущность | Service-класс | API-класс |
|---|---|---|
| Categories | `CategoryServiceTests` | `CategoriesApiTests` |
| Customers | `CustomerServiceTests` | `CustomersApiTests` |
| Discounts | `DiscountServiceTests` | `DiscountsApiTests` |
| Orders | `OrderServiceTests` | `OrdersApiTests` |
| Products | `ProductServiceTests` | `ProductsApiTests` |
| Reviews | `ReviewServiceTests` | `ReviewsApiTests` |
| Suppliers | `SupplierServiceTests` | `SuppliersApiTests` |

Паттерн одинаков для всех трёх проектов.

## BenchmarkScaleClasses.cs

Генератор (`ClassScaleManager.DiscoverTestClasses`) сканирует файлы по `[Fact]`/`[Theory]` и извлекает имена классов регулярным выражением — **менять его код не нужно**.

Три файла `BenchmarkScaleClasses.cs` (по одному на каждый тест-проект) сейчас не закоммичены и должны быть **удалены**. При следующем запуске BenchmarkRunner пересоздаст их автоматически с новыми именами классов.

## BaseTestCount

`BaseTestCount = 223` в `tools/BenchmarkRunner/Program.cs` считает тест-методы (`[Fact]`/`[Theory]`), а не классы. После слияния файлов методы никуда не исчезают — **константа остаётся 223, менять не нужно**.

## Стратегия выполнения

Все 21 группа обрабатываются параллельно через subagent-driven development. После завершения — `dotnet build` для верификации.

## Out of scope

- Изменение самих тест-методов (логика, структура, количество)
- Изменение базовых классов (`AppServiceTestBase`, `ComponentTestBase` и аналогов)
- Изменение `ClassScaleManager.cs` или любого другого кода BenchmarkRunner
