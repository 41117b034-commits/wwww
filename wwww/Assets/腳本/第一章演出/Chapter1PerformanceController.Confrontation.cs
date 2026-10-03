using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Chapter1PerformanceController
{
    [Header("Wedding Confrontation")]
    [Min(1f)] public float leaderApproachSeconds = 4.4f;
    [Min(1f)] public float leaderQuestionSeconds = 2.8f;
    [Min(0.1f)] public float weddingSpeakerCameraSeconds = 0.8f;
    private Vector3 weddingDramaCenter;
    private float weddingDramaHeight;
    private string weddingDramaBeat;
    [Tooltip("放在火堆旁、由警察踢倒的酒甕與竹杯。場景預先擺放，劇情不會搬移道具。")]
    public Transform[] incidentGroundWineProps;
    private readonly Dictionary<Transform, Vector3[]> wineSupportVertices = new Dictionary<Transform, Vector3[]>();

    private void SetWeddingDramaBeat(string beat)
    {
        weddingDramaBeat = beat;
        Debug.Log("[Wedding Confrontation] " + beat);
    }

    private Vector3 WeddingActorFloor(Transform actor)
    {
        Vector3 floor = actor.position;
        if (TryGetIncidentSurfaceY(floor, out float y)) floor.y = y;
        return floor;
    }

    private IEnumerator WeddingCameraTo(Vector3 position, Vector3 focus, float fov, float seconds)
    {
        if (incidentCameraView == null) yield break;
        RestoreIncidentShotOccluders();
        if (incidentCameraPoseLock == null)
            incidentCameraPoseLock = BeginCinematicCameraPoseLock(incidentCameraView);
        Vector3 start = incidentCameraView.position;
        Quaternion rotation = incidentCameraView.rotation;
        Quaternion destination = Quaternion.LookRotation(focus - position, Vector3.up);
        Camera camera = incidentCameraView.GetComponent<Camera>();
        float initialFov = camera != null ? camera.fieldOfView : fov;
        float duration = Mathf.Max(0.05f, seconds);
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            SetCinematicCameraPose(incidentCameraView, incidentCameraPoseLock,
                Vector3.Lerp(start, position, t), Quaternion.Slerp(rotation, destination, t));
            if (camera != null) camera.fieldOfView = Mathf.Lerp(initialFov, fov, t);
            yield return null;
        }
        SetCinematicCameraPose(incidentCameraView, incidentCameraPoseLock, position, destination);
        if (camera != null) camera.fieldOfView = fov;
    }

    private IEnumerator WeddingSpeakerShot(Transform speaker, bool policeSpeaker)
    {
        if (speaker == null) yield break;
        float height = Mathf.Max(1.7f, weddingDramaHeight);
        Vector3 floor = WeddingActorFloor(speaker);
        // Both angles remain on the near side of the conversation axis. The
        // reverse angle puts the leader in the centre and the villagers behind.
        Vector3 offset = policeSpeaker
            ? new Vector3(1.62f, 0.91f, -0.46f)
            : new Vector3(-0.68f, 0.87f, -1.64f);
        yield return WeddingCameraTo(floor + offset * height,
            floor + Vector3.up * height * 0.72f, 46f, weddingSpeakerCameraSeconds);
    }

    private IEnumerator WeddingActionShot()
    {
        // Match the approved reference: police left, leader centre, the same
        // fire and buildings behind, with all bystanders in their original slots.
        float height = Mathf.Max(1.7f, weddingDramaHeight);
        yield return WeddingCameraTo(
            weddingDramaCenter + new Vector3(-0.55f, 1.1f, -4.15f) * height,
            weddingDramaCenter + new Vector3(-0.28f, 0.50f, 0.05f) * height,
            50f, 0.9f);
    }

    private IEnumerator WeddingCupAndConfrontation()
    {
        yield return WeddingKickGroundWine();
        yield return WeddingLeaderConfrontation();
    }

    private IEnumerator WeddingKickGroundWine()
    {
        if(primaryPoliceActor==null||incidentGroundWineProps==null||incidentGroundWineProps.Length==0)
        {
            Debug.LogError("[Wedding Confrontation] Ground wine props are missing.");
            yield break;
        }
        Chapter1IncidentRig rig=PrepareDoorwayRig(primaryPoliceActor,true);
        rig.ClearInteractionPose();
        rig.batonVisible=true;
        rig.batonInLeftHand=false;
        rig.smoothLocomotion=true;
        float height=rig.Height;
        var props=new List<Transform>();
        foreach(var prop in incidentGroundWineProps)if(prop!=null&&prop.gameObject.activeInHierarchy)props.Add(prop);
        if(props.Count==0)yield break;
        // Aim along the outside of the pit, with a slight outward component.
        // Kicking toward the wedding centre used to roll the jars into the fire.
        Vector3 wineCenter=Vector3.zero;
        foreach(var prop in props){SetWineOnGround(prop);wineCenter+=prop.position;}
        wineCenter/=props.Count;
        Vector3 away=Vector3.ProjectOnPlane(wineCenter-GetFireCenterPosition(),Vector3.up).normalized;
        Vector3 tangent=Vector3.Cross(Vector3.up,away);
        if(Vector3.Dot(tangent,wineCenter-primaryPoliceActor.position)<0f)tangent=-tangent;
        Vector3 approach=(tangent+away*0.35f).normalized;
        Vector3 side=Vector3.Cross(Vector3.up,approach);
        // Strike the large jar once. All four nearby props react to the same
        // impact frame, rather than making the officer kick each cup in turn.
        Transform impactProp=null;
        Bounds impactBounds=default;
        float largest=0f;
        foreach(var prop in props)
        {
            if(!TryGetVisibleBounds(prop,out Bounds candidate))continue;
            float volume=candidate.size.x*candidate.size.y*candidate.size.z;
            if(volume<=largest)continue;
            largest=volume;impactProp=prop;impactBounds=candidate;
        }
        if(impactProp==null)yield break;
        SetWeddingDramaBeat("police-wine-approach");
        ShowLine("旁白","警察踢倒擺在地上的酒。",9f);
        Vector3 focus=wineCenter-approach*height*0.30f;
        if(TryGetIncidentSurfaceY(focus,out float floor))focus.y=floor;
        // The full figure and the grounded props stay in the same frame, so
        // the approach and the boot making contact are both visible.
        yield return WeddingCameraTo(focus-approach*height*0.75f+side*height*2.05f+Vector3.up*height*0.85f,
            focus+Vector3.up*height*0.46f,50f,0.65f);
        var falls=new List<Coroutine>();
        float radius=Mathf.Abs(approach.x)*impactBounds.extents.x+Mathf.Abs(approach.z)*impactBounds.extents.z;
        Vector3 contact=impactBounds.center-approach*radius;
        contact.y=impactBounds.min.y+height*0.12f;
        // A diagonal ray reaches the curved jar itself; the bounding box can
        // put the boot visibly outside the surface even at zero toe error.
        Physics.SyncTransforms();
        Vector3 rayOrigin=impactBounds.center-approach*(radius+height);
        rayOrigin.y=contact.y;
        Ray contactRay=new Ray(rayOrigin,approach);
        float nearest=height+radius*2f;
        foreach(var collider in impactProp.GetComponentsInChildren<Collider>())
        {
            if(collider.enabled && collider.Raycast(contactRay,out RaycastHit hitInfo,nearest))
            {
                nearest=hitInfo.distance;
                contact=hitInfo.point;
            }
        }
        // Leave room for the shin to extend ahead of the knee at contact.
        Vector3 destination=contact-approach*height*0.48f-side*height*0.075f;
        destination.y=primaryPoliceActor.position.y;
        Vector3 start=primaryPoliceActor.position;
        Quaternion from=primaryPoliceActor.rotation;
        rig.Face(destination-start);
        Quaternion facing=primaryPoliceActor.rotation;
        primaryPoliceActor.rotation=from;
        for(float elapsed=0f;elapsed<0.35f;elapsed+=Time.deltaTime)
        {
            primaryPoliceActor.rotation=Quaternion.Slerp(from,facing,Mathf.SmoothStep(0,1,elapsed/0.35f));
            yield return null;
        }
        float duration=Mathf.Max(0.5f,Vector3.ProjectOnPlane(destination-start,Vector3.up).magnitude/(height*0.32f));
        rig.walking=true;
        for(float elapsed=0f;elapsed<duration;elapsed+=Time.deltaTime)
        {
            PlaceDoorwayActor(rig,Vector3.Lerp(start,destination,WatchWalkProgress(Mathf.Clamp01(elapsed/duration))));
            yield return null;
        }
        PlaceDoorwayActor(rig,destination);
        rig.walking=false;
        from=primaryPoliceActor.rotation;
        rig.Face(approach);
        facing=primaryPoliceActor.rotation;
        primaryPoliceActor.rotation=from;
        for(float elapsed=0f;elapsed<0.30f;elapsed+=Time.deltaTime)
        {
            primaryPoliceActor.rotation=Quaternion.Slerp(from,facing,Mathf.SmoothStep(0,1,elapsed/0.30f));
            yield return null;
        }
        primaryPoliceActor.rotation=facing;
        yield return new WaitForSeconds(0.18f);
        SetWeddingDramaBeat("police-wine-kick");
        rig.kickContact=contact;
        rig.kicking=true;
        bool hit=false;
        const float kickSeconds=1.05f;
        for(float elapsed=0f;elapsed<kickSeconds;elapsed+=Time.deltaTime)
        {
            float progress=Mathf.Clamp01(elapsed/kickSeconds);
            rig.kickProgress=!hit&&progress>=0.46f?0.52f:progress;
            // Let this frame's IK reach the impact pose before reading the
            // toe; preserve that pose even if a slow frame skips its time.
            yield return new WaitForEndOfFrame();
            if(!hit&&rig.kickProgress>=0.46f&&Vector3.Distance(rig.KickToePosition,contact)<height*0.012f)
            {
                hit=true;
                PlayPoliceEventClip(cupCrashClip);
                foreach(var prop in props)
                {
                    float spread=Vector3.Dot(prop.position-impactBounds.center,side)/height;
                    Vector3 fallDirection=(approach+side*Mathf.Clamp(spread*0.4f,-0.16f,0.16f)).normalized;
                    falls.Add(StartCoroutine(ToppleGroundWine(prop,fallDirection,height)));
                }
                Debug.Log("[Wedding Wine Kick] Single impact: "+props.Count+" props; toe distance="+Vector3.Distance(rig.KickToePosition,contact).ToString("F3"));
                SetWeddingDramaBeat("police-wine-contact");
            }
        }
        rig.kicking=false;
        rig.kickProgress=0f;
        if(!hit)Debug.LogError("[Wedding Wine Kick] Boot did not reach "+impactProp.name);
        yield return new WaitForSeconds(0.25f);
        foreach(var fall in falls)yield return fall;
        SetWeddingDramaBeat("police-wine-toppled");
        yield return new WaitForSeconds(0.7f);
    }

    private IEnumerator ToppleGroundWine(Transform prop,Vector3 direction,float height)
    {
        if(!TryGetVisibleBounds(prop,out Bounds bounds))yield break;
        // Kinematic choreography also supports the original concave jar
        // colliders, without replacing or duplicating the imported meshes.
        Rigidbody body=prop.GetComponent<Rigidbody>();
        if(body!=null){body.isKinematic=true;body.useGravity=false;}
        Vector3 start=prop.position;
        Quaternion rotation=prop.rotation;
        float radius=Mathf.Abs(direction.x)*bounds.extents.x+Mathf.Abs(direction.z)*bounds.extents.z;
        Vector3 pivot=bounds.center+direction*radius;
        pivot.y=bounds.min.y;
        Vector3 axis=Vector3.Cross(Vector3.up,direction);
        const float seconds=0.62f;
        for(float elapsed=0f;elapsed<seconds;elapsed+=Time.deltaTime)
        {
            float t=Mathf.Clamp01(elapsed/seconds);
            float angle=t<0.8f?Mathf.Lerp(0f,96f,Mathf.Pow(t/0.8f,0.75f))
                :Mathf.Lerp(96f,90f,Mathf.SmoothStep(0,1,(t-0.8f)/0.2f));
            Quaternion turn=Quaternion.AngleAxis(angle,axis);
            prop.SetPositionAndRotation(pivot+turn*(start-pivot)+direction*height*0.20f*t,turn*rotation);
            SetWineOnGround(prop);
            yield return null;
        }
        Quaternion final=Quaternion.AngleAxis(90f,axis);
        prop.SetPositionAndRotation(pivot+final*(start-pivot)+direction*height*0.20f,final*rotation);
        SetWineOnGround(prop);
    }

    private void SetWineOnGround(Transform prop)
    {
        if(!wineSupportVertices.TryGetValue(prop,out Vector3[] points))
        {
            var support=new List<Vector3>();
            foreach(var filter in prop.GetComponentsInChildren<MeshFilter>())
            {
                var mesh=filter.sharedMesh;
                if(mesh==null||!mesh.isReadable)continue;
                foreach(var vertex in BuildWineSupportEnvelope(mesh))
                    support.Add(prop.InverseTransformPoint(filter.transform.TransformPoint(vertex)));
            }
            points=support.ToArray();
            wineSupportVertices[prop]=points;
        }
        // Use the actual surface against the terrain. A rotated cylinder's
        // bounding box has empty corners below its skin, making it hover.
        Terrain groundTerrain=null;
        foreach(var terrain in Terrain.activeTerrains)
        {
            if(terrain.terrainData==null)continue;
            Vector3 local=prop.position-terrain.transform.position, size=terrain.terrainData.size;
            if(local.x>=0f&&local.z>=0f&&local.x<=size.x&&local.z<=size.z){groundTerrain=terrain;break;}
        }
        if(points.Length>0&&groundTerrain!=null)
        {
            float clearance=float.PositiveInfinity;
            foreach(var local in points)
            {
                Vector3 world=prop.TransformPoint(local);
                float ground=groundTerrain.SampleHeight(world)+groundTerrain.transform.position.y;
                clearance=Mathf.Min(clearance,world.y-ground);
            }
            prop.position+=Vector3.up*(0.02f-clearance);
        }
        else if(TryGetVisibleBounds(prop,out Bounds bounds)&&TryGetIncidentSurfaceY(bounds.center,out float ground))
            prop.position+=Vector3.up*(ground+0.02f-bounds.min.y);
    }

    // The imported jars contain over a million vertices each. Keep the outer
    // points of a small grid along all three axes, once per prop, so each frame
    // grounds the curved surface without millions of terrain queries.
    private static IEnumerable<Vector3> BuildWineSupportEnvelope(Mesh mesh)
    {
        const int grid = 20;
        Vector3[] vertices = mesh.vertices;
        if (vertices.Length <= grid * grid * 6) return vertices;
        int[] minima = new int[grid * grid * 3];
        int[] maxima = new int[minima.Length];
        for (int i = 0; i < minima.Length; i++) minima[i] = maxima[i] = -1;
        Bounds bounds = mesh.bounds;
        Vector3 min = bounds.min, size = bounds.size;
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 v = vertices[i];
            int x = Mathf.Clamp((int)((v.x - min.x) / Mathf.Max(size.x, 0.0001f) * grid), 0, grid - 1);
            int y = Mathf.Clamp((int)((v.y - min.y) / Mathf.Max(size.y, 0.0001f) * grid), 0, grid - 1);
            int z = Mathf.Clamp((int)((v.z - min.z) / Mathf.Max(size.z, 0.0001f) * grid), 0, grid - 1);
            for (int axis = 0; axis < 3; axis++)
            {
                int cell = axis * grid * grid + (axis == 0 ? y * grid + z : axis == 1 ? x * grid + z : x * grid + y);
                if (minima[cell] < 0 || v[axis] < vertices[minima[cell]][axis]) minima[cell] = i;
                if (maxima[cell] < 0 || v[axis] > vertices[maxima[cell]][axis]) maxima[cell] = i;
            }
        }
        var indices = new HashSet<int>();
        foreach (int i in minima) if (i >= 0) indices.Add(i);
        foreach (int i in maxima) if (i >= 0) indices.Add(i);
        var support = new List<Vector3>(indices.Count);
        foreach (int i in indices) support.Add(vertices[i]);
        return support;
    }

    private IEnumerator WeddingLeaderConfrontation()
    {
        Transform leader = groomActor != null ? groomActor : shovedVillagerActor;
        Transform officer = primaryPoliceActor;
        if (leader == null || officer == null)
        {
            Debug.LogError("[Wedding Confrontation] Leader or police actor is missing.");
            yield break;
        }
        shovedVillagerActor = leader;
        Chapter1IncidentRig leaderRig = PrepareDoorwayRig(leader, false);
        Chapter1IncidentRig officerRig = PrepareDoorwayRig(officer, true);
        float height = Mathf.Max(leaderRig.Height, officerRig.Height);
        leaderRig.smoothLocomotion = true;
        leaderRig.strideScale = 0.82f;
        leaderRig.frozen = false;
        leaderRig.speakingWeight = 0f;
        officerRig.pointTarget = null;
        officerRig.batonInLeftHand = true; // Right hand is free for the shove.
        yield return WeddingActionShot();

        Vector3 start = leader.position;
        Vector3 destination = officer.position + new Vector3(0.64f, 0f, 0.045f) * height;
        destination.y = start.y;
        // Curve around the near edge of the roasting pit, never through it.
        Vector3 control1 = start + Vector3.back * height * 0.80f;
        Vector3 control2 = destination + Vector3.right * height * 0.60f;
        Quaternion startRotation = leader.rotation;
        leaderRig.Face(control1 - start);
        Quaternion walkingRotation = leader.rotation;
        leader.rotation = startRotation;
        ShowLine("旁白", "一名族人走上前，向警察質問。", leaderApproachSeconds + 0.7f);
        SetWeddingDramaBeat("leader-approach");
        for (float t = 0f; t < 0.4f; t += Time.deltaTime)
        {
            leader.rotation = Quaternion.Slerp(startRotation, walkingRotation, Mathf.SmoothStep(0, 1, t / 0.4f));
            yield return null;
        }
        leaderRig.walking = true;
        float duration = Mathf.Max(1f, leaderApproachSeconds);
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            // Gentle acceleration and deceleration without stopping at waypoints.
            t = t * t * (3f - 2f * t);
            float u = 1f - t;
            Vector3 position = u*u*u*start + 3f*u*u*t*control1 + 3f*u*t*t*control2 + t*t*t*destination;
            Vector3 tangent = 3f*u*u*(control1-start) + 6f*u*t*(control2-control1) + 3f*t*t*(destination-control2);
            Quaternion from = leader.rotation;
            leaderRig.Face(tangent);
            leader.rotation = Quaternion.Slerp(from, leader.rotation, 1f - Mathf.Exp(-8f * Time.deltaTime));
            PlaceDoorwayActor(leaderRig, position);
            Quaternion officerFrom = officer.rotation;
            officerRig.Face(leader.position - officer.position);
            officer.rotation = Quaternion.Slerp(officerFrom, officer.rotation, 1f - Mathf.Exp(-4f * Time.deltaTime));
            yield return null;
        }
        PlaceDoorwayActor(leaderRig, destination);
        leaderRig.walking = false;
        leaderRig.Face(officer.position - leader.position);
        officerRig.Face(leader.position - officer.position);
        leaderRig.conversationTarget = officer;
        SetWeddingDramaBeat("leader-question");
        ShowLine("族人", "我們只是辦婚禮，為什麼要這樣對待我們？", leaderQuestionSeconds);
        for (float elapsed = 0f; elapsed < leaderQuestionSeconds; elapsed += Time.deltaTime)
        {
            leaderRig.speakingWeight = Mathf.SmoothStep(0f, 1f, elapsed / 0.35f);
            yield return null;
        }

        leaderRig.speakingWeight = 0f;
        leaderRig.conversationTarget = null;
        officerRig.pushTarget = leaderRig;
        Vector3 officerStart = officer.position;
        Vector3 pushDirection = Vector3.ProjectOnPlane(leader.position - officer.position, Vector3.up).normalized;
        Vector3 officerContact = officerStart + pushDirection * height * 0.22f;
        SetWeddingDramaBeat("police-shove");
        for (float elapsed = 0f; elapsed < 0.34f; elapsed += Time.deltaTime)
        {
            float t = Mathf.SmoothStep(0, 1, elapsed / 0.34f);
            officerRig.pushWeight = t;
            PlaceDoorwayActor(officerRig, Vector3.Lerp(officerStart, officerContact, t));
            yield return null;
        }
        officerRig.pushWeight = 1f;
        PlaceDoorwayActor(officerRig, officerContact);
        // Reaction begins after contact, so there is no invisible remote shove.
        ShowLine("旁白", "警察猛地推開他，他踉蹌後退，差點跌倒。", 3.8f);
        SetWeddingDramaBeat("shove-contact");
        Vector3 contactStart = leader.position;
        for (float elapsed = 0f; elapsed < 0.26f; elapsed += Time.deltaTime)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / 0.26f);
            leaderRig.stumbleWeight = 0.62f * t;
            PlaceDoorwayActor(leaderRig, contactStart + pushDirection * height * 0.06f * t);
            PlaceDoorwayActor(officerRig, officerContact + pushDirection * height * 0.08f * t);
            yield return null;
        }
        PlaceDoorwayActor(leaderRig, contactStart + pushDirection * height * 0.06f);
        PlaceDoorwayActor(officerRig, officerContact + pushDirection * height * 0.08f);
        SetWeddingDramaBeat("leader-stumble");
        Vector3 leaderStart = leader.position;
        const float stumbleSeconds = 1.65f;
        for (float elapsed = 0f; elapsed < stumbleSeconds; elapsed += Time.deltaTime)
        {
            float t = Mathf.Clamp01(elapsed / stumbleSeconds);
            float travel = 1f - Mathf.Pow(1f - t, 2f);
            leaderRig.stumbleProgress = t;
            leaderRig.stumbleWeight = t < 0.2f ? Mathf.Lerp(0.62f, 1f, Mathf.SmoothStep(0, 1, t / 0.2f))
                : Mathf.Lerp(1f, 0.36f, Mathf.SmoothStep(0, 1, (t-0.2f)/0.8f));
            PlaceDoorwayActor(leaderRig, leaderStart + pushDirection * height * 0.38f * travel);
            officerRig.pushWeight = 1f - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.04f, 0.48f, t));
            yield return null;
        }
        PlaceDoorwayActor(leaderRig, leaderStart + pushDirection * height * 0.38f);
        officerRig.pushWeight = 0f;
        officerRig.pushTarget = null;
        leaderRig.stumbleProgress = 1f;
        SetWeddingDramaBeat("leader-recover");
        for (float elapsed = 0f; elapsed < 0.9f; elapsed += Time.deltaTime)
        {
            leaderRig.stumbleWeight = Mathf.Lerp(0.36f, 0f, Mathf.SmoothStep(0, 1, elapsed / 0.9f));
            yield return null;
        }
        leaderRig.stumbleWeight = 0f;
        yield return new WaitForSeconds(0.75f);
        SetWeddingDramaBeat("confrontation-complete");
    }
}
