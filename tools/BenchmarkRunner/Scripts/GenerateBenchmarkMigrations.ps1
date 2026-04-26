# GenerateBenchmarkMigrations.ps1 — одноразовый скрипт, генерирует 100 benchmark-миграций
# Запускать из корня репозитория: .\tools\BenchmarkRunner\Scripts\GenerateBenchmarkMigrations.ps1

$repoRoot       = Resolve-Path "$PSScriptRoot\..\..\.."
$migrationsPath = Join-Path $repoRoot "src\FastIntegrationTests.Infrastructure\Migrations"
$templatePath   = Join-Path $migrationsPath "20260421152746_AddCategoryCustomerSupplierReviewDiscount.Designer.cs"

# Шаблон Designer.cs — BuildTargetModel идентичен для всех 100 (модель не меняется)
$templateContent = Get-Content $templatePath -Raw

# Символ кавычки для вставки в C# verbatim-строки внутри here-string
$q = '"'

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
    $pairIdx  = [Math]::Floor(($i - 1) / 2)

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
                @$q$upSql$q);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @$q$downSql$q);
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
