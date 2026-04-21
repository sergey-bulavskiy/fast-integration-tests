// tools/BenchmarkRunner/Migrations/MigrationManager.cs
namespace BenchmarkRunner.Migrations;

/// <summary>Добавляет и удаляет фейковые .cs файлы миграций для бенчмарка.</summary>
class MigrationManager
{
    private readonly string _migrationsPath;
    private const string FakePrefix = "Benchmark_Fake_";

    /// <summary>Инициализирует менеджер с корневой директорией репозитория.</summary>
    public MigrationManager(string repoRoot)
    {
        _migrationsPath = Path.Combine(
            repoRoot, "src", "FastIntegrationTests.Infrastructure", "Migrations");
    }

    /// <summary>Создаёт указанное количество фейковых миграций в папке Infrastructure.</summary>
    public void AddFakeMigrations(int count)
    {
        Console.WriteLine($"\n[MIGRATIONS] Adding {count} fake migrations...");
        for (var i = 1; i <= count; i++)
        {
            var name        = $"{FakePrefix}{i:D3}";
            var timestamp   = $"29990101{i:D6}";
            var migrationId = $"{timestamp}_{name}";
            var path        = Path.Combine(_migrationsPath, $"{migrationId}.cs");
            File.WriteAllText(path, GenerateContent(name, migrationId, i));
        }
        Console.WriteLine($"[MIGRATIONS] Added {count} fake migrations");
    }

    /// <summary>Удаляет все файлы фейковых миграций из папки Infrastructure.</summary>
    public void RemoveFakeMigrations()
    {
        Console.WriteLine("\n[MIGRATIONS] Removing fake migrations...");
        var files = Directory.GetFiles(_migrationsPath, $"*{FakePrefix}*");
        foreach (var f in files) File.Delete(f);
        Console.WriteLine($"[MIGRATIONS] Removed {files.Length} files");
    }

    private static string GenerateContent(string name, string migrationId, int index)
    {
        var isOdd   = index % 2 == 1;
        var upSql   = isOdd ? OddUpSql(index)  : EvenUpSql(index);
        var downSql = isOdd ? OddDownSql(index) : EvenDownSql(index);

        return
$@"using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FastIntegrationTests.Infrastructure.Migrations
{{
    [Migration(""{migrationId}"")]
    public class {name} : Migration
    {{
        protected override void Up(MigrationBuilder migrationBuilder)
        {{
            migrationBuilder.Sql(@""{upSql}"");
        }}

        protected override void Down(MigrationBuilder migrationBuilder)
        {{
            migrationBuilder.Sql(@""{downSql}"");
        }}
    }}
}}
";
    }

    private static string OddUpSql(int i) =>
$@"
CREATE TABLE benchmark_ref_{i:D3} (
    id         SERIAL       PRIMARY KEY,
    code       VARCHAR(20)  NOT NULL,
    name       VARCHAR(100) NOT NULL,
    created_at TIMESTAMP    NOT NULL DEFAULT NOW()
);
INSERT INTO benchmark_ref_{i:D3} (code, name)
SELECT 'CODE_' || gs, 'Reference value number ' || gs
FROM generate_series(1, 300) gs;
";

    private static string OddDownSql(int i) =>
        $"DROP TABLE IF EXISTS benchmark_ref_{i:D3};";

    private static string EvenUpSql(int i) =>
$@"
ALTER TABLE """"Products"""" ADD COLUMN benchmark_col_{i:D3} TEXT NULL;
UPDATE """"Products"""" SET benchmark_col_{i:D3} = 'default_value';
ALTER TABLE """"Products"""" ALTER COLUMN benchmark_col_{i:D3} SET NOT NULL;
ALTER TABLE """"Products"""" ALTER COLUMN benchmark_col_{i:D3} SET DEFAULT 'default_value';
";

    private static string EvenDownSql(int i) =>
        $@"ALTER TABLE """"Products"""" DROP COLUMN IF EXISTS benchmark_col_{i:D3};";
}
