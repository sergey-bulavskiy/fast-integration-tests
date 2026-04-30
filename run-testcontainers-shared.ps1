param(
    [int]$Threads = 4
)

$start = Get-Date

Write-Host "TestcontainersShared | threads=$Threads"
dotnet test tests/FastIntegrationTests.Tests.TestcontainersShared `
    -- xUnit.MaxParallelThreads=$Threads

$elapsed = (Get-Date) - $start
Write-Host "`nВремя выполнения: $($elapsed.ToString('mm\:ss\.fff'))"
