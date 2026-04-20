using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FastIntegrationTests.Infrastructure.Migrations
{
    /// <summary>
    /// Add_IsAvailable_To_Products
    /// </summary>
    [Migration("20260416000001_Add_IsAvailable_To_Products")]
    public partial class Add_IsAvailable_To_Products : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE "Products" ADD COLUMN "IsAvailable" boolean NOT NULL DEFAULT true;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE "Products" DROP COLUMN "IsAvailable";
");
        }
    }
}
