using UnityEngine;

// The mouth breaks pursuit. Survivors gather on the left-hand forest path beyond the rear exit.
[RequireComponent(typeof(BoxCollider))]
public sealed class Chapter4RockShelter : MonoBehaviour
{
    public BoxCollider entrance, bend, rearTrail, farHollow, forestBend, rally;
    public Transform[] escapeTrail;
    BoxCollider chamber;

    public bool Contains(Vector3 feet)
    {
        if (!chamber) chamber = GetComponent<BoxCollider>();
        return Inside(chamber, feet);
    }

    public bool AtRefuge(Vector3 feet) => rally ? Inside(rally, feet) : Contains(feet);
    public bool Sheltered(Vector3 feet) => Contains(feet) || Inside(entrance, feet) || Inside(bend, feet) ||
        Inside(rearTrail, feet) || Inside(farHollow, feet) || Inside(forestBend, feet) || Inside(rally, feet);

    static bool Inside(BoxCollider volume, Vector3 feet)
    {
        if (!volume) return false;
        Vector3 chest = feet + Vector3.up * .85f;
        return (volume.ClosestPoint(chest) - chest).sqrMagnitude < .0001f;
    }
}
