// tools/BenchmarkRunner/Scale/ClassScaleManager.cs
using System.Text;
using System.Text.RegularExpressions;

namespace BenchmarkRunner.Scale;

/// <summary>Генерирует и удаляет файлы подклассов для масштабирования числа тест-классов.</summary>
public class ClassScaleManager
{
    private const string GeneratedFileName = "BenchmarkScaleClasses.cs";

    private readonly string[] _testProjectPaths;

    /// <summary>Инициализирует менеджер с корневой директорией репозитория.</summary>
    public ClassScaleManager(string repoRoot)
    {
        _testProjectPaths =
        [
            Path.Combine(repoRoot, "tests", "FastIntegrationTests.Tests.IntegreSQL"),
            Path.Combine(repoRoot, "tests", "FastIntegrationTests.Tests.Respawn"),
            Path.Combine(repoRoot, "tests", "FastIntegrationTests.Tests.Testcontainers"),
            Path.Combine(repoRoot, "tests", "FastIntegrationTests.Tests.TestcontainersShared"),
        ];
    }

    /// <summary>
    /// Генерирует <c>(scaleFactor - 1)</c> подклассов для каждого тест-класса в четырёх проектах.
    /// Каждый подкласс получает свой <c>IClassFixture</c> — честный per-class overhead.
    /// </summary>
    public void AddScaleClasses(int scaleFactor)
    {
        if (scaleFactor <= 1) return;

        Console.WriteLine($"\n[SCALE] Adding {scaleFactor - 1} extra copies per class (total factor: {scaleFactor})...");
        foreach (var projectPath in _testProjectPaths)
        {
            var classes = DiscoverTestClasses(projectPath);
            var content = GenerateScaleFile(classes, scaleFactor);
            File.WriteAllText(Path.Combine(projectPath, GeneratedFileName), content);
            Console.WriteLine($"[SCALE] {Path.GetFileName(projectPath)}: {classes.Count} classes × {scaleFactor - 1} copies");
        }
    }

    /// <summary>Удаляет сгенерированные файлы из четырёх тест-проектов.</summary>
    public void RemoveScaleClasses()
    {
        foreach (var projectPath in _testProjectPaths)
        {
            var path = Path.Combine(projectPath, GeneratedFileName);
            if (!File.Exists(path)) continue;
            File.Delete(path);
            Console.WriteLine($"[SCALE] Removed {Path.GetFileName(projectPath)}/{GeneratedFileName}");
        }
    }

    private static List<TestClassInfo> DiscoverTestClasses(string projectPath)
    {
        var result = new List<TestClassInfo>();
        var sep    = Path.DirectorySeparatorChar;

        var files = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{sep}obj{sep}") &&
                        !f.Contains($"{sep}bin{sep}") &&
                        !f.Contains($"{sep}Infrastructure{sep}") &&
                        !Path.GetFileName(f).Equals("GlobalUsings.cs",  StringComparison.OrdinalIgnoreCase) &&
                        !Path.GetFileName(f).Equals(GeneratedFileName,   StringComparison.OrdinalIgnoreCase));

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            if (!content.Contains("[Fact]") && !content.Contains("[Theory]")) continue;

            var ns          = ExtractNamespace(content);
            var className   = ExtractClassName(content);
            var fixtureType = ExtractFixtureType(content);

            if (ns is null || className is null) continue;
            result.Add(new TestClassInfo(ns, className, fixtureType));
        }

        return result;
    }

    private static string? ExtractNamespace(string content)
    {
        var m = Regex.Match(content, @"^namespace\s+([\w.]+)\s*[;{]", RegexOptions.Multiline);
        return m.Success ? m.Groups[1].Value : null;
    }

    private static string? ExtractClassName(string content)
    {
        var m = Regex.Match(content, @"public\s+class\s+(\w+)");
        return m.Success ? m.Groups[1].Value : null;
    }

    private static string? ExtractFixtureType(string content)
    {
        var m = Regex.Match(content, @"public\s+\w+\((\w+Fixture)\s+fixture\)");
        return m.Success ? m.Groups[1].Value : null;
    }

    private static string GenerateScaleFile(List<TestClassInfo> classes, int scaleFactor)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// BenchmarkScaleClasses.cs — сгенерирован BenchmarkRunner, не редактировать");
        sb.AppendLine("// ReSharper disable All");
        sb.AppendLine("#pragma warning disable");

        foreach (var group in classes.GroupBy(c => c.Namespace))
        {
            sb.AppendLine();
            sb.AppendLine($"namespace {group.Key}");
            sb.AppendLine("{");
            foreach (var cls in group)
            {
                for (var i = 2; i <= scaleFactor; i++)
                {
                    if (cls.FixtureType is null)
                    {
                        sb.AppendLine($"    public class {cls.ClassName}_{i} : {cls.ClassName} {{ }}");
                    }
                    else
                    {
                        sb.AppendLine($"    public class {cls.ClassName}_{i} : {cls.ClassName}");
                        sb.AppendLine("    {");
                        sb.AppendLine($"        public {cls.ClassName}_{i}({cls.FixtureType} fixture) : base(fixture) {{ }}");
                        sb.AppendLine("    }");
                    }
                }
            }
            sb.AppendLine("}");
        }

        return sb.ToString();
    }

    private record TestClassInfo(string Namespace, string ClassName, string? FixtureType);
}
