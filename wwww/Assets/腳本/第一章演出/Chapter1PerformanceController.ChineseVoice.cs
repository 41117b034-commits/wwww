using UnityEngine;

public partial class Chapter1PerformanceController
{
    GameChineseVoice chineseSpeech;
    GameChineseVoice ChineseSpeech=>chineseSpeech?chineseSpeech:(chineseSpeech=gameObject.AddComponent<GameChineseVoice>());
    float BeginChineseLine(string speaker,string line,float seconds)
    {
        if(!GameLanguageSettings.ChineseVoice)return seconds;
        if(speaker=="系統"||speaker=="任務"||speaker=="動作"||speaker=="提示")return seconds;
        ChineseSpeech.Speak(speaker,line);
        return Mathf.Max(seconds,ChineseSpeech.Remaining+.15f);
    }
}
