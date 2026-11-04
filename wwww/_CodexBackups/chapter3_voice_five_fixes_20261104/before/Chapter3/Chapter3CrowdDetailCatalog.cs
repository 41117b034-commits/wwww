using System;
using UnityEngine;

// Authored background-only derivatives; the original meshes remain the near view.
public sealed class Chapter3CrowdDetailCatalog : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        public Mesh source, distant, posedDistant;
        public int sourceVertices, sourceBones;
    }
    public bool automatic;
    public float enterDistance=12f, leaveDistance=9f;
    public string simplifierCommit;
    public Entry[] entries=Array.Empty<Entry>();
}
