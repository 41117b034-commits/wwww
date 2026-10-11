using UnityEngine;

// Day and night use separate scene objects, but share one character identity.
public sealed class Chapter2DayCouncilIdentity : MonoBehaviour
{
    public Chapter2Actor councilActor;
    [TextArea] public string daytimeRole;
    public void Apply()
    {
        var actor=GetComponent<Chapter2Actor>();
        if(!actor||!councilActor)return;
        actor.characterName=actor.name=councilActor.DisplayName;
        actor.romanizedName=councilActor.romanizedName;
        actor.isChild=councilActor.isChild;
        actor.dialogueRole=daytimeRole+"你與夜間劇情的同名人物是同一人；目前是早晨，夜間會議尚未發生，不可說已開過會或透露之後的選擇、死亡和結局。";
    }
}
