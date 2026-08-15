using UnityEngine;

public class Chapter1CircleDancer : MonoBehaviour
{
    public Transform center;
    public string fallbackCenterName = "DanceTrigger";
    public bool playOnAwake = true;
    public float orbitSpeedDegrees = 18f;
    public float stepHeight = 0.06f;
    public float stepFrequency = 2.2f;
    public bool faceCenter = true;

    private bool dancing;
    private float radius;
    private float baseHeight;
    private float startAngle;
    private float phase;

    private void Start()
    {
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
        dancing = playOnAwake;
    }

    private void Update()
    {
        if (!dancing || center == null)
        {
            return;
        }

        float angle = startAngle + Time.time * orbitSpeedDegrees * Mathf.Deg2Rad;
        float bob = Mathf.Abs(Mathf.Sin(Time.time * Mathf.PI * 2f * stepFrequency + phase)) * stepHeight;
        Vector3 targetPosition = center.position + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * radius;
        targetPosition.y = baseHeight + bob;
        transform.position = targetPosition;

        if (faceCenter)
        {
            Vector3 direction = center.position - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            }
        }
    }

    public void SetDancing(bool enabled)
    {
        dancing = enabled;
    }
}
