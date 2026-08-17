using UnityEngine;

public class Chapter1CircleDancer : MonoBehaviour
{
    [Header("是否參加繞圈舞")]
    [Tooltip("任務 NPC 請取消勾選。即使其他腳本呼叫 SetDancing(true)，也不會再開始繞圈。")]
    public bool canCircleDance = true;

    [Tooltip("canCircleDance 關閉時，強制切回 Idle 動畫。")]
    public bool forceIdleWhenExcluded = true;

    public Animator animator;
    public string idleStateName = "Idle";

    [Header("繞圈設定")]
    public Transform center;
    public string fallbackCenterName = "DanceTrigger";
    public bool playOnAwake = true;
    public float orbitSpeedDegrees = 18f;
    public float stepHeight = 0.06f;
    public float stepFrequency = 2.2f;
    public float radialStepDistance = 0.08f;
    public float swayDegrees = 4f;
    public float swayFrequency = 1.15f;

    [Range(0f, 0.35f)]
    public float individualSpeedVariation = 0.1f;

    public bool faceCenter = true;

    private bool dancing;
    private float radius;
    private float baseHeight;
    private float startAngle;
    private float phase;
    private float speedScale = 1f;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }
    }

    private void Start()
    {
        if (!canCircleDance)
        {
            dancing = false;
            ForceIdle();
            return;
        }

        if (center == null)
        {
            GameObject fallbackCenter = GameObject.Find(fallbackCenterName);

            if (fallbackCenter != null)
            {
                center = fallbackCenter.transform;
            }
        }

        if (center == null)
        {
            dancing = false;
            enabled = false;
            return;
        }

        Vector3 offset = transform.position - center.position;
        offset.y = 0f;

        if (offset.sqrMagnitude < 0.25f)
        {
            offset = transform.forward * 2f;
        }

        radius = offset.magnitude;
        baseHeight = transform.position.y;
        startAngle = Mathf.Atan2(offset.x, offset.z);
        phase = Random.Range(0f, Mathf.PI * 2f);

        speedScale =
            1f + Random.Range(
                -individualSpeedVariation,
                individualSpeedVariation
            );

        dancing = playOnAwake && canCircleDance;
    }

    private void Update()
    {
        if (!canCircleDance)
        {
            dancing = false;
            return;
        }

        if (!dancing || center == null)
        {
            return;
        }

        float angle =
            startAngle
            + Time.time
            * orbitSpeedDegrees
            * speedScale
            * Mathf.Deg2Rad;

        float stepRhythm =
            Mathf.Sin(
                Time.time
                * Mathf.PI
                * 2f
                * stepFrequency
                + phase
            );

        float bob =
            Mathf.Abs(stepRhythm)
            * stepHeight;

        float radialStep =
            Mathf.Sin(
                Time.time
                * Mathf.PI
                * stepFrequency
                + phase
            )
            * radialStepDistance;

        float currentRadius =
            Mathf.Max(
                0.5f,
                radius + radialStep
            );

        Vector3 targetPosition =
            center.position
            + new Vector3(
                Mathf.Sin(angle),
                0f,
                Mathf.Cos(angle)
            )
            * currentRadius;

        targetPosition.y =
            baseHeight + bob;

        transform.position =
            targetPosition;

        if (faceCenter)
        {
            Vector3 direction =
                center.position
                - transform.position;

            direction.y = 0f;

            if (direction.sqrMagnitude > 0.01f)
            {
                float sway =
                    Mathf.Sin(
                        Time.time
                        * Mathf.PI
                        * 2f
                        * swayFrequency
                        + phase
                    )
                    * swayDegrees;

                Quaternion facing =
                    Quaternion.LookRotation(
                        direction.normalized,
                        Vector3.up
                    );

                transform.rotation =
                    facing
                    * Quaternion.Euler(
                        0f,
                        0f,
                        sway
                    );
            }
        }
    }

    public void SetDancing(bool shouldDance)
    {
        if (!canCircleDance)
        {
            dancing = false;
            ForceIdle();
            return;
        }

        dancing = shouldDance;
    }

    public void StopDancing()
    {
        dancing = false;

        if (!canCircleDance)
        {
            ForceIdle();
        }
    }

    public void SetCanCircleDance(bool allowed)
    {
        canCircleDance = allowed;

        if (!allowed)
        {
            dancing = false;
            ForceIdle();
        }
    }

    private void ForceIdle()
    {
        if (!forceIdleWhenExcluded)
        {
            return;
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }

        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return;
        }

        animator.applyRootMotion = false;

        if (string.IsNullOrWhiteSpace(idleStateName))
        {
            return;
        }

        int idleHash =
            Animator.StringToHash(
                idleStateName.Trim()
            );

        if (animator.HasState(0, idleHash))
        {
            animator.CrossFade(
                idleHash,
                0.15f,
                0
            );
        }
        else
        {
            Debug.LogWarning(
                "[Chapter1CircleDancer] "
                + gameObject.name
                + " 找不到 Animator State："
                + idleStateName
            );
        }
    }
}