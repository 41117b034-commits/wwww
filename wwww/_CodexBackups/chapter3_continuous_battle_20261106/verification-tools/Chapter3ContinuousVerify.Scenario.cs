using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Object=UnityEngine.Object;

public static partial class Chapter3ContinuousVerify
{
    static float maxFade,endingPlayerDrift,maxMonaStep;
    static Vector3? endingPosition,lastMona;
    static float monaTime;
    static IEnumerator ContinuousScenario()
    {
        yield return null;yield return null;f=Object.FindFirstObjectByType<Chapter3Flow>();
        Check("formal third chapter opens without black",f&&f.startAtStation&&f.ui.fade.color.a==0);
        f.Carry(Chapter3Weapons.Weapon.Rifle);f.EnterSchool();yield return Wait(()=>f.school.Exploring,20,"school");
        var s=f.school;var b=s.Battle;var p=f.player;var a=s.GetComponentInChildren<Chapter3SchoolAmbient>();
        var people=Enumerable.Range(0,10).Select(i=>a.Ambusher(i).GetComponent<Chapter3SchoolCrowdActor>()).ToArray();
        Check("three native children already present before alarm",people.Take(3).All(c=>c.name.Contains("賽德克孩童")&&c.gameObject.activeInHierarchy&&!c.UsingOriginalSkin));
        p.Warp(s.tado.transform.position+new Vector3(.5f,.06f,-2.5f),s.tado.transform.position+Vector3.up);
        yield return Wait(()=>b.CurrentStage==Chapter3SchoolBattle.Stage.Choice,40,"Tado choice");
        b.Choose(Chapter3SchoolBattle.Mission.Tado);yield return Wait(()=>b.Fighting,40,"fight");
        yield return new WaitForSeconds(22);
        var audio=a.GetComponent<Chapter3SchoolBattleAudio>();
        Check("licensed melee crowd audio plays",audio&&audio.Source&&audio.Source.clip&&audio.Source.isPlaying&&audio.Source.volume>0);
        Check("three children shelter instead of attacking",people.Take(3).All(c=>(bool)Get(c,"cowering")&&!c.HoldingAtNeck));
        var childPositions=people.Take(3).Select(c=>c.transform.position).ToArray();
        yield return new WaitForSeconds(4);
        Check("sheltering children stay in their corners",people.Take(3).Where((c,i)=>Flat(c.transform.position,childPositions[i])<.04f).Count()==3);
        p.Warp(new Vector3(-16,.06f,18),new Vector3(-20.5f,.6f,18.5f));yield return Shot("sheltering-children-"+mode);
        var officer=b.Enemies.First(e=>e&&!e.Defeated);officer.Hit(100,p.transform.position);
        yield return new WaitForSeconds(3);
        var actor=officer.GetComponent<Chapter2Actor>();
        yield return new WaitForEndOfFrame();
        var mesh=new Mesh();float lowest=float.PositiveInfinity;
        foreach(var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>())
        {skin.BakeMesh(mesh);foreach(var vertex in mesh.vertices)lowest=Mathf.Min(lowest,skin.transform.TransformPoint(vertex).y);}
        Object.Destroy(mesh);
        report.observations.Add("Defeated officer actual posed mesh lowest y="+lowest+" root="+actor.transform.position);
        report.observations.Add("Fallen hips="+actor.Rig.Hips.position+" head="+actor.Rig.Head.position+" hands="+actor.Rig.LeftHand.position+" / "+actor.Rig.RightHand.position);
        Check("actual fallen police mesh rests on floor",Mathf.Abs(lowest-.025f)<.035f);
        p.Warp(officer.transform.position+new Vector3(-2,.06f,-3),officer.transform.position+Vector3.up*.3f);yield return Shot("grounded-officer-"+mode);
        p.Warp(new Vector3(12,.06f,34),new Vector3(-5,1,28));
        foreach(var e in b.Enemies.Where(e=>e&&!e.Defeated))e.Hit(100,p.transform.position);
        yield return Wait(()=>b.Step==4,5,"cleared");yield return new WaitForSeconds(3);
        var starts=people.Select(c=>c.transform.position).ToArray();var playerStart=p.transform.position;
        yield return Keys(Key.E,.15f);Check("one whistle releases entire party",b.RetreatCalled&&b.RetreatSignalCount==1);
        yield return new WaitForSeconds(5);
        report.observations.Add("No movement keys; player="+playerStart+" -> "+p.transform.position+" withdrawal movement="+string.Join(",",people.Select((c,i)=>Flat(c.transform.position,starts[i]).ToString("F2"))));
        Check("all ten start walking without player movement input",people.Where((c,i)=>Flat(c.transform.position,starts[i])>.3f).Count()==10&&Flat(p.transform.position,playerStart)<.5f);
        Check("whistle fades crowd ambience",!audio.Source.isPlaying&&audio.Source.volume==0);
        yield return new WaitForSeconds(22);
        yield return Shot("immediate-withdrawal-"+mode);
        if(mode=="visual")yield break;
        int survivors=a.CivilianFollowerCount;
        foreach(var point in new[]{new Vector3(12,0,35.5f),new Vector3(17,0,35.5f),new Vector3(18,0,44.8f),new Vector3(24.2f,0,45.4f),new Vector3(24.2f,0,61),new Vector3(22,0,66),new Vector3(12,0,72)})
            yield return StoryWalk(point,.7f,40);
        yield return StoryWalk(new Vector3(0,0,78),3.8f,30,()=>b.CurrentStage==Chapter3SchoolBattle.Stage.Regroup);
        yield return Wait(()=>b.Rescue&&b.Rescue.CurrentStage==Chapter3Rescue.Stage.Advancing,30,"forest route");
        Check("Mona staged behind shelter before player reaches it",b.Rescue.Ending.EntrancePrepared&&b.Rescue.Ending.Mona.transform.position.z>111.5f);
        var ending=b.Rescue.Ending;ending.PersistResult=false;
        string previous=PlayerPrefs.GetString(Chapter3ResultStore.SaveKey,"");bool hadPrevious=PlayerPrefs.HasKey(Chapter3ResultStore.SaveKey);
        yield return Forest(mode=="leave");
        yield return Wait(()=>a.WithdrawalArrived&&a.CivilianArrivedCount==survivors,80,"all survivors arrive");
        Check("ten original party members remain visible and follow into forest",a.ForestFollowerCount==10&&people.All(c=>c.gameObject.activeInHierarchy&&c.transform.position.z>90));
        Check("entire route has no black frames",maxFade<.001f);
        Check("ending never relocates player",endingPlayerDrift<.001f);
        Check("Mona walks into scene without teleport",maxMonaStep<=Chapter3Player.WalkSpeed+.08f);
        report.observations.Add("maxFade="+maxFade+" playerDrift="+endingPlayerDrift+" Mona speed="+maxMonaStep+" party speed="+maxPartySpeed);
        Check("chapter save unchanged",PlayerPrefs.HasKey(Chapter3ResultStore.SaveKey)==hadPrevious&&PlayerPrefs.GetString(Chapter3ResultStore.SaveKey,"")==previous);
        yield return Keys(Key.F8,.12f);yield return Wait(()=>s.Exploring,20,"F8");
        Check("replay resets ending crowd and sound",!ending.Active&&!a.Withdrawing&&!audio.Source.isPlaying&&people.Take(3).All(c=>c.gameObject.activeInHierarchy&&!(bool)Get(c,"cowering")));
    }
    public static void MonitorContinuity()
    {
        if(!f||report==null)return;maxFade=Mathf.Max(maxFade,f.ui.fade.color.a);
        var e=f.school?f.school.GetComponent<Chapter3Ending>():null;
        if(!e||!e.Active)return;
        if(f.DialogueView.Framing)report.errors.Add("Ending unexpectedly moved the dialogue camera");
        if(!endingPosition.HasValue)endingPosition=f.player.transform.position;
        endingPlayerDrift=Mathf.Max(endingPlayerDrift,Flat(endingPosition.Value,f.player.transform.position));
        if(e.Mona)
        {
            if(lastMona.HasValue&&Time.time>monaTime)maxMonaStep=Mathf.Max(maxMonaStep,Flat(e.Mona.transform.position,lastMona.Value)/(Time.time-monaTime));
            lastMona=e.Mona.transform.position;monaTime=Time.time;
        }
    }
}
