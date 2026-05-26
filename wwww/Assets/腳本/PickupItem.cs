using UnityEngine;

public class PickupItem : MonoBehaviour
{
    [Header("拿取設定")]
    public Transform holdPoint;
    public float pickupRange = 10f;
    public KeyCode pickupKey = KeyCode.E;

    private GameObject heldObject;
    private Rigidbody heldRb;

    void Update()
    {
        if (Input.GetKeyDown(pickupKey))
        {
            Debug.Log("按下 E");

            if (heldObject == null)
            {
                TryPickup();
            }
            else
            {
                DropObject();
            }
        }

        if (heldObject != null && holdPoint != null)
        {
            heldObject.transform.position = holdPoint.position;
            heldObject.transform.rotation = holdPoint.rotation;
        }
    }

    void TryPickup()
    {
        if (holdPoint == null)
        {
            Debug.LogWarning("HoldPoint 尚未指定！");
            return;
        }

        if (Camera.main == null)
        {
            Debug.LogWarning("找不到 Main Camera，請確認攝影機 Tag 是 MainCamera！");
            return;
        }

        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        RaycastHit hit;

        Debug.DrawRay(ray.origin, ray.direction * pickupRange, Color.red, 2f);

        if (Physics.Raycast(ray, out hit, pickupRange))
        {
            Debug.Log("射線打到：" + hit.collider.name + "，Tag：" + hit.collider.tag);

            if (hit.collider.CompareTag("Pickup"))
            {
                heldObject = hit.collider.gameObject;
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
            else
            {
                Debug.LogWarning("打到的物件不是 Pickup 標籤！");
            }
        }
        else
        {
            Debug.LogWarning("射線沒有打到任何物件，請把畫面中心對準物品！");
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