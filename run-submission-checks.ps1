# Run from the repository root, on the submission branch.
# Does not modify any database or deploy the application.
$ErrorActionPreference = 'Continue'
$reportDir = Join-Path (Get-Location) 'submission-check-results'
New-Item -ItemType Directory -Force -Path $reportDir | Out-Null
$results = New-Object System.Collections.Generic.List[object]
$steps = @(
    @{ Name = 'Restore'; Args = @('restore','GmachManagement.slnx') },
    @{ Name = 'Build'; Args = @('build','GmachManagement.slnx','--no-restore','-c','Release') },
    @{ Name = 'Tests'; Args = @('test','GmachManagement.slnx','--no-restore','-c','Release','--logger',"trx;LogFileName=submission-tests.trx") },
    @{ Name = 'Coverage'; Args = @('test','GmachManagement.Tests/GmachManagement.Tests.csproj','--no-restore','-c','Release','--collect:XPlat Code Coverage') },
    @{ Name = 'Vulnerabilities'; Args = @('list','GmachManagement.slnx','package','--vulnerable','--include-transitive') },
    @{ Name = 'Publish'; Args = @('publish','GmachManagement.API/GmachManagement.API.csproj','-c','Release','-o',(Join-Path $reportDir 'api-publish')) }
)
foreach ($step in $steps) {
    Write-Host ("=== " + $step.Name + " ===") -ForegroundColor Cyan
    $log = Join-Path $reportDir ($step.Name + '.log')
    & dotnet @($step.Args) 2>&1 | Tee-Object -FilePath $log
    $exit = $LASTEXITCODE
    $results.Add([pscustomobject]@{ Step = $step.Name; ExitCode = $exit; Log = $log })
}
$results | Format-Table -AutoSize
$results | Export-Csv -NoTypeInformation -Encoding UTF8 (Join-Path $reportDir 'summary.csv')
if ($results.Where({ $_.ExitCode -ne 0 }).Count -gt 0) {
    Write-Warning "Some checks failed. See submission-check-results logs."
    exit 1
}
Write-Host 'All commands exited successfully. Review vulnerability warnings and integration requirements separately.' -ForegroundColor Green
