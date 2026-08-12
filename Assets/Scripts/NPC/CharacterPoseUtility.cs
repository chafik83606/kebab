using UnityEngine;

/// <summary>
/// Corrige la T-pose (bras en croix) quand l'animation idle est absente.
/// </summary>
public static class CharacterPoseUtility
{
    public static void ApplyRelaxedPose(Transform root)
    {
        if (root == null) return;

        ApplyArm(root, true,
            "Arm_Upper.L", "Upper Arm.L", "LeftArm", "upper_arm.L", "mixamorig:LeftArm");
        ApplyArm(root, false,
            "Arm_Upper.R", "Upper Arm.R", "RightArm", "upper_arm.R", "mixamorig:RightArm");
    }

    private static void ApplyArm(Transform root, bool isLeft, params string[] upperNames)
    {
        Transform upper = FindBone(root, upperNames);
        if (upper == null) return;

        float sign = isLeft ? 1f : -1f;
        upper.localRotation *= Quaternion.Euler(10f, sign * 8f, sign * 72f);

        Transform lower = FindBone(upper,
            isLeft ? "Arm_Lower.L" : "Arm_Lower.R",
            isLeft ? "Lower Arm.L" : "Lower Arm.R",
            isLeft ? "LeftForeArm" : "RightForeArm",
            isLeft ? "mixamorig:LeftForeArm" : "mixamorig:RightForeArm");
        if (lower != null)
            lower.localRotation *= Quaternion.Euler(sign * 18f, 0f, 0f);
    }

    public static Transform FindBone(Transform root, params string[] names)
    {
        if (root == null) return null;
        for (int i = 0; i < names.Length; i++)
        {
            var t = FindByName(root, names[i]);
            if (t != null) return t;
        }
        return null;
    }

    private static Transform FindByName(Transform root, string name)
    {
        if (root.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
            return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindByName(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }
}
