using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FastIntegrationTests.Infrastructure.Migrations
{
    /// <summary>
    /// Create_Inventory_System
    /// </summary>
    [Migration("20260416000013_Create_Inventory_System")]
    public partial class Create_Inventory_System : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE "Warehouses" (
    "Id" serial PRIMARY KEY,
    "Name" varchar(200) NOT NULL,
    "Address" varchar(500) NULL,
    "IsActive" boolean NOT NULL DEFAULT true,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now()
);

CREATE TABLE "ProductStock" (
    "ProductId" integer NOT NULL REFERENCES "Products"("Id") ON DELETE CASCADE,
    "WarehouseId" integer NOT NULL REFERENCES "Warehouses"("Id") ON DELETE CASCADE,
    "QuantityOnHand" integer NOT NULL DEFAULT 0,
    "QuantityReserved" integer NOT NULL DEFAULT 0,
    "ReorderPoint" integer NOT NULL DEFAULT 10,
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    PRIMARY KEY ("ProductId", "WarehouseId"),
    CONSTRAINT "CK_ProductStock_QuantityOnHand" CHECK ("QuantityOnHand" >= 0),
    CONSTRAINT "CK_ProductStock_QuantityReserved" CHECK ("QuantityReserved" >= 0 AND "QuantityReserved" <= "QuantityOnHand")
);
CREATE INDEX "IX_ProductStock_WarehouseId" ON "ProductStock" ("WarehouseId");
CREATE INDEX "IX_ProductStock_BelowReorder" ON "ProductStock" ("ProductId") WHERE "QuantityOnHand" <= "ReorderPoint";

CREATE TABLE "StockMovements" (
    "Id" bigserial PRIMARY KEY,
    "ProductId" integer NOT NULL REFERENCES "Products"("Id") ON DELETE CASCADE,
    "WarehouseId" integer NOT NULL REFERENCES "Warehouses"("Id") ON DELETE CASCADE,
    "Delta" integer NOT NULL,
    "Reason" varchar(100) NOT NULL,
    "ReferenceId" integer NULL,
    "MovedAt" timestamp with time zone NOT NULL DEFAULT now()
);
CREATE INDEX "IX_StockMovements_ProductId_WarehouseId" ON "StockMovements" ("ProductId", "WarehouseId");
CREATE INDEX "IX_StockMovements_MovedAt" ON "StockMovements" ("MovedAt" DESC);

CREATE OR REPLACE FUNCTION trg_update_stock_timestamp()
RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    NEW."UpdatedAt" = now();
    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_productstock_updated_at
BEFORE UPDATE ON "ProductStock"
FOR EACH ROW EXECUTE FUNCTION trg_update_stock_timestamp();
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP TRIGGER IF EXISTS trg_productstock_updated_at ON "ProductStock";
DROP FUNCTION IF EXISTS trg_update_stock_timestamp();
DROP TABLE IF EXISTS "StockMovements";
DROP TABLE IF EXISTS "ProductStock";
DROP TABLE IF EXISTS "Warehouses";
");
        }
    }
}
