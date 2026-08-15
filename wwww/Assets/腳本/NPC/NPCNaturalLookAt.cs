using UnityEngine;

public class NPCNaturalLookAt : MonoBehaviour
{
    [Header("參考")]
    public Transform player;

    [Header("看向玩家")]
    public bool lookAtPlayer = true;
    public float lookDistance = 4f;
    public float rotateSpeed = 2f;

    [Header("回到原本方向")]
    public bool returnToOriginalRotation = true;

    private Quaternion originalRotation;

    private void Start()
    {
        // 記住 NPC 原本面向方向
        originalRotation = transform.rotation;

        // 如果沒指定玩家，自動尋找 XR Origin
        if (player == null)
        {
            GameObject xrOrigin = GameObject.Find("XR Origin (VR)");

            if (xrOrigin == null)
                xrOrigin = GameObject.Find("XR Origin");

            if (xrOrigin != null)
                player = xrOrigin.transform;
        }
    }

    private void Update()
    {
        if (!lookAtPlayer || player == null)
            return;

        Vector3 direction = player.position - transform.position;

        // 不讓 NPC 抬頭低頭，只水平轉身
        direction.y = 0f;

        float distance = direction.magnitude;

        if (distance <= lookDistance)
        {
            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation =
                    Quaternion.LookRotation(direction.normalized);

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotateSpeed * Time.deltaTime
                );
            }
        }
        else if (returnToOriginalRotation)
        {
            // 玩家離開後慢慢轉回原本方向
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                originalRotation,
                rotateSpeed * Time.deltaTime
            );
        }
    }
}