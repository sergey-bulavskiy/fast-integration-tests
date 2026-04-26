using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FastIntegrationTests.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"CREATE TABLE load_tmp_065 (
    id         SERIAL       PRIMARY KEY,
    code       VARCHAR(20)  NOT NULL,
    name       VARCHAR(100) NOT NULL,
    created_at TIMESTAMP    NOT NULL DEFAULT NOW()
);
INSERT INTO load_tmp_065 (code, name)
SELECT 'CODE_' || gs, 'Value ' || gs
FROM generate_series(1, 300) gs;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"DROP TABLE IF EXISTS load_tmp_065;");
        }
    }
}