using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public static partial class Chapter3FiveFixProbe
{
    // Explicit local movement fixture. Does not represent a full natural walk.
    static IEnumerator MovementScenario()
    {
        yield return Press(Key.F8);
        yield return Wait(()=>flow.Region=="School"&&flow.school.Exploring,10,"F8 school ready");
        Physics.SyncTransforms();
        int closedTriangles=FenceTriangleCount(-4.5f,4.5f);
        int sideTriangles=FenceTriangleCount(7,10);
        Check("F8 centre fence has visible triangles",closedTriangles>0);
        Check("F8 side fence has visible triangles",sideTriangles>0);
        Check("F8 centre boundary fence blocks a walking capsule",
            !Chapter3PathMotor.Clear(new Vector3(0,0,-21),new Vector3(0,0,-25)));
        Check("F8 side boundary fence blocks a walking capsule",
            !Chapter3PathMotor.Clear(new Vector3(8,0,-21),new Vector3(8,0,-25)));
        yield return ProbeFenceMotor(false);
        yield return Shot("school-closed-fence");

        yield return Press(Key.F7);
        yield return Wait(()=>flow.Region=="Combat"&&flow.school.ApproachPrepared,12,"F7 approach preparation");
        Physics.SyncTransforms();
        Check("continuous approach removes the visible centre fence",FenceTriangleCount(-4.5f,4.5f)==0);
        Check("continuous approach retains the visible side fence",FenceTriangleCount(7,10)==sideTriangles);
        Check("continuous approach opens centre passage",
            Chapter3PathMotor.Clear(new Vector3(0,0,-21),new Vector3(0,0,-25)));
        Check("continuous approach retains solid side fence",
            !Chapter3PathMotor.Clear(new Vector3(8,0,-21),new Vector3(8,0,-25)));
        yield return ProbeFenceMotor(true);
        flow.player.Warp(new Vector3(0,.06f,-27),new Vector3(0,1,-18));
        yield return Shot("school-open-centre-solid-sides");
        yield return Press(Key.F8);
        yield return Wait(()=>flow.Region=="School"&&flow.school.Exploring,10,"F8 restores school");
        Physics.SyncTransforms();
        Check("F8 restores visible centre fence after approach",FenceTriangleCount(-4.5f,4.5f)==closedTriangles);
        Check("F8 restores centre fence after approach",
            !Chapter3PathMotor.Clear(new Vector3(0,0,-21),new Vector3(0,0,-25)));
    }

    static int FenceTriangleCount(float minimumX,float maximumX)
    {
        var bounds=new Bounds(new Vector3((minimumX+maximumX)*.5f,.85f,-23),
            new Vector3(maximumX-minimumX,1.3f,.30f));
        int count=0,meshes=0;
        foreach(var filter in flow.schoolRegion.GetComponentsInChildren<MeshFilter>())
        {
            if(!filter.name.StartsWith("Combined_")&&!filter.name.StartsWith("School geometry "))continue;
            var mesh=filter.sharedMesh;
            Check("school static mesh readable "+filter.name,mesh&&mesh.isReadable);
            var vertices=mesh.vertices;var triangles=mesh.triangles;meshes++;
            for(int i=0;i<triangles.Length;i+=3)
            {
                var a=filter.transform.TransformPoint(vertices[triangles[i]]);
                if(!bounds.Contains(a))continue;
                var b=filter.transform.TransformPoint(vertices[triangles[i+1]]);
                if(!bounds.Contains(b))continue;
                var c=filter.transform.TransformPoint(vertices[triangles[i+2]]);
                if(bounds.Contains(c))count++;
            }
        }
        Check("actual saved school geometry inspected",meshes>0);
        Note("Visible fence triangles x="+minimumX+".."+maximumX+" count="+count);
        return count;
    }

    static IEnumerator ProbeFenceMotor(bool open)
    {
        var school=flow.school;var actor=school.bawan;
        Vector3 original=actor.transform.position,oldFacing=actor.facing;
        bool oldEnabled=school.enabled;
        school.enabled=false;
        float x=open?0:8;
        Vector3 from=new Vector3(x,original.y,-20),target=new Vector3(x,0,-26);
        actor.transform.position=from;
        var motor=new Chapter3PathMotor();
        int movements=0,unsafeSteps=0;
        float deadline=Time.realtimeSinceStartup+(open?8:3);
        try
        {
            Physics.SyncTransforms();
            while(Time.realtimeSinceStartup<deadline&&Mathf.Abs(actor.transform.position.z-target.z)>.09f)
            {
                var previous=actor.transform.position;
                motor.Move(actor,target,3.35f);
                var current=actor.transform.position;
                if(Vector3.ProjectOnPlane(current-previous,Vector3.up).sqrMagnitude>.000001f)
                {
                    movements++;
                    if(!Chapter3PathMotor.Clear(previous,current))unsafeSteps++;
                }
                yield return null;
            }
            Check((open?"open":"closed")+" fence motor never crosses an occupied step",unsafeSteps==0);
            Check(open?"motor walks through the opened passage":"motor stays inside the visible side fence",
                open?Mathf.Abs(actor.transform.position.z-target.z)<.1f:actor.transform.position.z>-22.5f);
            Note("Fence motor fixture open="+open+" movements="+movements+" final="+actor.transform.position);
        }
        finally
        {
            actor.transform.position=original;actor.Face(original+oldFacing);
            school.enabled=oldEnabled;Physics.SyncTransforms();
        }
    }

    // Call immediately after the normal readiness dialogue accepts departure.
    // Observe actual station escort; the driver only follows the leader.
    static IEnumerator ObserveStationDeparture()
    {
        var station=flow.station;
        yield return Wait(()=>station.CurrentStage==Chapter3StationController.Stage.Departure&&
            flow.player.canMove,25,"station escort starts");
        var rear=station.pihu;var lead=station.bawan;
        Check("right formation companion is Pihu",rear.characterName.Contains("比胡"));
        yield return Frames(3);
        int movingSamples=0,reverseFrames=0,walkStateChanges=0;
        bool previousWalk=rear.Rig.walking;
        Vector3 previous=rear.transform.position,previousHeading=rear.Rig.Forward;
        float deadline=Time.realtimeSinceStartup+35;
        while(flow.Region=="Station"&&station.CurrentStage==Chapter3StationController.Stage.Departure&&
            Time.realtimeSinceStartup<deadline)
        {
            if(Vector3.ProjectOnPlane(lead.transform.position-flow.player.transform.position,Vector3.up).magnitude>3)
                Move(lead.transform.position);
            yield return new WaitForEndOfFrame();
            Vector3 current=rear.transform.position;
            Vector3 delta=Vector3.ProjectOnPlane(current-previous,Vector3.up);
            if(!station.CompanionsWaiting&&delta.sqrMagnitude>.000025f)
            {
                movingSamples++;
                if(Vector3.Angle(previousHeading,rear.Rig.Forward)>90)reverseFrames++;
                if(previousWalk!=rear.Rig.walking)walkStateChanges++;
            }
            previous=current;previousHeading=rear.Rig.Forward;previousWalk=rear.Rig.walking;
            if(movingSamples==25)yield return Shot("station-pihu-walking");
        }
        Check("station rear companion has sustained walking samples",movingSamples>=20);
        Check("station rear companion does not flip heading while walking",reverseFrames==0);
        Check("station both companion rigs blend walk and idle",lead.Rig.smoothLocomotion&&rear.Rig.smoothLocomotion);
        Note("Station rear samples="+movingSamples+" headingReversals="+reverseFrames+
            " walkingStateChanges="+walkStateChanges+" remainingRegion="+flow.Region);
    }
}
