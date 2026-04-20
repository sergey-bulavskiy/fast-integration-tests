using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FastIntegrationTests.Infrastructure.Migrations
{
    /// <summary>
    /// Create_Customers_And_Addresses
    /// </summary>
    [Migration("20260416000008_Create_Customers_And_Addresses")]
    public partial class Create_Customers_And_Addresses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE "Customers" (
    "Id" serial PRIMARY KEY,
    "Email" varchar(320) NOT NULL,
    "FirstName" varchar(100) NOT NULL,
    "LastName" varchar(100) NOT NULL,
    "Phone" varchar(30) NULL,
    "IsActive" boolean NOT NULL DEFAULT true,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX "IX_Customers_Email" ON "Customers" ("Email");
CREATE INDEX "IX_Customers_IsActive_CreatedAt" ON "Customers" ("IsActive", "CreatedAt" DESC);

CREATE TABLE "CustomerAddresses" (
    "Id" serial PRIMARY KEY,
    "CustomerId" integer NOT NULL REFERENCES "Customers"("Id") ON DELETE CASCADE,
    "Line1" varchar(255) NOT NULL,
    "Line2" varchar(255) NULL,
    "City" varchar(100) NOT NULL,
    "PostalCode" varchar(20) NOT NULL,
    "Country" varchar(2) NOT NULL DEFAULT 'RU',
    "IsDefault" boolean NOT NULL DEFAULT false,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now()
);
CREATE INDEX "IX_CustomerAddresses_CustomerId" ON "CustomerAddresses" ("CustomerId");
CREATE INDEX "IX_CustomerAddresses_CustomerId_IsDefault" ON "CustomerAddresses" ("CustomerId", "IsDefault") WHERE "IsDefault" = true;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP TABLE IF EXISTS "CustomerAddresses";
DROP TABLE IF EXISTS "Customers";
");
        }
    }
}
