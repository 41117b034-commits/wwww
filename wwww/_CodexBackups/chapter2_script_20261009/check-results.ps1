param([string]$Label)
$dir = $PSScriptRoot
$expected = Get-Content -LiteralPath (Join-Path $dir 'expected-lines.json') -Raw | ConvertFrom-Json
$result = Get-Content -LiteralPath (Join-Path $dir ($Label+'-result.json')) -Raw | ConvertFrom-Json
$branch = if($Label.Contains('refuse')){'refuse'}else{'support'}
$lines = @($expected.common)+@($expected.$branch)
$names = @('達多·莫那')*4+@('瓦旦')*5+@('莫那·魯道')*6+@('巴萬·拿威')*3+@('瓦旦')+@('莫那·魯道')*7
if($branch -eq 'support'){$names += @('達多·莫那')*2+@('巴萬與眾戰士')*3+@('莫那·魯道')*4}else{$names += @('玩家')+@('莫那·魯道')*7}
$failures = [Collections.Generic.List[string]]::new()
if($result.lines.Count -ne $lines.Count){$failures.Add('Line count mismatch')}
for($i=0;$i -lt [Math]::Min($lines.Count,$result.lines.Count);$i++){
    if($result.lines[$i].text -cne $lines[$i]){$failures.Add("Text mismatch at $i")}
    if($result.lines[$i].speaker -cne $names[$i]){$failures.Add("Speaker mismatch at $i")}
    if($result.lines[$i].visualLines -ne 1){$failures.Add("Visual line count mismatch at $i")}
}
if(!$result.completed -or $result.playingAfterExit){$failures.Add('Did not complete and stop Play Mode')}
if($result.errors -ne 0){$failures.Add('Runtime errors')}
if(!$result.prefsUnchanged){$failures.Add('Preview modified previous result')}
if($branch -eq 'support'){
    if(!$result.endingCard -or !$result.allStanding -or !$result.knifePlanted){$failures.Add('Missing support choreography')}
    if($result.knifeTipHeight -gt .01 -or $result.knifeTipHeight -lt -.25){$failures.Add('Knife ground contact out of bounds')}
}else{
    if($result.endingCard -or $result.allStanding -or $result.knifePlanted -or !$result.approached){$failures.Add('Wrong refusal choreography')}
    if($result.minApproachFireDistance -lt 1.1){$failures.Add('Refusal walk crossed fire')}
}
$summary = [ordered]@{label=$Label;passed=($failures.Count -eq 0);dialogueLines=$result.lines.Count;errors=$result.errors;warnings=$result.warnings;playingAfterExit=$result.playingAfterExit;failures=@($failures.ToArray())}
$summary | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $dir ($Label+'-checks.json')) -Encoding utf8
$summary | ConvertTo-Json -Depth 5
if($failures.Count -gt 0){exit 1}
