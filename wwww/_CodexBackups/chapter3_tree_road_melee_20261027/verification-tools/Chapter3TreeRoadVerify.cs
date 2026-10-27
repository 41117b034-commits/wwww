using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class Chapter3TreeRoadVerify
{
    const string Dir="_CodexBackups/chapter3_tree_road_melee_20261027/",Key="Chapter3TreeRoadVerify";
    static Chapter3TreeRoadDriver driver;
    static Chapter3Flow flow;static Report report;static string mode;
    static bool oldBackground;
    [Serializable] sealed class Report
    {public bool completed;public string mode;public List<string> checks=new List<string>(),observations=new List<string>(),errors=new List<string>();}
    static Chapter3TreeRoadVerify()
    {
        Directory.CreateDirectory(Dir);EditorApplication.update+=Tick;
        EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode)File.WriteAllText(Dir+"edit-state.txt","playing="+Application.isPlaying+" dirty="+SceneManager.GetActiveScene().isDirty+" compileFailed="+EditorUtility.scriptCompilationFailed);};
        File.WriteAllText(Dir+"ready.txt",DateTime.UtcNow.ToString("O"));
    }
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(File.Exists(Dir+"command.txt"))
        {
            string c=File.ReadAllText(Dir+"command.txt").Trim();File.Delete(Dir+"command.txt");
            if(c.StartsWith("run-")&&!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorUtility.scriptCompilationFailed&&SceneManager.GetActiveScene().path=="Assets/Scenes/第三章.unity"&&!SceneManager.GetActiveScene().isDirty)
            {SessionState.SetString(Key,c.Substring(4));EditorApplication.isPlaying=true;}
            else if(c=="stop"){SessionState.EraseString(Key);EditorApplication.isPlaying=false;}
            else File.WriteAllText(Dir+"command-error.txt","Not clean compiled third chapter in Edit Mode");
        }
        if(Application.isPlaying&&!string.IsNullOrEmpty(SessionState.GetString(Key,""))&&!driver)
            driver=new GameObject("Temporary five-request verification").AddComponent<Chapter3TreeRoadDriver>();
    }
    public static IEnumerator Run()
    {
        mode=SessionState.GetString(Key,"");report=new Report{mode=mode};oldBackground=Application.runInBackground;Application.runInBackground=true;
        Application.logMessageReceived+=Log;
        var stack=new Stack<IEnumerator>();stack.Push(Scenario());
        while(stack.Count>0)
        {
            bool more=false;object next=null;Exception failure=null;
            try{more=stack.Peek().MoveNext();if(more)next=stack.Peek().Current;}catch(Exception e){failure=e;}
            if(failure!=null){report.errors.Add(failure.ToString());Dump();yield return Shot("failure-"+mode);break;}
            if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}
            if(next is IEnumerator nested)stack.Push(nested);else yield return next;
        }
        report.completed=report.errors.Count==0;File.WriteAllText(Dir+"report-"+mode+".json",JsonUtility.ToJson(report,true));
        Application.logMessageReceived-=Log;Application.runInBackground=oldBackground;SessionState.EraseString(Key);EditorApplication.isPlaying=false;
    }
    static void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception)report.errors.Add(message);}
    static IEnumerator Scenario()
    {
        yield return null;yield return null;flow=Object.FindFirstObjectByType<Chapter3Flow>();Check("formal scene",flow&&flow.startAtStation);
        if(mode=="trees")
        {
            flow.Carry(Chapter3Weapons.Weapon.Rifle);flow.combat.lineSeconds=.2f;flow.EnterCombat();
            yield return Wait(()=>flow.combat.Fighting,15,"combat");
            foreach(var e in flow.combat.Enemies)e.HoldFire(300);
            foreach(var t in flow.combatRegion.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("交戰道路林木 ")&&t.position.x>0&&t.position.z<-70))
                report.observations.Add(t.name+" at "+t.position+" colliders="+string.Join(";",t.GetComponentsInChildren<Collider>().Select(c=>c.GetType().Name+" "+c.bounds)));
            yield break;
        }
        if(mode=="coverroad"){yield return CoverRoad();yield break;}
        if(mode=="route"){yield return Route();yield break;}
        if(mode=="inspect")
        {
            flow.EnterSchool();yield return Wait(()=>flow.school.Exploring,20,"school exploring");
            foreach(var p in new[]{new Vector3(12,0,38),new Vector3(10,0,37),new Vector3(10,0,35),new Vector3(12,0,36),new Vector3(13,0,33)})
                report.observations.Add("target="+p+" clear="+Chapter3PathMotor.Clear(p,p)+" collisions="+string.Join(";",Physics.OverlapCapsule(p+Vector3.up*.42f,p+Vector3.up*1.25f,.36f).Select(c=>c.name+" at "+c.transform.position+" bounds="+c.bounds)));
            yield break;
        }
        yield return Campus(mode=="knife");
    }
    static IEnumerator CoverRoad()
    {
        flow.Carry(Chapter3Weapons.Weapon.Rifle);flow.combat.lineSeconds=.2f;flow.EnterCombat();
        yield return Wait(()=>flow.combat.Fighting,15,"mountain fighting");var combat=flow.combat;
        foreach(var enemy in combat.Enemies){enemy.HoldFire(300);enemy.advances=false;}
        var previous=combat.pihu.transform.position;float minX=previous.x,maxSpeed=0;int frames=0;float began=Time.realtimeSinceStartup;
        while(!combat.CompanionsInCover)
        {
            if(Time.realtimeSinceStartup-began>25)throw new Exception("cover timeout at "+combat.pihu.transform.position+" target="+combat.CompanionCoverPoint(1));
            yield return null;var now=combat.pihu.transform.position;minX=Mathf.Min(minX,now.x);maxSpeed=Mathf.Max(maxSpeed,Vector3.ProjectOnPlane(now-previous,Vector3.up).magnitude/Mathf.Max(.001f,Time.deltaTime));previous=now;frames++;
        }
        Check("Pihu walks only on right side",minX>=5&&maxSpeed<2.8f&&frames>5);
        Check("existing nearby tree selected",combat.PihuCoverTree&&combat.PihuCoverTree.name=="交戰道路林木 49");
        var trunk=combat.PihuCoverTree.GetComponentInChildren<CapsuleCollider>();
        Check("tree trunk blocks threat ray",trunk.Raycast(new Ray(combat.WorldPoint(new Vector3(0,.8f,12)),(combat.CompanionCoverPoint(1)+Vector3.up*.8f-combat.WorldPoint(new Vector3(0,.8f,12))).normalized),out _,40));
        Check("both crouch in separate cover",combat.bawan.GetComponent<Chapter3SchoolCompanionPose>().UnderFire&&combat.pihu.GetComponent<Chapter3SchoolCompanionPose>().UnderFire);
        report.observations.Add("Pihu tree="+combat.PihuCoverTree.position+" cover="+combat.CompanionCoverPoint(1)+" minX="+minX+" maxSpeed="+maxSpeed);
        flow.player.Warp(combat.WorldPoint(new Vector3(3,0,-7)),combat.CompanionCoverPoint(1)+Vector3.up*.9f);yield return Shot("pihu-right-tree");
        flow.player.Warp(combat.WorldPoint(new Vector3(0,0,-6)),combat.WorldPoint(new Vector3(0,1,4)));
        foreach(var enemy in combat.Enemies)enemy.Hit(100,flow.player.transform.position);
        yield return Wait(()=>flow.Approach.Active,10,"approach begins");
        Check("cover poses release",!combat.bawan.GetComponent<Chapter3SchoolCompanionPose>().UnderFire&&!combat.pihu.GetComponent<Chapter3SchoolCompanionPose>().UnderFire);
        flow.Approach.enabled=false;var env=flow.GetComponent<Chapter3ApproachEnvironment>();
        int samples=0,walked=0;
        foreach(float z in new[]{-38f,-32f,-27f,-23f})
        for(float x=-14;x<4;x+=1)
        {
            var from=new Vector3(x,0,z);var to=from+Vector3.forward*.8f;
            if(!env.Walkable(from)||!env.Walkable(to))continue;
            object[] args={from,Vector3.zero,0};typeof(Chapter3ApproachEnvironment).GetMethod("Closest",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(env,args);
            if(Vector3.ProjectOnPlane(from-(Vector3)args[1],Vector3.up).magnitude<2.6f)continue;
            from.y=env.GroundHeight(from)+.04f;flow.player.Warp(from,to+Vector3.up);Physics.SyncTransforms();
            for(int i=0;i<16;i++){flow.player.Move(Vector3.forward*.05f);yield return null;}
            samples++;float distance=flow.player.transform.position.z-from.z;if(distance>.6f)walked++;
            report.observations.Add("road shoulder from="+from+" progress="+distance);
            if(z==-27&&x==-2){flow.player.FocusOn(new Vector3(0,2,10));yield return Shot("wide-road-walking");}
        }
        Check("multiple visible road shoulders traversable",samples>=6&&walked==samples);
        Check("green ridge remains restricted",!env.Walkable(new Vector3(7,0,-32)));
        Check("outside road stays restricted",!env.Walkable(new Vector3(-20,0,-32)));
        // Resume actual companion follow from the combat exit; only setup and
        // the edge probes above use placement fixtures.
        flow.player.Warp(combat.WorldPoint(new Vector3(0,0,-6)),combat.WorldPoint(new Vector3(0,1,5)));
        flow.Approach.enabled=true;float progress=0;began=Time.realtimeSinceStartup;
        while(flow.Approach.Active)
        {
            if(Time.realtimeSinceStartup-began>125)throw new Exception("approach timeout player="+flow.player.transform.position+" leader="+combat.bawan.transform.position+" rear="+combat.pihu.transform.position);
            var guide=flow.Approach.FollowGuide;progress=Mathf.Max(progress,guide.Project(flow.player.transform.position,progress+2));
            float lead=guide.Project(combat.bawan.transform.position);
            var target=flow.Approach.CurrentWaypoint==4?flow.Approach.WorldWaypoints[4]:guide.Sample(Mathf.Min(lead-2.6f,progress+.7f),out _);
            var delta=Vector3.ProjectOnPlane(target-flow.player.transform.position,Vector3.up);
            if(delta.magnitude>.07f)flow.player.Move(Vector3.ClampMagnitude(delta,2.8f*Time.deltaTime));
            flow.player.FocusOn(combat.bawan.transform.position+Vector3.up);yield return null;
            
        }
        Check("same companions complete continuous approach",flow.Approach.Completed&&flow.Region=="School");yield return Shot("school-road-arrival");
    }
    static IEnumerator Route()
    {
        var station=flow.station;yield return Wait(()=>station.Opening!=null,20,"station initialized");
        station.StopAllCoroutines();station.Opening.Cancel();flow.ui.chat.SetActive(false);flow.ui.fade.color=Color.clear;flow.ui.Say("",null);flow.ui.objective.text="跟隨同伴，沿山路前往霧社";
        var path=(Vector3[])Invoke(station,"TightDepartureCorners",station.departurePath);
        float Length(Vector3[] points){float d=0;for(int i=1;i<points.Length;i++)d+=Vector3.Distance(points[i-1],points[i]);return d;}
        Check("tight route shorter",Length(path)<Length(station.departurePath)-1);
        report.observations.Add("old="+Length(station.departurePath)+" new="+Length(path)+" points="+string.Join(";",path.Select(p=>p.ToString())));
        station.bawan.transform.position=path[0];station.pihu.transform.position=path[0]+Vector3.back*1.6f;
        flow.player.Warp(path[0]+Vector3.back*3.8f,path[1]+Vector3.up);flow.player.canMove=flow.player.canLook=true;
        Set(station,"HasRifle",true);Set(station,"CurrentStage",Chapter3StationController.Stage.Departure);
        var escort=(IEnumerator)Invoke(station,"Escort",station.departurePath);bool done=false;
        station.StartCoroutine(Complete(escort,()=>done=true));
        Vector3 last=station.bawan.transform.position,lastRear=station.pihu.transform.position;
        float start=Time.realtimeSinceStartup;int samples=0;float maxStep=0,maxSpeed=0,playerProgress=0,lastSample=Time.time;
        while(!done)
        {
            if(Time.realtimeSinceStartup-start>100)throw new Exception("route timeout waypoint="+station.Waypoint+" leader="+station.bawan.transform.position+" player="+flow.player.transform.position);
            var guide=station.RouteGuide;
            playerProgress=Mathf.Max(playerProgress,guide.Project(flow.player.transform.position,playerProgress+2));
            float leaderProgress=guide.Project(station.bawan.transform.position);
            var target=guide.Sample(Mathf.Min(leaderProgress-3.1f,playerProgress+.65f),out _);
            Vector3 travel=Vector3.ProjectOnPlane(target-flow.player.transform.position,Vector3.up);
            if(travel.magnitude>.08f)flow.player.Move(Vector3.ClampMagnitude(travel,2.8f*Time.deltaTime));
            flow.player.FocusOn(station.bawan.transform.position+Vector3.up*1.2f);yield return null;
            float step=Mathf.Max(Vector3.ProjectOnPlane(last-station.bawan.transform.position,Vector3.up).magnitude,Vector3.ProjectOnPlane(lastRear-station.pihu.transform.position,Vector3.up).magnitude);
            maxStep=Mathf.Max(maxStep,step);maxSpeed=Mathf.Max(maxSpeed,step/Mathf.Max(.0001f,Time.time-lastSample));lastSample=Time.time;
            last=station.bawan.transform.position;lastRear=station.pihu.transform.position;samples++;
            if(station.Waypoint==12&&!File.Exists(Dir+"route-corner-final.png"))yield return Shot("route-corner-final");
        }
        report.observations.Add("continuous movement samples="+samples+" max sampled step="+maxStep+" max speed="+maxSpeed);
        Check("escort walked revised route",samples>60&&maxSpeed<2.1f);yield return Shot("route-arrived");
    }
    static IEnumerator Complete(IEnumerator routine,Action complete){yield return routine;complete();}
    static IEnumerator Combat()
    {
        flow.Carry(Chapter3Weapons.Weapon.Rifle);flow.combat.lineSeconds=.2f;flow.EnterCombat();
        yield return Wait(()=>flow.combat.Fighting,15,"combat fighting");var combat=flow.combat;
        foreach(var enemy in combat.Enemies){enemy.HoldFire(300);enemy.advances=false;}
        report.observations.Add("Combat fixture: officers hold fire for hit/cover inspection; actual weapon Attack and collision remain enabled.");
        yield return Wait(()=>combat.CompanionsInCover,20,"companions reached cover");
        Check("both companions in cover",combat.CompanionsInCover);
        flow.player.Warp(combat.WorldPoint(new Vector3(0,0,-7)),combat.WorldPoint(new Vector3(-3,1,-3)));yield return Shot("combat-cover");
        var rocks=combat.GetComponentsInChildren<Transform>().Where(t=>t.name=="山路岩石掩體").ToArray();
        var boxes=rocks.SelectMany(t=>t.GetComponents<BoxCollider>()).ToArray();var meshes=rocks.SelectMany(t=>t.GetComponentsInChildren<MeshCollider>()).ToArray();
        Check("three rocks use visible mesh",boxes.Length==3&&boxes.All(c=>!c.enabled)&&meshes.Length>=3&&meshes.All(c=>c.enabled));
        Chapter3Enemy target=null;Vector3 origin=default;string previousHit="";int candidates=0;
        foreach(var enemy in combat.Enemies)
        {
            for(float z=-6;z<6&&target==null;z+=.5f)for(float x=-6;x<6&&target==null;x+=.5f)
            {
                Vector3 from=combat.WorldPoint(new Vector3(x,1.65f,z));Vector3 aim=enemy.AimPoint-from;
                if(!combat.Trace(from,aim.normalized,aim.magnitude+.1f,out var now)||now.collider.GetComponentInParent<Chapter3Enemy>()!=enemy)continue;
                foreach(var box in boxes)box.enabled=true;foreach(var mesh in meshes)mesh.enabled=false;Physics.SyncTransforms();
                bool old=combat.Trace(from,aim.normalized,aim.magnitude+.1f,out var before)&&boxes.Contains(before.collider);
                foreach(var box in boxes)box.enabled=false;foreach(var mesh in meshes)mesh.enabled=true;Physics.SyncTransforms();
                candidates++;if(old){target=enemy;origin=from;previousHit=before.collider.name;}
            }
            if(target)break;
        }
        Check("reproduced old box blocking exposed body",target);
        report.observations.Add("old hit="+previousHit+" exposed torso origin="+origin+" target="+target.name+" torso="+target.AimPoint+" candidate="+candidates);
        flow.player.Warp(origin-Vector3.up*1.65f,target.AimPoint);yield return null;flow.player.FocusOn(target.AimPoint);yield return new WaitForEndOfFrame();
        yield return Shot("combat-exposed-body-before");
        Check("actual rifle trigger",combat.weapons.Attack());yield return new WaitForSeconds(.7f);
        Check("exposed torso officer defeated",target.Defeated);yield return new WaitForSeconds(2);yield return Shot("combat-exposed-body-after");
        Check("cover still blocks solid centre",Physics.Raycast(combat.WorldPoint(new Vector3(-3.2f,1,-5)),Vector3.forward,out var stone,5)&&stone.collider is MeshCollider);
        // Leave the shooting-inspection point at the far left of the rock.
        // A direct follower controller cannot steer the player around stone.
        flow.player.Warp(combat.WorldPoint(new Vector3(0,0,-6)),combat.WorldPoint(new Vector3(0,1,4)));
        foreach(var enemy in combat.Enemies)if(!enemy.Defeated)enemy.Hit(100,flow.player.transform.position);
        yield return Wait(()=>flow.Approach.Active,10,"post-combat walk");
        Check("crouch clears after battle",!combat.bawan.GetComponent<Chapter3SchoolCompanionPose>().UnderFire&&!combat.pihu.GetComponent<Chapter3SchoolCompanionPose>().UnderFire);
        Vector3 leadBefore=combat.bawan.transform.position,rearBefore=combat.pihu.transform.position;
        float exitAt=Time.time+20;while(Time.time<exitAt){Follow(combat.bawan.transform.position,3.5f);yield return null;}
        report.observations.Add("cover exit leader="+leadBefore+" -> "+combat.bawan.transform.position+" rear="+rearBefore+" -> "+combat.pihu.transform.position+" player="+flow.player.transform.position);
        Check("both companions walked out of cover",Vector3.Distance(leadBefore,combat.bawan.transform.position)>3&&Vector3.Distance(rearBefore,combat.pihu.transform.position)>3);
        yield return Shot("combat-companions-emerged");
    }
    static IEnumerator Campus(bool knife)
    {
        flow.Carry(knife?Chapter3Weapons.Weapon.Knife:Chapter3Weapons.Weapon.Rifle);flow.EnterSchool();
        yield return Wait(()=>flow.school.Exploring,20,"school exploring");
        flow.player.Warp(flow.school.tado.transform.position+Vector3.back*2,flow.school.tado.transform.position+Vector3.up*1.2f);
        yield return Wait(()=>flow.ui.subtitle.text.StartsWith("前面就是學校了"),10,"briefing first");
        Check("first subtitle only first paragraph",!flow.ui.subtitle.text.Contains("等等"));yield return Shot("briefing-first-"+mode);
        yield return Wait(()=>flow.ui.subtitle.text.StartsWith("等等"),10,"briefing second");
        Check("second subtitle separate",!flow.ui.subtitle.text.Contains("前面就是"));yield return Shot("briefing-second-"+mode);
        yield return Wait(()=>flow.school.Battle.CurrentStage==Chapter3SchoolBattle.Stage.Choice,18,"choice");var battle=flow.school.Battle;
        Check("choose Tado",battle.Choose(Chapter3SchoolBattle.Mission.Tado));
        yield return Wait(()=>battle.Fighting,30,"school fighting");Check("six police in Tado route",battle.Enemies.Count==6);
        Check("no premature retreat",!battle.CallRetreat());
        report.observations.Add("Campus fixture: player health is restored each frame while observing real police sabre contacts. Combat targets use scene placement fixtures for weapon inspection; guide and ten-person withdrawal must physically walk.");
        float observe=Time.time+14;while(Time.time<observe){Set(battle,"Health",100);yield return null;}
        report.observations.Add("background melee contacts="+string.Join(",",battle.Enemies.Skip(2).Select(e=>e.Melee?e.Melee.Contacts:-1))+" health="+string.Join(",",battle.Enemies.Skip(2).Select(e=>e.Health)));
        Check("four sabres and no firearms",battle.Enemies.Skip(2).All(e=>e.Melee&&e.Melee.Weapon&&!e.rifle.enabled&&!e.rifle.weapon.gameObject.activeSelf&&e.Shots==0));
        Check("allies contribute to battle",battle.Enemies.Skip(2).Any(e=>e.Health<100));
        Check("police sabre contacts with allies",battle.Enemies.Skip(2).All(e=>e.Melee&&e.Melee.Contacts>0));
        flow.player.Warp(new Vector3(-8,0,29.7f),new Vector3(-12,1.2f,28.3f));yield return Shot("campus-active-combat-"+mode);

        if(mode=="retreat")
        {
            foreach(var enemy in battle.Enemies)if(enemy&&!enemy.Defeated)enemy.Hit(100,flow.player.transform.position);
            for(int i=0;i<4;i++)Invoke(battle,"AdvanceAssaultStage");
            flow.school.tado.transform.position=new Vector3(12,0,36);
            report.observations.Add("Isolated retreat diagnostic: all officers defeated and four stage handoffs invoked; guide positioned at final support rally.");
        }
        for(int stage=0;mode!="retreat"&&stage<Chapter3SchoolBattle.AssaultCombatStages;stage++)
        {
            int[] indices=stage<2?new[]{stage}:stage==2?new[]{2,3}:new[]{4,5};
            yield return Wait(()=>battle.Enemies[indices[0]]!=null,5,"stage guards");
            foreach(int index in indices)
            {
                var enemy=battle.Enemies[index];enemy.HoldFire(10);
                yield return Defeat(enemy,knife);
            }
            Check("stage "+stage+" police defeated",indices.All(i=>battle.Enemies[i].Defeated));
            var waypoints=battle.AssaultWaypoints;int routeIndex=0;float start=Time.realtimeSinceStartup;
            // Player follows the authored route using CharacterController.Move;
            // only the weapon inspection above uses position fixtures.
            flow.player.Warp(flow.school.tado.transform.position+Vector3.back*2,flow.school.tado.transform.position+Vector3.up);
            while(battle.Step==stage)
            {
                if(Time.realtimeSinceStartup-start>100)throw new Exception("stage walk timeout "+stage+" player="+flow.player.transform.position+" tado="+flow.school.tado.transform.position+" guide="+Get(battle,"guideIndex"));
                Set(battle,"Health",100);
                Follow(flow.school.tado.transform.position,2);yield return null;routeIndex++;
            }
            Check("stage "+stage+" completed by walking",battle.Step==stage+1);yield return Shot("campus-stage-"+stage+"-"+mode);
            if(stage==1)
            {
                Check("two old guards do not unlock retreat",battle.Step==2&&!battle.CallRetreat());
                battle.Damage(1000,flow.player.transform.position);Check("campus checkpoint down",battle.CurrentStage==Chapter3SchoolBattle.Stage.Down);
                Check("campus retry",battle.Retry());yield return null;
                Check("campus retry preserves progress",battle.Step==2&&battle.Enemies.Count==6&&battle.Enemies[0]==null&&battle.Enemies[1]==null);
                yield return MeleePlayer(battle);
            }
        }
        Check("all four support officers defeated",battle.CampusDefeatedCount==4);
        flow.player.Warp(flow.school.tado.transform.position+Vector3.back*3,flow.school.tado.transform.position+Vector3.up);
        Check("retreat after full battle",battle.CallRetreat());Check("no duplicate signal",!battle.CallRetreat());
        float began=Time.realtimeSinceStartup;Vector3[] previous=null;float maxTravel=0,maxSpeed=0;int frames=0;
        var ambient=flow.school.GetComponentInChildren<Chapter3SchoolAmbient>();
        while(battle.Fighting)
        {
            if(Time.realtimeSinceStartup-began>150)throw new Exception("retreat timeout count="+battle.RetreatArrivedCount+" tado="+flow.school.tado.transform.position+" player="+flow.player.transform.position);
            Follow(flow.school.tado.transform.position,2);Set(battle,"Health",100);yield return null;
            var positions=Enumerable.Range(0,10).Select(i=>ambient.Ambusher(i).position).ToArray();
            if(previous!=null)for(int i=0;i<10;i++)
            {float travel=Vector3.Distance(previous[i],positions[i]);maxTravel=Mathf.Max(maxTravel,travel);maxSpeed=Mathf.Max(maxSpeed,travel/Mathf.Max(.0001f,Time.deltaTime));}
            previous=positions;frames++;
        }
        Check("all ten physically arrived",battle.RetreatArrived&&battle.RetreatArrivedCount==10);
        report.observations.Add("retreat frames="+frames+" max displacement="+maxTravel+" max speed="+maxSpeed);
        Check("retreat continuous movement",maxSpeed<3.3f&&frames>100);
        yield return Shot("all-ten-retreated-"+mode);
        yield return Wait(()=>battle.Rescue&&battle.Rescue.Active,35,"regroup hands off to rescue");
        Check("original rescue continuation reached",battle.Rescue&&battle.Rescue.Active);
        flow.EnterSchool();yield return Wait(()=>flow.school.Exploring,20,"F8 equivalent reset");
        Check("reset two calm original officers",flow.school.Battle.Enemies.Count==2);
    }
    static IEnumerator MeleePlayer(Chapter3SchoolBattle battle)
    {
        var enemy=battle.Enemies[2];
        flow.player.Warp(new Vector3(-8,0,29.7f),new Vector3(-12,1.2f,28.3f));
        var hand=enemy.actor.Rig.RightHand.position;float movement=0,observeUntil=Time.time+4;bool raised=false,cut=false;
        while(Time.time<observeUntil)
        {
            yield return null;movement=Mathf.Max(movement,Vector3.Distance(hand,enemy.actor.Rig.RightHand.position));
            float started=(float)Get(enemy.Melee,"started"),phase=started<0?-1:(Time.time-started)/1.15f;
            if(phase>.15f&&phase<.45f&&!raised){raised=true;yield return Shot("duel-raised-"+mode);}
            if(phase>.6f&&phase<.85f&&!cut){cut=true;yield return Shot("duel-cut-"+mode);}
        }
        report.observations.Add("sabre hand travel="+movement);
        Check("sabre hand visibly moves",movement>.18f);
        foreach(var e in battle.Enemies)if(e&&e!=enemy)e.HoldFire(30);
        Set(battle,"Health",100);
        flow.player.Warp(enemy.transform.position+Vector3.right*1.45f,enemy.AimPoint);
        float until=Time.time+12;while(Time.time<until&&battle.Health==100)yield return null;
        report.observations.Add("melee player health="+battle.Health+" player="+flow.player.transform.position+" enemy="+enemy.transform.position+" contacts="+enemy.Melee.Contacts);
        Check("nearby sabre strike damages player by 20",battle.Health==80);
        yield return Shot("sabre-close-contact-"+mode);
        flow.player.Warp(enemy.transform.position+Vector3.right*4,enemy.AimPoint);Set(battle,"Health",100);
        until=Time.time+4;while(Time.time<until)yield return null;
        Check("sabre cannot hurt distant player",battle.Health==100);
        Check("melee guards never fire rifle",battle.Enemies.Skip(2).All(e=>e.Shots==0));
    }
    static void Dump()
    {
        if(!flow||!flow.school||!flow.school.Battle)return;
        var ambient=flow.school.GetComponentInChildren<Chapter3SchoolAmbient>();if(!ambient||!ambient.Withdrawing)return;
        var legs=(int[])Get(ambient,"withdrawalLeg");var routes=(Vector3[][])Get(ambient,"withdrawalRoutes");
        for(int i=0;i<10;i++)
        {
            var actor=ambient.Ambusher(i);var target=routes[i]!=null&&legs[i]<routes[i].Length?routes[i][legs[i]]:actor.position;
            report.observations.Add("retreater "+i+" pos="+actor.position+" leg="+legs[i]+" target="+target+" obstacles="+string.Join(";",Physics.OverlapCapsule(target+Vector3.up*.42f,target+Vector3.up*1.25f,.36f).Select(c=>c.name+" at "+c.transform.position)));
        }
    }
    static IEnumerator Defeat(Chapter3Enemy enemy,bool knife)
    {
        float began=Time.realtimeSinceStartup;
        while(!enemy.Defeated)
        {
            if(Time.realtimeSinceStartup-began>22)throw new Exception("weapon timeout "+enemy.name+" feedback="+flow.combat.weapons.Feedback);
            Set(flow.school.Battle,"Health",100);
            Vector3[] offsets={Vector3.back,Vector3.left,Vector3.right,Vector3.forward};bool positioned=false;
            foreach(var direction in offsets)
            {
                var pos=enemy.transform.position+direction*(knife?1.3f:3);
                flow.player.Warp(pos,enemy.AimPoint);Physics.SyncTransforms();
                var ray=enemy.AimPoint-flow.player.view.transform.position;
                if(flow.combat.Trace(flow.player.view.transform.position,ray.normalized,ray.magnitude+.1f,out var hit)&&hit.collider.GetComponentInParent<Chapter3Enemy>()==enemy){positioned=true;break;}
            }
            Check("unobstructed "+enemy.name,positioned);
            yield return null;flow.player.FocusOn(enemy.AimPoint);yield return new WaitForEndOfFrame();
            var w=flow.combat.weapons;
            if(w.Ready){if(!knife&&!w.Loaded)w.Reload();else w.Attack();}
            yield return new WaitForSeconds(.8f);
        }
    }
    static void Follow(Vector3 target,float distance)
    {
        var delta=Vector3.ProjectOnPlane(target-flow.player.transform.position,Vector3.up);
        if(delta.magnitude>distance)flow.player.Move(delta.normalized*2.8f*Time.deltaTime);
        flow.player.FocusOn(target+Vector3.up*1.2f);
    }
    static object Invoke(object target,string method,params object[] args)=>target.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,args);
    static object Get(object target,string field)=>target.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);
    static void Set(object target,string property,object value)=>target.GetType().GetField("<"+property+">k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
    static void Check(string name,bool passed){if(!passed)throw new Exception("FAILED: "+name);report.checks.Add(name);File.WriteAllText(Dir+"progress.txt",name);}
    static IEnumerator Wait(Func<bool> condition,float timeout,string label)
    {float start=Time.realtimeSinceStartup;while(!condition()){if(Time.realtimeSinceStartup-start>timeout)throw new Exception("Timeout: "+label);yield return null;}}
    static IEnumerator Shot(string name)
    {
        yield return new WaitForEndOfFrame();var camera=flow.player.view;var previous=camera.targetTexture;var active=RenderTexture.active;
        var rt=new RenderTexture(1600,900,24);var texture=new Texture2D(1600,900,TextureFormat.RGB24,false);
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();File.WriteAllBytes(Dir+name+".png",texture.EncodeToPNG());}
        finally{camera.targetTexture=previous;RenderTexture.active=active;Object.Destroy(rt);Object.Destroy(texture);}
    }
}
public sealed class Chapter3TreeRoadDriver:MonoBehaviour {IEnumerator Start(){yield return Chapter3TreeRoadVerify.Run();}}
