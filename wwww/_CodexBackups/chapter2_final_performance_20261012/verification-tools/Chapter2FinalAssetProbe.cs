using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
public static class Chapter2FinalAssetProbe
{
 public static void Apply(){
  if(Application.isPlaying)throw new InvalidOperationException("Stop Play first");
  AssetDatabase.SaveAssets();
  foreach(var path in Directory.GetFiles("Assets/Chapter2/Props","NaturalGrip_*.asset").Concat(new[]{"Assets/Chapter2/Poses/IwanCouncilElder.asset"})){
   var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);MeshUtility.SetMeshCompression(mesh,ModelImporterMeshCompression.Low);Chapter2CouncilStoryAuthoring.SaveBinary(mesh);GC.Collect();
  }
  var c=UnityEngine.Object.FindFirstObjectByType<Chapter2Controller>();
  var s=new StringBuilder("playing="+Application.isPlaying+" dirty="+c.gameObject.scene.isDirty+" failed="+EditorUtility.scriptCompilationFailed+"\n");
  foreach(var a in c.dayGroup.GetComponentsInChildren<Chapter2Actor>(true).Concat(c.nightGroup.GetComponentsInChildren<Chapter2Actor>(true))){
   var face=a.GetComponent<Chapter2SpeechFace>();
   s.AppendLine(a.name+" police="+a.police+" face="+(face!=null));
   if(face)foreach(var p in face.surfaces)s.AppendLine(" vertices="+p.renderer.sharedMesh.vertexCount+" shapes="+p.renderer.sharedMesh.blendShapeCount+" head="+p.head.name+" body="+p.body.sharedMaterial.shader.name+" queue="+p.body.sharedMaterial.renderQueue);
  }
  s.AppendLine("Exploration seconds="+c.explorationSeconds);
  File.WriteAllText(Chapter2FinalPerformanceProbe.Dir+"final-assets.txt",s.ToString());
 }
}
