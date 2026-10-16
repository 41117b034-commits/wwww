$ErrorActionPreference = 'Stop'
$checkDir = $PSScriptRoot
$scenarios = @('test-audio','test-lifecycle','test-dialogue','test-live-llm','test-cancel')
for($index=0;$index -lt $scenarios.Count;$index++) {
    $scenario = $scenarios[$index]
    if(Test-Path -LiteralPath (Join-Path $checkDir 'queue-stop.txt')) { Write-Output 'Queue stopped by owner'; exit 2 }
    if($index -gt 0) {
        Start-Sleep -Seconds 3
        Set-Content -LiteralPath (Join-Path $checkDir 'command.txt') -Value $scenario -Encoding utf8
    }
    [pscustomobject]@{ mode=$scenario; state='running'; index=$index; total=$scenarios.Count } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $checkDir 'queue-status.json') -Encoding utf8
    $deadline = [DateTime]::UtcNow.AddMinutes(16)
    $reportPath=Join-Path $checkDir ($scenario+'.json')
    while(-not (Test-Path -LiteralPath $reportPath)) {
        if(Test-Path -LiteralPath (Join-Path $checkDir 'queue-stop.txt')) { Write-Output 'Queue stopped by owner'; exit 2 }
        if(Test-Path -LiteralPath (Join-Path $checkDir 'command-error.txt')) { throw (Get-Content -LiteralPath (Join-Path $checkDir 'command-error.txt') -Raw) }
        if([DateTime]::UtcNow -gt $deadline) { throw ('Timeout: '+$scenario) }
        Start-Sleep -Seconds 2
    }
    Start-Sleep -Seconds 1
    $report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
    Write-Output ($scenario+': completed='+$report.completed+' checks='+$report.checks.Count+' errors='+$report.errors.Count+' warnings='+$report.warnings.Count+' seconds='+[Math]::Round($report.elapsed,1))
    if(-not $report.completed -or $report.errors.Count -gt 0) {
        [pscustomobject]@{ mode=$scenario; state='failed'; errors=$report.errors } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $checkDir 'queue-status.json') -Encoding utf8
        exit 1
    }
}
[pscustomobject]@{ state='completed'; total=$scenarios.Count } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $checkDir 'queue-status.json') -Encoding utf8
Set-Content -LiteralPath (Join-Path $checkDir 'command.txt') -Value 'preflight' -Encoding utf8


