# Спецификация: тяжёлые тесты в существующих тест-классах

## Цель

Сделать тест-сьют неоднородным по объёму SQL-работы, чтобы бенчмарк показывал, как соотношение «бизнес-время / инфраструктурный overhead» меняется при более реалистичной нагрузке. Сейчас все тесты делают 1–3 SQL-операции; тяжёлые тесты добавят ~15–22 операции на тест.

## Охват

**84 тест-класса** × 2 тяжёлых теста = **168 новых методов**.

Три проекта: `Tests.IntegreSQL`, `Tests.Respawn`, `Tests.Testcontainers`.  
Семь сущностей: Products, Categories, Suppliers, Customers, Discounts, Reviews, Orders.  
Четыре типа класса на сущность: `*ServiceCr`, `*ServiceUd`, `*ApiCr`, `*ApiUd`.

Тяжёлые тесты добавляются **в существующие классы**, не в новые файлы.

## Шаблоны тяжёлых тестов

### H1 — цепочка create → update → verify → delete

Применяется ко всем сущностям в обоих типах классов (Cr и Ud).

Применяется ко всем сущностям. Для Reviews (нет UpdateAsync): вместо Update используется ApproveAsync как «действие над сущностью».

**Сценарий (реальная часть, Products / Categories / Suppliers / Customers / Discounts):**
1. Создать сущность A
2. Обновить A через UpdateAsync (изменить поля)
3. GetById A → проверить изменённые поля
4. Удалить A
5. Убедиться что GetById возвращает NotFoundException / 404

**Сценарий H1 для Reviews:**
1. Создать отзыв A
2. ApproveAsync A → GetById A → проверить Status = Approved
3. DeleteAsync A → убедиться NotFoundException

**Накрутка:**
```csharp
// benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
for (var i = 0; i < 5; i++)
{
    var extra = await Sut.CreateAsync(...);
    await Sut.GetByIdAsync(extra.Id);
}
await Sut.GetAllAsync();
```

Итого: ~6 реальных + 11 накрутки = **~17 SQL-операций**.

### H2 — lifecycle для сущностей со статусами, bulk create для CRUD-сущностей

**Для Products, Categories, Suppliers** (нет статусных переходов):
1. Создать 3 сущности
2. GetAll → проверить count = 3
3. GetById каждой

Накрутка:
```csharp
// benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
for (var i = 0; i < 4; i++)
{
    var extra = await Sut.CreateAsync(...);
    await Sut.GetByIdAsync(extra.Id);
}
await Sut.GetAllAsync();
```

Итого: ~7 реальных + 9 накрутки = **~16 SQL-операций**.

**Для Customers** (Active → Banned → Active → Inactive):
1. Создать покупателя
2. BanAsync → проверить статус Banned
3. ActivateAsync → проверить статус Active
4. DeactivateAsync → проверить статус Inactive

Накрутка: 3 дополнительных Create + GetById каждого + GetAll = 7 ops.  
Итого: **~16 SQL-операций**.

**Для Discounts** (create → Activate → Deactivate → Delete):
1. Создать скидку (`ExistsByCode` + INSERT = 2 ops)
2. ActivateAsync → проверить IsActive = true
3. DeactivateAsync → проверить IsActive = false
4. UpdateAsync (изменить процент) → GetById → verify
5. DeleteAsync → verify NotFoundException

Накрутка: 2 дополнительных Create (каждый 2 ops) + GetAll = 5 ops.  
Итого: **~17 SQL-операций**.

**Для Reviews** (Pending → Approved; Pending → Rejected):
1. Создать 2 отзыва
2. ApproveAsync первый → GetById → verify Approved
3. RejectAsync второй → GetById → verify Rejected
4. GetAll → verify count = 2

Накрутка: Create 4 + GetById каждого + GetAll = 9 ops.  
Итого: **~18 SQL-операций**.

**Для Orders** — специальный сценарий без накрутки (см. ниже).

## Orders: специальные тяжёлые тесты

Orders уже имеет `FullLifecycle_CreateConfirmShipCompleteGetById_StatusIsCompleted` (~12 SQL ops). Два новых тяжёлых теста:

### Orders H1 — MultiItem lifecycle с проверкой суммы

```
Создать 3 товара разной цены         → 3 INSERT
Создать заказ с 3 позициями          → 3 SELECT (цены) + 1 INSERT (Order) + 3 INSERT (Items)
ConfirmAsync                         → 1 SELECT + 1 UPDATE
ShipAsync                            → 1 SELECT + 1 UPDATE
CompleteAsync                        → 1 SELECT + 1 UPDATE
GetById → проверить Status=Completed и TotalAmount = сумма × количества  → 1 SELECT
```

Итого: **~16 SQL-операций**. Накрутка не нужна — сценарий реалистичный и самодостаточный.

### Orders H2 — создать несколько заказов, GetAll, отменить часть

```
Создать товар                        → 1 INSERT
Создать 4 заказа (у каждого 1 позиция) → 4 × (1 SELECT + 2 INSERT) = 12 ops
GetAll → verify count = 4           → 1 SELECT
CancelAsync для 2 заказов           → 2 × (1 SELECT + 1 UPDATE) = 4 ops
GetAll → verify 2 Cancelled + 2 New → 1 SELECT
```

Накрутка:
```csharp
// benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
var extraProduct = await CreateProductAsync(...);
for (var i = 0; i < 3; i++)
{
    var extra = await CreateOrderAsync(extraProduct.Id);
    await GetOrderByIdAsync(extra.Id);
}
```

Итого: ~19 реальных + 7 накрутки = **~26 SQL-операций**.

## Тест-специфика по типу класса

### `*ServiceCr` и `*ServiceUd`

Используют `Sut` (экземпляр `I*Service`), создаваемый в `InitializeAsync`.  
Оба класса получают **H1 и H2** — разделение Cr/Ud по содержанию тестов условное; тяжёлые тесты естественно включают и reads и writes.

### `*ApiCr` и `*ApiUd`

Используют `Client` (`HttpClient`).  
Те же сценарии H1 и H2, но через HTTP-эндпоинты.  
Хелпер-методы (`CreateProductAsync`, `CreateOrderAsync` и т.д.) уже есть в существующих классах — тяжёлые тесты переиспользуют их.

## Реплицирование по подходам

Три проекта получают **идентичную логику** тестов. Меняется только:

| Проект | Базовый класс (Service) | Базовый класс (API) | Особенности |
|---|---|---|---|
| `Tests.IntegreSQL` | `AppServiceTestBase` | `ComponentTestBase` | нет конструктора с fixture |
| `Tests.Respawn` | `RespawnServiceTestBase` | `RespawnApiTestBase` | конструктор `(RespawnFixture fixture)` |
| `Tests.Testcontainers` | `ContainerServiceTestBase` | `ContainerApiTestBase` | конструктор `(ContainerFixture fixture)` |

Суффиксы в именах классов: нет суффикса (IntegreSQL), `Respawn`, `Container`.

## Именование методов

Тяжёлые тесты не маркируются суффиксом `_Heavy`. Имена описывают сценарий:

- `CreateUpdateDeleteVerify_PersistsCorrectly` (H1 для Products)
- `FullStatusCycle_ActiveBannedActiveInactive` (H2 для Customers)
- `MultiItemOrder_FullLifecycle_TotalAmountCorrect` (Orders H1)
- `MultipleOrders_CancelPartial_StatusesCorrect` (Orders H2)

## Что НЕ входит в scope

- Новые тест-классы не создаются.
- Инфраструктурные файлы (базовые классы, фикстуры) не изменяются.
- `BenchmarkRunner` не изменяется — тяжёлые тесты подхватываются автоматически при запуске `dotnet test`.
- XML-документация на каждый тяжёлый метод обязательна (по соглашению в CLAUDE.md).
