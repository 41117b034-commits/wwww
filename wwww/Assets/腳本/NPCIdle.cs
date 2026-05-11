using UnityEngine;

public class NPCIdle : MonoBehaviour
{
    private Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();

        if (anim != null)
        {
            anim.Play("Idle");
        }
    }
}