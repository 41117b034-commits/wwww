param([string]$Request=$env:WUSHE_TTS_REQUEST)
$ErrorActionPreference='Stop'
$requestData=[IO.File]::ReadAllText($Request,[Text.Encoding]::UTF8)|ConvertFrom-Json
$voice=New-Object -ComObject SAPI.SpVoice
$male=New-Object -ComObject SAPI.SpObjectToken
$female=New-Object -ComObject SAPI.SpObjectToken
$male.SetId('HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Speech_OneCore\Voices\Tokens\MSTTS_V110_zhTW_ZhiweiM')
$female.SetId('HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Speech_OneCore\Voices\Tokens\MSTTS_V110_zhTW_HanhanM')
$voice.Rate=-1
$voice.Volume=100
foreach($job in $requestData.jobs){
    $voice.Voice=if($job.female){$female}else{$male}
    $stream=New-Object -ComObject SAPI.SpFileStream
    $format=New-Object -ComObject SAPI.SpAudioFormat
    $format.Type=22
    $stream.Format=$format
    try {
        $stream.Open($job.path,3,$false)
        $voice.AudioOutputStream=$stream
        # Treat all text as data, including generated dialogue containing XML.
        $null=$voice.Speak([string]$job.text,16)
    } finally { $stream.Close(); [void][Runtime.InteropServices.Marshal]::ReleaseComObject($stream) }
}
[void][Runtime.InteropServices.Marshal]::ReleaseComObject($voice)
