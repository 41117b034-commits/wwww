using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static partial class Chapter3ContinuousVerify
{
    static void FixRocks()
    {
        var material=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Chapter3/School/School masonry.mat"));
        material.name="School rock";
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Rock_pack/Rock_03/rock_03_diffuse.jpg");
        material.SetTexture("_BaseMap",texture);material.SetTexture("_MainTex",texture);
        material.SetTextureScale("_BaseMap",Vector2.one);material.SetTextureOffset("_BaseMap",Vector2.zero);
        string path="Assets/Chapter3/School/School rock.mat";
        if(AssetDatabase.LoadAssetAtPath<Material>(path))throw new Exception("Material already exists");
        AssetDatabase.CreateAsset(material,path);AssetDatabase.SaveAssetIfDirty(material);
        int count=0;
        foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())
        foreach(var t in root.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("可碰撞岩石 ")))
        foreach(var r in t.GetComponentsInChildren<Renderer>(true)){r.sharedMaterial=material;EditorUtility.SetDirty(r);count++;}
        if(count==0)throw new Exception("No school rocks");
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        File.WriteAllText(Dir+"rock-fix.txt","renderers="+count+" texture="+AssetDatabase.GetAssetPath(texture));
    }
    static IEnumerator Motion()
    {
        yield return null;yield return null;f=Object.FindFirstObjectByType<Chapter3Flow>();
        if(mode=="motion")RecordChecks();
        f.Carry(Chapter3Weapons.Weapon.Rifle);f.EnterSchool();yield return Wait(()=>f.school.Exploring,20,"school");
        var s=f.school;var b=s.Battle;var p=f.player;
        p.Warp(s.tado.transform.position+new Vector3(.5f,.06f,-2.5f),s.tado.transform.position+Vector3.up);
        yield return Wait(()=>b.CurrentStage==Chapter3SchoolBattle.Stage.Observing,10,"Tado talking");
        yield return new WaitForSeconds(.5f);yield return Shot("rock-"+mode);
        foreach(var r in s.GetComponentsInChildren<Renderer>(true).Where(r=>r.name.StartsWith("可碰撞岩石 ")).Take(2))
            report.observations.Add("rock="+r.name+" material="+AssetDatabase.GetAssetPath(r.sharedMaterial)+" texture="+AssetDatabase.GetAssetPath(r.sharedMaterial.mainTexture));
        s.StopAllCoroutines();b.StopAllCoroutines();s.enabled=false;b.enabled=false;
        f.ui.Say("",null);f.ui.fade.color=Color.clear;
        var rescue=s.GetComponent<Chapter3Rescue>();if(!rescue)rescue=s.gameObject.AddComponent<Chapter3Rescue>();
        Set(b,"Rescue",rescue);Set(b,"CurrentStage",Chapter3SchoolBattle.Stage.Complete);
        rescue.Begin(s,b);Set(rescue,"CurrentStage",Chapter3Rescue.Stage.Choice);rescue.Choose(Chapter3Rescue.Decision.Rescue);
        foreach(var e in b.Enemies)if(e)e.Hit(100,p.transform.position);
        p.Warp(new Vector3(-1.6f,.06f,92),new Vector3(0,1,98));
        s.bawan.transform.position=new Vector3(-3.4f,0,94);s.tado.transform.position=new Vector3(16,0,105);
        typeof(Chapter3Rescue).GetField("tadoIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(rescue,4);
        Check("support fixture entered",rescue.WorkLift(true,2.1f));
        yield return new WaitForSeconds(.5f);
        var rig=s.bawan.Rig;var rows=new List<string>{"time,rootY,headY,hipsY,walking,x,z,step"};
        float until=Time.time+8,minY=100,maxY=-100,maxJump=0,lastY=rig.Head.position.y;int toggles=0;bool walking=rig.walking;
        var before=s.bawan.transform.position;
        while(Time.time<until)
        {
            var d=new Vector3(.3f,0,1).normalized*p.speed*Time.deltaTime;p.Move(d);
            yield return new WaitForEndOfFrame();
            minY=Mathf.Min(minY,rig.Head.position.y);maxY=Mathf.Max(maxY,rig.Head.position.y);maxJump=Mathf.Max(maxJump,Mathf.Abs(lastY-rig.Head.position.y));lastY=rig.Head.position.y;
            if(walking!=rig.walking)toggles++;walking=rig.walking;
            rows.Add(Time.time+","+s.bawan.transform.position.y+","+rig.Head.position.y+","+rig.Hips.position.y+","+rig.walking+","+s.bawan.transform.position.x+","+s.bawan.transform.position.z+","+Flat(before,s.bawan.transform.position));before=s.bawan.transform.position;
        }
        File.WriteAllLines(Dir+"motion-"+mode+".csv",rows);
        report.observations.Add("Bawan support head range="+(maxY-minY)+" largest frame jump="+maxJump+" walk/idle switches="+toggles+" smooth="+rig.smoothLocomotion);
        if(mode=="motion")Check("continuous support escort without repeated walk-idle switches",toggles<=4);
        p.FocusOn(s.bawan.Rig.Head.position);yield return Shot("support-"+mode);
        report.observations.Add("Position fixtures initialize support. Measurement uses actual rescue Update, CharacterController.Move, actor grounding and rig LateUpdate. Not human keyboard input.");
    }
    static void RecordChecks()
    {
        bool had=PlayerPrefs.HasKey(Chapter3ResultStore.SaveKey);string previous=PlayerPrefs.GetString(Chapter3ResultStore.SaveKey,"");
        File.WriteAllText(Dir+"save-before.json",previous);
        try
        {
            if(had)Check("existing user save loads before tests",Chapter3ResultStore.Load()!=null);
            foreach(var mission in new[]{Chapter3SchoolBattle.Mission.Bawan,Chapter3SchoolBattle.Mission.Pihu,Chapter3SchoolBattle.Mission.Tado})
            foreach(var choice in new[]{Chapter3Rescue.Decision.Rescue,Chapter3Rescue.Decision.Leave})
            {
                var result=Chapter3ResultStore.Build(mission,choice,Chapter3Weapons.Weapon.Rifle,11);
                Check(mission+" "+choice+" single player added to chosen lane",result.LeftCount==(mission==Chapter3SchoolBattle.Mission.Bawan?2:1)&&result.RightCount==(mission==Chapter3SchoolBattle.Mission.Pihu?2:1)&&result.CentralCount==(mission==Chapter3SchoolBattle.Mission.Tado?12:11)&&result.LeftCount+result.RightCount+result.CentralCount==14);
                Check(mission+" "+choice+" saves new count snapshot",Chapter3ResultStore.SaveCompleted(result));
                var loaded=Chapter3ResultStore.Load();Check("v3 roundtrip retains actual party",loaded!=null&&loaded.centralNpcCount==11&&loaded.CentralCount==result.CentralCount&&loaded.squadSurvivors==(choice==Chapter3Rescue.Decision.Rescue?4:3));
            }
            var smaller=Chapter3ResultStore.Build(Chapter3SchoolBattle.Mission.Tado,Chapter3Rescue.Decision.Rescue,Chapter3Weapons.Weapon.Knife,5);
            Check("count is runtime snapshot, not hardcoded ten extras",smaller.CentralCount==6&&Chapter3ResultStore.Copy(smaller).centralNpcCount==5);
            foreach(int version in new[]{1,2})
            {
                var build=typeof(Chapter3ResultStore).GetMethod("BuildVersion",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
                var legacy=(Chapter3ResultStore.Record)build.Invoke(null,new object[]{version,Chapter3SchoolBattle.Mission.Bawan,Chapter3Rescue.Decision.Rescue,Chapter3Weapons.Weapon.Knife,0});
                Check("legacy version "+version+" saves",Chapter3ResultStore.SaveCompleted(legacy));
                var loaded=Chapter3ResultStore.Load();Check("legacy version "+version+" loads without invented crowd count",loaded!=null&&loaded.version==version&&loaded.centralNpcCount==0);
            }
            smaller.centralNpcCount=0;Check("invalid central party rejected",!Chapter3ResultStore.IsValid(smaller));
        }
        finally
        {if(had)PlayerPrefs.SetString(Chapter3ResultStore.SaveKey,previous);else PlayerPrefs.DeleteKey(Chapter3ResultStore.SaveKey);PlayerPrefs.Save();}
        Check("user completed save restored exactly",PlayerPrefs.HasKey(Chapter3ResultStore.SaveKey)==had&&PlayerPrefs.GetString(Chapter3ResultStore.SaveKey,"")==previous);
    }
    static IEnumerator Speed()
    {
        yield return null;yield return null;f=Object.FindFirstObjectByType<Chapter3Flow>();
        f.Carry(Chapter3Weapons.Weapon.Rifle);f.EnterSchool();yield return Wait(()=>f.school.Exploring,20,"school");
        var s=f.school;var p=f.player;var actor=s.bawan;
        s.enabled=false;s.StopAllCoroutines();s.Battle.enabled=false;s.Battle.StopAllCoroutines();f.ui.Say("",null);
        p.Warp(new Vector3(-18,.06f,-20),new Vector3(0,1,-20));actor.transform.position=new Vector3(-18,0,-17);
        yield return null;
        Check("normal player speed is 2.7 after scene handoff",Mathf.Abs(p.speed-2.7f)<.001f);
        var motor=new Chapter3PathMotor();float elapsed=0;var playerStart=p.transform.position;var actorStart=actor.transform.position;
        while(elapsed<2)
        {
            elapsed+=Time.deltaTime;
            p.Move(Vector3.right*p.speed*Time.deltaTime);motor.Move(actor,new Vector3(0,0,-17),Chapter3Player.WalkSpeed);
            yield return null;
        }
        float playerPace=Flat(playerStart,p.transform.position)/elapsed,actorPace=Flat(actorStart,actor.transform.position)/elapsed;
        Check("actual controller and path motor travel at matching pace",Mathf.Abs(playerPace-2.7f)<.03f&&Mathf.Abs(actorPace-playerPace)<.03f);
        report.observations.Add("Over "+elapsed+" seconds: player="+playerPace+" m/s; Bawan="+actorPace+" m/s; real collision-aware Move methods on an unobstructed school approach.");
        var ambient=s.GetComponentInChildren<Chapter3SchoolAmbient>();ambient.enabled=false;
        var walker=ambient.spectators[0].GetComponent<Chapter3SchoolCrowdActor>();
        var blocker=ambient.spectators[1].GetComponent<Chapter3SchoolCrowdActor>();
        walker.transform.position=new Vector3(-18,0,-12);blocker.transform.position=new Vector3(-15,0,-12);
        walker.FollowingWithdrawal=blocker.FollowingWithdrawal=true;Physics.SyncTransforms();
        var corner=blocker.transform.position;
        Check("moving queue does not displace authored corner",Chapter3SchoolAmbient.OpenWithdrawalCorner(corner,corner+Vector3.right*3,walker)==corner);
        Check("moving queue still blocks actual movement",!Chapter3PathMotor.ClearCrowdPoint(corner,walker));
        blocker.FollowingWithdrawal=false;
        var alternative=Chapter3SchoolAmbient.OpenWithdrawalCorner(corner,corner+Vector3.right*3,walker);
        Check("stationary occupant still gets collision-clear detour",alternative!=corner&&Chapter3PathMotor.ClearCrowdPoint(alternative,walker));
        var voice=f.ui.voice.GetComponent<GameChineseVoice>();
        foreach(var entry in new[]{new[]{"莫那 · 魯道","左側、右側和進入會場的人，各有幾個？把人數和情況告訴我。"},new[]{"達多 · 莫那","進入會場的有12人，包括他、我和一起進入會場的同伴。"},new[]{"巴萬 · 拿威","左側通路2人，他和我一起打通側門，都回來了。"}})
        {
            f.ui.voice.Speak(entry[0],entry[1]);yield return null;
            Check("new rollcall recording plays offline: "+entry[0],voice.LastProvider=="recorded-neural"&&voice.HasAudio);
            report.observations.Add("Voice "+voice.CurrentRole+" provider="+voice.LastProvider+" seconds="+voice.Output.clip.length);
            f.ui.voice.Stop();
        }
    }
    static IEnumerator Separation()
    {
        yield return null;yield return null;f=Object.FindFirstObjectByType<Chapter3Flow>();
        f.Carry(Chapter3Weapons.Weapon.Rifle);f.EnterSchool();yield return Wait(()=>f.school.Exploring,20,"school");
        var s=f.school;var a=s.GetComponentInChildren<Chapter3SchoolAmbient>();s.enabled=false;a.enabled=false;s.Battle.enabled=false;
        var walker=a.spectators[3].GetComponent<Chapter3SchoolCrowdActor>();var blocker=a.spectators[4].GetComponent<Chapter3SchoolCrowdActor>();
        walker.transform.position=new Vector3(.81f,0,65.60f);blocker.transform.position=new Vector3(.17f,0,65.70f);
        walker.FollowingWithdrawal=true;blocker.FollowingWithdrawal=false;Physics.SyncTransforms();
        var motor=new Chapter3PathMotor();var target=new Vector3(-3.4f,0,64.8f);float until=Time.time+5;
        while(Time.time<until){motor.Move(walker,target,2.65f);yield return null;}
        var start=walker.transform.position;start.y=0;
        report.observations.Add("walker="+walker.transform.position+" target="+target+" path="+string.Join(";",(List<Vector3>)Get(motor,"path"))+" search="+Get(motor,"searching")+" iterations="+Get(motor,"iterations"));
        report.observations.Add("target clear="+Chapter3PathMotor.ClearCrowdPoint(target,walker)+" direct="+Invoke(motor,"ClearForActor",start,target));
        foreach(var c in Physics.OverlapCapsule(target+Vector3.up*.42f,target+Vector3.up*1.25f,.36f))report.observations.Add("target overlap="+c.name+" bounds="+c.bounds);
        foreach(float x in new[]{.5f,1f,1.5f})foreach(float z in new[]{65f,65.5f,66f})report.observations.Add("grid "+x+","+z+" clear="+Invoke(motor,"ClearForActor",start,new Vector3(x,0,z)));
        foreach(var c in Physics.OverlapCapsule(start+Vector3.up*.42f,start+Vector3.up*1.25f,.5f))report.observations.Add("near "+c.name+" root="+c.transform.position+" bounds="+c.bounds);
        foreach(float distance in new[]{.07f,.5f})for(int i=0;i<8;i++)
        {
            var next=start+Quaternion.Euler(0,i*45,0)*Vector3.forward*distance;
            report.observations.Add("step="+distance+" angle="+i*45+" motor="+Invoke(motor,"ClearForActor",start,next)+" body="+Invoke(walker,"Clear",next));
        }
        Check("nearby standing person can be physically walked around",Flat(walker.transform.position,target)<.2f);
    }
}

