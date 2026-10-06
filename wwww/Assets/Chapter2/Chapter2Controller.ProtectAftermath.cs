using System.Collections;
using UnityEngine;

public sealed partial class Chapter2Controller
{
    IEnumerator OrderSurvivorsToTrees(Chapter2Rifle rifle)
    {
        var threat=new GameObject("Survivors rifle aim").transform;
        threat.SetParent(dayGroup.transform);
        rifle.aimTarget=threat;
        threat.position=SurvivorChest(workers[1]);
        CameraBeat="threat-raising";
        ui.Line("","");
        for(float t=0;t<1;t+=Time.deltaTime/.75f)
        {
            rifle.aim=Mathf.SmoothStep(0,1,t);
            officer.Face(threat.position);yield return null;
        }
        rifle.aim=1;
        CameraBeat="forced-order";
        ui.Line("日本警察","給我去砍樹");
        // Keep the officer looking at the survivors, including while they are crouching.
        for(float t=0;t<3.2f;t+=Time.deltaTime)
        {
            float sweep=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.9f,2.3f,t));
            threat.position=Vector3.Lerp(SurvivorChest(workers[1]),SurvivorChest(workers[2]),sweep);
            officer.Face(threat.position);yield return null;
        }
        ui.Line("","");CameraBeat="survivors-standing";
        for(int i=1;i<workers.Length;i++)workers[i].GetComponent<Chapter2GriefReaction>().StandUp((i-1)*.22f);
        foreach(var worker in workers)
        {
            var reaction=worker.GetComponent<Chapter2GriefReaction>();
            if(!reaction)continue;
            while(!reaction.Standing)
            {
                threat.position=SurvivorChest(workers[2]);officer.Face(threat.position);
                yield return null;
            }
        }
        var starts=new Vector3[workers.Length];var destinations=new Vector3[workers.Length];
        var facings=new Vector3[workers.Length];float walkDuration=0;
        for(int i=1;i<workers.Length;i++)
        {
            starts[i]=workers[i].transform.position;facings[i]=workers[i].Rig.Forward;
            destinations[i]=sacredTree.position+new Vector3(i==1?2.6f:-2.7f,0,-1.65f);
            destinations[i].y=starts[i].y;
            walkDuration=Mathf.Max(walkDuration,Mathf.Max(2.6f,Vector3.Distance(starts[i],destinations[i])/.85f)+(i-1)*.25f);
        }
        for(float t=0;t<1;t+=Time.deltaTime/.65f)
        {
            for(int i=1;i<workers.Length;i++)workers[i].Face(starts[i]+Vector3.Slerp(facings[i],(destinations[i]-starts[i]).normalized,Mathf.SmoothStep(0,1,t)));
            yield return null;
        }
        CameraBeat="survivors-walking";
        for(float t=0;t<walkDuration;t+=Time.deltaTime)
        {
            for(int i=1;i<workers.Length;i++)
            {
                float distance=Vector3.Distance(starts[i],destinations[i]);
                float duration=Mathf.Max(2.6f,distance/.85f);
                float k=Mathf.SmoothStep(0,1,Mathf.Clamp01((t-(i-1)*.25f)/duration));
                // The arc stays in front of the trunk and behind the fallen companion.
                Vector3 middle=Vector3.Lerp(starts[i],destinations[i],.5f);
                middle.z=Mathf.Min(middle.z,sacredTree.position.z-2.8f);
                workers[i].transform.position=(1-k)*(1-k)*starts[i]+2*(1-k)*k*middle+k*k*destinations[i];
                if(k>=1)workers[i].Face(sacredTree.position);
            }
            threat.position=(SurvivorChest(workers[1])+SurvivorChest(workers[2]))*.5f;
            officer.Face(threat.position);yield return null;
        }
        for(int i=1;i<workers.Length;i++){workers[i].transform.position=destinations[i];workers[i].Face(sacredTree.position);}
        CameraBeat="survivors-at-trees";
        yield return new WaitForSeconds(1.2f);
        // The main flow fades only after both survivors have returned to the trees.
    }

    static Vector3 SurvivorChest(Chapter2Actor actor)
    {
        return Vector3.Lerp(actor.Rig.Hips.position,actor.Rig.Head.position,.6f);
    }
}
