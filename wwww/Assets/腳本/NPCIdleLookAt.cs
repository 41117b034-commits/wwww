using UnityEngine;

public class NPCIdleLookAt : MonoBehaviour
{
    [Header("Animation")]
    public string idleStateName = "Idle";

    [Header("Look At")]
    public Transform player;
    public float lookDistance = 5f;
    public float rotateSpeed = 3f;

    private Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();

        if (anim != null)
        {
            anim.Play(idleStateName);
        }
    }

    void Update()
    {
        LookAtPlayer();
    }

    void LookAtPlayer()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= lookDistance)
        {
            Vector3 direction = player.position - transform.position;
            direction.y = 0;

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    Time.deltaTime * rotateSpeed
                );
            }
        }
    }
}