param(
    [int]$Threads = 4
)

$start = Get-Date

Write-Host "Respawn | threads=$Threads"
dotnet test tests/FastIntegrationTests.Tests.Respawn `
    -- xUnit.MaxParallelThreads=$Threads

$elapsed = (Get-Date) - $start
Write-Host "`nВремя выполнения: $($elapsed.ToString('mm\:ss\.fff'))"
