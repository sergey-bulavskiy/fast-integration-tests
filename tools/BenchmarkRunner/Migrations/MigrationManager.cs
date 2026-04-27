// tools/BenchmarkRunner/Migrations/MigrationManager.cs
namespace BenchmarkRunner.Migrations;

/// <summary>Управляет benchmark-миграциями: скрывает часть для Scenario 1, восстанавливает после.</summary>
public class MigrationManager
{
    private readonly string _migrationsPath;
    private readonly string _hiddenPath;

    /// <summary>Инициализирует менеджер с корневой директорией репозитория.</summary>
    public MigrationManager(string repoRoot)
    {
        _migrationsPath = Path.Combine(
            repoRoot, "src", "FastIntegrationTests.Infrastructure", "Migrations");
        _hiddenPath = Path.Combine(_migrationsPath, "__hidden");
    }

    /// <summary>
    /// Скрывает последние <paramref name="count"/> benchmark-миграций (timestamp 20990101...)
    /// перемещая их пары (.cs + .Designer.cs) в <c>__hidden/</c>.
    /// Вызывающий код отвечает за последующий rebuild.
    /// </summary>
    public void HideMigrations(int count)
    {
        if (count <= 0) return;

        Directory.CreateDirectory(_hiddenPath);

        var csFiles = Directory.GetFiles(_migrationsPath, "20990101*.cs")
            .Where(f => !Path.GetDirectoryName(f)!.EndsWith("__hidden", StringComparison.OrdinalIgnoreCase)
                     && !Path.GetFileName(f).EndsWith(".Designer.cs"))
            .Order()
            .TakeLast(count)
            .ToList();

        if (csFiles.Count < count)
            Console.Error.WriteLine($"[MIGRATIONS] Warning: requested to hide {count} but only {csFiles.Count} benchmark migrations found");

        Console.WriteLine($"\n[MIGRATIONS] Hiding {csFiles.Count}/{count} benchmark migrations...");
        var movedFiles = 0;
        foreach (var cs in csFiles)
        {
            var designer = Path.Combine(
                Path.GetDirectoryName(cs)!,
                Path.GetFileNameWithoutExtension(cs) + ".Designer.cs");

            File.Move(cs, Path.Combine(_hiddenPath, Path.GetFileName(cs)));
            movedFiles++;
            if (File.Exists(designer))
            {
                File.Move(designer, Path.Combine(_hiddenPath, Path.GetFileName(designer)));
                movedFiles++;
            }
        }
        Console.WriteLine($"[MIGRATIONS] Hidden {csFiles.Count} migrations ({movedFiles} files)");
    }

    /// <summary>
    /// Восстанавливает все скрытые миграции из <c>__hidden/</c> обратно в папку Migrations.
    /// No-op если папка пуста или не существует.
    /// </summary>
    public void RestoreHiddenMigrations()
    {
        if (!Directory.Exists(_hiddenPath)) return;

        var files = Directory.GetFiles(_hiddenPath);
        if (files.Length == 0) return;

        Console.WriteLine($"\n[MIGRATIONS] Restoring {files.Length} hidden migration files...");
        foreach (var f in files)
            File.Move(f, Path.Combine(_migrationsPath, Path.GetFileName(f)));
        Directory.Delete(_hiddenPath);
        Console.WriteLine($"[MIGRATIONS] Restored {files.Length} files");
    }
}
