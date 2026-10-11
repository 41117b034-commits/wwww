using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Object=UnityEngine.Object;
[InitializeOnLoad]
public static class Chapter2MonaDetailProbe
{
    const string Dir="_CodexBackups/day_identity_mona_20261011/";
    static Chapter2MonaDetailProbe(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||Application.isPlaying||!File.Exists(Dir+"detail-command.txt"))return;
        File.Delete(Dir+"detail-command.txt");
        try{Inspect();}catch(Exception e){File.WriteAllText(Dir+"detail-error.txt",e.ToString());}
    }
    static void Inspect()
    {
        var c=Object.FindFirstObjectByType<Chapter2Controller>();var actor=c.mona;
        var a=actor.GetComponentInChildren<Animator>();var head=a.GetComponentsInChildren<Transform>().First(t=>t.name=="Head");
        var hands=actor.GetComponent<Chapter2CouncilHands>();var skin=hands.fistRenderer;var mesh=skin.sharedMesh;
        var vertices=mesh.vertices;var weights=mesh.boneWeights;var hand=skin.bones.First(t=>t.name=="R_Hand");int slot=Array.IndexOf(skin.bones,hand);
        var bind=mesh.bindposes[slot];var inv=Quaternion.Inverse(hands.rightPalmBasis);float length=0;
        var s=new System.Text.StringBuilder("index,x,y,z,weight\n");
        for(int i=0;i<vertices.Length;i++)if(W(weights[i],slot)>.1f){var p=inv*bind.MultiplyPoint3x4(vertices[i]);length=Mathf.Max(length,p.z);s.AppendLine(i+","+p.x+","+p.y+","+p.z+","+W(weights[i],slot));}
        File.WriteAllText(Dir+"hand-vertices.csv",s.ToString());File.WriteAllText(Dir+"hand-triangles.txt",string.Join(",",mesh.triangles));
        var cam=c.player.view;var p0=cam.transform.position;var rot=cam.transform.rotation;var ortho=cam.orthographic;var size=cam.orthographicSize;
        var active=c.nightGroup.activeSelf;var f=Vector3.ProjectOnPlane(actor.facing,Vector3.up).normalized;
        var canvas=c.ui.canvas;var enabled=canvas.enabled;canvas.enabled=false;c.nightGroup.SetActive(true);
        cam.transform.position=head.position+f*.8f;cam.transform.LookAt(head.position);cam.orthographic=true;cam.orthographicSize=.20f;
        var sky=RenderSettings.ambientLight;RenderSettings.ambientLight=Color.white*.8f;
        var lit=new GameObject("temporary portrait light").AddComponent<Light>();lit.type=LightType.Directional;lit.intensity=1;lit.transform.rotation=cam.transform.rotation;
        try{Chapter2RefineProbe.Shot("mona-neutral-calibration");var old=skin.sharedMesh;skin.sharedMesh=hands.refusalMesh;skin.SetBlendShapeWeight(hands.angerShape,100);Chapter2RefineProbe.Shot("mona-angry-calibration");skin.SetBlendShapeWeight(hands.angerShape,0);skin.sharedMesh=old;File.WriteAllText(Dir+"mona-calibration.txt","head="+head.position+" camera="+cam.transform.position+" faceForward="+f+" h="+skin.bounds.size.y+" handLength="+length);}
        finally{Object.DestroyImmediate(lit.gameObject);canvas.enabled=enabled;c.nightGroup.SetActive(active);RenderSettings.ambientLight=sky;cam.transform.SetPositionAndRotation(p0,rot);cam.orthographic=ortho;cam.orthographicSize=size;}
        Chapter2CouncilStoryAuthoring.SaveBinary(hands.refusalMesh);
    }
    static float W(BoneWeight w,int s)=>(w.boneIndex0==s?w.weight0:0)+(w.boneIndex1==s?w.weight1:0)+(w.boneIndex2==s?w.weight2:0)+(w.boneIndex3==s?w.weight3:0);
}

