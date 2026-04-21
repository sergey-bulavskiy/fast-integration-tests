// tools/BenchmarkRunner/Report/ReportGenerator.cs
using System.Text.Json;
using BenchmarkRunner.Models;

namespace BenchmarkRunner.Report;

/// <summary>Генерирует HTML отчёт и results.json из данных бенчмарка.</summary>
public class ReportGenerator
{
    private readonly string _templatePath;
    private readonly string _outputDir;

    /// <summary>Инициализирует генератор с корневой директорией репозитория.</summary>
    public ReportGenerator(string repoRoot)
    {
        _templatePath = Path.Combine(AppContext.BaseDirectory, "Report", "report-template.html");
        _outputDir    = Path.Combine(repoRoot, "benchmark-results");
    }

    /// <summary>Сериализует отчёт в JSON, инлайнит в HTML шаблон, сохраняет оба файла.</summary>
    public void Generate(BenchmarkReport report)
    {
        Directory.CreateDirectory(_outputDir);

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented        = true,
        };

        var json     = JsonSerializer.Serialize(report, options);
        var jsonPath = Path.Combine(_outputDir, "results.json");
        File.WriteAllText(jsonPath, json);
        Console.WriteLine($"\n[REPORT] results.json saved");

        var template = File.ReadAllText(_templatePath);
        var html     = template.Replace("/*INJECT_JSON*/", json);
        var htmlPath = Path.Combine(_outputDir, "report.html");
        File.WriteAllText(htmlPath, html);
        Console.WriteLine($"[REPORT] report.html saved: {htmlPath}");
    }
}
