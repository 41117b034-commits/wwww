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

    private (Vector3 eye, Vector3 focus, float fov) WeddingConfrontationShot(
        Transform officer, Transform leader, Vector3 axis, float height, float close, float fall)
    {
        Vector3 nearSide = Vector3.Cross(Vector3.up, axis);
        Vector3 first = WeddingActorFloor(officer);
        Vector3 second = WeddingActorFloor(leader);
        Vector3 center = Vector3.Lerp(first, second, Mathf.Lerp(.53f, .62f, fall));
        Vector3 focus = center + Vector3.up * (Mathf.Lerp(.57f, .72f, close) - .31f * fall) * height;
        float fov = Mathf.Lerp(48f, 41f, close) + 6f * fall;
        Camera camera = incidentCameraView ? incidentCameraView.GetComponent<Camera>() : null;
        float aspect = camera ? camera.aspect : 1.6f;
        float span = Mathf.Abs(Vector3.Dot(second - first, axis)) + height * (.75f + fall * .56f);
        float widthDistance = span / (2f * Mathf.Tan(fov * .5f * Mathf.Deg2Rad) * aspect * .84f);
        float distance = Mathf.Max(widthDistance, height * (Mathf.Lerp(1.92f, 1.32f, close) + .58f * fall));
        Vector3 viewSide = (nearSide - axis * Mathf.Lerp(.12f, .46f, close)).normalized;
        Vector3 eye = center + viewSide * distance + Vector3.up * height * Mathf.Lerp(.96f, .78f, fall);
        return (eye, focus, fov);
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
        Vector3 start = leader.position;
        // Face across the open south lane. Falling along this line leaves the
        // groom in front of the feast canopy, clear of its posts and the guests.
        Vector3 axis = new Vector3(.85f, 0f, -.53f).normalized;
        Vector3 destination = officer.position + axis * (.64f * height);
        destination.y = start.y;
        var approachShot = WeddingConfrontationShot(officer, leader, axis, height, 0f, 0f);
        yield return WeddingCameraTo(approachShot.eye, approachShot.focus, approachShot.fov, .75f);
        void Frame(float close, float fall)
        {
            var shot = WeddingConfrontationShot(officer, leader, axis, height, close, fall);
            SetDoorwayCamera(shot.eye, shot.focus, shot.fov);
        }
        // Curve around the near edge of the roasting pit, never through it.
        Vector3 control1 = start + new Vector3(-.32f, 0f, -.82f) * height;
        Vector3 control2 = destination + axis * (.48f * height);
        Quaternion startRotation = leader.rotation;
        leaderRig.Face(control1 - start);
        Quaternion walkingRotation = leader.rotation;
        leader.rotation = startRotation;
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
            Frame(Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.30f, 1f, t)), 0f);
            yield return null;
        }
        PlaceDoorwayActor(leaderRig, destination);
        leaderRig.walking = false;
        leaderRig.Face(officer.position - leader.position);
        officerRig.Face(leader.position - officer.position);
        leaderRig.conversationTarget = officer;
        SetWeddingDramaBeat("leader-question");
        Frame(1f, 0f);
        float spokenSeconds = ShowCharacterLine(leader, leader == groomActor ? "新郎" : "族人",
            "我們只是辦婚禮，為什麼要這樣對待我們？", leaderQuestionSeconds);
        for (float elapsed = 0f; elapsed < spokenSeconds || (GameLanguageSettings.ChineseVoice&&ChineseSpeech.Busy); elapsed += Time.deltaTime)
        {
            leaderRig.weddingQuestionProgress = Mathf.Clamp01(elapsed / Mathf.Max(.1f, spokenSeconds));
            leaderRig.speakingWeight = Mathf.SmoothStep(0f, 1f, elapsed / .36f)
                * Mathf.SmoothStep(0f, 1f, (spokenSeconds - elapsed) / .42f);
            yield return null;
        }

        leaderRig.speakingWeight = 0f;
        leaderRig.weddingQuestionProgress = -1f;
        leaderRig.conversationTarget = null;
        officerRig.pushTarget = leaderRig;
        Vector3 officerStart = officer.position;
        Vector3 pushDirection = Vector3.ProjectOnPlane(leader.position - officer.position, Vector3.up).normalized;
        Vector3 officerContact = officerStart + pushDirection * height * .25f;
        SetWeddingDramaBeat("police-shove");
        for (float elapsed = 0f; elapsed < 0.34f; elapsed += Time.deltaTime)
        {
            float t = Mathf.SmoothStep(0, 1, elapsed / 0.34f);
            officerRig.pushWeight = t;
            PlaceDoorwayActor(officerRig, Vector3.Lerp(officerStart, officerContact, t));
            Frame(1f, 0f);
            yield return null;
        }
        officerRig.pushWeight = 1f;
        PlaceDoorwayActor(officerRig, officerContact);
        // Give the rig a frame to plant the palm before the groom reacts.
        yield return null;
        SetWeddingDramaBeat("shove-contact");
        Vector3 contactStart = leader.position;
        for (float elapsed = 0f; elapsed < .14f; elapsed += Time.deltaTime)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / .14f);
            leaderRig.stumbleWeight = .42f * t;
            PlaceDoorwayActor(leaderRig, contactStart + pushDirection * height * .035f * t);
            PlaceDoorwayActor(officerRig, officerContact + pushDirection * height * .05f * t);
            Frame(1f, 0f);
            yield return null;
        }
        Vector3 fallStart = leader.position;
        Vector3 landing = fallStart + pushDirection * height * .58f;
        Terrain terrain = FindTerrainSurface(landing, out float floor);
        leaderRig.BeginWeddingFall(terrain, terrain ? floor : WeddingActorFloor(leader).y);
        SetWeddingDramaBeat("leader-fall");
        const float fallSeconds = .92f;
        for (float elapsed = 0f; elapsed < fallSeconds; elapsed += Time.deltaTime)
        {
            float t = elapsed / fallSeconds;
            leaderRig.shoveFallProgress = t;
            leaderRig.stumbleWeight = .42f * (1f - Mathf.SmoothStep(0f, 1f, t / .35f));
            PlaceDoorwayActor(leaderRig, Vector3.Lerp(fallStart, landing, 1f - (1f - t) * (1f - t)));
            officerRig.pushWeight = 1f - Mathf.SmoothStep(0f, 1f, t / .35f);
            officerRig.shoveLookDown = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.22f, .95f, t));
            Frame(1f, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        PlaceDoorwayActor(leaderRig, landing);
        officerRig.pushWeight = 0f;
        officerRig.pushTarget = null;
        leaderRig.stumbleWeight = 0f;
        leaderRig.shoveFallProgress = 1f;
        Frame(1f, 1f);
        SetWeddingDramaBeat("leader-grounded");
        for (float elapsed = 0f; elapsed < 1.18f; elapsed += Time.deltaTime)
        {
            officerRig.shoveLookDown = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.60f, 1.18f, elapsed));
            yield return null;
        }
        officerRig.shoveLookDown = 0f;
        SetWeddingDramaBeat("confrontation-complete");
    }
}
