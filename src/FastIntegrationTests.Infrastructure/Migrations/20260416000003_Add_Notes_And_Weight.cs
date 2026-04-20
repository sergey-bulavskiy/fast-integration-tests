using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FastIntegrationTests.Infrastructure.Migrations
{
    /// <summary>
    /// Add_Notes_And_Weight
    /// </summary>
    [Migration("20260416000003_Add_Notes_And_Weight")]
    public partial class Add_Notes_And_Weight : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE ""Products"" ADD COLUMN ""Notes"" text NULL;
ALTER TABLE ""Products"" ADD COLUMN ""WeightGrams"" integer NULL;
ALTER TABLE ""Orders"" ADD COLUMN ""CustomerNote"" text NULL;
ALTER TABLE ""Orders"" ADD COLUMN ""InternalNote"" text NULL;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE ""Products"" DROP COLUMN ""Notes"";
ALTER TABLE ""Products"" DROP COLUMN ""WeightGrams"";
ALTER TABLE ""Orders"" DROP COLUMN ""CustomerNote"";
ALTER TABLE ""Orders"" DROP COLUMN ""InternalNote"";
");
        }
    }
}
