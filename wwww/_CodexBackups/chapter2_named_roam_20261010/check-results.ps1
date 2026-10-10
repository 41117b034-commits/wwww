$ErrorActionPreference='Stop'
$taskDir='_CodexBackups/chapter2_named_roam_20261010'
foreach($taskLabel in @('protect-full','final-west','final-behind')){
    $taskReport=Get-Content "$taskDir/$taskLabel-result.json" -Raw -Encoding UTF8 | ConvertFrom-Json
    $taskChecks=[ordered]@{
        finished=($taskReport.finished -and !$taskReport.playing -and !$taskReport.dirty)
        noErrors=($taskReport.errors -eq 0)
        nineNamedNPCs=($taskReport.participants -eq 9 -and $taskReport.allNamed -and $taskReport.childPersona)
        threeMeterRange=($taskReport.allInside -and $taskReport.allOutside)
        guideRoams=($taskReport.guideRoamed -gt 1.5)
        nearbyArrival=($taskReport.spawnDistance -le 4.6 -and $taskReport.approachSeconds -gt .2 -and $taskReport.approachSeconds -lt 4 -and $taskReport.approachLength -le 6.6)
        guideVisible=($taskReport.guideInFront -and $taskReport.bodyJump -lt .1 -and $taskReport.noArrowsWhileApproaching)
        exchange=($taskReport.exchangeVisible -and $taskReport.facesOfficer -and $taskReport.stepForward -gt 1 -and $taskReport.subtitleName -eq '阿威·比胡')
        navigation=($taskReport.pathsComplete -and $taskReport.pathsTested -eq 5 -and $taskReport.arrived -and $taskReport.waited)
        fixedWitnesses=($taskReport.witnessRadius -lt 1 -and $taskReport.patrolsStopped)
    }
    if($taskLabel -eq 'protect-full'){$taskChecks.fullTimer=($taskReport.configuredSeconds -eq 180 -and $taskReport.exploreElapsed -ge 180 -and $taskReport.expiryWaited -and $taskReport.answer.Length -gt 0 -and $taskReport.officerChat)}
    if($taskLabel -eq 'final-west'){$taskChecks.felled=$taskReport.treesFell}else{$taskChecks.bothWitnesses=($taskReport.rescue1 -and $taskReport.rescue2)}
    if($taskLabel -eq 'final-behind'){$taskChecks.officerKeyboard=$taskReport.officerChat}
    $taskPassed=@($taskChecks.Values | Where-Object {$_ -ne $true}).Count -eq 0
    [ordered]@{label=$taskLabel;passed=$taskPassed;checks=$taskChecks;warnings=$taskReport.warnings} | ConvertTo-Json -Depth 5 | Set-Content "$taskDir/$taskLabel-checks.json" -Encoding UTF8
    if(!$taskPassed){throw "$taskLabel failed: $($taskChecks | ConvertTo-Json -Compress)"}
    Write-Output "$taskLabel passed"
}
