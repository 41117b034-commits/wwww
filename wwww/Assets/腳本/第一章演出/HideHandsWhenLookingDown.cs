using UnityEngine;

/// <summary>
/// 玩家低頭時隱藏手模型，只關 Renderer，不停用 XR / 手部互動。
/// 掛在 XR Origin (VR) 或 Hands_Rigged 都可以。
/// </summary>
public class HideHandsWhenLookingDown : MonoBehaviour
{
    [Header("References")]
    [Tooltip("玩家頭部相機。留空會自動找 Main Camera。")]
    public Camera playerCamera;

    [Tooltip("手模型根物件，例如 Hands_Rigged。")]
    public Transform handsRoot;

    [Header("Hide Settings")]
    [Tooltip("相機往下看的程度。0=水平，1=正下方。建議 0.45~0.65。")]
    [Range(0f, 1f)]
    public float hideWhenLookingDownDot = 0.52f;

    [Tooltip("往上看回多少才重新顯示，避免臨界點閃爍。")]
    [Range(0f, 1f)]
    public float showAgainDot = 0.38f;

    [Tooltip("只隱藏 Renderer，不停用整個 Hands_Rigged。建議保持勾選。")]
    public bool rendererOnly = true;

    private Renderer[] handRenderers;
    private bool handsHidden;

    private void Awake()
    {
        ResolveReferences();
        CacheRenderers();
        SetHandsVisible(true);
    }

    private void Start()
    {
        ResolveReferences();
        CacheRenderers();
    }

    private void LateUpdate()
    {
        ResolveReferences();

        if (playerCamera == null || handsRoot == null)
            return;

        if (handRenderers == null || handRenderers.Length == 0)
            CacheRenderers();

        // forward 與 Vector3.down 的點積：
        // 0 = 水平看，1 = 完全正下方。
        float downDot = Vector3.Dot(
            playerCamera.transform.forward.normalized,
            Vector3.down);

        if (!handsHidden && downDot >= hideWhenLookingDownDot)
        {
            SetHandsVisible(false);
        }
        else if (handsHidden && downDot <= showAgainDot)
        {
            SetHandsVisible(true);
        }
    }

    private void ResolveReferences()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (handsRoot == null)
        {
            GameObject found = GameObject.Find("Hands_Rigged");
            if (found != null)
                handsRoot = found.transform;
        }
    }

    private void CacheRenderers()
    {
        if (handsRoot == null)
            return;

        handRenderers = handsRoot.GetComponentsInChildren<Renderer>(true);
    }

    private void SetHandsVisible(bool visible)
    {
        if (handsRoot == null)
            return;

        if (rendererOnly)
        {
            if (handRenderers == null || handRenderers.Length == 0)
                CacheRenderers();

            if (handRenderers != null)
            {
                foreach (Renderer r in handRenderers)
                {
                    if (r != null)
                        r.enabled = visible;
                }
            }
        }
        else
        {
            handsRoot.gameObject.SetActive(visible);
        }

        handsHidden = !visible;
    }

    [ContextMenu("Test Hide Hands")]
    public void TestHideHands()
    {
        SetHandsVisible(false);
    }

    [ContextMenu("Test Show Hands")]
    public void TestShowHands()
    {
        SetHandsVisible(true);
    }
}
