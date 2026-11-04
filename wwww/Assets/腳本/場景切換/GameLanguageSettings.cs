using System;
using UnityEngine;

// Numeric values remain compatible with existing menu scenes and saved choices.
public enum GameSubtitleLanguage { Chinese=0, Japanese=1, Seediq=2, None=3 }
public static class GameLanguageSettings
{
    public const string VoicePreferenceKey="WusheEvent.VoiceLanguage";
    const string LegacyKey="WusheEvent.SubtitleLanguage";
    public static event Action Changed;
    public static GameSubtitleLanguage CurrentLanguage
    {
        get
        {
            int value=PlayerPrefs.GetInt(VoicePreferenceKey,PlayerPrefs.GetInt(LegacyKey,0));
            return value==2?GameSubtitleLanguage.Seediq:GameSubtitleLanguage.Chinese;
        }
    }
    public static bool ChineseVoice=>CurrentLanguage==GameSubtitleLanguage.Chinese;
    public static bool SubtitlesEnabled=>true;
    public static void SetLanguage(GameSubtitleLanguage language)
    {
        PlayerPrefs.SetInt(VoicePreferenceKey,language==GameSubtitleLanguage.Seediq?2:0);
        PlayerPrefs.Save();Changed?.Invoke();
    }
    // Language selection affects audio only, including old Japanese/None saves.
    public static string LocalizeSpeaker(string chineseText)=>chineseText;
    public static string LocalizeSubtitle(string chineseText)=>chineseText;
    public static string LocalizeInterfaceText(string chineseText)=>chineseText;
}
