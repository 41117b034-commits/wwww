using System;
using UnityEngine;

// The facial patch is small; only its triangle locations are stored here.
// The source body and its existing hand poses remain unchanged on disk.
public sealed class Chapter2FaceCut:ScriptableObject
{
    [Serializable] public class Submesh { public int[] triangleStarts; }
    public Mesh face;
    public Submesh[] removed;
}
