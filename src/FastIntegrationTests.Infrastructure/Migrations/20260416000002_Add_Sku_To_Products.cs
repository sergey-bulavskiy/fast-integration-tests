using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FastIntegrationTests.Infrastructure.Migrations
{
    /// <summary>
    /// Add_Sku_To_Products
    /// </summary>
    [Migration("20260416000002_Add_Sku_To_Products")]
    public partial class Add_Sku_To_Products : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE "Products" ADD COLUMN "Sku" varchar(100) NULL;
CREATE UNIQUE INDEX "IX_Products_Sku" ON "Products" ("Sku") WHERE "Sku" IS NOT NULL;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP INDEX IF EXISTS "IX_Products_Sku";
ALTER TABLE "Products" DROP COLUMN "Sku";
");
        }
    }
}
