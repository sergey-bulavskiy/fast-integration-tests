using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FastIntegrationTests.Infrastructure.Migrations
{
    /// <summary>
    /// Add_FullTextSearch_To_Products
    /// </summary>
    [Migration("20260416000014_Add_FullTextSearch_To_Products")]
    public partial class Add_FullTextSearch_To_Products : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE "Products" ADD COLUMN "SearchVector" tsvector NULL;

UPDATE "Products"
SET "SearchVector" = to_tsvector('russian', coalesce("Name", '') || ' ' || coalesce("Description", ''));

CREATE INDEX "IX_Products_SearchVector" ON "Products" USING gin("SearchVector");

CREATE OR REPLACE FUNCTION trg_update_product_search_vector()
RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    NEW."SearchVector" = to_tsvector('russian',
        coalesce(NEW."Name", '') || ' ' || coalesce(NEW."Description", ''));
    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_products_search_vector
BEFORE INSERT OR UPDATE OF "Name", "Description" ON "Products"
FOR EACH ROW EXECUTE FUNCTION trg_update_product_search_vector();
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP TRIGGER IF EXISTS trg_products_search_vector ON "Products";
DROP FUNCTION IF EXISTS trg_update_product_search_vector();
DROP INDEX IF EXISTS "IX_Products_SearchVector";
ALTER TABLE "Products" DROP COLUMN IF EXISTS "SearchVector";
");
        }
    }
}
