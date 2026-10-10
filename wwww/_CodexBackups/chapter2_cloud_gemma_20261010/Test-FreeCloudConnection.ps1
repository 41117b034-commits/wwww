$ErrorActionPreference = 'Stop'
$report = [ordered]@{ success=$false; model=$null; statusCode=0; seconds=0; answer=$null; error=$null; usage=$null }
$client = $null; $response = $null; $request = $null; $plain = $null; $cloudKey = $null
try {
    $configPath = Join-Path $env:LOCALAPPDATA 'WusheNPC/cloud-gemma.json'
    $config = Get-Content -LiteralPath $configPath -Raw -Encoding UTF8 | ConvertFrom-Json
    if (-not $config.enabled -or -not $config.freeTierConfirmed -or $config.model -notin @('gemma-4-26b-a4b-it','gemma-4-31b-it')) {
        throw [InvalidOperationException]::new('Free cloud configuration is not enabled.')
    }
    $report.model = $config.model
    $plain = [System.Security.Cryptography.ProtectedData]::Unprotect([Convert]::FromBase64String($config.protectedKey), $null, [System.Security.Cryptography.DataProtectionScope]::CurrentUser)
    $cloudKey = [Text.Encoding]::UTF8.GetString($plain)
    [Array]::Clear($plain,0,$plain.Length)
    $persona = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'local-regression/unity-persona-0.txt') -Raw -Encoding UTF8
    $body = @{
        systemInstruction=@{parts=@(@{text=$persona})}
        contents=@(@{role='user';parts=@(@{text='你猜我叫甚麼名字'})})
        generationConfig=@{temperature=0.65;maxOutputTokens=256;thinkingConfig=@{thinkingLevel='minimal'}}
    } | ConvertTo-Json -Depth 10 -Compress
    $handler = [System.Net.Http.HttpClientHandler]::new()
    $handler.AllowAutoRedirect = $false
    $client = [System.Net.Http.HttpClient]::new($handler)
    $client.Timeout = [TimeSpan]::FromSeconds(30)
    $uri = 'https://generativelanguage.googleapis.com/v1beta/models/' + $config.model + ':generateContent'
    $request = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::Post,$uri)
    $request.Headers.Add('x-goog-api-key',$cloudKey)
    $request.Content = [System.Net.Http.StringContent]::new($body,[Text.Encoding]::UTF8,'application/json')
    $watch = [Diagnostics.Stopwatch]::StartNew()
    $response = $client.SendAsync($request).GetAwaiter().GetResult()
    $report.seconds = [Math]::Round($watch.Elapsed.TotalSeconds,3)
    $report.statusCode = [int]$response.StatusCode
    $parsed = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult() | ConvertFrom-Json
    if ($response.IsSuccessStatusCode) {
        $report.answer = (($parsed.candidates[0].content.parts | Where-Object { -not $_.thought } | ForEach-Object { $_.text }) -join '').Trim()
        $report.success = -not [string]::IsNullOrWhiteSpace($report.answer)
        $report.usage = $parsed.usageMetadata
    } else {
        $report.error = $parsed.error.status
    }
} catch {
    # Never print exceptions, headers or raw response bodies that might echo credentials.
    $report.error = 'Connection or configuration check failed: ' + $_.Exception.GetType().Name
} finally {
    if ($plain) { [Array]::Clear($plain,0,$plain.Length) }
    $cloudKey = $null
    if ($response) { $response.Dispose() }
    if ($request) { $request.Dispose() }
    if ($client) { $client.Dispose() }
}
$report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'live-cloud-connection.json') -Encoding UTF8
$report | ConvertTo-Json -Depth 8
