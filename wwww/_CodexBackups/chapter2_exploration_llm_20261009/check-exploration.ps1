param([string]$Label='final-local-v2',[string]$ExpiryReport='expiry-pending')
$r=Get-Content -LiteralPath (Join-Path $PSScriptRoot ($Label+'-result.json')) -Raw | ConvertFrom-Json
$failures=[Collections.Generic.List[string]]::new()
foreach($field in @('noArrowsDuringExplore','workersWait','allPrompts','allOutsideHidden','insideAccepted','outsideRejected','typingPDoesNotSkip','returnedToEscort','arrivedAtTree')) {
    if(!$r.$field){$failures.Add($field)}
}
$expiry=Get-Content -LiteralPath (Join-Path $PSScriptRoot ($ExpiryReport+'-result.json')) -Raw | ConvertFrom-Json
if(!$expiry.forcedDeadlineWhilePending -or !$expiry.expiryWaited -or !$expiry.returnedToEscort -or !$expiry.arrivedAtTree -or $expiry.expiryError -or [string]::IsNullOrWhiteSpace($expiry.expiryReply) -or $expiry.errors -ne 0){$failures.Add('Pending answer at deadline did not finish and resume escort')}
if($r.count -ne 5 -or $r.answers.Count -ne 5){$failures.Add('Five roadside characters must be tested')}
if($r.configuredSeconds -ne 180 -or $r.explorationElapsed -lt 180){$failures.Add('Three-minute timer')}
if($r.escortFirstJump -gt .05){$failures.Add('Player snapped when escort resumed')}
if($r.errors -ne 0 -or $r.dirty -or $r.playingAfterExit){$failures.Add('Runtime or exit state')}
if($r.introduction[0] -cne '前面就是西仔希克。這片森林，守護著我們的生活。' -or $r.introduction[1] -cne '現在開始你可以自由探索，了解這裡的環境'){$failures.Add('Introduction order or text')}
foreach($answer in $r.answers){if($answer.error -or [string]::IsNullOrWhiteSpace($answer.answer)){$failures.Add('Missing real reply: '+$answer.actor)}}
if($r.expiryError -or $r.expiryReply -notmatch '阿山'){$failures.Add('Same NPC did not remember introduced player name')}
if($r.answers[-1].answer -match '阿山'){$failures.Add('Player name leaked into a different NPC conversation')}
$summary=[ordered]@{label=$Label;pendingReplyCase=$ExpiryReport;passed=($failures.Count -eq 0);participants=$r.count;errors=$r.errors;warnings=$r.warnings;explorationElapsed=$r.explorationElapsed;replySeconds=@($r.answers.seconds);failures=@($failures.ToArray())}
$summary | ConvertTo-Json -Depth 4 | Tee-Object -FilePath (Join-Path $PSScriptRoot ($Label+'-checks.json'))
