param(
    [int]$Repeat = 5,
    [int]$Threads = 4
)

$env:TEST_REPEAT = $Repeat
$start = Get-Date

Write-Host "Respawn | repeat=$Repeat | threads=$Threads"
dotnet test tests/FastIntegrationTests.Tests `
    --filter "FullyQualifiedName~Tests.Respawn" `
    -- xUnit.MaxParallelThreads=$Threads

$elapsed = (Get-Date) - $start
Write-Host "`nВремя выполнения: $($elapsed.ToString('mm\:ss\.fff'))"
