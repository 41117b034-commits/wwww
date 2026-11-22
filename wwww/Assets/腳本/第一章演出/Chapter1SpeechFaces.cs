using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class Chapter1SpeechFaces : MonoBehaviour
{
    private readonly List<Material> ownedMaterials = new List<Material>();
    private Chapter2SpeechFace face;

    public static Chapter2SpeechFace For(Transform actor)
    {
        if (!actor) return null;
        if (actor.TryGetComponent(out Chapter1SpeechFaces installed)) return installed.face;
        if (actor.TryGetComponent(out Chapter2SpeechFace existing)) return existing;
        return actor.gameObject.AddComponent<Chapter1SpeechFaces>().Install();
    }

    private Chapter2SpeechFace Install()
    {
        Animator animator = GetComponentInChildren<Animator>(true);
        Transform head = Chapter1WeddingRigBones.Resolve(animator, HumanBodyBones.Head, "Head", "頭");
        if (!head) return null;
        var surfaces = new List<Chapter2SpeechFace.Surface>();
        foreach (SkinnedMeshRenderer body in animator.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (!body.sharedMesh || Array.IndexOf(body.bones, head) < 0) continue;
            string meshName = body.sharedMesh.name;
            const string gripSuffix = " incident grip";
            if (meshName.EndsWith(gripSuffix, StringComparison.Ordinal))
                meshName = meshName.Substring(0, meshName.Length - gripSuffix.Length);
            var profile = Resources.Load<Chapter1FaceProfile>("WeddingFaces/" + meshName);
            if (!profile || !profile.cut || !profile.cut.face || !profile.bodyShader || !profile.faceShader) continue;
            var patchObject = new GameObject("Wedding speech face");
            patchObject.layer = body.gameObject.layer;
            patchObject.transform.SetParent(body.transform, false);
            var patch = patchObject.AddComponent<SkinnedMeshRenderer>();
            patch.sharedMesh = profile.cut.face;
            patch.bones = body.bones;
            patch.rootBone = body.rootBone;
            patch.localBounds = body.localBounds;
            patch.updateWhenOffscreen = body.updateWhenOffscreen;
            patch.renderingLayerMask = body.renderingLayerMask;
            Material[] originals = body.sharedMaterials;
            patch.sharedMaterials = SpeechMaterials(originals, profile.faceShader, 2599);
            body.sharedMaterials = SpeechMaterials(originals, profile.bodyShader, 2600);
            var overrides = new MaterialPropertyBlock();
            body.GetPropertyBlock(overrides);
            patch.SetPropertyBlock(overrides);
            surfaces.Add(new Chapter2SpeechFace.Surface
            {
                body = body, renderer = patch, cut = profile.cut, head = head,
                origin = profile.origin, right = profile.right, up = profile.up, forward = profile.forward
            });
        }
        if (surfaces.Count == 0) return null;
        face = gameObject.AddComponent<Chapter2SpeechFace>();
        face.surfaces = surfaces.ToArray();
        return face;
    }

    private Material[] SpeechMaterials(Material[] originals, Shader shader, int queue)
    {
        var materials = new Material[originals.Length];
        for (int i = 0; i < originals.Length; i++)
        {
            if (!originals[i]) continue;
            var material = new Material(originals[i]) { shader = shader, renderQueue = queue };
            materials[i] = material;
            ownedMaterials.Add(material);
        }
        return materials;
    }

    private void OnDestroy()
    {
        foreach (Material material in ownedMaterials) if (material) Destroy(material);
    }
}
