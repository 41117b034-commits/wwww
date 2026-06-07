using UnityEngine;

public class PickupItem : MonoBehaviour
{
    [Header("拿取設定")]
    public Transform holdPoint;
    public Transform cameraTransform;
    public float pickupRange = 15f;
    public KeyCode pickupKey = KeyCode.F;

    [Header("旋轉設定")]
    public float rotateSpeed = 90f;
    public KeyCode rotateLeftKey = KeyCode.R;
    public KeyCode rotateRightKey = KeyCode.T;
    public KeyCode rotateUpKey = KeyCode.Y;
    public KeyCode rotateDownKey = KeyCode.U;

    private GameObject heldObject;
    private Rigidbody heldRb;

    void Update()
    {
        if (Input.GetKeyDown(pickupKey))
        {
            Debug.Log("按下 F");

            if (heldObject == null)
            {
                TryPickupNearby();
            }
            else
            {
                DropObject();
            }
        }

        if (heldObject != null && holdPoint != null)
        {
            HoldObject();
            RotateHeldObject();
        }
    }

    void TryPickupNearby()
    {
        if (holdPoint == null)
        {
            Debug.LogWarning("HoldPoint 尚未指定！");
            return;
        }

        if (cameraTransform == null)
        {
            Debug.LogWarning("Camera Transform 尚未指定！");
            return;
        }

        Collider[] hits = Physics.OverlapSphere(cameraTransform.position, pickupRange);

        GameObject nearestObject = null;
        float nearestDistance = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            Debug.Log("附近偵測到：" + hit.name + "，Tag：" + hit.tag);

            if (hit.CompareTag("Pickup"))
            {
                float distance = Vector3.Distance(cameraTransform.position, hit.transform.position);

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestObject = hit.gameObject;
                }
            }
        }

        if (nearestObject == null)
        {
            Debug.LogWarning("附近沒有 Pickup 物品！");
            return;
        }

        heldObject = nearestObject;
        heldRb = heldObject.GetComponent<Rigidbody>();

        if (heldRb != null)
        {
            heldRb.useGravity = false;
            heldRb.isKinematic = true;
        }

        heldObject.transform.SetParent(holdPoint);
        heldObject.transform.localPosition = Vector3.zero;
        heldObject.transform.localRotation = Quaternion.identity;

        Debug.Log("拿起物品：" + heldObject.name);
    }

    void HoldObject()
    {
        heldObject.transform.position = holdPoint.position;
    }

    void RotateHeldObject()
    {
        if (Input.GetKey(rotateLeftKey))
        {
            heldObject.transform.Rotate(Vector3.up, -rotateSpeed * Time.deltaTime, Space.World);
        }

        if (Input.GetKey(rotateRightKey))
        {
            heldObject.transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
        }

        if (Input.GetKey(rotateUpKey))
        {
            heldObject.transform.Rotate(Vector3.right, rotateSpeed * Time.deltaTime, Space.World);
        }

        if (Input.GetKey(rotateDownKey))
        {
            heldObject.transform.Rotate(Vector3.right, -rotateSpeed * Time.deltaTime, Space.World);
        }
    }

    void DropObject()
    {
        if (heldObject == null) return;

        heldObject.transform.SetParent(null);

        if (heldRb != null)
        {
            heldRb.isKinematic = false;
            heldRb.useGravity = true;
        }

        Debug.Log("放下物品：" + heldObject.name);

        heldObject = null;
        heldRb = null;
    }
}