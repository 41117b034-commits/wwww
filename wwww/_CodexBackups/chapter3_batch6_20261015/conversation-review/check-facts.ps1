$ErrorActionPreference = 'Stop'
$reviewRoot = Join-Path $PSScriptRoot ''
$source = Get-Content -LiteralPath (Join-Path $reviewRoot 'Chapter3EndingConversation.cs') -Raw
$begin = $source.IndexOf('    string Outcome()=>')
$end = $source.IndexOf('    string Fallback(string text)')
$methods = $source.Substring($begin, $end-$begin)
$wrapper = @"
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
public class ConversationFactsReview {
    public class Chapter3SchoolBattle { public enum Mission { Bawan, Pihu } }
    public class Chapter3Weapons { public enum Weapon { Knife, Rifle } }
    public class Data { public bool pihuSurvived; public Chapter3SchoolBattle.Mission mission; public Chapter3Weapons.Weapon weapon; }
    Data result;
    int CompletedTurns=0;
    public ConversationFactsReview(bool survived, bool left) { result=new Data { pihuSurvived=survived, mission=left?Chapter3SchoolBattle.Mission.Bawan:Chapter3SchoolBattle.Mission.Pihu }; }
    public string Reply(string text) { return FixedFactReply(text); }
    public string Prompt() { return Persona(); }
$methods
    static bool Has(string text,params string[] words){foreach(string word in words)if(text.Contains(word))return true;return false;}
}
"@
Add-Type -TypeDefinition $wrapper -WarningAction SilentlyContinue
$checks = [System.Collections.Generic.List[object]]::new()
function Check([string]$name, [bool]$condition, [string]$reply) { $checks.Add([pscustomobject]@{ name=$name; pass=$condition; reply=$reply }) }
foreach ($survived in @($true,$false)) {
    foreach ($left in @($true,$false)) {
        $subject = [ConversationFactsReview]::new($survived,$left)
        $mode = "survived=$survived,left=$left"
        $reply=$subject.Reply('達多還活著嗎？')
        Check "$mode Tado status" ($reply.Contains('達多還活著，沒有受傷') -and $reply.Contains('接應處') -and !$reply.Contains('你選擇')) $reply
        $reply=$subject.Reply('巴萬受傷了嗎？')
        Check "$mode Bawan injury" ($reply.Contains('我就是巴萬，還活著，沒有受傷') -and $reply.Contains('左側')) $reply
        $reply=$subject.Reply('巴萬你知道比胡死了嗎？')
        Check "$mode Bawan vocative Pihu subject" (($survived -and $reply.Contains('他還活著') -and !$reply.Contains('已經死了')) -or (!$survived -and $reply.Contains('達多回去確認，比胡已經死了') -and $reply.Contains('告訴我'))) $reply
        $reply=$subject.Reply('巴萬，你知道達多還活著嗎？')
        Check "$mode Bawan vocative Tado subject" ($reply.StartsWith('達多還活著，沒有受傷')) $reply
        $reply=$subject.Reply('達多還活著嗎？巴萬受傷了嗎？')
        Check "$mode two named subjects" ($reply.Contains('達多還活著，沒有受傷') -and $reply.Contains('我就是巴萬，還活著，沒有受傷')) $reply
        foreach ($question in @('莫那是不是一直都跟著我們？','莫那有跟去學校嗎？','莫那知道比胡死了嗎？')) {
            $reply=$subject.Reply($question)
            Check "$mode $question" ($reply.Contains('沒有跟我們到學校') -and $reply.Contains('尚未到接應處') -and $reply.Contains('稍後向他報告') -and $reply.Contains('才會知道')) $reply
        }
        $reply=$subject.Reply('小隊現在幾人？')
        Check "$mode old team question" ($reply.Contains($(if($survived){'四人'}else{'三人'})) -and $reply.Contains('莫那不算')) $reply
        $reply=$subject.Reply('比胡的結果是什麼？')
        Check "$mode old result question" (($survived -and $reply.Contains('他還活著')) -or (!$survived -and $reply.Contains('達多') -and $reply.Contains('死') -and $reply.Contains('告訴我'))) $reply
        $reply=$subject.Reply('你親眼見到右邊的事嗎？')
        Check "$mode old witnessed route question" (($left -and $reply.Contains('聽他們說')) -or (!$left -and $reply.Contains('我與達多走左側') -and $reply.Contains('聽你們說'))) $reply
        foreach ($question in @('我很害怕，心裡一直靜不下來。','我想先緩一口氣，把這些感受慢慢說清楚。')) {
            $reply=$subject.Reply($question)
            Check "$mode emotional question remains generated" ([string]::IsNullOrEmpty($reply)) $reply
        }
        $prompt=$subject.Prompt()
        Check "$mode concise phase-correct persona" ($prompt.Length -lt 400 -and $prompt.Contains('尚未抵達接應處') -and $prompt.Contains('稍後聽報告才知道')) $prompt
    }
}
$watch=[Diagnostics.Stopwatch]::StartNew()
$subject.Reply('巴萬'+('是不是'*50)) | Out-Null
$watch.Stop()
Check 'Repeated connectors remain bounded' ($watch.ElapsedMilliseconds -lt 100) "$($watch.ElapsedMilliseconds) ms"
$report=[pscustomobject]@{ completed=$true; passed=@($checks|Where-Object pass).Count; failed=@($checks|Where-Object { !$_.pass }).Count; source='Extracted fixed-fact methods from staged source, without Unity'; checks=$checks }
$report|ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $reviewRoot 'fact-checks.json') -Encoding utf8
$report|Select-Object completed,passed,failed,source|ConvertTo-Json
if($report.failed -gt 0) { $checks|Where-Object { !$_.pass }|ConvertTo-Json -Depth 5; exit 1 }

