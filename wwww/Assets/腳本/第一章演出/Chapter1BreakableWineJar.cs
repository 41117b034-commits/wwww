using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 第一章酒罐破裂。
/// 修正重點：
/// 1. 每一個「看得到的碎片」都會被找出來，不只處理有 Rigidbody 的物件。
/// 2. 全部碎片使用同一個地面高度，不再有部分碎片浮在空中。
/// 3. 所有 Rigidbody 都固定成 Kinematic，不會再被物理彈飛。
/// </summary>
public class Chapter1BreakableWineJar : MonoBehaviour
{
    [Header("Break Model")]
    public GameObject brokenPrefab;

    [Tooltip("最大碎片約佔完整酒罐尺寸的比例。")]
    [Range(0.2f, 1.5f)]
    public float largestShardToJarRatio = 0.45f;

    [Tooltip("整體額外大小倍率。")]
    [Range(0.3f, 3f)]
    public float brokenScaleMultiplier = 0.85f;

    [Header("Automatic Trigger")]
    public bool breakWhenTilted = true;

    [Range(5f, 170f)]
    public float breakTiltAngle = 55f;

    public bool breakOnCollision = true;
    public float minimumImpactSpeed = 0.5f;
    public float armDelay = 1f;

    [Header("Sound")]
    public AudioClip breakClip;

    [Range(0f, 2f)]
    public float breakVolume = 1.8f;

    [Range(0f, 1f)]
    public float spatialBlend = 0.35f;

    public float audioMinDistance = 5f;
    public float audioMaxDistance = 30f;

    [Header("Natural Ground Layout")]
    [Tooltip("保留 Broken Prefab 原本碎片位置，再稍微散開。")]
    public bool preservePrefabLayout = true;

    [Range(0f, 1f)]
    public float extraScatter = 0.12f;

    [Tooltip("全部碎片共用這個地面高度。負值會再壓進地面一點。")]
    public float groundYOffset = -0.02f;

    [Tooltip("使用完整酒罐在破裂瞬間的最低點作為地面高度。")]
    public bool useIntactJarBottomAsGround = true;

    [Tooltip("若不使用酒罐最低點，改從酒罐位置往下 Raycast 找地面。")]
    public LayerMask groundLayers = ~0;

    public float groundRayStartHeight = 6f;
    public float groundRayDistance = 30f;

    [Header("Shard Rotation")]
    [Tooltip("只做小角度隨機傾斜，避免大片碎片直立。")]
    [Range(0f, 40f)]
    public float randomTiltDegrees = 12f;

    [Range(0f, 180f)]
    public float randomYawDegrees = 80f;

    [Header("Lifetime")]
    public bool autoRemoveBrokenObject = true;
    public float brokenObjectLifetime = 6f;

    [Header("Debug")]
    public bool debugLog = true;

    private Quaternion initialRotation;
    private bool broken;
    private float armedAt;

    private void Awake()
    {
        initialRotation = transform.rotation;
        armedAt = Time.time + Mathf.Max(0f, armDelay);
    }

    private void Start()
    {
        initialRotation = transform.rotation;

        if (debugLog)
        {
            Debug.Log(
                "[BreakableWineJar] READY：等待酒罐傾倒 "
                + breakTiltAngle
                + " 度。",
                this);
        }
    }

    private void Update()
    {
        if (broken || !breakWhenTilted || Time.time < armedAt)
            return;

        float angle = Quaternion.Angle(
            initialRotation,
            transform.rotation);

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

        if (!TryGetRendererBounds(gameObject, out Bounds intactBounds))
        {
            intactBounds = new Bounds(
                transform.position,
                Vector3.one);
        }

        Vector3 jarCenter = intactBounds.center;

        PlayBreakSound(jarCenter);

        // 先關完整酒罐 Collider，避免後面找地面時打到自己。
        Collider[] intactColliders =
            GetComponentsInChildren<Collider>(true);

        foreach (Collider c in intactColliders)
        {
            if (c != null)
                c.enabled = false;
        }

        float groundY = ResolveGroundY(intactBounds);

        if (brokenPrefab != null)
        {
            GameObject brokenRoot =
                Instantiate(
                    brokenPrefab,
                    transform.position,
                    transform.rotation);

            FreezeAllRigidbodies(brokenRoot);
            DisableAllColliders(brokenRoot, out Collider[] brokenCols, out bool[] oldStates);

            ScaleBrokenPiecesToJar(
                brokenRoot,
                intactBounds);

            List<Transform> pieces =
                CollectVisiblePieceRoots(brokenRoot);

            Vector3 prefabCenter =
                GetAveragePiecePosition(
                    pieces,
                    brokenRoot.transform.position);

            foreach (Transform piece in pieces)
            {
                if (piece == null)
                    continue;

                // 保留原 Prefab 的碎片排列，只做少量向外散開。
                if (preservePrefabLayout)
                {
                    Vector3 dir =
                        piece.position - prefabCenter;

                    dir.y = 0f;

                    if (dir.sqrMagnitude > 0.0001f)
                    {
                        piece.position +=
                            dir.normalized * extraScatter;
                    }
                }

                // 小幅度隨機旋轉，避免全部角度一樣。
                Vector3 euler = piece.eulerAngles;

                piece.rotation =
                    Quaternion.Euler(
                        euler.x + Random.Range(
                            -randomTiltDegrees,
                            randomTiltDegrees),
                        euler.y + Random.Range(
                            -randomYawDegrees,
                            randomYawDegrees),
                        euler.z + Random.Range(
                            -randomTiltDegrees,
                            randomTiltDegrees));

                ForcePieceToGroundY(
                    piece,
                    groundY + groundYOffset);
            }

            RestoreAllColliders(
                brokenCols,
                oldStates);

            // 最後再清一次速度並鎖死物理。
            FreezeAllRigidbodies(brokenRoot);

            if (autoRemoveBrokenObject)
            {
                Destroy(
                    brokenRoot,
                    Mathf.Max(
                        0.1f,
                        brokenObjectLifetime));
            }

            if (debugLog)
            {
                Debug.Log(
                    "[BreakableWineJar] "
                    + pieces.Count
                    + " 個可見碎片已全部壓到同一地面高度 Y="
                    + groundY.ToString("F3"),
                    brokenRoot);
            }
        }
        else
        {
            Debug.LogError(
                "[BreakableWineJar] Broken Prefab 沒有指定！",
                this);
        }

        gameObject.SetActive(false);
    }

    private float ResolveGroundY(Bounds intactBounds)
    {
        if (useIntactJarBottomAsGround)
        {
            return intactBounds.min.y;
        }

        Vector3 rayOrigin =
            new Vector3(
                intactBounds.center.x,
                intactBounds.max.y + groundRayStartHeight,
                intactBounds.center.z);

        RaycastHit[] hits =
            Physics.RaycastAll(
                rayOrigin,
                Vector3.down,
                groundRayStartHeight + groundRayDistance,
                groundLayers,
                QueryTriggerInteraction.Ignore);

        if (hits != null && hits.Length > 0)
        {
            // 找最低的有效碰撞面，避免打到火堆、NPC、酒罐等較高物件。
            float lowest = float.PositiveInfinity;
            bool found = false;

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null)
                    continue;

                float y = hit.point.y;

                if (y < lowest)
                {
                    lowest = y;
                    found = true;
                }
            }

            if (found)
                return lowest;
        }

        return intactBounds.min.y;
    }

    private void ForcePieceToGroundY(
        Transform piece,
        float targetGroundY)
    {
        if (!TryGetPieceBounds(piece, out Bounds b))
            return;

        float deltaY =
            targetGroundY - b.min.y;

        piece.position +=
            Vector3.up * deltaY;

        // 再算一次，做第二次誤差修正。
        if (TryGetPieceBounds(piece, out b))
        {
            deltaY =
                targetGroundY - b.min.y;

            piece.position +=
                Vector3.up * deltaY;
        }
    }

    private List<Transform> CollectVisiblePieceRoots(
        GameObject brokenRoot)
    {
        List<Transform> result =
            new List<Transform>();

        HashSet<Transform> seen =
            new HashSet<Transform>();

        Renderer[] renderers =
            brokenRoot.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            Transform t = renderer.transform;

            // 往上找到 BrokenRoot 的直接子物件。
            // 這樣無論 Rigidbody 放在哪層，每個看得到的碎片都一定會被處理。
            while (t.parent != null
                   && t.parent != brokenRoot.transform)
            {
                t = t.parent;
            }

            if (t != brokenRoot.transform
                && seen.Add(t))
            {
                result.Add(t);
            }
        }

        return result;
    }

    private Vector3 GetAveragePiecePosition(
        List<Transform> pieces,
        Vector3 fallback)
    {
        if (pieces == null || pieces.Count == 0)
            return fallback;

        Vector3 sum = Vector3.zero;
        int count = 0;

        foreach (Transform t in pieces)
        {
            if (t == null)
                continue;

            sum += t.position;
            count++;
        }

        return count > 0
            ? sum / count
            : fallback;
    }

    private void FreezeAllRigidbodies(
        GameObject root)
    {
        Rigidbody[] rbs =
            root.GetComponentsInChildren<Rigidbody>(true);

        foreach (Rigidbody rb in rbs)
        {
            if (rb == null)
                continue;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    private void DisableAllColliders(
        GameObject root,
        out Collider[] colliders,
        out bool[] states)
    {
        colliders =
            root.GetComponentsInChildren<Collider>(true);

        states =
            new bool[colliders.Length];

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider c = colliders[i];

            if (c == null)
                continue;

            states[i] = c.enabled;
            c.enabled = false;
        }
    }

    private void RestoreAllColliders(
        Collider[] colliders,
        bool[] states)
    {
        if (colliders == null || states == null)
            return;

        int count =
            Mathf.Min(
                colliders.Length,
                states.Length);

        for (int i = 0; i < count; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = states[i];
        }
    }

    private void ScaleBrokenPiecesToJar(
        GameObject brokenRoot,
        Bounds intactBounds)
    {
        Renderer[] shardRenderers =
            brokenRoot.GetComponentsInChildren<Renderer>(true);

        float largestShardSize = 0f;

        foreach (Renderer r in shardRenderers)
        {
            if (r == null)
                continue;

            float size =
                Mathf.Max(
                    r.bounds.size.x,
                    r.bounds.size.y,
                    r.bounds.size.z);

            largestShardSize =
                Mathf.Max(
                    largestShardSize,
                    size);
        }

        float jarSize =
            Mathf.Max(
                intactBounds.size.x,
                intactBounds.size.y,
                intactBounds.size.z);

        if (largestShardSize <= 0.0001f
            || jarSize <= 0.0001f)
        {
            return;
        }

        float desiredLargestShardSize =
            jarSize * largestShardToJarRatio;

        float scale =
            desiredLargestShardSize
            / largestShardSize
            * brokenScaleMultiplier;

        brokenRoot.transform.localScale *=
            scale;
    }

    private bool TryGetPieceBounds(
        Transform piece,
        out Bounds bounds)
    {
        bounds = default;

        if (piece == null)
            return false;

        Renderer[] renderers =
            piece.GetComponentsInChildren<Renderer>(true);

        bool found = false;

        foreach (Renderer r in renderers)
        {
            if (r == null)
                continue;

            if (!found)
            {
                bounds = r.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(r.bounds);
            }
        }

        return found;
    }

    private bool TryGetRendererBounds(
        GameObject root,
        out Bounds bounds)
    {
        bounds = default;

        if (root == null)
            return false;

        Renderer[] renderers =
            root.GetComponentsInChildren<Renderer>(true);

        bool found = false;

        foreach (Renderer r in renderers)
        {
            if (r == null)
                continue;

            if (!found)
            {
                bounds = r.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(r.bounds);
            }
        }

        return found;
    }

    private void PlayBreakSound(
        Vector3 position)
    {
        if (breakClip == null)
            return;

        GameObject audioObject =
            new GameObject(
                "WineJar_BreakSound");

        audioObject.transform.position =
            position;

        AudioSource source =
            audioObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = spatialBlend;
        source.minDistance =
            Mathf.Max(
                0.1f,
                audioMinDistance);
        source.maxDistance =
            Mathf.Max(
                source.minDistance + 0.1f,
                audioMaxDistance);
        source.rolloffMode =
            AudioRolloffMode.Linear;
        source.volume = 1f;

        source.PlayOneShot(
            breakClip,
            Mathf.Clamp(
                breakVolume,
                0f,
                2f));

        Destroy(
            audioObject,
            Mathf.Max(
                0.5f,
                breakClip.length + 0.5f));
    }
}
