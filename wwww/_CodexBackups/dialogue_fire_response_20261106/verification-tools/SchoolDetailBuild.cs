using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

public static class SchoolDetailBuild
{
    const string Dir="_CodexBackups/dialogue_fire_response_20261106/",Output="Assets/Chapter3/Resources/Chapter3/SchoolNativeDetail";
    static Queue<Chapter3CrowdDetailCatalog.Entry> jobs;
    static Chapter3CrowdDetailCatalog catalog;
    static List<Chapter3CrowdDetailCatalog.Entry> entries;
    static Type engineType;
    static readonly List<string> notes=new List<string>();
    public static void Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
        var flow=Object.FindFirstObjectByType<Chapter3Flow>();
        var sources=new[]{flow.school.bawan,flow.school.pihu,flow.school.tado}.SelectMany(a=>a.GetComponentsInChildren<SkinnedMeshRenderer>(true)).Select(s=>s.sharedMesh).Where(m=>m&&m.vertexCount>60000).Distinct().ToArray();
        var actors=Resources.Load<Chapter3CrowdDetailCatalog>("Chapter3/ActorDetailCatalog");
        jobs=new Queue<Chapter3CrowdDetailCatalog.Entry>(actors.entries.Where(e=>sources.Contains(e.source)));
        if(jobs.Count!=3)throw new Exception("Expected three existing native body donors, found "+jobs.Count);
        catalog=Resources.Load<Chapter3CrowdDetailCatalog>("Chapter3/CrowdDetailCatalog");entries=catalog.entries.ToList();
        string core="_CodexBackups/chapter3_hands_fire_20261019/verification-tools/UnityMeshSimplifier/";
        if(!File.ReadAllText(core+"SOURCE.txt").Contains("53fdb3122645bcd3ad2c258235200dff3dfbaa9b"))throw new Exception("Pinned source mismatch");
        engineType=Assembly.Load(File.ReadAllBytes(core+"Chapter3OfflineMeshSimplifier.dll")).GetType("UnityMeshSimplifier.MeshSimplifier",true);
        Directory.CreateDirectory(Output);AssetDatabase.Refresh();EditorApplication.update+=Step;
    }
    static void Step()
    {
        try
        {
            if(jobs.Count==0)
            {
                catalog.entries=entries.ToArray();catalog.enterDistance=5;catalog.leaveDistance=3.5f;
                EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssetIfDirty(catalog);EditorApplication.update-=Step;
                notes.Add("completed=true");File.WriteAllLines(Dir+"school-detail-generation.txt",notes);return;
            }
            var entry=jobs.Dequeue();var source=entry.source;var input=entry.distant;
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source,out string guid,out long id);
            string path=Output+"/"+guid+".asset";var result=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(!result)
            {
                var engine=Activator.CreateInstance(engineType);var prop=engineType.GetProperty("SimplificationOptions");var options=prop.GetValue(engine);
                foreach(var n in new[]{"PreserveBorderEdges","PreserveUVSeamEdges","PreserveUVFoldoverEdges"})options.GetType().GetField(n).SetValue(options,true);prop.SetValue(engine,options);
                engineType.GetMethod("Initialize").Invoke(engine,new object[]{input});engineType.GetMethod("SimplifyMesh").Invoke(engine,new object[]{.22f});
                result=(Mesh)engineType.GetMethod("ToMesh").Invoke(engine,null);result.name=source.name+" school distant";result.bounds=source.bounds;
                Validate(source,result);AssetDatabase.CreateAsset(result,path);AssetDatabase.SaveAssetIfDirty(result);
            }
            Validate(source,result);
            entries.RemoveAll(e=>e.source==source&&e.sourceBones==source.bindposes.Length);
            entries.Add(new Chapter3CrowdDetailCatalog.Entry{source=source,distant=result,sourceVertices=source.vertexCount,sourceBones=source.bindposes.Length});
            notes.Add(source.name+" source="+guid+" vertices="+source.vertexCount+" -> "+result.vertexCount+" bones="+result.bindposes.Length+" submeshes="+result.subMeshCount+" blendshapes="+result.blendShapeCount);
            File.WriteAllLines(Dir+"school-detail-generation.txt",notes);
        }
        catch(Exception e){EditorApplication.update-=Step;notes.Add(e.ToString());File.WriteAllLines(Dir+"school-detail-generation.txt",notes);Debug.LogException(e);}
    }
    static void Validate(Mesh source,Mesh result)
    {
        if(!result||result.vertexCount<3||result.vertexCount>=source.vertexCount||source.subMeshCount!=result.subMeshCount||!source.bindposes.SequenceEqual(result.bindposes)||source.blendShapeCount!=result.blendShapeCount)throw new Exception("Invalid mesh layout");
        if(result.uv.Length!=result.vertexCount||result.boneWeights.Length!=result.vertexCount)throw new Exception("Missing UV or weights");
        for(int i=0;i<source.blendShapeCount;i++)if(source.GetBlendShapeName(i)!=result.GetBlendShapeName(i))throw new Exception("Blendshape order changed");
        foreach(var w in result.boneWeights)if(Mathf.Abs(w.weight0+w.weight1+w.weight2+w.weight3-1)>.015f||w.boneIndex0>=result.bindposes.Length||w.boneIndex1>=result.bindposes.Length||w.boneIndex2>=result.bindposes.Length||w.boneIndex3>=result.bindposes.Length)throw new Exception("Invalid bone weights");
    }
}
