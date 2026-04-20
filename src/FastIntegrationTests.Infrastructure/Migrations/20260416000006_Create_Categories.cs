using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FastIntegrationTests.Infrastructure.Migrations
{
    /// <summary>
    /// Create_Categories
    /// </summary>
    [Migration("20260416000006_Create_Categories")]
    public partial class Create_Categories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE "Categories" (
    "Id" serial PRIMARY KEY,
    "Name" varchar(200) NOT NULL,
    "Slug" varchar(200) NOT NULL,
    "ParentId" integer NULL REFERENCES "Categories"("Id") ON DELETE SET NULL,
    "SortOrder" integer NOT NULL DEFAULT 0,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX "IX_Categories_Slug" ON "Categories" ("Slug");
CREATE INDEX "IX_Categories_ParentId" ON "Categories" ("ParentId");

CREATE TABLE "ProductCategories" (
    "ProductId" integer NOT NULL REFERENCES "Products"("Id") ON DELETE CASCADE,
    "CategoryId" integer NOT NULL REFERENCES "Categories"("Id") ON DELETE CASCADE,
    PRIMARY KEY ("ProductId", "CategoryId")
);
CREATE INDEX "IX_ProductCategories_CategoryId" ON "ProductCategories" ("CategoryId");
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP TABLE IF EXISTS "ProductCategories";
DROP TABLE IF EXISTS "Categories";
");
        }
    }
}
