using UnityEngine;

// The orientation is stored in head-bone space so poses and scene scale can change.
public sealed class Chapter1FaceProfile : ScriptableObject
{
    public Chapter2FaceCut cut;
    public Vector3 origin, right, up, forward;
    public Shader bodyShader, faceShader;
}
