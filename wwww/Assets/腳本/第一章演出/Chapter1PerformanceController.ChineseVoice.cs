using UnityEngine;

public partial class Chapter1PerformanceController
{
    GameChineseVoice chineseSpeech;
    Chapter2SpeechFace speakingFace;
    AudioSource receiverSpeechSource;
    string pendingFaceLine;
    float pendingFaceSeconds;

    private void OnEnable() => GameLanguageSettings.Changed += ClearGuestSpeech;

    private void ClearGuestSpeech()
    {
        if (speakingFace) speakingFace.EndGuestLine();
        speakingFace = null;
        pendingFaceLine = null;
        if (receiverSpeechSource) receiverSpeechSource.Stop();
        if (policeDialogueAudio) policeDialogueAudio.Stop();
    }

    private void StartGuestSpeech(Transform actor, string line, float seconds, AudioSource performed)
    {
        if (speakingFace) speakingFace.EndGuestLine();
        pendingFaceLine = null;
        speakingFace = actor ? Chapter1SpeechFaces.For(actor) : null;
        if (!speakingFace) return;
        if (GameLanguageSettings.ChineseVoice && chineseSpeech && chineseSpeech.Busy)
        {
            if (!chineseSpeech.HasAudio)
            {
                pendingFaceLine = line;
                pendingFaceSeconds = seconds;
                return;
            }
            performed = chineseSpeech.Output;
        }
        speakingFace.BeginGuestLine(line, seconds, performed);
    }

    private void UpdatePendingGuestSpeech()
    {
        if (pendingFaceLine == null || !speakingFace || !chineseSpeech) return;
        if (chineseSpeech.Busy && !chineseSpeech.HasAudio) return;
        speakingFace.BeginGuestLine(pendingFaceLine, pendingFaceSeconds,
            chineseSpeech.HasAudio ? chineseSpeech.Output : null);
        pendingFaceLine = null;
    }
    GameChineseVoice ChineseSpeech=>chineseSpeech?chineseSpeech:(chineseSpeech=gameObject.AddComponent<GameChineseVoice>());
    float BeginChineseLine(string speaker,string line,float seconds)
    {
        if(!GameLanguageSettings.ChineseVoice)return seconds;
        if(speaker=="系統"||speaker=="任務"||speaker=="動作"||speaker=="提示")return seconds;
        ChineseSpeech.Speak(speaker,line);
        return ChineseSpeech.HasAudio ? ChineseSpeech.Remaining+.12f : seconds;
    }
}
