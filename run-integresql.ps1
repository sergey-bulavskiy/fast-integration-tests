param(
    [int]$Threads = 4
)

$start = Get-Date

Write-Host "IntegreSQL | threads=$Threads"
dotnet test tests/FastIntegrationTests.Tests.IntegreSQL `
    -- xUnit.MaxParallelThreads=$Threads

$elapsed = (Get-Date) - $start
Write-Host "`nВремя выполнения: $($elapsed.ToString('mm\:ss\.fff'))"
