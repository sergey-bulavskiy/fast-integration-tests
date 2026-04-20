using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FastIntegrationTests.Infrastructure.Migrations
{
    /// <summary>
    /// Create_Tags_System
    /// </summary>
    [Migration("20260416000007_Create_Tags_System")]
    public partial class Create_Tags_System : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE "Tags" (
    "Id" serial PRIMARY KEY,
    "Name" varchar(100) NOT NULL,
    "Color" varchar(7) NOT NULL DEFAULT '#6b7280',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX "IX_Tags_Name" ON "Tags" ("Name");

CREATE TABLE "ProductTags" (
    "ProductId" integer NOT NULL REFERENCES "Products"("Id") ON DELETE CASCADE,
    "TagId" integer NOT NULL REFERENCES "Tags"("Id") ON DELETE CASCADE,
    "AssignedAt" timestamp with time zone NOT NULL DEFAULT now(),
    PRIMARY KEY ("ProductId", "TagId")
);
CREATE INDEX "IX_ProductTags_TagId" ON "ProductTags" ("TagId");
CREATE INDEX "IX_ProductTags_AssignedAt" ON "ProductTags" ("AssignedAt");
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP TABLE IF EXISTS "ProductTags";
DROP TABLE IF EXISTS "Tags";
");
        }
    }
}
