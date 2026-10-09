param([string]$Label)
$result=Get-Content -LiteralPath (Join-Path $PSScriptRoot ($Label+'-result.json')) -Raw | ConvertFrom-Json
$failures=[Collections.Generic.List[string]]::new()
foreach($field in @('finished','allInside','allOutside','hintHidden','noArrowsWhileApproaching','guideInFront','waited','arrived','patrolsStopped','pathsComplete')){
    if(!$result.$field){$failures.Add($field)}
}
if($result.errors -ne 0){$failures.Add('runtime errors')}
if($result.playing -or $result.dirty){$failures.Add('scene not clean after run')}
if($result.participants -ne 7 -or $result.pathsTested -ne 5){$failures.Add('cast or path sample count')}
if($result.frontDistance -lt 1 -or $result.frontDistance -gt 3.8 -or $result.frontAngle -gt 49){$failures.Add('guide not in front')}
if($result.bodyJump -gt .1 -or $result.viewJump -gt .1 -or $result.guideMaxStep -gt .6){$failures.Add('unexpected teleport')}
if($result.witnessRadius -gt .91 -or $result.witnessRadius -lt .1){$failures.Add('witness patrol range')}
if($Label -eq 'protect-full'){
    if($result.configuredSeconds -ne 180 -or $result.exploreElapsed -lt 180){$failures.Add('full timer')}
    if(!$result.expiryWaited -or [string]::IsNullOrWhiteSpace($result.answer) -or ![string]::IsNullOrEmpty($result.error)){$failures.Add('pending real LLM answer')}
    if(!$result.rescue1 -or !$result.rescue2 -or $result.stage -ne 'Meeting'){$failures.Add('both witnesses must examine casualty and reach meeting')}
}
if($Label.Contains('west') -and (!$result.treesFell -or $result.stage -ne 'Meeting')){$failures.Add('fell branch')}
$summary=[ordered]@{label=$Label;passed=$failures.Count -eq 0;errors=$result.errors;warnings=$result.warnings;frontDistance=$result.frontDistance;frontAngle=$result.frontAngle;witnessRadius=$result.witnessRadius;failures=@($failures.ToArray())}
$summary | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $PSScriptRoot ($Label+'-checks.json')) -Encoding utf8
$summary | ConvertTo-Json -Depth 5
if($failures.Count){exit 1}
