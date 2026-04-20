using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FastIntegrationTests.Infrastructure.Migrations
{
    /// <summary>
    /// Create_PriceHistory_With_Trigger
    /// </summary>
    [Migration("20260416000011_Create_PriceHistory_With_Trigger")]
    public partial class Create_PriceHistory_With_Trigger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE "PriceHistory" (
    "Id" bigserial PRIMARY KEY,
    "ProductId" integer NOT NULL REFERENCES "Products"("Id") ON DELETE CASCADE,
    "OldPrice" numeric(18,2) NOT NULL,
    "NewPrice" numeric(18,2) NOT NULL,
    "ChangedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "ChangedBy" varchar(100) NULL
);
CREATE INDEX "IX_PriceHistory_ProductId_ChangedAt" ON "PriceHistory" ("ProductId", "ChangedAt" DESC);
CREATE INDEX "IX_PriceHistory_ChangedAt" ON "PriceHistory" ("ChangedAt" DESC);

CREATE OR REPLACE FUNCTION trg_record_price_change()
RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF OLD."Price" IS DISTINCT FROM NEW."Price" THEN
        INSERT INTO "PriceHistory" ("ProductId", "OldPrice", "NewPrice")
        VALUES (NEW."Id", OLD."Price", NEW."Price");
    END IF;
    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_products_price_change
AFTER UPDATE ON "Products"
FOR EACH ROW EXECUTE FUNCTION trg_record_price_change();
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP TRIGGER IF EXISTS trg_products_price_change ON "Products";
DROP FUNCTION IF EXISTS trg_record_price_change();
DROP TABLE IF EXISTS "PriceHistory";
");
        }
    }
}
