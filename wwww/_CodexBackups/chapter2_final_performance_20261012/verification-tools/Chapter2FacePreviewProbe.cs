using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class Chapter2FacePreviewProbe
{
    public static void Apply(){
        var c=Object.FindFirstObjectByType<Chapter2Controller>();
        foreach(var a in c.nightGroup.GetComponentsInChildren<Chapter2Actor>()){
            var face=a.GetComponent<Chapter2SpeechFace>();if(!face)continue;
            foreach(var s in face.surfaces)for(int i=0;i<s.renderer.sharedMesh.blendShapeCount;i++)s.renderer.SetBlendShapeWeight(i,0);
            Chapter2FinalPerformanceProbe.Portrait(a,"neutral-"+a.name,true);
            foreach(var s in face.surfaces)s.renderer.SetBlendShapeWeight(s.renderer.sharedMesh.GetBlendShapeIndex(Chapter2SpeechFace.Jaw),80);
            Chapter2FinalPerformanceProbe.Portrait(a,"open-"+a.name,true);
        }
    }
}
