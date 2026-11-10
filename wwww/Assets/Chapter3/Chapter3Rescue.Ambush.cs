using UnityEngine;

public sealed partial class Chapter3Rescue
{
    public Chapter3Enemy AmbushGuard {get;private set;}
    public bool AmbushShotFired {get;private set;}
    public bool AmbushRevealed {get;private set;}
    public Vector3 AmbushHiddenPosition=>new Vector3(-9.35f,0,93.85f);
    readonly Chapter3PathMotor ambushMotor=new Chapter3PathMotor();
    static readonly Vector3[] ambushExit={new Vector3(-9.4f,0,94.3f),new Vector3(-6,0,95),new Vector3(1.5f,0,97)};
    bool ambushRevealing;
    int ambushLeg;

    void PrepareAmbush(bool checkpoint)
    {
        // The existing 1.58 m roadside rock hides the torso. The head and an
        // outside shoulder can be glimpsed from the approaching forest path.
        var go=Instantiate(combat.enemyTemplates[0],school.transform);
        go.name="林路岩石後埋伏的警察";
        go.transform.position=checkpoint?ambushExit[ambushExit.Length-1]:AmbushHiddenPosition;
        AmbushGuard=go.GetComponent<Chapter3Enemy>();
        AmbushGuard.combat=combat;AmbushGuard.advances=false;AmbushGuard.attackInterval=7;
        AmbushGuard.engagementRange=17;AmbushGuard.escortTarget=null;
        AmbushGuard.AttackEnabled=false;AmbushGuard.StagedAimTarget=school.pihu;AmbushGuard.StagedAimWeight=.35f;
        battle.Enemies.Add(AmbushGuard);go.SetActive(true);
        ambushMotor.Reset();ambushLeg=checkpoint?ambushExit.Length:0;
        AmbushShotFired=AmbushRevealed=checkpoint;ambushRevealing=false;
    }
    void UpdateAmbush()
    {
        if(!ambushRevealing||!AmbushGuard||AmbushGuard.Defeated)return;
        if(ambushLeg<ambushExit.Length)
        {
            Vector3 target=ambushExit[ambushLeg];
            ambushMotor.Move(AmbushGuard.actor,target,1.55f,.08f);
            if(Flat(AmbushGuard.transform.position,target)<.14f)ambushLeg++;
        }
        if(ambushLeg>=ambushExit.Length){AmbushRevealed=true;ambushRevealing=false;}
    }
    void ReleaseAmbush()
    {
        if(AmbushGuard)
        {
            if(battle)battle.Enemies.Remove(AmbushGuard);
            AmbushGuard.gameObject.SetActive(false);Destroy(AmbushGuard.gameObject);
        }
        AmbushGuard=null;AmbushShotFired=AmbushRevealed=ambushRevealing=false;
        ambushMotor.Reset();ambushLeg=0;
    }
}
