using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static class Chapter2FinalPerformanceAuthoring
{
 [MenuItem("Tools/Chapter 2/Apply Final Council Performance")]
 public static void Apply(){
  if(Application.isPlaying)throw new InvalidOperationException("Stop Play first.");
  var c=Object.FindFirstObjectByType<Chapter2Controller>();
  var old=c.leaders[4];
  if(old.GetComponentsInChildren<SkinnedMeshRenderer>(true).Any(s=>AssetDatabase.GetAssetPath(s.sharedMesh).Contains("55823c37"))){
   var replace=typeof(Chapter2CouncilCastAuthoring).GetMethod("Replace",BindingFlags.NonPublic|BindingFlags.Static);
   var donor=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/人/部落女/部落女性2/部落女姓2.fbx");
   var actor=(Chapter2Actor)replace.Invoke(null,new object[]{old,donor});c.leaders[4]=actor;
   actor.transform.localScale*=1.58f/1.72f;actor.seatedHipHeight=.53f;
   // A distinct supporting elder: preserve the long garment, use a smaller
   // stature, broader face and dark earth-red cloth instead of the donor's white.
   foreach(var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>(true)){
    var mesh=Object.Instantiate(skin.sharedMesh);mesh.name="Iwan council elder";
    var head=skin.bones.First(t=>t.name.EndsWith("Head"));int slot=Array.IndexOf(skin.bones,head);
    var v=mesh.vertices;var weights=mesh.boneWeights;var bind=mesh.bindposes[slot];var inv=bind.inverse;
    for(int i=0;i<v.Length;i++){
     var w=weights[i];float weight=(w.boneIndex0==slot?w.weight0:0)+(w.boneIndex1==slot?w.weight1:0)+(w.boneIndex2==slot?w.weight2:0)+(w.boneIndex3==slot?w.weight3:0);
     if(weight<.5f)continue;var p=bind.MultiplyPoint3x4(v[i]);p.x*=1.07f;p.y*=.97f;v[i]=Vector3.Lerp(v[i],inv.MultiplyPoint3x4(p),weight);
    }
    mesh.vertices=v;mesh.RecalculateNormals();MeshUtility.SetMeshCompression(mesh,ModelImporterMeshCompression.Low);string path="Assets/Chapter2/Poses/IwanCouncilElder.asset";
    UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(new Object[]{mesh},path,false);Object.DestroyImmediate(mesh);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
    skin.sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
    skin.sharedMaterials=skin.sharedMaterials.Select(m=>{
     string matPath="Assets/Chapter2/Materials/Iwan_"+m.name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
     if(!mat){mat=new Material(m);mat.SetColor("_BaseColor",new Color(.57f,.35f,.28f));AssetDatabase.CreateAsset(mat,matPath);}return mat;
    }).ToArray();
   }
   var seat=c.nightGroup.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Council seat").OrderBy(t=>Vector3.Distance(t.position,actor.transform.position)).First();
   var inward=Vector3.ProjectOnPlane(c.campfire.position-actor.transform.position,Vector3.up).normalized;
   seat.position=actor.transform.position-inward*.25f+Vector3.up*.21f;seat.localScale=new Vector3(.42f,.21f,.42f);
  }
  Chapter2CastNames.Apply(c);
  foreach(var actor in c.nightGroup.GetComponentsInChildren<Chapter2Actor>(true)){
   var drama=actor.GetComponent<Chapter2CouncilDrama>()??actor.gameObject.AddComponent<Chapter2CouncilDrama>();
   drama.hasKnife=actor!=c.mona&&actor!=c.watan;
   if(!drama.hasKnife)continue;
   if(!drama.knife)drama.knife=Chapter2CouncilKnifeAuthoring.CreateKnife(actor.transform);
   if(actor.isChild){drama.knife.localScale=new Vector3(.66f/actor.transform.lossyScale.x,.66f/actor.transform.lossyScale.y,.66f/actor.transform.lossyScale.z);}
   drama.knifeTip=drama.knife.Find("刀尖");
   Chapter2KnifeGripAuthoring.Apply(actor,drama);EditorUtility.SetDirty(drama);
  }
  EditorUtility.SetDirty(c);EditorSceneManager.MarkSceneDirty(c.gameObject.scene);EditorSceneManager.SaveScene(c.gameObject.scene);AssetDatabase.SaveAssets();
  Chapter2SpeechFaceAuthoring.Apply();
  // SaveAssets can rewrite generated meshes as YAML; retain compact binary
  // assets so GitHub collaborators can download each file normally.
  foreach(var actor in c.nightGroup.GetComponentsInChildren<Chapter2Actor>(true)){
   var drama=actor.GetComponent<Chapter2CouncilDrama>();
   if(drama&&drama.gripMesh)Chapter2CouncilStoryAuthoring.SaveBinary(drama.gripMesh);
  }
  var elderMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Chapter2/Poses/IwanCouncilElder.asset");
  if(elderMesh)Chapter2CouncilStoryAuthoring.SaveBinary(elderMesh);
 }
}
