# 117 миграций как базовое состояние — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Добавить 100 benchmark-миграций в репозиторий (итого 117), заменить Add/Remove механизм в BenchmarkRunner на Hide/Restore, чтобы Scenario 2 и Scenario 3 честно запускались при 117 миграциях.

**Architecture:** 100 настоящих EF Core миграций с timestamp `20990101...` коммитятся в `Infrastructure/Migrations/`. Они всегда последние по алфавитной сортировке, что позволяет `HideMigrations(N)` надёжно прятать ровно N из них в `__hidden/`. Scenario 1 скрывает/восстанавливает per-point; Scenarios 2 и 3 работают без манипуляций.

**Tech Stack:** C# / .NET 8, EF Core 8, PowerShell, xUnit

---

## Файловая карта

| Файл | Действие |
|---|---|
| `tools/BenchmarkRunner/Scripts/GenerateBenchmarkMigrations.ps1` | создать (одноразовый скрипт генерации) |
| `src/FastIntegrationTests.Infrastructure/Migrations/20990101000NNN_*.cs` (×200) | создать (100 пар, скрипт генерирует) |
| `src/FastIntegrationTests.Infrastructure/FastIntegrationTests.Infrastructure.csproj` | изменить: `<Compile Remove>` для `__hidden/` |
| `.gitignore` | изменить: добавить `**/Migrations/__hidden/` |
| `tools/BenchmarkRunner/Migrations/MigrationManager.cs` | изменить: Add/Remove → Hide/Restore |
| `tools/BenchmarkRunner/Program.cs` | изменить: новая константа, структура сценариев |
| `CLAUDE.md` | изменить: 17 → 117 для Сценариев 2 и 3 |
| `README.md` | изменить: то же |

---

## Task 1: Сгенерировать 100 benchmark-миграций

**Files:**
- Create: `tools/BenchmarkRunner/Scripts/GenerateBenchmarkMigrations.ps1`
- Create: `src/FastIntegrationTests.Infrastructure/Migrations/20990101000NNN_*.cs` (×200, скрипт)
- Modify: `src/FastIntegrationTests.Infrastructure/FastIntegrationTests.Infrastructure.csproj`
- Modify: `.gitignore`

- [ ] **Step 1: Создать скрипт генерации**

Создать файл `tools/BenchmarkRunner/Scripts/GenerateBenchmarkMigrations.ps1`:

```powershell
# GenerateBenchmarkMigrations.ps1 — одноразовый скрипт, генерирует 100 benchmark-миграций
# Запускать из корня репозитория: .\tools\BenchmarkRunner\Scripts\GenerateBenchmarkMigrations.ps1

$repoRoot       = Resolve-Path "$PSScriptRoot\..\..\.."
$migrationsPath = Join-Path $repoRoot "src\FastIntegrationTests.Infrastructure\Migrations"
$templatePath   = Join-Path $migrationsPath "20260421152746_AddCategoryCustomerSupplierReviewDiscount.Designer.cs"

# Шаблон Designer.cs — BuildTargetModel идентичен для всех 100 (модель не меняется)
$templateContent = Get-Content $templatePath -Raw

# 50 пар имён: нечётные — CREATE, чётные — DROP предыдущей таблицы
$oddNames = @(
    "AddShippingRates",      "CreateProductVariants",  "AddLoyaltyPoints",
    "CreateWarehouseSlots",  "AddReturnRequests",      "CreateNotificationQueue",
    "AddSubscriptionPlans",  "CreateAuditTrail",       "AddGiftCards",
    "CreateBundleOffers",    "AddInventoryAlerts",     "CreateShippingZones",
    "AddPriceRules",         "CreateVendorPortal",     "AddCustomerTags",
    "CreateFlashSales",      "AddProductLabels",       "CreateReturnPolicies",
    "AddWishlistItems",      "CreateAffiliateCodes",   "AddSearchFilters",
    "CreatePaymentMethods",  "AddOrderComments",       "CreatePickupLocations",
    "AddCategoryAliases",    "CreateProductBundles",   "AddReviewVotes",
    "CreateCouponRules",     "AddSupplierContracts",   "CreateDeliverySlots",
    "AddProductDimensions",  "CreateLoyaltyTiers",     "AddCustomerGroups",
    "CreateStoreLocations",  "AddTaxRates",            "CreateEventLog",
    "AddRecommendations",    "CreateExportJobs",       "AddProductTags",
    "CreateInvoiceLines",    "AddCustomFields",        "CreateSavedSearches",
    "AddExchangeRates",      "CreateNotifications",    "AddContentPages",
    "CreateWorkflowRules",   "AddApiKeys",             "CreateImportBatch",
    "AddSessionHistory",     "CreateAccessRules"
)
$evenNames = @(
    "DropShippingRates",     "DropProductVariants",    "DropLoyaltyPoints",
    "DropWarehouseSlots",    "DropReturnRequests",     "DropNotificationQueue",
    "DropSubscriptionPlans", "DropAuditTrail",         "DropGiftCards",
    "DropBundleOffers",      "DropInventoryAlerts",    "DropShippingZones",
    "DropPriceRules",        "DropVendorPortal",       "DropCustomerTags",
    "DropFlashSales",        "DropProductLabels",      "DropReturnPolicies",
    "DropWishlistItems",     "DropAffiliateCodes",     "DropSearchFilters",
    "DropPaymentMethods",    "DropOrderComments",      "DropPickupLocations",
    "DropCategoryAliases",   "DropProductBundles",     "DropReviewVotes",
    "DropCouponRules",       "DropSupplierContracts",  "DropDeliverySlots",
    "DropProductDimensions", "DropLoyaltyTiers",       "DropCustomerGroups",
    "DropStoreLocations",    "DropTaxRates",           "DropEventLog",
    "DropRecommendations",   "DropExportJobs",         "DropProductTags",
    "DropInvoiceLines",      "DropCustomFields",       "DropSavedSearches",
    "DropExchangeRates",     "DropNotifications",      "DropContentPages",
    "DropWorkflowRules",     "DropApiKeys",            "DropImportBatch",
    "DropSessionHistory",    "DropAccessRules"
)

for ($i = 1; $i -le 100; $i++) {
    $ts       = "20990101{0:D6}" -f $i
    $isOdd    = ($i % 2) -eq 1
    $pairIdx  = [int](($i - 1) / 2)

    if ($isOdd) {
        $name      = $oddNames[$pairIdx]
        $tableNum  = "{0:D3}" -f $i
        $upSql     = "CREATE TABLE load_tmp_$tableNum (`n    id         SERIAL       PRIMARY KEY,`n    code       VARCHAR(20)  NOT NULL,`n    name       VARCHAR(100) NOT NULL,`n    created_at TIMESTAMP    NOT NULL DEFAULT NOW()`n);`nINSERT INTO load_tmp_$tableNum (code, name)`nSELECT 'CODE_' || gs, 'Value ' || gs`nFROM generate_series(1, 300) gs;"
        $downSql   = "DROP TABLE IF EXISTS load_tmp_$tableNum;"
    } else {
        $name      = $evenNames[$pairIdx]
        $prevNum   = "{0:D3}" -f ($i - 1)
        $upSql     = "DROP TABLE IF EXISTS load_tmp_$prevNum;"
        $downSql   = "CREATE TABLE load_tmp_$prevNum (`n    id         SERIAL       PRIMARY KEY,`n    code       VARCHAR(20)  NOT NULL,`n    name       VARCHAR(100) NOT NULL,`n    created_at TIMESTAMP    NOT NULL DEFAULT NOW()`n);`nINSERT INTO load_tmp_$prevNum (code, name)`nSELECT 'CODE_' || gs, 'Value ' || gs`nFROM generate_series(1, 300) gs;"
    }

    $migrationId = "${ts}_${name}"

    $csContent = @"
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FastIntegrationTests.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class $name : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @""$upSql"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @""$downSql"");
        }
    }
}
"@

    # Designer.cs — копируем шаблон, меняем только ID миграции и имя класса
    $designerContent = $templateContent `
        -replace '\[Migration\(".*?"\)\]', "[Migration(""$migrationId"")]" `
        -replace 'partial class AddCategoryCustomerSupplierReviewDiscount', "partial class $name"

    $enc = [System.Text.Encoding]::UTF8
    [System.IO.File]::WriteAllText((Join-Path $migrationsPath "$migrationId.cs"),          $csContent,      $enc)
    [System.IO.File]::WriteAllText((Join-Path $migrationsPath "$migrationId.Designer.cs"), $designerContent, $enc)

    Write-Host "[$i/100] $migrationId"
}

Write-Host "`nГотово! Сгенерировано 100 пар миграций в $migrationsPath"
```

- [ ] **Step 2: Запустить скрипт**

```powershell
.\tools\BenchmarkRunner\Scripts\GenerateBenchmarkMigrations.ps1
```

Ожидаемый результат: вывод `[1/100] 20990101000001_AddShippingRates` ... `[100/100] 20990101000100_DropAccessRules`, затем `Готово!`.

Проверить что создалось 200 файлов:

```powershell
(Get-ChildItem src\FastIntegrationTests.Infrastructure\Migrations -Filter "20990101*").Count
```

Ожидаемый результат: `200`.

- [ ] **Step 3: Добавить exclusion в Infrastructure.csproj**

Файл `src/FastIntegrationTests.Infrastructure/FastIntegrationTests.Infrastructure.csproj` — добавить `<ItemGroup>` перед закрывающим `</Project>`:

```xml
  <ItemGroup>
    <Compile Remove="Migrations\__hidden\**" />
  </ItemGroup>

</Project>
```

- [ ] **Step 4: Добавить __hidden/ в .gitignore**

В корневой `.gitignore` добавить строку (в конец файла):

```
**/Migrations/__hidden/
```

- [ ] **Step 5: Проверить сборку**

```powershell
dotnet build
```

Ожидаемый результат: `Build succeeded` без ошибок.

- [ ] **Step 6: Проверить что число тестов не изменилось**

```powershell
dotnet test tests/FastIntegrationTests.Tests.IntegreSQL --list-tests 2>&1 | Select-String "::" | Measure-Object | Select-Object Count
```

Ожидаемый результат: `Count : 223` (benchmark-миграции не добавляют тест-методов).

- [ ] **Step 7: Commit**

```powershell
git add tools/BenchmarkRunner/Scripts/GenerateBenchmarkMigrations.ps1
git add src/FastIntegrationTests.Infrastructure/Migrations/20990101*.cs
git add src/FastIntegrationTests.Infrastructure/Migrations/20990101*.Designer.cs
git add src/FastIntegrationTests.Infrastructure/FastIntegrationTests.Infrastructure.csproj
git add .gitignore
git commit -m "feat: добавить 100 benchmark-миграций (timestamp 2099, create/drop pattern)"
```

---

## Task 2: Рефакторинг MigrationManager — Hide/Restore

**Files:**
- Modify: `tools/BenchmarkRunner/Migrations/MigrationManager.cs`

- [ ] **Step 1: Заменить содержимое MigrationManager.cs**

```csharp
// tools/BenchmarkRunner/Migrations/MigrationManager.cs
namespace BenchmarkRunner.Migrations;

/// <summary>Управляет benchmark-миграциями: скрывает часть для Scenario 1, восстанавливает после.</summary>
public class MigrationManager
{
    private readonly string _migrationsPath;
    private readonly string _hiddenPath;

    /// <summary>Инициализирует менеджер с корневой директорией репозитория.</summary>
    public MigrationManager(string repoRoot)
    {
        _migrationsPath = Path.Combine(
            repoRoot, "src", "FastIntegrationTests.Infrastructure", "Migrations");
        _hiddenPath = Path.Combine(_migrationsPath, "__hidden");
    }

    /// <summary>
    /// Скрывает последние <paramref name="count"/> benchmark-миграций (timestamp 20990101...)
    /// перемещая их пары (.cs + .Designer.cs) в <c>__hidden/</c>.
    /// Вызывающий код отвечает за последующий rebuild.
    /// </summary>
    public void HideMigrations(int count)
    {
        if (count <= 0) return;

        Directory.CreateDirectory(_hiddenPath);

        var csFiles = Directory.GetFiles(_migrationsPath, "*.cs")
            .Where(f => !f.Contains("__hidden") && !Path.GetFileName(f).EndsWith(".Designer.cs"))
            .Order()
            .TakeLast(count)
            .ToList();

        Console.WriteLine($"\n[MIGRATIONS] Hiding {count} benchmark migrations...");
        foreach (var cs in csFiles)
        {
            var designer = Path.Combine(
                Path.GetDirectoryName(cs)!,
                Path.GetFileNameWithoutExtension(cs) + ".Designer.cs");

            File.Move(cs, Path.Combine(_hiddenPath, Path.GetFileName(cs)));
            if (File.Exists(designer))
                File.Move(designer, Path.Combine(_hiddenPath, Path.GetFileName(designer)));
        }
        Console.WriteLine($"[MIGRATIONS] Hidden {csFiles.Count} migrations ({csFiles.Count * 2} files)");
    }

    /// <summary>
    /// Восстанавливает все скрытые миграции из <c>__hidden/</c> обратно в папку Migrations.
    /// No-op если папка пуста или не существует.
    /// </summary>
    public void RestoreHiddenMigrations()
    {
        if (!Directory.Exists(_hiddenPath)) return;

        var files = Directory.GetFiles(_hiddenPath);
        if (files.Length == 0) return;

        Console.WriteLine($"\n[MIGRATIONS] Restoring {files.Length} hidden migration files...");
        foreach (var f in files)
            File.Move(f, Path.Combine(_migrationsPath, Path.GetFileName(f)));
        Console.WriteLine($"[MIGRATIONS] Restored {files.Length} files");
    }
}
```

- [ ] **Step 2: Проверить сборку BenchmarkRunner**

```powershell
dotnet build tools/BenchmarkRunner
```

Ожидаемый результат: `Build succeeded`. Если есть ошибки "не найден метод AddFakeMigrations/RemoveFakeMigrations" — это значит Program.cs ещё использует старые методы; они будут исправлены в Task 3.

- [ ] **Step 3: Проверить Hide/Restore вручную**

Запустить из корня репозитория:

```powershell
dotnet run --project tools/BenchmarkRunner -- --scale 1
```

Нажать Ctrl+C сразу после запуска (до Press Enter). Потом вручную в PowerShell:

```powershell
# Убедиться что __hidden не существует
Test-Path "src\FastIntegrationTests.Infrastructure\Migrations\__hidden"
# Ожидаемый результат: False
```

Полноценную проверку HideMigrations сделает Task 3 через итоговый тест.

- [ ] **Step 4: Commit**

```powershell
git add tools/BenchmarkRunner/Migrations/MigrationManager.cs
git commit -m "refactor: MigrationManager — Add/Remove → Hide/Restore benchmark-миграций"
```

---

## Task 3: Обновить Program.cs

**Files:**
- Modify: `tools/BenchmarkRunner/Program.cs`

- [ ] **Step 1: Добавить константу MaxMigrations и исправить startup cleanup**

В `tools/BenchmarkRunner/Program.cs` найти блок констант (строки ~15–16):

```csharp
const int BaseMigrations   = 17;
```

Заменить на:

```csharp
const int BaseMigrations   = 17;   // реальных миграций в репозитории
const int MaxMigrations    = 117;  // с benchmark-миграциями — базовое состояние бенчмарка
```

Найти startup cleanup (~строка 51):

```csharp
// Убрать фейковые миграции и scale-классы, которые могли остаться от прерванного прогона
migrationManager.RemoveFakeMigrations();
classScaleManager.RemoveScaleClasses();
```

Заменить на:

```csharp
// Восстановить скрытые миграции и убрать scale-классы после возможного прерванного прогона
migrationManager.RestoreHiddenMigrations();
classScaleManager.RemoveScaleClasses();
```

- [ ] **Step 2: Обновить Warmup**

Найти (~строка 63):

```csharp
var warmup = runner.Warmup(new BenchmarkScenario(approach, "warmup", BaseMigrations, defaultThreads));
```

Заменить на:

```csharp
var warmup = runner.Warmup(new BenchmarkScenario(approach, "warmup", MaxMigrations, defaultThreads));
```

- [ ] **Step 3: Обновить Scenario 1 — инвертировать логику миграций**

Найти блок Scenario 1 (~строки 75–96):

```csharp
    foreach (var migrationCount in migrationCounts)
    {
        var fakesToAdd = migrationCount - BaseMigrations;
        try
        {
            if (fakesToAdd > 0)
            {
                migrationManager.AddFakeMigrations(fakesToAdd);
                runner.Build();
            }
            foreach (var approach in approaches)
                RunOrAbort(new BenchmarkScenario(approach, "migrations", migrationCount, defaultThreads, defaultClassScale));
        }
        finally
        {
            if (fakesToAdd > 0)
            {
                migrationManager.RemoveFakeMigrations();
                runner.Build();
            }
        }
    }
```

Заменить на:

```csharp
    foreach (var migrationCount in migrationCounts)
    {
        var toHide = MaxMigrations - migrationCount;
        try
        {
            if (toHide > 0)
            {
                migrationManager.HideMigrations(toHide);
                runner.Build();
            }
            foreach (var approach in approaches)
                RunOrAbort(new BenchmarkScenario(approach, "migrations", migrationCount, defaultThreads, defaultClassScale));
        }
        finally
        {
            if (toHide > 0)
            {
                migrationManager.RestoreHiddenMigrations();
                runner.Build();
            }
        }
    }
```

- [ ] **Step 4: Обновить Scenario 2 — BaseMigrations → MaxMigrations**

Найти (~строка 115):

```csharp
            foreach (var approach in approaches)
                RunOrAbort(new BenchmarkScenario(approach, "scale", BaseMigrations, defaultThreads, scale));
```

Заменить на:

```csharp
            foreach (var approach in approaches)
                RunOrAbort(new BenchmarkScenario(approach, "scale", MaxMigrations, defaultThreads, scale));
```

- [ ] **Step 5: Обновить Scenario 3 — BaseMigrations → MaxMigrations**

Найти (~строка 135):

```csharp
        foreach (var parallelism in parallelismThreads)
            foreach (var approach in approaches)
                RunOrAbort(new BenchmarkScenario(approach, "parallelism", BaseMigrations, parallelism, defaultClassScale));
```

Заменить на:

```csharp
        foreach (var parallelism in parallelismThreads)
            foreach (var approach in approaches)
                RunOrAbort(new BenchmarkScenario(approach, "parallelism", MaxMigrations, parallelism, defaultClassScale));
```

- [ ] **Step 6: Проверить сборку**

```powershell
dotnet build tools/BenchmarkRunner
```

Ожидаемый результат: `Build succeeded` без ошибок и предупреждений.

- [ ] **Step 7: Проверить Hide/Restore через запуск BenchmarkRunner**

```powershell
dotnet run --project tools/BenchmarkRunner -- --scale 1
```

На строке `Press Enter to start, Ctrl+C to cancel...` — нажать Enter.

В логе должны появиться строки:
```
[MIGRATIONS] Hiding 100 benchmark migrations (200 files)
```
(при первой точке Scenario 1 — migrationCount=17, toHide=100).

Нажать Ctrl+C сразу после этой строки. Затем проверить:

```powershell
(Get-ChildItem src\FastIntegrationTests.Infrastructure\Migrations\__hidden).Count
```

Ожидаемый результат: `200` (100 пар).

Запустить снова чтобы проверить RestoreHiddenMigrations при startup:

```powershell
dotnet run --project tools/BenchmarkRunner -- --scale 1
```

В начале лога должно появиться:

```
[MIGRATIONS] Restoring 200 hidden migration files...
[MIGRATIONS] Restored 200 files
```

Нажать Ctrl+C для выхода.

- [ ] **Step 8: Commit**

```powershell
git add tools/BenchmarkRunner/Program.cs
git commit -m "feat: Program.cs — MaxMigrations=117, Hide/Restore в Scenario 1, Scenarios 2-3 на 117 миграциях"
```

---

## Task 4: Обновить документацию

**Files:**
- Modify: `CLAUDE.md`
- Modify: `README.md`

- [ ] **Step 1: Обновить CLAUDE.md — таблица сценариев**

Найти в `CLAUDE.md` строки таблицы (раздел «Три сценария»):

```markdown
| 2 — Масштаб числа тестов | 17 миграций, `--threads` | ClassScale: 1, 5, 10, 20, 50 |
| 3 — Параллелизм | 17 миграций, `--scale` | потоков: 1, 2, 4, 8 |
```

Заменить на:

```markdown
| 2 — Масштаб числа тестов | 117 миграций, `--threads` | ClassScale: 1, 5, 10, 20, 50 |
| 3 — Параллелизм | 117 миграций, `--scale` | потоков: 1, 2, 4, 8 |
```

- [ ] **Step 2: Обновить README.md — таблица сценариев**

Найти в `README.md` строки 209–210:

```markdown
| 2 — Масштаб тестов | ClassScale: 1, 5, 10, 20, 50 | `--threads`, 17 миграций |
| 3 — Параллелизм | потоков: 1, 2, 4, 8 | `--scale`, 17 миграций |
```

Заменить на:

```markdown
| 2 — Масштаб тестов | ClassScale: 1, 5, 10, 20, 50 | `--threads`, 117 миграций |
| 3 — Параллелизм | потоков: 1, 2, 4, 8 | `--scale`, 117 миграций |
```

- [ ] **Step 3: Убедиться что "17 миграций" не осталось в контексте сценариев**

```powershell
Select-String "17 миграций" CLAUDE.md, README.md
```

Ожидаемый результат: нет совпадений (или только в разделе Scenario 1 где 17 — одна из точек варьирования, но не "фиксируется").

- [ ] **Step 4: Commit**

```powershell
git add CLAUDE.md README.md
git commit -m "docs: Scenarios 2 и 3 — 17 → 117 миграций в таблице сценариев"
```

---

## Итоговая проверка

- [ ] `dotnet build` — весь solution без ошибок
- [ ] `dotnet test tests/FastIntegrationTests.Tests.IntegreSQL` — 223 теста проходят (с 117 миграциями)
- [ ] `Select-String "AddFakeMigrations|RemoveFakeMigrations" tools/BenchmarkRunner --include *.cs -r` — нет результатов
- [ ] `(Get-ChildItem src\FastIntegrationTests.Infrastructure\Migrations -Filter "20990101*").Count` — `200`
- [ ] BenchmarkRunner стартует и корректно скрывает/восстанавливает миграции в Scenario 1
