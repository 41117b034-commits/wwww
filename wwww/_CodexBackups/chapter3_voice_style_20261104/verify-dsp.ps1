$ErrorActionPreference='Stop'
$engine='C:/Program Files/Unity/Hub/Editor/6000.0.58f1/Editor/Data/Managed/UnityEngine'
Add-Type -Path "$engine/UnityEngine.CoreModule.dll"
Add-Type -Path "$engine/UnityEngine.AudioModule.dll"
$refs=@("$engine/UnityEngine.CoreModule.dll","$engine/UnityEngine.AudioModule.dll","$PSHOME/ref/netstandard.dll","$PSHOME/ref/System.Runtime.dll")
$helper=@'
public static class VoiceFileCheck {
    public static float[] Read(byte[] b,out int rate,out int channels) {
        rate=0;channels=0;int start=0,bytes=0;
        for(int p=12;p+8<=b.Length;) {
            int n=System.BitConverter.ToInt32(b,p+4);string id=System.Text.Encoding.ASCII.GetString(b,p,4);
            if(id=="fmt "){if(System.BitConverter.ToInt16(b,p+8)!=1||System.BitConverter.ToInt16(b,p+22)!=16)throw new System.Exception("PCM16 required");channels=System.BitConverter.ToInt16(b,p+10);rate=System.BitConverter.ToInt32(b,p+12);}
            if(id=="data"){start=p+8;bytes=n;}p+=8+n+n%2;
        }
        var data=new float[bytes/2];for(int i=0;i<data.Length;i++)data[i]=System.BitConverter.ToInt16(b,start+i*2)/32768f;return data;
    }
    public static byte[] Wave(float[] data,int rate,int channels,float volume) {
        var b=new byte[44+data.Length*2];System.Text.Encoding.ASCII.GetBytes("RIFF").CopyTo(b,0);System.BitConverter.GetBytes(b.Length-8).CopyTo(b,4);
        System.Text.Encoding.ASCII.GetBytes("WAVEfmt ").CopyTo(b,8);System.BitConverter.GetBytes(16).CopyTo(b,16);System.BitConverter.GetBytes((short)1).CopyTo(b,20);System.BitConverter.GetBytes((short)channels).CopyTo(b,22);
        System.BitConverter.GetBytes(rate).CopyTo(b,24);System.BitConverter.GetBytes(rate*channels*2).CopyTo(b,28);System.BitConverter.GetBytes((short)(channels*2)).CopyTo(b,32);System.BitConverter.GetBytes((short)16).CopyTo(b,34);
        System.Text.Encoding.ASCII.GetBytes("data").CopyTo(b,36);System.BitConverter.GetBytes(data.Length*2).CopyTo(b,40);for(int i=0;i<data.Length;i++)System.BitConverter.GetBytes((short)(data[i]*volume*32767)).CopyTo(b,44+i*2);return b;
    }
    public static float Peak(float[] data){float peak=0;foreach(float x in data){if(float.IsNaN(x)||float.IsInfinity(x))throw new System.Exception("Non-finite sample");peak=System.Math.Max(peak,System.Math.Abs(x));}return peak;}
}
'@
Add-Type -TypeDefinition ((Get-Content 'Assets/Chapter3/Chapter3VoiceTone.cs' -Raw)+$helper) -ReferencedAssemblies $refs
$dir='_CodexBackups/chapter3_voice_style_20261104'
$catalog=Get-Content 'Assets/Resources/Chapter3Voice/catalog.json' -Raw | ConvertFrom-Json
$checked=@()
foreach($entry in $catalog.entries){
    $file='Assets/Resources/'+$entry.resource+'.wav'
    $rate=0;$channels=0
    $data=[VoiceFileCheck]::Read([IO.File]::ReadAllBytes($file),[ref]$rate,[ref]$channels)
    $before=[VoiceFileCheck]::Peak($data)
    if($entry.chinese -eq '前面有巡警。靠著岩石走，別站在路中央。'){
        [IO.File]::WriteAllBytes("$dir/third-chapter-before.wav",[VoiceFileCheck]::Wave($data,$rate,$channels,.85))
    }
    [Chapter3VoiceTone]::Process($data,$channels,$rate)
    $peak=[VoiceFileCheck]::Peak($data)
    if($peak -gt .901 -or $peak -lt .005){throw "Invalid output: $file peak=$peak"}
    if($entry.chinese -eq '前面有巡警。靠著岩石走，別站在路中央。'){
        [IO.File]::WriteAllBytes("$dir/third-chapter-style-preview.wav",[VoiceFileCheck]::Wave($data,[int]($rate*.95),$channels,.85))
    }
    $checked+=@{resource=$entry.resource;sourcePeak=$before;processedPeak=$peak;seconds=$data.Length/$channels/$rate/.95}
}
# Verify zero input and independent stereo filter states.
$silent=New-Object float[] 24000
[Chapter3VoiceTone]::Process($silent,1,24000)
if([VoiceFileCheck]::Peak($silent) -ne 0){throw 'Silence changed'}
$stereo=New-Object float[] 48000
for($i=0;$i -lt 24000;$i++){$stereo[$i*2]=.1*[Math]::Sin($i*2*[Math]::PI*148/24000)}
[Chapter3VoiceTone]::Process($stereo,2,24000)
for($i=0;$i -lt 24000;$i++){if($stereo[$i*2+1] -ne 0){throw 'Stereo leakage'}}
@{completed=$true;layer='Actual production C# DSP compiled and executed outside Unity; not Editor Play Mode';count=$checked.Count;silencePreserved=$true;stereoIndependent=$true;clips=$checked}|ConvertTo-Json -Depth 5|Set-Content "$dir/report-dsp.json"
Write-Output "DSP passed: $($checked.Count) clips; silence and stereo isolation; previews saved."
