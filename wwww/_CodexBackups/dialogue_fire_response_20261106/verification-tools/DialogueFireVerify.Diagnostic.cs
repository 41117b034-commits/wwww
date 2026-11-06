using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Profiling;
using Object=UnityEngine.Object;

public static partial class DialogueFireVerify
{
    static bool observing;
    static readonly List<string> inputLog=new List<string>();
    static double rawPressAt;
    static int rawPresses,polledPresses;
    static void RawInput(InputEventPtr e,InputDevice device)
    {
        if(!observing||(!e.IsA<StateEvent>()&&!e.IsA<DeltaStateEvent>()))return;
        if(device is Keyboard k)
        {
            foreach(var key in k.allKeys)if(key.ReadValueFromEvent(e,out float v)&&v>.5f&&!key.isPressed)
            {inputLog.Add("raw key="+key.keyCode+" frame="+Time.frameCount);File.WriteAllLines(Dir+"live-input.txt",inputLog);}
        }
        if(!(device is Mouse m))return;
        if(m.leftButton.ReadValueFromEvent(e,out float value)&&value>.5f&&!m.leftButton.isPressed)
        {rawPressAt=Time.realtimeSinceStartupAsDouble;rawPresses++;inputLog.Add("raw press #"+rawPresses+" frame="+Time.frameCount+" eventTime="+e.time);}
    }
    public static void ObserveInput()
    {
        if(!observing||!f)return;
        if(Keyboard.current?.rKey.wasPressedThisFrame==true){inputLog.Add("polled R frame="+Time.frameCount+" focused="+Application.isFocused);File.WriteAllLines(Dir+"live-input.txt",inputLog);}
        if(Mouse.current?.leftButton.wasPressedThisFrame==true)
        {
            var w=f.combat.weapons;polledPresses++;
            inputLog.Add("polled #"+polledPresses+" frame="+Time.frameCount+" ready="+w.Ready+" loaded="+w.Loaded+" reload="+w.Reloading+" focused="+Application.isFocused+" fighting="+f.combat.Fighting+" inputMs="+((Time.realtimeSinceStartupAsDouble-rawPressAt)*1000)+" shots="+w.Shots);
            File.WriteAllLines(Dir+"live-input.txt",inputLog);
        }
    }
    static IEnumerator Investigate()
    {
        yield return Frames(5);f=Object.FindFirstObjectByType<Chapter3Flow>();Check("formal third chapter",f&&f.startAtStation);
        report.notes.Add("Input update mode="+InputSystem.settings.updateMode+" vSync="+QualitySettings.vSyncCount+" targetFPS="+Application.targetFrameRate);
        f.Carry(Chapter3Weapons.Weapon.Rifle);f.EnterSchool();yield return Wait(()=>f.school.Exploring,30,"school exploration");
        f.player.Warp(f.school.tado.transform.position+new Vector3(.5f,.06f,-2.5f),f.school.tado.transform.position+Vector3.up);
        yield return Wait(()=>f.school.Battle.CurrentStage==Chapter3SchoolBattle.Stage.Choice,45,"Tado briefing");
        f.school.Battle.Choose(Chapter3SchoolBattle.Mission.Tado);yield return Wait(()=>f.school.Battle.Fighting,30,"school fighting");
        f.player.Warp(new Vector3(-5,.06f,16),new Vector3(0,1.2f,23));f.player.canLook=false;yield return new WaitForSeconds(3);
        yield return Capture("school-diagnostic");
        Inventory();yield return Sample("school-live",6);
        if(mode=="diagnostic-clothing")
        {
            var detail=Object.FindFirstObjectByType<Chapter3CrowdDetail>();var original=(Chapter3CrowdDetailCatalog)Field(detail,"catalog");
            var closer=Object.Instantiate(original);closer.enterDistance=5;closer.leaveDistance=3.5f;
            detail.GetType().GetField("catalog",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(detail,closer);
            yield return Frames(3);Inventory();yield return Sample("school-clothing-five-metres",6);
            detail.GetType().GetField("catalog",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(detail,original);Object.Destroy(closer);
        }
        bool enabled=f.player.view.enabled;f.player.view.enabled=false;yield return Sample("school-no-render",3);f.player.view.enabled=enabled;
        yield return Frames(3);
        f.player.canLook=false;
        inputLog.Clear();rawPresses=polledPresses=0;observing=true;InputSystem.onEvent+=RawInput;
        File.WriteAllText(Dir+"native-ready.txt",DateTime.UtcNow.ToString("O"));
        double until=Time.realtimeSinceStartupAsDouble+600;var w=f.combat.weapons;int lastShots=w.Shots;
        while(Time.realtimeSinceStartupAsDouble<until&&!File.Exists(Dir+"native-finish.txt"))
        {
            if(mode=="native-final"&&!w.Loaded&&!w.Reloading&&w.Ready)w.RequestReload();
            if(w.Shots!=lastShots){inputLog.Add("SHOT frame="+Time.frameCount+" count="+w.Shots+" rawToShotMs="+((Time.realtimeSinceStartupAsDouble-rawPressAt)*1000));lastShots=w.Shots;File.WriteAllLines(Dir+"live-input.txt",inputLog);}
            yield return null;
        }
        observing=false;InputSystem.onEvent-=RawInput;report.notes.AddRange(inputLog);
        report.notes.Add("native raw="+rawPresses+" polled="+polledPresses+" shots="+w.Shots);Check("diagnostic recorded",true);
        if(mode=="native-final")
        {
            Check("all eight native clicks produce exactly one shot",rawPresses==8&&polledPresses==8&&w.Shots==8);
            report.notes.Add("Native Windows mouse clicks with driver-requested reloads after recoil; physical keyboard input was not observed from the automation tool. Camera looking is fixed for measurement; crowd animations/collisions remain active.");
            yield return GeometryChecks();
        }
    }
    static IEnumerator Sample(string label,float seconds)
    {
        var names=new[]{"PlayerLoop","BehaviourUpdate","PreLateUpdate.ScriptRunBehaviourLateUpdate","MeshSkinning.Skin","Physics.Simulate","Animator.Update","UpdateRendererBoundingVolumes","RenderLoop.Draw","GC.Alloc"};
        var rec=names.Select(n=>Recorder.Get(n)).ToArray();foreach(var r in rec)r.enabled=true;
        var frames=new List<double>();var totals=new double[rec.Length];double end=Time.realtimeSinceStartupAsDouble+seconds,previous=Time.realtimeSinceStartupAsDouble;
        while(Time.realtimeSinceStartupAsDouble<end)
        {
            yield return null;double now=Time.realtimeSinceStartupAsDouble;frames.Add((now-previous)*1000);previous=now;
            for(int i=0;i<rec.Length;i++)totals[i]+=rec[i].elapsedNanoseconds/1000000.0;
        }
        frames.Sort();report.notes.Add(label+" n="+frames.Count+" medianMs="+frames[frames.Count/2]+" p95Ms="+frames[(int)(frames.Count*.95)]+" "+string.Join("; ",names.Select((n,i)=>n+"="+(totals[i]/frames.Count).ToString("F2"))));Save();
    }
    static void Inventory()
    {
        var skins=Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None).Where(s=>s.enabled&&s.sharedMesh).ToArray();
        foreach(var s in skins.OrderByDescending(s=>s.sharedMesh.vertexCount).Take(25))
        {var crowd=s.GetComponentInParent<Chapter3SchoolCrowdActor>();report.notes.Add("skin="+s.name+" vertices="+s.sharedMesh.vertexCount+" distance="+Vector3.Distance(f.player.view.transform.position,s.bounds.center)+" crowd="+(crowd?crowd.name:"")+" visible="+s.isVisible);}
        report.notes.Add("active skins="+skins.Length+" vertices="+skins.Sum(s=>(long)s.sharedMesh.vertexCount));Save();
        var detail=Object.FindFirstObjectByType<Chapter3CrowdDetail>();var catalog=Resources.Load<Chapter3CrowdDetailCatalog>("Chapter3/CrowdDetailCatalog");
        report.notes.Add("crowd bindings="+detail.BindingCount+" far="+detail.DistantCount+" enabled="+detail.DetailEnabled);
        foreach(var s in skins.Where(s=>s.name.StartsWith("Crowd pose")).Take(12))
        {
            var c=s.GetComponentInParent<Chapter3SchoolCrowdActor>();
            report.notes.Add("fallback "+c.name+" skin="+s.sharedMesh.name+" bones="+s.bones.Length+" original="+string.Join(";",c.GetComponentsInChildren<MeshFilter>(true).Select(m=>m.sharedMesh.name+" "+UnityEditor.AssetDatabase.GetAssetPath(m.sharedMesh)))+" matching="+string.Join(";",catalog.entries.Where(e=>e.sourceVertices==s.sharedMesh.vertexCount).Select(e=>e.source.name+" "+UnityEditor.AssetDatabase.GetAssetPath(e.source)+" distantBones="+(e.posedDistant?e.posedDistant.bindposes.Length:-1))));
        }
        Save();
    }
}
