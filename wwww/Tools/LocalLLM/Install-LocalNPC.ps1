param([string]$Destination = 'D:\WusheLocalLLM')
$ErrorActionPreference = 'Stop'
# Run once on a Windows exhibition computer. Downloads only; no account or API fees.
$taskRoot = [IO.Path]::GetFullPath($Destination)
New-Item -Path (Join-Path $taskRoot 'runtime'),(Join-Path $taskRoot 'models') -ItemType Directory -Force | Out-Null
$taskArchive = Join-Path $taskRoot 'llama-cpu.zip'
$taskModel = Join-Path $taskRoot 'models/qwen2.5-1.5b-instruct-q4_k_m.gguf'
$taskExpected = '6A1A2EB6D15622BF3C96857206351BA97E1AF16C30D7A74EE38970E434E9407E'
if (!(Test-Path -LiteralPath (Join-Path $taskRoot 'runtime/llama-server.exe'))) {
    & curl.exe -L --fail --retry 2 --connect-timeout 20 --max-time 300 -o $taskArchive 'https://github.com/ggml-org/llama.cpp/releases/download/b11526/llama-b11526-bin-win-cpu-x64.zip'
    if ($LASTEXITCODE -ne 0) { throw 'Runtime download failed. Retry this script.' }
    if ((Get-FileHash -LiteralPath $taskArchive -Algorithm SHA256).Hash -ne '222759C0B6E068C52B52F600D506397868C75D7ED2F9535C53D254457C425D70') { throw 'Runtime checksum mismatch.' }
    Expand-Archive -LiteralPath $taskArchive -DestinationPath (Join-Path $taskRoot 'runtime') -Force
}
if (!(Test-Path -LiteralPath $taskModel) -or (Get-FileHash -LiteralPath $taskModel -Algorithm SHA256).Hash -ne $taskExpected) {
    $taskPartial = $taskModel + '.download'
    & curl.exe -L --fail --retry 2 --connect-timeout 20 --max-time 1800 -C - -o $taskPartial 'https://huggingface.co/Qwen/Qwen2.5-1.5B-Instruct-GGUF/resolve/main/qwen2.5-1.5b-instruct-q4_k_m.gguf?download=true'
    if ($LASTEXITCODE -ne 0) { throw 'Model download paused. Retry this script to resume.' }
    if ((Get-FileHash -LiteralPath $taskPartial -Algorithm SHA256).Hash -ne $taskExpected) { throw 'Model checksum mismatch. The model was not installed.' }
    Move-Item -LiteralPath $taskPartial -Destination $taskModel -Force
}
$taskProject = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
[ordered]@{
    endpoint = 'http://127.0.0.1:18080/v1/chat/completions'
    model = 'wushe-npc'
    executable = (Join-Path $taskRoot 'runtime/llama-server.exe')
    modelFile = $taskModel
    autoStart = $true
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskProject 'Assets/StreamingAssets/Chapter2LocalLLM.json') -Encoding utf8
Write-Output "Ready: $taskRoot. Unity starts the local server on entering exploration and stops its own server on leaving Play Mode."
