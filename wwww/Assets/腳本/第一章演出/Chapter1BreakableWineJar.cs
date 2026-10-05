using System.Collections;
using UnityEngine;

/// <summary>
/// 酒罐倒下後自動破裂。
/// 這版重點：每一片碎片都會先找地面並貼地，不再整坨浮在半空。
/// </summary>
public class Chapter1BreakableWineJar : MonoBehaviour
{
    [Header("Break Model")]
    public GameObject brokenPrefab;

    public bool autoMatchBrokenSizeToIntact = true;

    [Range(0.2f, 4f)]
    public float brokenScaleMultiplier = 1f;

    public bool alignBrokenBoundsCenter = true;
    public bool alignBrokenBottom = true;
    public float brokenVerticalOffset = 0f;

    [Header("Auto Trigger")]
    public bool breakWhenTilted = true;

    [Range(5f, 170f)]
    public float breakTiltAngle = 60f;

    public bool breakOnCollision = true;
    public float minimumImpactSpeed = 0.6f;
    public float armDelay = 1f;

    [Header("Sound")]
    public AudioClip breakClip;

    [Range(0f, 2f)]
    public float breakVolume = 1.6f;

    [Range(0f, 1f)]
    public float spatialBlend = 0.45f;

    public float audioMinDistance = 4f;
    public float audioMaxDistance = 30f;

    [Header("Fragments Physics")]
    [Tooltip("建議先關閉，這樣碎片會穩穩落地。")]
    public bool pushFragments = false;

    public float fragmentForce = 0.08f;
    public float fragmentUpForce = 0f;
    public float fragmentTorque = 0.2f;

    [Header("Force Fragments To Ground")]
    [Tooltip("勾選後，每一片碎片會獨立往下找地面。")]
    public bool forceEveryFragmentToGround = true;

    [Tooltip("從碎片上方多少距離往下射線找地。")]
    public float groundRayStartHeight = 3f;

    [Tooltip("往下最多找多遠。")]
    public float groundRayDistance = 12f;

    [Tooltip("碎片最低點離地高度。0.01~0.03 可避免穿地。")]
    public float fragmentGroundClearance = 0.015f;

    [Tooltip("哪些 Layer 算地面。預設 Everything。")]
    public LayerMask groundLayers = ~0;

    [Tooltip("生成後先貼地一次，再延遲補貼一次，避免 Rigidbody 初始化後彈起。")]
    public bool secondGroundSnap = true;

    [Tooltip("第二次貼地延遲。")]
    public float secondGroundSnapDelay = 0.08f;

    [Header("Broken Object Lifetime")]
    public bool autoRemoveBrokenObject = true;

    [Min(0.1f)]
    public float brokenObjectLifetime = 5f;

    [Header("Intact Object")]
    public bool destroyIntactObject = true;

    [Header("Debug")]
    public bool debugLog = true;

    private bool broken;
    private Quaternion initialRotation;
    private float armedAt;

    private void Awake()
    {
        initialRotation = transform.rotation;
        armedAt = Time.time + Mathf.Max(0f, armDelay);
    }

    private void Start()
    {
        initialRotation = transform.rotation;
    }

    private void Update()
    {
        if (broken || !breakWhenTilted || Time.time < armedAt)
            return;

        float angle = Quaternion.Angle(initialRotation, transform.rotation);

        if (angle >= breakTiltAngle)
            BreakNow();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (broken || !breakOnCollision || Time.time < armedAt)
            return;

        if (collision.relativeVelocity.magnitude >= minimumImpactSpeed)
            BreakNow();
    }

    [ContextMenu("Break Now")]
    public void BreakNow()
    {
        if (broken)
            return;

        broken = true;

        Bounds intactBounds;
        bool hasIntactBounds = TryGetRendererBounds(gameObject, out intactBounds);

        Vector3 spawnPosition = transform.position;
        Quaternion spawnRotation = transform.rotation;

        PlayBreakSound(hasIntactBounds ? intactBounds.center : spawnPosition);

        if (brokenPrefab != null)
        {
            GameObject brokenObject =
                Instantiate(brokenPrefab, spawnPosition, spawnRotation);

            Bounds brokenBounds;
            bool hasBrokenBounds =
                TryGetRendererBounds(brokenObject, out brokenBounds);

            if (autoMatchBrokenSizeToIntact
                && hasIntactBounds
                && hasBrokenBounds)
            {
                float intactSize =
                    Mathf.Max(0.001f, intactBounds.size.magnitude);

                float brokenSize =
                    Mathf.Max(0.001f, brokenBounds.size.magnitude);

                float autoScale = intactSize / brokenSize;

                brokenObject.transform.localScale *=
                    autoScale * Mathf.Max(0.01f, brokenScaleMultiplier);

                TryGetRendererBounds(brokenObject, out brokenBounds);
            }
            else
            {
                brokenObject.transform.localScale *=
                    Mathf.Max(0.01f, brokenScaleMultiplier);

                TryGetRendererBounds(brokenObject, out brokenBounds);
            }

            if (hasIntactBounds
                && TryGetRendererBounds(brokenObject, out brokenBounds))
            {
                if (alignBrokenBoundsCenter)
                {
                    brokenObject.transform.position +=
                        intactBounds.center - brokenBounds.center;

                    TryGetRendererBounds(brokenObject, out brokenBounds);
                }

                if (alignBrokenBottom)
                {
                    brokenObject.transform.position +=
                        Vector3.up * (intactBounds.min.y - brokenBounds.min.y);

                    TryGetRendererBounds(brokenObject, out brokenBounds);
                }

                brokenObject.transform.position +=
                    Vector3.up * brokenVerticalOffset;
            }

            Rigidbody[] pieces =
                brokenObject.GetComponentsInChildren<Rigidbody>(true);

            if (forceEveryFragmentToGround)
            {
                SnapEveryFragmentToGround(pieces);
            }

            foreach (Rigidbody rb in pieces)
            {
                if (rb == null)
                    continue;

                rb.isKinematic = false;
                rb.useGravity = true;

                if (pushFragments)
                {
                    Vector2 circle = Random.insideUnitCircle;

                    Vector3 force =
                        new Vector3(
                            circle.x * fragmentForce,
                            fragmentUpForce,
                            circle.y * fragmentForce);

                    rb.AddForce(force, ForceMode.Impulse);

                    rb.AddTorque(
                        Random.insideUnitSphere * fragmentTorque,
                        ForceMode.Impulse);
                }
                else
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }

            if (forceEveryFragmentToGround && secondGroundSnap)
            {
                StartCoroutine(
                    SecondGroundSnapRoutine(
                        brokenObject,
                        pieces));
            }

            if (autoRemoveBrokenObject)
            {
                Destroy(
                    brokenObject,
                    Mathf.Max(0.1f, brokenObjectLifetime));
            }
        }

        if (destroyIntactObject)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }

    private void SnapEveryFragmentToGround(Rigidbody[] pieces)
    {
        if (pieces == null)
            return;

        foreach (Rigidbody rb in pieces)
        {
            if (rb == null)
                continue;

            Renderer[] renderers =
                rb.GetComponentsInChildren<Renderer>(true);

            if (renderers == null || renderers.Length == 0)
                continue;

            Bounds b = renderers[0].bounds;

            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    b.Encapsulate(renderers[i].bounds);
            }

            Vector3 rayOrigin =
                new Vector3(
                    b.center.x,
                    b.max.y + groundRayStartHeight,
                    b.center.z);

            if (Physics.Raycast(
                rayOrigin,
                Vector3.down,
                out RaycastHit hit,
                groundRayDistance + groundRayStartHeight,
                groundLayers,
                QueryTriggerInteraction.Ignore))
            {
                float deltaY =
                    hit.point.y
                    + fragmentGroundClearance
                    - b.min.y;

                rb.position += Vector3.up * deltaY;
            }
        }
    }

    private IEnumerator SecondGroundSnapRoutine(
        GameObject brokenObject,
        Rigidbody[] pieces)
    {
        yield return new WaitForSecondsRealtime(
            Mathf.Max(0f, secondGroundSnapDelay));

        if (brokenObject == null)
            yield break;

        SnapEveryFragmentToGround(pieces);

        foreach (Rigidbody rb in pieces)
        {
            if (rb == null)
                continue;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private bool TryGetRendererBounds(GameObject root, out Bounds bounds)
    {
        bounds = default;

        if (root == null)
            return false;

        Renderer[] renderers =
            root.GetComponentsInChildren<Renderer>(true);

        bool found = false;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

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

    private void PlayBreakSound(Vector3 position)
    {
        if (breakClip == null)
            return;

        GameObject audioObject =
            new GameObject("WineJar_BreakSound");

        audioObject.transform.position = position;

        AudioSource source =
            audioObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = spatialBlend;
        source.minDistance = Mathf.Max(0.1f, audioMinDistance);
        source.maxDistance = Mathf.Max(source.minDistance + 0.1f, audioMaxDistance);
        source.rolloffMode = AudioRolloffMode.Linear;
        source.volume = 1f;

        source.PlayOneShot(
            breakClip,
            Mathf.Clamp(breakVolume, 0f, 2f));

        Destroy(
            audioObject,
            Mathf.Max(0.5f, breakClip.length + 0.5f));
    }
}
