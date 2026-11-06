param([switch]$RemainingOnly,[string]$Only)
$ErrorActionPreference='Stop'
$evidence=Join-Path (Get-Location) '_CodexBackups/chapter3_follow_input_20261106'
$jobs=@(
    @{mode='navigation'; command='navigation'; channel='probe-command.txt'},
    @{mode='foliage-continuous-final'; command='foliage-continuous-final'; channel='probe-command.txt'},
    @{mode='input-final'; command='input-final'; channel='probe-command.txt'},
    @{mode='speed'; command='run-speed'; channel='command.txt'},
    @{mode='leave'; command='run-leave'; channel='command.txt'},
    @{mode='rescue'; command='run-rescue'; channel='command.txt'}
)
if($RemainingOnly) { $jobs=@($jobs | Where-Object { $_.mode -in @('leave','rescue') }) }
if($Only) { $jobs=@($jobs | Where-Object { $_.mode -eq $Only }) }
foreach($job in $jobs) {
    $reportFile=Join-Path $evidence ('report-'+$job.mode+'.json')
    if(Test-Path -LiteralPath $reportFile) { throw "Report already exists: $reportFile" }
    $started=[DateTime]::UtcNow
    [IO.File]::WriteAllText((Join-Path $evidence 'suite-next.txt'),$job.command)
    Move-Item -LiteralPath (Join-Path $evidence 'suite-next.txt') -Destination (Join-Path $evidence $job.channel)
    Write-Output ('START '+$job.mode)
    while($true) {
        Start-Sleep -Milliseconds 500
        if(([DateTime]::UtcNow-$started).TotalMinutes -gt 12) { throw ('Timeout '+$job.mode) }
        $state=Get-Item -LiteralPath (Join-Path $evidence 'edit-state.txt') -ErrorAction SilentlyContinue
        $result=Get-Item -LiteralPath $reportFile -ErrorAction SilentlyContinue
        if(!$state -or !$result -or $state.LastWriteTimeUtc -lt $started -or $state.LastWriteTimeUtc -lt $result.LastWriteTimeUtc) { continue }
        if([IO.File]::ReadAllText($state.FullName) -notmatch 'playing=False') { continue }
        $report=[IO.File]::ReadAllText($result.FullName) | ConvertFrom-Json
        Write-Output (@{mode=$job.mode;completed=$report.completed;checks=$report.checks.Count;errors=$report.errors} | ConvertTo-Json -Compress)
        if(!$report.completed -or $report.errors.Count -gt 0) { throw ('Failed '+$job.mode) }
        break
    }
}
