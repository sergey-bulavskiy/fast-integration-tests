using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FastIntegrationTests.Infrastructure.Migrations
{
    /// <summary>
    /// Create_AuditLog_With_Triggers
    /// </summary>
    [Migration("20260416000012_Create_AuditLog_With_Triggers")]
    public partial class Create_AuditLog_With_Triggers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE "AuditLog" (
    "Id" bigserial PRIMARY KEY,
    "TableName" varchar(100) NOT NULL,
    "RecordId" integer NOT NULL,
    "Operation" varchar(10) NOT NULL,
    "OldData" jsonb NULL,
    "NewData" jsonb NULL,
    "ChangedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "ChangedBy" varchar(100) NULL
);
CREATE INDEX "IX_AuditLog_TableName_RecordId" ON "AuditLog" ("TableName", "RecordId");
CREATE INDEX "IX_AuditLog_ChangedAt" ON "AuditLog" ("ChangedAt" DESC);
CREATE INDEX "IX_AuditLog_OldData" ON "AuditLog" USING gin ("OldData") WHERE "OldData" IS NOT NULL;
CREATE INDEX "IX_AuditLog_NewData" ON "AuditLog" USING gin ("NewData") WHERE "NewData" IS NOT NULL;

CREATE OR REPLACE FUNCTION trg_audit_products()
RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF TG_OP = 'DELETE' THEN
        INSERT INTO "AuditLog"("TableName","RecordId","Operation","OldData")
        VALUES ('Products', OLD."Id", 'DELETE', to_jsonb(OLD));
        RETURN OLD;
    ELSIF TG_OP = 'UPDATE' THEN
        INSERT INTO "AuditLog"("TableName","RecordId","Operation","OldData","NewData")
        VALUES ('Products', NEW."Id", 'UPDATE', to_jsonb(OLD), to_jsonb(NEW));
        RETURN NEW;
    ELSIF TG_OP = 'INSERT' THEN
        INSERT INTO "AuditLog"("TableName","RecordId","Operation","NewData")
        VALUES ('Products', NEW."Id", 'INSERT', to_jsonb(NEW));
        RETURN NEW;
    END IF;
END;
$$;

CREATE TRIGGER trg_products_audit
AFTER INSERT OR UPDATE OR DELETE ON "Products"
FOR EACH ROW EXECUTE FUNCTION trg_audit_products();

CREATE OR REPLACE FUNCTION trg_audit_orders()
RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF TG_OP = 'DELETE' THEN
        INSERT INTO "AuditLog"("TableName","RecordId","Operation","OldData")
        VALUES ('Orders', OLD."Id", 'DELETE', to_jsonb(OLD));
        RETURN OLD;
    ELSIF TG_OP = 'UPDATE' THEN
        INSERT INTO "AuditLog"("TableName","RecordId","Operation","OldData","NewData")
        VALUES ('Orders', NEW."Id", 'UPDATE', to_jsonb(OLD), to_jsonb(NEW));
        RETURN NEW;
    ELSIF TG_OP = 'INSERT' THEN
        INSERT INTO "AuditLog"("TableName","RecordId","Operation","NewData")
        VALUES ('Orders', NEW."Id", 'INSERT', to_jsonb(NEW));
        RETURN NEW;
    END IF;
END;
$$;

CREATE TRIGGER trg_orders_audit
AFTER INSERT OR UPDATE OR DELETE ON "Orders"
FOR EACH ROW EXECUTE FUNCTION trg_audit_orders();
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP TRIGGER IF EXISTS trg_products_audit ON "Products";
DROP TRIGGER IF EXISTS trg_orders_audit ON "Orders";
DROP FUNCTION IF EXISTS trg_audit_products();
DROP FUNCTION IF EXISTS trg_audit_orders();
DROP TABLE IF EXISTS "AuditLog";
");
        }
    }
}
