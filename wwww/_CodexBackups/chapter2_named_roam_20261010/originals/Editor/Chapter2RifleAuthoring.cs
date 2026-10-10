using UnityEditor;
using UnityEngine;

public static class Chapter2RifleAuthoring
{
    public static void Apply(Chapter2Actor actor)
    {
        var motion=actor.GetComponent<Chapter2Rifle>();
        if(motion&&motion.weapon&&motion.modelVersion>=2)return;
        var old=motion&&motion.weapon?motion.weapon.Find("督工步槍"):actor.transform.Find("督工步槍");
        if(!old)throw new System.InvalidOperationException("Chapter 2 officer rifle is missing.");
        // Keep the supplied rifle and its converted materials. Normalize its longest axis.
        old.SetParent(null,true);old.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
        if(motion&&motion.weapon)Object.DestroyImmediate(motion.weapon.gameObject);
        Bounds b=BoundsOf(old);Vector3 axis=b.size.x>b.size.y&&b.size.x>b.size.z?Vector3.right:(b.size.y>b.size.z?Vector3.up:Vector3.forward);
        old.rotation=Quaternion.AngleAxis(180,Vector3.up)*Quaternion.FromToRotation(axis,Vector3.forward);
        b=BoundsOf(old);old.localScale*=1.2f/b.size.z;b=BoundsOf(old);
        old.position-=new Vector3(b.center.x,b.center.y,b.min.z);
        var root=new GameObject("Chapter2 shoulder rifle").transform;
        old.SetParent(root,true);root.SetParent(actor.transform,true);
        if(!motion)motion=actor.gameObject.AddComponent<Chapter2Rifle>();motion.weapon=root;motion.modelVersion=2;
        var muzzle=new GameObject("Rifle muzzle").transform;muzzle.SetParent(root,false);muzzle.localPosition=new Vector3(0,0,1.2f);motion.muzzle=muzzle;
        var flash=GameObject.CreatePrimitive(PrimitiveType.Sphere);flash.name="Brief muzzle flash";Object.DestroyImmediate(flash.GetComponent<Collider>());
        flash.transform.SetParent(muzzle,false);flash.transform.localPosition=new Vector3(0,0,.07f);flash.transform.localScale=new Vector3(.10f,.10f,.23f);
        string path="Assets/Chapter2/Materials/Rifle flash.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(material,path);}
        material.color=new Color(1,.78f,.32f);flash.GetComponent<Renderer>().sharedMaterial=material;
        flash.SetActive(false);motion.muzzleFlash=flash;
        root.SetPositionAndRotation(actor.transform.position+new Vector3(.2f,.8f,0),Quaternion.LookRotation(new Vector3(0,.8f,.4f)));
    }
    static Bounds BoundsOf(Transform root)
    {var rs=root.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
}
