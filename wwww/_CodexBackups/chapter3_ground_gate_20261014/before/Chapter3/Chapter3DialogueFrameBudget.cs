using UnityEngine;

// The CPU model shares this laptop with Unity. Give it time during desktop chat;
// keep headset timing untouched and restore the prior frame settings afterwards.
public sealed class Chapter3DialogueFrameBudget
{
    bool held;
    int previousRate,previousVsync;
    public void Begin(bool vr)
    {
        if(held||vr)return;
        held=true;previousRate=Application.targetFrameRate;previousVsync=QualitySettings.vSyncCount;
        QualitySettings.vSyncCount=0;Application.targetFrameRate=20;
    }
    public void End()
    {
        if(!held)return;
        held=false;Application.targetFrameRate=previousRate;QualitySettings.vSyncCount=previousVsync;
    }
}
