using UnityEngine;

public class NPCIdleLookAt : MonoBehaviour
{
    [Header("Animation")]
    public string idleStateName = "Idle";

    [Header("Look At")]
    public Transform player;
    public float lookDistance = 25f;
    public float rotateSpeed = 8f;

    [Header("方向修正")]
    public float yRotationOffset = 90f;

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
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);

                // 修正模型側面看人的問題
                targetRotation *= Quaternion.Euler(0f, yRotationOffset, 0f);

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    Time.deltaTime * rotateSpeed
                );
            }
        }
    }
}