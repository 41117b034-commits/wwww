using UnityEngine;

[DefaultExecutionOrder(1000)]
public sealed class Chapter1NpcGrounding : MonoBehaviour
{
    public Animator animator;
    public LayerMask groundLayers = ~0;
    public float footClearance = 0.025f;
    public float groundProbeUp = 8f;
    public float groundProbeDown = 20f;
    public float hardSnapThreshold = 0.2f;
    public float followSpeed = 24f;
    public float maximumCorrection = 8f;

    private Transform leftFoot;
    private Transform rightFoot;
    private Renderer[] renderers;
    private bool loggedFirstCorrection;

    private void Awake()
    {
        CacheReferences();
    }

    public void Configure(Animator sourceAnimator, LayerMask layers)
    {
        animator = sourceAnimator;
        groundLayers = layers;
        CacheReferences();
    }

    public void SnapImmediately()
    {
        CorrectHeight(true);
    }

    private void LateUpdate()
    {
        CorrectHeight(false);
    }

    private void CacheReferences()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        renderers = GetComponentsInChildren<Renderer>(true);
        leftFoot = null;
        rightFoot = null;

        if (animator != null
            && animator.avatar != null
            && animator.avatar.isValid
            && animator.isHuman)
        {
            leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        }
    }

    private void CorrectHeight(bool immediate)
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        if (!TryGetRequiredCorrection(out float correction))
        {
            return;
        }

        float maxCorrection = Mathf.Max(0.2f, maximumCorrection);
        correction = Mathf.Clamp(correction, -maxCorrection, maxCorrection);

        if (Mathf.Abs(correction) < 0.002f)
        {
            return;
        }

        float appliedCorrection = correction;
        if (!immediate && Mathf.Abs(correction) <= Mathf.Max(0.02f, hardSnapThreshold))
        {
            float blend = 1f - Mathf.Exp(-Mathf.Max(0.1f, followSpeed) * Time.deltaTime);
            appliedCorrection *= blend;
        }

        Vector3 position = transform.position;
        position.y += appliedCorrection;
        transform.position = position;

        if (!loggedFirstCorrection && Mathf.Abs(correction) >= 0.08f)
        {
            loggedFirstCorrection = true;
            Debug.Log(
                "[Chapter1 Grounding] Corrected " + name
                + " by " + correction.ToString("0.000") + "m.");
        }
    }

    private bool TryGetRequiredCorrection(out float correction)
    {
        correction = 0f;
        bool foundFoot = false;
        float bestCorrection = float.NegativeInfinity;

        if (TryGetFootCorrection(leftFoot, out float leftCorrection))
        {
            bestCorrection = leftCorrection;
            foundFoot = true;
        }

        if (TryGetFootCorrection(rightFoot, out float rightCorrection))
        {
            bestCorrection = foundFoot
                ? Mathf.Max(bestCorrection, rightCorrection)
                : rightCorrection;
            foundFoot = true;
        }

        if (foundFoot)
        {
            correction = bestCorrection;
            return true;
        }

        if (!TryGetRendererBounds(out Bounds bounds)
            || !TryGetGroundY(bounds.center, out float groundY))
        {
            return false;
        }

        correction = groundY + Mathf.Max(0f, footClearance) - bounds.min.y;
        return true;
    }

    private bool TryGetFootCorrection(Transform foot, out float correction)
    {
        correction = 0f;
        if (foot == null || !TryGetGroundY(foot.position, out float groundY))
        {
            return false;
        }

        correction = groundY + Mathf.Max(0f, footClearance) - foot.position.y;
        return true;
    }

    private bool TryGetRendererBounds(out Bounds bounds)
    {
        bounds = new Bounds(transform.position, Vector3.zero);
        bool found = false;

        if (renderers == null || renderers.Length == 0)
        {
            renderers = GetComponentsInChildren<Renderer>(true);
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled)
            {
                continue;
            }

            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return found;
    }

    private bool TryGetGroundY(Vector3 position, out float groundY)
    {
        Terrain[] terrains = Terrain.activeTerrains;
        for (int i = 0; i < terrains.Length; i++)
        {
            Terrain terrain = terrains[i];
            if (terrain == null || terrain.terrainData == null)
            {
                continue;
            }

            Vector3 terrainPosition = terrain.transform.position;
            Vector3 terrainSize = terrain.terrainData.size;
            bool inside = position.x >= terrainPosition.x
                && position.x <= terrainPosition.x + terrainSize.x
                && position.z >= terrainPosition.z
                && position.z <= terrainPosition.z + terrainSize.z;

            if (inside)
            {
                groundY = terrain.SampleHeight(position) + terrainPosition.y;
                return true;
            }
        }

        Vector3 origin = position + Vector3.up * Mathf.Max(0.5f, groundProbeUp);
        float distance = Mathf.Max(1f, groundProbeUp + groundProbeDown);
        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            Vector3.down,
            distance,
            groundLayers,
            QueryTriggerInteraction.Ignore);
        bool foundGround = false;
        groundY = float.NegativeInfinity;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null || hit.normal.y < 0.35f)
            {
                continue;
            }

            Transform hitTransform = hit.collider.transform;
            if (hitTransform == transform
                || hitTransform.IsChildOf(transform)
                || transform.IsChildOf(hitTransform)
                || hitTransform.GetComponentInParent<Animator>() != null)
            {
                continue;
            }

            if (!foundGround || hit.point.y > groundY)
            {
                groundY = hit.point.y;
                foundGround = true;
            }
        }

        return foundGround;
    }
}
