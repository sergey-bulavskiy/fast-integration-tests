param(
    [int]$Repeat = 5,
    [int]$Threads = 4
)

$env:TEST_REPEAT = $Repeat
$start = Get-Date

Write-Host "Testcontainers | repeat=$Repeat | threads=$Threads"
dotnet test tests/FastIntegrationTests.Tests `
    --filter "FullyQualifiedName~Tests.Testcontainers" `
    -- xUnit.MaxParallelThreads=$Threads

$elapsed = (Get-Date) - $start
Write-Host "`nВремя выполнения: $($elapsed.ToString('mm\:ss\.fff'))"
