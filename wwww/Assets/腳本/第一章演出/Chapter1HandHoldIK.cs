using UnityEngine;

/// <summary>
/// 必須和 Animator 掛在同一個 GameObject。
/// Chapter1CircleDancer 會自動建立並指定 owner。
/// </summary>
public class Chapter1HandHoldIK : MonoBehaviour
{
    public Chapter1CircleDancer owner;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (animator == null || owner == null)
        {
            return;
        }

        owner.ApplyHandHoldingIK(animator);
    }
}
