using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class Chapter2ChatFramingProbe
{
    const string Dir="_CodexBackups/day_identity_mona_20261011/", Session="ChatFramingProbe";
    static Chapter2Controller c;
    static Chapter2AmbientNPC[] people;
    static float at;
    static int phase,index;
    static Vector3 position,local;
    static Quaternion rotation,localRotation;
    static Results results;
    [Serializable] public class Check {public string name;public float approach,distance,positionRestore,rotationRestore,viewRestore;public bool opened,stayedOpen,closed;public Vector3 headViewport;}
    [Serializable] public class Results {public List<Check> checks=new List<Check>();public int errors;public bool outsideRejected,timeoutClosed,timeoutRestored,keyOpened,escapeClosed;}
    static Chapter2ChatFramingProbe(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=State;Application.logMessageReceived+=Log;}
    static void Log(string m,string t,LogType k){if(results!=null&&(k==LogType.Exception||k==LogType.Error||k==LogType.Assert)){results.errors++;File.AppendAllText(Dir+"chat-errors.txt",m+"\n"+t+"\n");}}
    static void State(PlayModeStateChange s)
    {
        if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Session,false)){phase=0;index=0;at=Time.realtimeSinceStartup+1;results=new Results();}
        if(s==PlayModeStateChange.EnteredEditMode&&results!=null){File.WriteAllText(Dir+"chat-results.json",JsonUtility.ToJson(results,true));SessionState.EraseBool(Session);results=null;}
    }
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(File.Exists(Dir+"chat-command.txt")){File.Delete(Dir+"chat-command.txt");SessionState.SetBool(Session,true);EditorApplication.isPaused=false;EditorApplication.isPlaying=true;}
        if(!Application.isPlaying||results==null||Time.realtimeSinceStartup<at)return;
        if(phase==0)
        {
            c=Object.FindFirstObjectByType<Chapter2Controller>();if(!c)return;
            c.StopAllCoroutines();c.saveResult=false;c.chapterAudio.StopAllAudio();
            foreach(var v in c.GetComponentsInChildren<UnityEngine.Video.VideoPlayer>())v.Stop();
            c.ui.videoImage.gameObject.SetActive(false);c.ui.fade.color=Color.clear;c.ui.Line("","");
            c.StartCoroutine(c.exploration.Explore(180));
            people=c.dayGroup.GetComponentsInChildren<Chapter2AmbientNPC>().OrderBy(x=>x.name).ToArray();
            foreach(var p in people)p.patrolEnabled=false; var audit=new System.Text.StringBuilder();foreach(var p in people){var a=p.GetComponent<Chapter2Actor>();var link=p.GetComponent<Chapter2DayCouncilIdentity>();audit.AppendLine(a.DisplayName+" | council="+(link?link.councilActor.DisplayName:"independent")+" | "+c.GetComponent<Chapter2LocalDialogue>().Persona(p));}File.WriteAllText(Dir+"day-personas.txt",audit.ToString());
            phase=1;at=Time.realtimeSinceStartup+.5f;return;
        }
        if(phase==1)
        {
            if(index>=people.Length*3){phase=7;return;}
            var npc=people[index/3];float distance=new[]{.12f,1.5f,2.95f}[index%3];
            c.player.Warp(npc.transform.position+Vector3.back*distance,npc.transform.position);
            position=c.player.transform.position;rotation=c.player.transform.rotation;
            local=c.player.view.transform.localPosition;localRotation=c.player.view.transform.localRotation;
            bool opened=c.exploration.TryOpen(npc);
            results.checks.Add(new Check{name=npc.name,approach=distance,opened=opened});
            phase=2;at=Time.realtimeSinceStartup+.5f;return;
        }
        if(phase==2)
        {
            var npc=people[index/3];var check=results.checks.Last();var actor=npc.GetComponent<Chapter2Actor>();
            check.stayedOpen=c.exploration.ChatOpen;
            check.distance=Vector3.ProjectOnPlane(c.player.view.transform.position-npc.transform.position,Vector3.up).magnitude;
            check.headViewport=c.player.view.WorldToViewportPoint(actor.Rig.Head.position);
            if(index%3==0)Chapter2RefineProbe.Shot("chat-"+npc.name);
            c.exploration.CloseChat();
            check.closed=!c.exploration.ChatOpen;check.positionRestore=Vector3.Distance(position,c.player.transform.position);
            check.rotationRestore=Quaternion.Angle(rotation,c.player.transform.rotation);
            check.viewRestore=Vector3.Distance(local,c.player.view.transform.localPosition)+Quaternion.Angle(localRotation,c.player.view.transform.localRotation);
            index++;phase=1;at=Time.realtimeSinceStartup+.05f;return;
        }
        if(phase==4)
        {
            c.player.Warp(people[0].transform.position+Vector3.back*4,people[0].transform.position);
            results.outsideRejected=!c.exploration.TryOpen(people[0]);
            c.player.Warp(people[0].transform.position+Vector3.back*.2f,people[0].transform.position);
            position=c.player.transform.position;local=c.player.view.transform.localPosition;
            c.exploration.TryOpen(people[0]);
            typeof(Chapter2Exploration).GetField("deadline",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(c.exploration,Time.unscaledTime+.1f);
            phase=5;at=Time.realtimeSinceStartup+.5f;return;
        }
        if(phase==5)
        {
            results.timeoutClosed=!c.exploration.ChatOpen&&!c.exploration.Active;
            results.timeoutRestored=Vector3.ProjectOnPlane(position-c.player.transform.position,Vector3.up).magnitude<.02f&&Vector3.Distance(local,c.player.view.transform.localPosition)<.001f;
            EditorApplication.isPlaying=false;phase=6;
        }
        if(phase==7)
        {
            var woman=people.First(p=>p.name=="露比·巴干");
            c.player.Warp(woman.transform.position+Vector3.back*.12f,woman.transform.position);
            c.player.FocusOn(woman.transform.position+Vector3.up*1.45f);
            phase=8;at=Time.realtimeSinceStartup+.3f;return;
        }
        if(phase==8){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(UnityEngine.InputSystem.Key.E));phase=9;at=Time.realtimeSinceStartup+.3f;return;}
        if(phase==9)
        {
            results.keyOpened=c.exploration.ChatOpen&&c.exploration.Current.name=="露比·巴干";
            Chapter2RefineProbe.Shot("chat-keyboard-E");
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(UnityEngine.InputSystem.Key.Escape));phase=10;at=Time.realtimeSinceStartup+.3f;return;
        }
        if(phase==10){results.escapeClosed=!c.exploration.ChatOpen;InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());phase=4;}
    }
}

