using System.Reflection;
using UnityEngine;
using UnityEditor;
using Object=UnityEngine.Object;
public static class Chapter2CandidatePreview{
 public static void Apply(){
  var c=Object.FindFirstObjectByType<Chapter2Controller>();bool active=c.nightGroup.activeSelf;c.nightGroup.SetActive(true);
  var old=c.leaders[4];var temp=Object.Instantiate(old,old.transform.parent);old.gameObject.SetActive(false);
  var method=typeof(Chapter2CouncilCastAuthoring).GetMethod("Replace",BindingFlags.NonPublic|BindingFlags.Static);
  Chapter2Actor a=null;
  try{
   a=(Chapter2Actor)method.Invoke(null,new object[]{temp,AssetDatabase.LoadAssetAtPath<GameObject>("Assets/人/畢專 賽德克青年/賽德克青年.fbx")});
   typeof(Chapter2Actor).GetMethod("Start",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(a,null);
   Chapter2FinalPerformanceProbe.Portrait(a,"candidate",false);
  }finally{if(a)Object.DestroyImmediate(a.gameObject);old.gameObject.SetActive(true);c.nightGroup.SetActive(active);}
 }
}
