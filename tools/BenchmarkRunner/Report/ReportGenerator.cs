// tools/BenchmarkRunner/Report/ReportGenerator.cs
using System.Text.Json;
using BenchmarkRunner.Models;

namespace BenchmarkRunner.Report;

/// <summary>Генерирует HTML отчёт и results.json из данных бенчмарка.</summary>
public class ReportGenerator
{
    private readonly string _templatePath;
    private readonly string _outputDir;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented        = true,
    };

    /// <summary>Инициализирует генератор с корневой директорией репозитория.</summary>
    public ReportGenerator(string repoRoot)
    {
        _templatePath = Path.Combine(AppContext.BaseDirectory, "Report", "report-template.html");
        _outputDir    = Path.Combine(repoRoot, "benchmark-results");
    }

    /// <summary>Сохраняет промежуточный results.json после каждой точки данных.</summary>
    public void SaveJson(BenchmarkReport report)
    {
        Directory.CreateDirectory(_outputDir);
        var json = JsonSerializer.Serialize(report, JsonOptions);
        File.WriteAllText(Path.Combine(_outputDir, "results.json"), json);
    }

    /// <summary>Сериализует отчёт в JSON, инлайнит в HTML шаблон, сохраняет оба файла.</summary>
    public void Generate(BenchmarkReport report)
    {
        SaveJson(report);
        Console.WriteLine($"\n[REPORT] results.json saved");

        var json     = JsonSerializer.Serialize(report, JsonOptions);
        var template = File.ReadAllText(_templatePath);
        var html     = template.Replace("/*INJECT_JSON*/", json);
        var htmlPath = Path.Combine(_outputDir, "report.html");
        File.WriteAllText(htmlPath, html);
        Console.WriteLine($"[REPORT] report.html saved: {htmlPath}");
    }
}
