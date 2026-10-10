using UnityEngine;

// A second-chapter-only mesh with an authored fist blend shape. The shared
// character importers and first-chapter pose calibration stay unchanged.
public sealed class Chapter2GriefHandPose : ScriptableObject
{
    public Mesh mesh;
    public Quaternion leftPalm,rightPalm;
    public int fistShape;
}
