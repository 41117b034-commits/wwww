using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using Object=UnityEngine.Object;

public static class Chapter3ActorDetailBuild
{
    const string Dir="_CodexBackups/chapter3_response_20261105/",Output="Assets/Chapter3/Resources/Chapter3/ActorDetailMeshes";
    static Queue<Mesh> jobs;
    static List<Chapter3CrowdDetailCatalog.Entry> entries;
    static Type engineType;
    static List<string> notes;
    public static void Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
        if(AssetDatabase.LoadAssetAtPath<Chapter3CrowdDetailCatalog>("Assets/Chapter3/Resources/Chapter3/ActorDetailCatalog.asset"))throw new Exception("Catalog already completed");
        var flow=Object.FindFirstObjectByType<Chapter3Flow>();
        var sources=flow.gameObject.scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SkinnedMeshRenderer>(true)).Select(s=>s.sharedMesh).Where(m=>m&&m.vertexCount>60000&&m.bindposes.Length>0).Distinct();
        jobs=new Queue<Mesh>(sources);entries=new List<Chapter3CrowdDetailCatalog.Entry>();notes=new List<string>();
        if(jobs.Count==0)throw new Exception("No native meshes found");
        string core="_CodexBackups/chapter3_hands_fire_20261019/verification-tools/UnityMeshSimplifier/";
        if(!File.ReadAllText(core+"SOURCE.txt").Contains("53fdb3122645bcd3ad2c258235200dff3dfbaa9b"))throw new Exception("Pinned source identity mismatch");
        engineType=Assembly.Load(File.ReadAllBytes(core+"Chapter3OfflineMeshSimplifier.dll")).GetType("UnityMeshSimplifier.MeshSimplifier",true);
        Directory.CreateDirectory(Output);AssetDatabase.Refresh();EditorApplication.update+=Step;
    }
    static void Step()
    {
        try
        {
            if(jobs.Count==0)
            {
                var catalog=ScriptableObject.CreateInstance<Chapter3CrowdDetailCatalog>();catalog.automatic=true;catalog.enterDistance=5;catalog.leaveDistance=3.5f;
                catalog.simplifierCommit="53fdb3122645bcd3ad2c258235200dff3dfbaa9b";catalog.entries=entries.ToArray();
                AssetDatabase.CreateAsset(catalog,"Assets/Chapter3/Resources/Chapter3/ActorDetailCatalog.asset");AssetDatabase.SaveAssetIfDirty(catalog);
                notes.Add("completed=true");EditorApplication.update-=Step;File.WriteAllLines(Dir+"actor-detail-generation.txt",notes);return;
            }
            var source=jobs.Dequeue();AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source,out string sourceGuid,out long sourceId);
            string output=Output+"/"+sourceGuid+"_"+sourceId+".asset";
            Mesh result=AssetDatabase.LoadAssetAtPath<Mesh>(output);
            var existing=Resources.Load<Chapter3CrowdDetailCatalog>("Chapter3/CrowdDetailCatalog").entries.FirstOrDefault(e=>e.source==source&&e.sourceBones==source.bindposes.Length);
            if(existing!=null)result=existing.distant;
            else if(!result)
            {
                using(var counts=source.GetBonesPerVertex())foreach(var n in counts)if(n>4)throw new Exception("Unsupported weights "+source.name);
                var engine=Activator.CreateInstance(engineType);var prop=engineType.GetProperty("SimplificationOptions");var options=prop.GetValue(engine);
                foreach(var n in new[]{"PreserveBorderEdges","PreserveUVSeamEdges","PreserveUVFoldoverEdges"})options.GetType().GetField(n).SetValue(options,true);prop.SetValue(engine,options);
                engineType.GetMethod("Initialize").Invoke(engine,new object[]{source});engineType.GetMethod("SimplifyMesh").Invoke(engine,new object[]{.18f});result=(Mesh)engineType.GetMethod("ToMesh").Invoke(engine,null);
                result.name=source.name;result.bounds=source.bounds;
                Validate(source,result);
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source,out string guid,out long id);AssetDatabase.CreateAsset(result,Output+"/"+guid+"_"+id+".asset");AssetDatabase.SaveAssetIfDirty(result);
            }
            Validate(source,result);
            entries.Add(new Chapter3CrowdDetailCatalog.Entry{source=source,distant=result,sourceVertices=source.vertexCount,sourceBones=source.bindposes.Length});
            notes.Add(source.name+" vertices="+source.vertexCount+" -> "+result.vertexCount+" triangles="+source.triangles.Length/3+" -> "+result.triangles.Length/3+" bones="+result.bindposes.Length+" submeshes="+result.subMeshCount+" blendshapes="+result.blendShapeCount);
            File.WriteAllLines(Dir+"actor-detail-generation.txt",notes);
        }
        catch(Exception e){EditorApplication.update-=Step;notes.Add(e.ToString());File.WriteAllLines(Dir+"actor-detail-generation.txt",notes);Debug.LogException(e);}
    }
    static void Validate(Mesh a,Mesh b)
    {
        if(!b||b.vertexCount<3||b.vertexCount>=a.vertexCount||a.subMeshCount!=b.subMeshCount||!a.bindposes.SequenceEqual(b.bindposes)||a.blendShapeCount!=b.blendShapeCount)throw new Exception("Mesh layout mismatch "+a.name);
        for(int i=0;i<a.blendShapeCount;i++)if(a.GetBlendShapeName(i)!=b.GetBlendShapeName(i))throw new Exception("Blendshape order mismatch");
        if(b.bindposes.Length>0&&b.boneWeights.Length!=b.vertexCount)throw new Exception("Missing skin weights");
        int bones=b.bindposes.Length;
        if(bones>0)foreach(var w in b.boneWeights)if(Mathf.Abs(w.weight0+w.weight1+w.weight2+w.weight3-1)>.015f||w.boneIndex0>=bones||w.boneIndex1>=bones||w.boneIndex2>=bones||w.boneIndex3>=bones)throw new Exception("Invalid skin weights");
    }
    public static void Append(Chapter3Flow flow)
    {
        string core="_CodexBackups/chapter3_hands_fire_20261019/verification-tools/UnityMeshSimplifier/";
        engineType=Assembly.Load(File.ReadAllBytes(core+"Chapter3OfflineMeshSimplifier.dll")).GetType("UnityMeshSimplifier.MeshSimplifier",true);
        var catalog=Resources.Load<Chapter3CrowdDetailCatalog>("Chapter3/ActorDetailCatalog");var list=catalog.entries.ToList();
        foreach(var enemy in flow.combat.Enemies.Take(1))
        {
            foreach(var skin in enemy.actor.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if(!enemy.actor.Rig.TryGetGripMeshes(skin,out var source,out var posed))continue;
                var entry=list.Single(e=>e.source==source);
                entry.posedDistant=Reduce(posed,.18f,Output+"/PoliceGrip.asset");
            }
            foreach(var filter in enemy.rifle.weapon.GetComponentsInChildren<MeshFilter>())
            {
                var source=filter.sharedMesh;if(source.vertexCount<60000)continue;
                if(list.Any(e=>e.source==source))continue;
                var result=Reduce(source,.08f,Output+"/NpcRifle.asset");
                list.Add(new Chapter3CrowdDetailCatalog.Entry{source=source,distant=result,sourceVertices=source.vertexCount,sourceBones=0});
            }
        }
        catalog.entries=list.ToArray();EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssetIfDirty(catalog);
        File.AppendAllText(Dir+"actor-detail-generation.txt","appended grip and rifle completed=true\n");
    }
    public static void Rifle()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode required for imported static mesh");
        string core="_CodexBackups/chapter3_hands_fire_20261019/verification-tools/UnityMeshSimplifier/";
        engineType=Assembly.Load(File.ReadAllBytes(core+"Chapter3OfflineMeshSimplifier.dll")).GetType("UnityMeshSimplifier.MeshSimplifier",true);
        var catalog=Resources.Load<Chapter3CrowdDetailCatalog>("Chapter3/ActorDetailCatalog");
        var entry=catalog.entries.Single(e=>e.sourceBones==0);entry.distant=Reduce(entry.source,.08f,Output+"/NpcRifle.asset");
        EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssetIfDirty(catalog);File.AppendAllText(Dir+"actor-detail-generation.txt","edit-mode rifle completed=true\n");
    }
    static Mesh Reduce(Mesh source,float quality,string path)
    {
        var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing)return existing;
        var engine=Activator.CreateInstance(engineType);var prop=engineType.GetProperty("SimplificationOptions");var options=prop.GetValue(engine);
        foreach(var n in new[]{"PreserveBorderEdges","PreserveUVSeamEdges","PreserveUVFoldoverEdges"})options.GetType().GetField(n).SetValue(options,source.bindposes.Length>0);prop.SetValue(engine,options);
        engineType.GetMethod("Initialize").Invoke(engine,new object[]{source});engineType.GetMethod("SimplifyMesh").Invoke(engine,new object[]{quality});var result=(Mesh)engineType.GetMethod("ToMesh").Invoke(engine,null);
        result.name=source.name;result.bounds=source.bounds;Validate(source,result);AssetDatabase.CreateAsset(result,path);AssetDatabase.SaveAssetIfDirty(result);
        File.AppendAllText(Dir+"actor-detail-generation.txt",source.name+" vertices="+source.vertexCount+" -> "+result.vertexCount+" blendshapes="+result.blendShapeCount+"\n");return result;
    }
}
