using UnityEngine;
internal static class Chapter1WeddingRigBones
{
    public static Transform Resolve(
        Animator animator,
        HumanBodyBones humanoidBone,
        params string[] genericNames)
    {
        if (animator == null)
        {
            return null;
        }

        if (animator.avatar != null
            && animator.avatar.isValid
            && animator.isHuman)
        {
            Transform humanoidTransform = animator.GetBoneTransform(humanoidBone);
            if (humanoidTransform != null)
            {
                return humanoidTransform;
            }
        }

        Transform[] transforms =
            animator.transform.GetComponentsInChildren<Transform>(true);
        for (int nameIndex = 0; nameIndex < genericNames.Length; nameIndex++)
        {
            string expected = Normalize(genericNames[nameIndex]);
            for (int transformIndex = 0;
                transformIndex < transforms.Length;
                transformIndex++)
            {
                Transform candidate = transforms[transformIndex];
                if (candidate != null && Normalize(candidate.name) == expected)
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private static string Normalize(string value)
    {
        return string.IsNullOrEmpty(value)
            ? string.Empty
            : value.ToLowerInvariant()
                .Replace("mixamorig:", string.Empty)
                .Replace("_", string.Empty)
                .Replace(" ", string.Empty);
    }
}