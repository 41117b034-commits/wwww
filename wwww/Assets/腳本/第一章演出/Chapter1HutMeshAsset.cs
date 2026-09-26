using UnityEngine;

// The main asset controls serialization for its mesh subasset, even when the
// project uses Force Text. Keep the generated hut mesh lossless and compact.
[PreferBinarySerialization]
public sealed class Chapter1HutMeshAsset : ScriptableObject
{
    public Mesh mesh;
}
