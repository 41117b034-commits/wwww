using UnityEngine;

// A Resources entry for one shared body mesh. Loading this reference does not
// instantiate its actor, run a chapter script or pull in the FBX's materials.
public sealed class ChapterEntryMesh : ScriptableObject
{
    public Mesh body;
}
