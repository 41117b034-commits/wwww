using UnityEngine;

public class Chapter1AnimatorCue : MonoBehaviour
{
    public Animator animator;

    public void SetTrigger(string triggerName)
    {
        if (animator != null && !string.IsNullOrEmpty(triggerName))
        {
            animator.SetTrigger(triggerName);
        }
    }

    public void SetBoolTrue(string boolName)
    {
        if (animator != null && !string.IsNullOrEmpty(boolName))
        {
            animator.SetBool(boolName, true);
        }
    }

    public void SetBoolFalse(string boolName)
    {
        if (animator != null && !string.IsNullOrEmpty(boolName))
        {
            animator.SetBool(boolName, false);
        }
    }
}
