using System;
using UnityEngine;
using UnityEngine.XR;

// Two loose stones pin a larger slab across the rear mouth. Each stays in the
// scene when moved. The existing terrain is the floor on both sides of the pile.
public sealed class Chapter4CaveExit
{
    readonly Chapter4Evacuation flow;
    readonly Chapter4RockShelter cave;
    readonly Chapter4Companion helper;
    readonly Transform frame;
    readonly BoxCollider seal;
    readonly Transform slab;
    readonly MeshCollider slabSurface;
    readonly Transform[] loose = new Transform[2];
    readonly Vector3[] home = new Vector3[2];
    readonly Vector3[] deposit = new Vector3[2];
    readonly bool[] placed = new bool[2];
    readonly Quaternion[] rest = new Quaternion[2];
    readonly MeshFilter model;
    readonly Vector3 modelCenter, modelSize;
    int carried = -1;
    float settle, push;
    bool puttingDown, working;
    Vector3 setDownFrom;
    Quaternion setDownRotation;

    public bool Open { get; private set; }
    public bool Carrying => carried >= 0;
    public Transform HeldStone => Carrying ? loose[carried] : null;
    public float BlockedAt { get; }
    public float QueueEnd => BlockedAt - 2.5f;
    Chapter4Player Player => flow.chapter.player;

    public Chapter4CaveExit(Chapter4Evacuation owner, Chapter4RockShelter shelter,
        MeshFilter donor, Chapter4Companion mona)
    {
        flow = owner;
        cave = shelter;
        model = donor;
        helper = mona ? mona : throw new InvalidOperationException("Missing cave helper.");
        var trail = cave.escapeTrail;
        frame = new GameObject("後洞口 · 坍落的石堆").transform;
        frame.SetParent(cave.transform.parent, false);
        Vector3 along = Vector3.ProjectOnPlane(trail[4].position - trail[2].position, Vector3.up);
        frame.SetPositionAndRotation(trail[3].position, Quaternion.LookRotation(along));
        BlockedAt = flow.ProjectProgress(frame.position, flow.ApproachLength, flow.RouteLength);

        // Measure the FBX in its imported orientation once. The wrapper of each
        // stone then has a centred pivot, real metre dimensions and its own yaw.
        var correction = Quaternion.Euler(-90, 0, -114.323006f);
        var source = donor.sharedMesh.bounds;
        modelCenter = correction * source.center;
        var xAxis = correction * (Vector3.right * source.size.x);
        var yAxis = correction * (Vector3.up * source.size.y);
        var zAxis = correction * (Vector3.forward * source.size.z);
        modelSize = Abs(xAxis) + Abs(yAxis) + Abs(zAxis);

        Stone("左壁崩岩", -2.48f, .36f, 0, new Vector3(2.65f, 2.02f, 2.02f), -13);
        Stone("左壁斷石", -2.17f, .66f, 1.15f, new Vector3(1.96f, 1.42f, 1.63f), 19);
        Stone("左壁石冠", -2.66f, .82f, 2.10f, new Vector3(1.52f, 1.05f, 1.26f), -28);
        Stone("左側碎塊", -1.79f, -.37f, 0, new Vector3(1.12f, .86f, .98f), 27);
        Stone("斜靠洞壁的碎岩", -3.08f, -.43f, .08f, new Vector3(2.1f, 1.68f, 1.62f), 42);
        Stone("左壁石腳", -1.74f, .95f, .06f, new Vector3(1.34f, 1.25f, 1.42f), -33);
        Stone("左壁石腳", -2.14f, -.89f, 0, new Vector3(.78f, .48f, .63f), 4);
        Stone("右壁崩岩", 2.60f, 1.15f, 0, new Vector3(2.68f, 2.24f, 1.9f), 9);
        Stone("右壁斷石", 2.34f, 1.36f, 1.35f, new Vector3(1.91f, 1.42f, 1.55f), -18);
        Stone("右壁石冠", 2.76f, 1.46f, 2.34f, new Vector3(1.30f, .89f, 1.22f), 32);
        Stone("右側碎塊", 1.78f, 1.2f, 0, new Vector3(1.02f, .75f, 1.24f), -24);
        Stone("右壁石腳", 3.12f, .18f, .05f, new Vector3(1.87f, 1.84f, 1.43f), -37);
        Stone("右壁石腳", 1.85f, 1.82f, 0, new Vector3(1.40f, .88f, .90f), 14);
        Stone("牆腳碎石", -2.35f, -2.02f, 0, new Vector3(.57f, .30f, .52f), 36);
        Stone("牆腳碎石", -2.42f, -2.48f, 0, new Vector3(.36f, .23f, .34f), -17);

        slab = Stone("卡住通路的大石", 0, 0, 0, new Vector3(2.66f, 1.36f, 1.39f), -7);
        loose[0] = Stone("鬆動的扁石", -.43f, -.22f, 0, new Vector3(.53f, .34f, .43f), -21);
        loose[1] = Stone("鬆動的稜石", .33f, -.06f, 0, new Vector3(.46f, .40f, .48f), 38);
        Physics.SyncTransforms();
        slabSurface = slab.GetComponentInChildren<MeshCollider>();
        for (int i = 0; i < loose.Length; i++)
        {
            var stone = loose[i];
            float halfHeight = stone.GetComponentInChildren<Renderer>().bounds.extents.y;
            Vector3 spot = stone.position;
            if (!slabSurface.Raycast(new Ray(spot + Vector3.up * 3, Vector3.down), out var hit, 5))
                throw new InvalidOperationException("Loose stone has no support on the cave slab.");
            home[i] = stone.position = new Vector3(spot.x, hit.point.y + halfHeight - .045f, spot.z);
            rest[i] = stone.rotation;
            deposit[i] = Ground(-1.83f - i * .13f, -1.55f - i * .78f) + Vector3.up * (halfHeight - .025f);
        }
        // Seal the seams between irregular meshes, including the sloping walls.
        var barrier = new GameObject("石堆通行邊界");
        barrier.transform.SetParent(frame, false);
        seal = barrier.AddComponent<BoxCollider>();
        seal.center = new Vector3(0, 2.1f, .47f);
        seal.size = new Vector3(8.8f, 5.8f, .4f);
    }

    static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

    Vector3 Ground(float x, float z)
    {
        Vector3 p = frame.TransformPoint(new Vector3(x, 0, z));
        if (flow.terrain.Raycast(new Ray(p + Vector3.up * 15, Vector3.down), out var hit, 30)) p.y = hit.point.y;
        return p;
    }

    Transform Stone(string name, float x, float z, float baseHeight, Vector3 size, float yaw)
    {
        var root = new GameObject(name).transform;
        root.SetParent(frame, false);
        root.localScale = new Vector3(size.x / modelSize.x, size.y / modelSize.y, size.z / modelSize.z);
        root.localRotation = Quaternion.Euler(0, yaw, 0);
        root.position = Ground(x, z) + Vector3.up * (baseHeight + size.y * .5f - .055f);
        var face = new GameObject("岩面", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
        face.transform.SetParent(root, false);
        face.transform.localRotation = Quaternion.Euler(-90, 0, -114.323006f);
        face.transform.localPosition = -modelCenter;
        face.GetComponent<MeshFilter>().sharedMesh = model.sharedMesh;
        face.GetComponent<MeshRenderer>().sharedMaterials = model.GetComponent<MeshRenderer>().sharedMaterials;
        face.GetComponent<MeshCollider>().sharedMesh = model.sharedMesh;
        return root;
    }

    public bool Outside(Vector3 feet) => Open && cave.Sheltered(feet) &&
        frame.InverseTransformPoint(feet).z >= 1.95f;

    int WithinReach()
    {
        var feet = Player.transform.position;
        Vector3 local = frame.InverseTransformPoint(feet);
        if (local.z > .25f || local.z < -2.05f || Mathf.Abs(local.x) > 2.1f) return -1;
        Vector3 reach = TrackedHand(out var hand) ? hand : feet + Vector3.up * 1.18f;
        int pick = -1;
        float nearest = 1.65f;
        for (int i = 0; i < loose.Length; i++)
        {
            if (placed[i]) continue;
            float distance = Vector3.Distance(reach, loose[i].position);
            if (distance < nearest) { nearest = distance; pick = i; }
        }
        return pick;
    }

    bool AtDeposit()
    {
        Vector3 local = frame.InverseTransformPoint(Player.transform.position);
        return local.x < -1.0f && Chapter4Evacuation.FlatDistance(Player.transform.position, deposit[carried]) < 1.55f;
    }

    bool TrackedHand(out Vector3 position)
    {
        position = default;
        if (!Player.IsVR) return false;
        var right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        var head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        if (!right.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 hand) ||
            !head.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 headset)) return false;
        position = Player.view.transform.position + Player.transform.TransformVector(hand - headset);
        return true;
    }

    public string Objective
    {
        get
        {
            string key = Player.IsVR ? "右手把主按鍵" : "E";
            if (Open) return "路通了，跟著族人走出洞口";
            if (puttingDown) return "把石頭放穩";
            if (Carrying) return AtDeposit() ? "按 " + key + " 把石頭放在岩壁旁" : "抱著石頭走到左側岩壁旁";
            if (placed[0] && placed[1]) return "讓開一步，莫那要把底下的大石推開";
            if (WithinReach() >= 0) return "按 " + key + " 搬起大石上鬆動的石頭";
            return "洞口被落石堵住了，靠近石堆幫莫那清路";
        }
    }

    public void Tick(bool inChamber, float dt)
    {
        if (!inChamber || Open) return;
        if (Player.InteractPressed && !puttingDown)
        {
            if (!Carrying)
            {
                carried = WithinReach();
                if (Carrying) HeldStone.GetComponentInChildren<MeshCollider>().enabled = false;
            }
            else if (AtDeposit())
            {
                setDownFrom = HeldStone.position;
                setDownRotation = HeldStone.rotation;
                settle = 0;
                puttingDown = true;
            }
        }
        if (Carrying) MoveLooseStone(dt);
        MoveSlab(dt);
    }

    void MoveLooseStone(float dt)
    {
        var stone = HeldStone;
        if (puttingDown)
        {
            settle = Mathf.MoveTowards(settle, 1, dt / .60f);
            float t = Mathf.SmoothStep(0, 1, settle);
            stone.position = Vector3.Lerp(setDownFrom, deposit[carried], t) + Vector3.up * (.10f * Mathf.Sin(t * Mathf.PI));
            stone.rotation = Quaternion.Slerp(setDownRotation, rest[carried], t);
            if (settle < 1) return;
            placed[carried] = true;
            stone.GetComponentInChildren<MeshCollider>().enabled = true;
            carried = -1;
            puttingDown = false;
            return;
        }
        var eye = Player.view.transform;
        var facing = Vector3.ProjectOnPlane(eye.forward, Vector3.up).normalized;
        Vector3 grip = eye.position + facing * .52f - Vector3.up * .40f;
        if (TrackedHand(out var hand)) grip = hand + facing * .09f;
        stone.position = Vector3.MoveTowards(stone.position, grip, dt * 4.5f);
    }

    void MoveSlab(float dt)
    {
        if (helper.Progress < BlockedAt - 12f) return;
        float t = Mathf.SmoothStep(0, 1, push);
        Vector3 stance = Ground(-.66f + 2.28f * t, -1.19f - .36f * t);
        helper.AssistTarget = stance;
        if (Chapter4Evacuation.FlatDistance(helper.transform.position, stance) > .16f) return;
        if (!working)
        {
            helper.Pose.Set(Chapter4CompanionPose.State.PushingStone);
            working = true;
        }
        Vector3 contact = Ground(-.45f + 2.28f * t, -.73f - .36f * t) + Vector3.up * .78f;
        var reach = new Ray(contact - frame.forward * .44f, frame.forward);
        if (slabSurface.Raycast(reach, out var surface, 1.5f))
            contact = surface.point;
        helper.StoneContact = contact;
        if (!placed[0] || !placed[1]) return;
        push = Mathf.MoveTowards(push, 1, dt / 3.5f);
        PlaceSlab(push);
        if (push < 1) return;
        seal.enabled = false;
        Open = true;
        ReleaseHelper();
        Physics.SyncTransforms();
    }

    void PlaceSlab(float progress)
    {
        float t = Mathf.SmoothStep(0, 1, progress);
        float tip = Mathf.Sin(t * Mathf.PI);
        slab.position = Ground(2.28f * t, -.36f * t) + Vector3.up * (.625f + .095f * tip);
        slab.localRotation = Quaternion.Euler(4 * tip, -7 + 14 * t, -9 * tip);
    }

    void ReleaseHelper()
    {
        helper.AssistTarget = null;
        helper.StoneContact = null;
        if (helper.Pose.CurrentState == Chapter4CompanionPose.State.PushingStone)
            helper.Pose.Set(Chapter4CompanionPose.State.Normal);
        working = false;
    }

    // All gas retry checkpoints are on the approach, before anyone handles a stone.
    public void Reset()
    {
        ReleaseHelper();
        carried = -1;
        puttingDown = Open = false;
        push = settle = 0;
        PlaceSlab(0);
        for (int i = 0; i < loose.Length; i++)
        {
            placed[i] = false;
            loose[i].SetPositionAndRotation(home[i], rest[i]);
            loose[i].GetComponentInChildren<MeshCollider>().enabled = true;
        }
        seal.enabled = true;
        Physics.SyncTransforms();
    }
}
