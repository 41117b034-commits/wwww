using System;
using System.Collections;
using UnityEngine;

public sealed class Chapter2Axe : MonoBehaviour
{
    public Transform bladeTip;
    public Material cutWood;
    public bool IsSwinging { get; private set; }
    public int Impacts { get; private set; }
    public float LastContactError { get; private set; }
    Transform scar;
    ParticleSystem chips;
    Vector3 homePosition;
    Quaternion homeRotation;
    void Awake(){homePosition=transform.localPosition;homeRotation=transform.localRotation;}
    public IEnumerator Swing(RaycastHit contact,Action impact)
    {
        IsSwinging=true;
        Vector3 start=transform.position;
        Quaternion startRotation=transform.rotation;
        Vector3 raised=start+transform.parent.up*.25f+transform.parent.right*.12f-transform.parent.forward*.13f;
        Quaternion raisedRotation=startRotation*Quaternion.Euler(-28,0,-28);
        // The forged edge points along local -X. Its tip lands on the real trunk mesh.
        Quaternion hitRotation=Quaternion.LookRotation(Vector3.Cross(Vector3.up,-contact.normal),Vector3.up);
        Vector3 localTip=transform.InverseTransformPoint(bladeTip.position);
        Vector3 hitPosition=contact.point-hitRotation*Vector3.Scale(localTip,transform.lossyScale);
        for(float t=0;t<1;t+=Time.deltaTime/.22f){transform.SetPositionAndRotation(Vector3.Lerp(start,raised,t),Quaternion.Slerp(startRotation,raisedRotation,t));yield return null;}
        for(float t=0;t<1;t+=Time.deltaTime/.20f){float u=t*t;transform.SetPositionAndRotation(Vector3.Lerp(raised,hitPosition,u),Quaternion.Slerp(raisedRotation,hitRotation,u));yield return null;}
        transform.SetPositionAndRotation(hitPosition,hitRotation);LastContactError=Vector3.Distance(bladeTip.position,contact.point);Impacts++;
        ShowImpact(contact);impact?.Invoke();
        yield return new WaitForSeconds(.1f);
        for(float t=0;t<1;t+=Time.deltaTime/.28f)
        {
            transform.position=Vector3.Lerp(hitPosition,transform.parent.TransformPoint(homePosition),Mathf.SmoothStep(0,1,t));
            transform.rotation=Quaternion.Slerp(hitRotation,transform.parent.rotation*homeRotation,t);yield return null;
        }
        transform.localPosition=homePosition;transform.localRotation=homeRotation;IsSwinging=false;
    }
    void ShowImpact(RaycastHit hit)
    {
        if(!scar)
        {
            scar=GameObject.CreatePrimitive(PrimitiveType.Cube).transform;scar.name="Fresh axe notch";Destroy(scar.GetComponent<Collider>());
            scar.GetComponent<Renderer>().sharedMaterial=cutWood;
            var debris=new GameObject("Flying wood chips");chips=debris.AddComponent<ParticleSystem>();
            var main=chips.main;main.playOnAwake=false;main.loop=false;main.startLifetime=.5f;main.startSpeed=1.8f;main.startSize=.035f;main.gravityModifier=1;main.maxParticles=80;main.simulationSpace=ParticleSystemSimulationSpace.World;
            var emission=chips.emission;emission.enabled=false;var shape=chips.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=40;shape.radius=.07f;
            var renderer=chips.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.Mesh;renderer.mesh=scar.GetComponent<MeshFilter>().sharedMesh;renderer.sharedMaterial=cutWood;
        }
        scar.SetParent(hit.collider.transform,true);scar.position=hit.point+hit.normal*.012f;
        scar.rotation=Quaternion.LookRotation(hit.normal,Vector3.up)*Quaternion.Euler(0,0,-12);
        scar.localScale=new Vector3(.12f+Mathf.Min(Impacts,10)*.03f,.035f+Mathf.Min(Impacts,10)*.006f,.012f);
        chips.transform.SetPositionAndRotation(hit.point+hit.normal*.04f,Quaternion.LookRotation(hit.normal+Vector3.up*.5f));chips.Emit(14);
    }
    void OnDestroy(){if(scar)Destroy(scar.gameObject);if(chips)Destroy(chips.gameObject);}
}
