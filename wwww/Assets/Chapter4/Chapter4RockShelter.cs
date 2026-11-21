using UnityEngine;

// Only the chamber beyond the mouth counts as shelter.
[RequireComponent(typeof(BoxCollider))]
public sealed class Chapter4RockShelter : MonoBehaviour
{
    BoxCollider chamber;
    public bool Contains(Vector3 feet)
    {
        if (!chamber) chamber = GetComponent<BoxCollider>();
        Vector3 chest = feet + Vector3.up * .85f;
        return (chamber.ClosestPoint(chest) - chest).sqrMagnitude < .0001f;
    }
}
