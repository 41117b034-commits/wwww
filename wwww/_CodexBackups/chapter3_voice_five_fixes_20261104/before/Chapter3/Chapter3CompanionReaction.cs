using UnityEngine;

[DefaultExecutionOrder(4450)]
public sealed class Chapter3CompanionReaction : MonoBehaviour
{
    public Chapter3Combat combat;
    public Chapter2Actor actor;
    public Vector3 AimPoint=>transform.position+Vector3.up*1.05f;
    public int KnifeReactions {get;private set;}
    public const string WarningLine="嘿，我們是打日本警察，不要攻擊我。";
    float hitAt=-10,until;
    public void ResetReaction(){hitAt=-10;until=0;KnifeReactions=0;if(actor)actor.speaking=false;}
    public void KnifeHit()
    {
        if(Time.time<hitAt+.8f)return;
        hitAt=Time.time;until=Time.time+3.8f;KnifeReactions++;
        WarnAgainstAttack();
        actor.GetComponent<Chapter3CompanionGesture>()?.AdjustStrap(1.2f);
    }
    public void WarnAgainstAttack()
    {
        until=Time.time+3.8f;actor.Face(combat.player.transform.position);actor.speaking=true;
        const string line=WarningLine;
        actor.GetComponent<Chapter2SpeechFace>()?.BeginLine(line,3.8f,null);
        combat.CompanionWarning(actor.DisplayName,line,3.8f);
    }
    void LateUpdate()
    {
        if(until<=0)return;
        if(!combat.Fighting||Time.time>=until){until=0;actor.speaking=false;return;}
        actor.Face(combat.player.transform.position);
        float t=(Time.time-hitAt)/.65f;
        if(t<1&&actor.Rig&&actor.Rig.Hips)
            actor.Rig.Hips.rotation=Quaternion.AngleAxis(-Mathf.Sin(t*Mathf.PI)*17,actor.transform.right)*actor.Rig.Hips.rotation;
    }
    void OnDisable(){if(actor)actor.speaking=false;until=0;}
}
