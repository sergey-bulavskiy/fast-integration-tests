using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FastIntegrationTests.Infrastructure.Migrations
{
    /// <summary>
    /// Add_ExternalRefs_To_Orders
    /// </summary>
    [Migration("20260416000005_Add_ExternalRefs_To_Orders")]
    public partial class Add_ExternalRefs_To_Orders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE ""Orders"" ADD COLUMN ""ExternalId"" uuid NULL;
ALTER TABLE ""Orders"" ADD COLUMN ""CustomerEmail"" varchar(320) NULL;
ALTER TABLE ""Orders"" ADD COLUMN ""TrackingCode"" varchar(100) NULL;
CREATE UNIQUE INDEX ""IX_Orders_ExternalId"" ON ""Orders"" (""ExternalId"") WHERE ""ExternalId"" IS NOT NULL;
CREATE INDEX ""IX_Orders_CustomerEmail"" ON ""Orders"" (""CustomerEmail"");
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP INDEX IF EXISTS ""IX_Orders_ExternalId"";
DROP INDEX IF EXISTS ""IX_Orders_CustomerEmail"";
ALTER TABLE ""Orders"" DROP COLUMN ""ExternalId"";
ALTER TABLE ""Orders"" DROP COLUMN ""CustomerEmail"";
ALTER TABLE ""Orders"" DROP COLUMN ""TrackingCode"";
");
        }
    }
}
