using UnityEngine;

// Stay inside the real forest collider, with room for the whole player capsule.
public sealed class Chapter2WorldBoundary : MonoBehaviour
{
    public float edgeInset = 2f;
    // The outer terrain is a backdrop. Stop while there is still forest ahead.
    public Vector2 explorationHalfSize = new Vector2(32, 36);
    public Bounds SafeBounds { get; private set; }
    public int Recoveries { get; private set; }
    Collider ground;
    Chapter2Player player;
    CharacterController motor;
    Vector3 lastSafe;
    bool hasSafe;

    public void Initialize(Chapter2Player owner)
    {
        player = owner; motor = owner.GetComponent<CharacterController>();
        foreach (var root in gameObject.scene.GetRootGameObjects())
            if (root.name == "Forest ground") { ground = root.GetComponent<Collider>(); break; }
        if (!ground) { enabled = false; return; }
        Physics.SyncTransforms();
        var bounds = ground.bounds;
        float inset = Mathf.Max(edgeInset, motor ? motor.radius + motor.skinWidth + .2f : .6f);
        bounds.size = new Vector3(Mathf.Max(1, bounds.size.x - 2 * inset), bounds.size.y, Mathf.Max(1, bounds.size.z - 2 * inset));
        bounds.size = new Vector3(Mathf.Min(bounds.size.x, explorationHalfSize.x * 2), bounds.size.y, Mathf.Min(bounds.size.z, explorationHalfSize.y * 2));
        SafeBounds = bounds;
        RememberSafe();
    }
    public bool GroundHeight(Vector3 point, out float height)
    {
        height = 0;
        if (!ground) return false;
        var ray = new Ray(new Vector3(point.x, ground.bounds.max.y + 10, point.z), Vector3.down);
        if (!ground.Raycast(ray, out var hit, ground.bounds.size.y + 30)) return false;
        height = hit.point.y; return true;
    }
    public bool Inside(Vector3 point) => point.x >= SafeBounds.min.x && point.x <= SafeBounds.max.x && point.z >= SafeBounds.min.z && point.z <= SafeBounds.max.z;
    Vector3 Clamp(Vector3 point)
    {
        point.x = Mathf.Clamp(point.x, SafeBounds.min.x, SafeBounds.max.x);
        point.z = Mathf.Clamp(point.z, SafeBounds.min.z, SafeBounds.max.z);
        return point;
    }
    bool Supported(Vector3 point)
    {
        float r = motor ? motor.radius + .1f : .5f;
        return GroundHeight(point, out _) && GroundHeight(point + Vector3.right * r, out _) &&
            GroundHeight(point - Vector3.right * r, out _) && GroundHeight(point + Vector3.forward * r, out _) && GroundHeight(point - Vector3.forward * r, out _);
    }
    public Vector3 Constrain(Vector3 current, Vector3 proposed)
    {
        if (!ground) return proposed;
        proposed = Clamp(proposed);
        if (Supported(proposed)) return proposed;
        // Slide along an edge instead of locking both movement axes.
        var alongX = new Vector3(proposed.x, proposed.y, current.z);
        if (Supported(alongX)) return alongX;
        var alongZ = new Vector3(current.x, proposed.y, proposed.z);
        if (Supported(alongZ)) return alongZ;
        return new Vector3(current.x, proposed.y, current.z);
    }
    void RememberSafe()
    {
        var p = player.transform.position;
        if (Inside(p) && GroundHeight(p, out float y) && p.y >= y - .25f && p.y < y + 2)
        { lastSafe = new Vector3(p.x, y + .08f, p.z); hasSafe = true; }
    }
    void LateUpdate()
    {
        if (!ground || !player || !player.canMove) return;
        var p = player.transform.position;
        bool supported = GroundHeight(p, out float y);
        if (!Inside(p) || !supported || p.y < y - .8f)
        {
            // Recovery also handles old/out-of-map positions without changing the view direction.
            var safe = Clamp(p);
            if (GroundHeight(safe, out float safeY)) safe.y = safeY + .08f;
            else if (hasSafe) safe = lastSafe;
            else return;
            player.RestoreGroundPosition(safe); Recoveries++;
        }
        RememberSafe();
    }
}
