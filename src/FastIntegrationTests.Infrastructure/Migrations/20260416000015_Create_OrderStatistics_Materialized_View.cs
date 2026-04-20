using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FastIntegrationTests.Infrastructure.Migrations
{
    /// <summary>
    /// Create_OrderStatistics_Materialized_View
    /// </summary>
    [Migration("20260416000015_Create_OrderStatistics_Materialized_View")]
    public partial class Create_OrderStatistics_Materialized_View : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE MATERIALIZED VIEW ""OrderStatisticsByDay"" AS
SELECT
    date_trunc('day', o.""CreatedAt"") AS ""Day"",
    COUNT(DISTINCT o.""Id"") AS ""OrderCount"",
    COUNT(DISTINCT oi.""ProductId"") AS ""UniqueProducts"",
    SUM(oi.""Quantity"") AS ""TotalItems"",
    SUM(oi.""Quantity"" * oi.""UnitPrice"") AS ""TotalRevenue"",
    AVG(oi.""Quantity"" * oi.""UnitPrice"") OVER (
        PARTITION BY date_trunc('day', o.""CreatedAt"")
    ) AS ""AvgOrderRevenue""
FROM ""Orders"" o
JOIN ""OrderItems"" oi ON oi.""OrderId"" = o.""Id""
GROUP BY date_trunc('day', o.""CreatedAt""), oi.""Quantity"", oi.""UnitPrice""
WITH DATA;

CREATE UNIQUE INDEX ""IX_OrderStatsByDay_Day"" ON ""OrderStatisticsByDay"" (""Day"");
CREATE INDEX ""IX_OrderStatsByDay_Revenue"" ON ""OrderStatisticsByDay"" (""TotalRevenue"" DESC);
CREATE INDEX ""IX_OrderStatsByDay_OrderCount"" ON ""OrderStatisticsByDay"" (""OrderCount"" DESC);

CREATE OR REPLACE FUNCTION refresh_order_statistics()
RETURNS void LANGUAGE plpgsql AS $$
BEGIN
    REFRESH MATERIALIZED VIEW ""OrderStatisticsByDay"";
END;
$$;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP FUNCTION IF EXISTS refresh_order_statistics();
DROP MATERIALIZED VIEW IF EXISTS ""OrderStatisticsByDay"";
");
        }
    }
}
