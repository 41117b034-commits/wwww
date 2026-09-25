using UnityEngine;

// A short, opaque vestibule extends in front of the original closed hut facade.
public sealed class Chapter1HutDoor : MonoBehaviour
{
    public Transform hinge;
    [HideInInspector] public Mesh originalFacadeMesh;
    public float openAngle = -96f;
    public float OpenAmount { get; private set; }
    public Vector3 Threshold => transform.TransformPoint(new Vector3(0f, 0.7f, -0.8f));

    public void SetOpen(float amount)
    {
        OpenAmount = Mathf.Clamp01(amount);
        if (hinge != null) hinge.localRotation = Quaternion.Euler(0f, openAngle * OpenAmount, 0f);
    }

    public static Chapter1HutDoor UpgradeExisting(GameObject entry, Material timber)
    {
        var door = entry.GetComponent<Chapter1HutDoor>();
        if (door == null) door = entry.AddComponent<Chapter1HutDoor>();
        if (door.hinge != null) return door;
        Transform shadow = entry.transform.Find("Shadowed doorway");
        if (shadow != null) shadow.localPosition = new Vector3(0f, 9.4f, 9.8f);
        for (int i = 0; i < 6; i++)
        {
            Transform line = entry.transform.Find("Interior plank " + i);
            if (line != null) line.gameObject.SetActive(false);
        }
        var pivot = new GameObject("Door hinge");
        pivot.transform.SetParent(entry.transform, false);
        pivot.transform.localPosition = new Vector3(-5.05f, 0.7f, -0.85f);
        door.hinge = pivot.transform;
        for (int i = 0; i < 8; i++)
            Box(pivot.transform, "Door plank " + (i + 1),
                new Vector3(0.635f + i * 1.26f, 9.25f, 0f), new Vector3(1.275f, 18.5f, 0.38f), timber);
        Box(pivot.transform, "Lower brace", new Vector3(5.05f, 4f, -0.28f), new Vector3(9.6f, 0.6f, 0.3f), timber);
        Box(pivot.transform, "Upper brace", new Vector3(5.05f, 14.4f, -0.28f), new Vector3(9.6f, 0.6f, 0.3f), timber);
        Box(pivot.transform, "Wooden handle", new Vector3(9.1f, 8.8f, -0.55f), new Vector3(0.35f, 1.45f, 0.5f), timber);
        Box(entry.transform, "Entry floor", new Vector3(0f, 0.35f, 4.6f), new Vector3(10.2f, 0.7f, 9.8f), timber);
        door.SetOpen(0f);
        return door;
    }

    static void Box(Transform parent, string label, Vector3 position, Vector3 size, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = label;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = size;
        part.GetComponent<Renderer>().sharedMaterial = material;
    }
}
