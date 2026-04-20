using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FastIntegrationTests.Infrastructure.Migrations
{
    /// <summary>
    /// Add_Performance_Indexes
    /// </summary>
    [Migration("20260416000004_Add_Performance_Indexes")]
    public partial class Add_Performance_Indexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE INDEX CONCURRENTLY IF NOT EXISTS ""IX_Products_Price"" ON ""Products"" (""Price"");
CREATE INDEX CONCURRENTLY IF NOT EXISTS ""IX_Products_Name_Text"" ON ""Products"" USING btree (""Name"" text_pattern_ops);
CREATE INDEX CONCURRENTLY IF NOT EXISTS ""IX_Orders_CreatedAt"" ON ""Orders"" (""CreatedAt"");
CREATE INDEX CONCURRENTLY IF NOT EXISTS ""IX_OrderItems_ProductId_Qty"" ON ""OrderItems"" (""ProductId"", ""Quantity"");
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP INDEX IF EXISTS ""IX_Products_Price"";
DROP INDEX IF EXISTS ""IX_Products_Name_Text"";
DROP INDEX IF EXISTS ""IX_Orders_CreatedAt"";
DROP INDEX IF EXISTS ""IX_OrderItems_ProductId_Qty"";
");
        }
    }
}
