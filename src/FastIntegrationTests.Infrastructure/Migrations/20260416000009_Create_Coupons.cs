using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FastIntegrationTests.Infrastructure.Migrations
{
    /// <summary>
    /// Create_Coupons
    /// </summary>
    [Migration("20260416000009_Create_Coupons")]
    public partial class Create_Coupons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE ""Coupons"" (
    ""Id"" serial PRIMARY KEY,
    ""Code"" varchar(50) NOT NULL,
    ""DiscountPercent"" numeric(5,2) NOT NULL,
    ""MaxUsageCount"" integer NOT NULL DEFAULT 1,
    ""UsedCount"" integer NOT NULL DEFAULT 0,
    ""ValidFrom"" timestamp with time zone NOT NULL,
    ""ValidTo"" timestamp with time zone NOT NULL,
    ""IsActive"" boolean NOT NULL DEFAULT true,
    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT ""CK_Coupons_DiscountPercent"" CHECK (""DiscountPercent"" > 0 AND ""DiscountPercent"" <= 100),
    CONSTRAINT ""CK_Coupons_ValidRange"" CHECK (""ValidTo"" > ""ValidFrom""),
    CONSTRAINT ""CK_Coupons_UsedCount"" CHECK (""UsedCount"" >= 0 AND ""UsedCount"" <= ""MaxUsageCount"")
);
CREATE UNIQUE INDEX ""IX_Coupons_Code"" ON ""Coupons"" (""Code"");
CREATE INDEX ""IX_Coupons_ValidTo_IsActive"" ON ""Coupons"" (""ValidTo"", ""IsActive"");

CREATE TABLE ""OrderCoupons"" (
    ""OrderId"" integer NOT NULL REFERENCES ""Orders""(""Id"") ON DELETE CASCADE,
    ""CouponId"" integer NOT NULL REFERENCES ""Coupons""(""Id"") ON DELETE RESTRICT,
    ""DiscountAmount"" numeric(18,2) NOT NULL,
    ""AppliedAt"" timestamp with time zone NOT NULL DEFAULT now(),
    PRIMARY KEY (""OrderId"", ""CouponId"")
);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP TABLE IF EXISTS ""OrderCoupons"";
DROP TABLE IF EXISTS ""Coupons"";
");
        }
    }
}
