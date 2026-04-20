using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FastIntegrationTests.Infrastructure.Migrations
{
    /// <summary>
    /// Create_ProductReviews_And_Helpfulness
    /// </summary>
    [Migration("20260416000010_Create_ProductReviews_And_Helpfulness")]
    public partial class Create_ProductReviews_And_Helpfulness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE "ProductReviews" (
    "Id" serial PRIMARY KEY,
    "ProductId" integer NOT NULL REFERENCES "Products"("Id") ON DELETE CASCADE,
    "AuthorName" varchar(100) NOT NULL,
    "AuthorEmail" varchar(320) NULL,
    "Rating" smallint NOT NULL,
    "Title" varchar(200) NULL,
    "Body" text NOT NULL,
    "IsApproved" boolean NOT NULL DEFAULT false,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT "CK_ProductReviews_Rating" CHECK ("Rating" >= 1 AND "Rating" <= 5)
);
CREATE INDEX "IX_ProductReviews_ProductId_IsApproved" ON "ProductReviews" ("ProductId", "IsApproved");
CREATE INDEX "IX_ProductReviews_CreatedAt" ON "ProductReviews" ("CreatedAt" DESC);

CREATE TABLE "ReviewHelpfulness" (
    "ReviewId" integer NOT NULL REFERENCES "ProductReviews"("Id") ON DELETE CASCADE,
    "VoterEmail" varchar(320) NOT NULL,
    "IsHelpful" boolean NOT NULL,
    "VotedAt" timestamp with time zone NOT NULL DEFAULT now(),
    PRIMARY KEY ("ReviewId", "VoterEmail")
);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP TABLE IF EXISTS "ReviewHelpfulness";
DROP TABLE IF EXISTS "ProductReviews";
");
        }
    }
}
