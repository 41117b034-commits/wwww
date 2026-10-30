using System.Collections.Generic;
using UnityEngine;

// Test the union of the visible road slabs, so bends have no artificial seams.
public sealed class Chapter3StationRoadBoundary
{
    readonly List<BoxCollider> roads=new List<BoxCollider>();
    readonly Transform region;
    public Chapter3StationRoadBoundary(Transform region)
    {
        this.region=region;
        foreach(var box in region.GetComponentsInChildren<BoxCollider>(true))
            if(box.name.StartsWith("前往霧社的土路 ")&&box.gameObject.activeSelf)roads.Add(box);
        if(roads.Count==0)Debug.LogError("[Chapter3] Station road surface is missing.");
    }
    public bool Contains(Vector3 point,float radius,bool allowRoad)
    {
        if(!OnSurface(point,allowRoad))return false;
        // A slightly conservative ring keeps the entire player capsule on road.
        for(int i=0;i<32;i++)
        {
            float angle=i*Mathf.PI/16;
            if(!OnSurface(point+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius,allowRoad))return false;
        }
        return true;
    }
    bool OnSurface(Vector3 point,bool allowRoad)
    {
        var courtyard=region.InverseTransformPoint(point);
        if(courtyard.x>=-10&&courtyard.x<=10&&courtyard.z>=-24&&courtyard.z<=2.7f)return true;
        if(!allowRoad)return false;
        foreach(var box in roads)
        {
            Vector3 local=box.transform.InverseTransformPoint(point)-box.center;
            if(Mathf.Abs(local.x)<=box.size.x*.5f&&Mathf.Abs(local.z)<=box.size.z*.5f)return true;
        }
        return false;
    }
}
