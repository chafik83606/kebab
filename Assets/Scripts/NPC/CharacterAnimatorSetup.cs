using UnityEngine;

/// <summary>
/// Lance idle / walk humanoid, ou pose de secours.
/// </summary>
public static class CharacterAnimatorSetup
{
    private static CharacterAnimCatalog catalog;

    public static void Setup(GameObject character, string prefabKey = null)
    {
        if (character == null) return;

        string key = !string.IsNullOrEmpty(prefabKey) ? prefabKey : character.name;
        key = key.Replace("(Clone)", "").Trim();

        if (TrySetupHumanoid(character, key))
            return;

        // CharCrafter / Generic : idle dédié, pas de retarget humanoid (sinon glissement)
        string norm = key.Replace(" ", "_");
        if (TryPlayLegacyClip(character, norm + "_Idle"))
            return;

        CharacterPoseUtility.ApplyRelaxedPose(character.transform);
        if (character.GetComponent<CharacterIdleSway>() == null)
            character.AddComponent<CharacterIdleSway>();
    }

    private static bool TrySetupHumanoid(GameObject character, string prefabKey)
    {
        if (catalog == null)
            catalog = Resources.Load<CharacterAnimCatalog>("CharacterAnimCatalog");
        if (catalog == null) return false;

        var controller = catalog.GetControllerFor(prefabKey);
        if (controller == null) return false;

        var animator = character.GetComponent<Animator>();
        if (animator == null) animator = character.AddComponent<Animator>();

        // TOUJOURS préférer l'avatar du prefab s'il est humanoid
        Avatar own = animator.avatar;
        Avatar chosen = null;
        if (own != null && own.isValid && own.isHuman)
            chosen = own;
        else
            chosen = catalog.GetAvatarFor(prefabKey);

        // Ne jamais coller un avatar femme sur un homme (et inversement)
        if (chosen == null || !chosen.isValid || !chosen.isHuman)
            return false;

        bool femaleChar = CharacterAnimCatalog.IsFemaleName(prefabKey);
        if (femaleChar && chosen.name.IndexOf("Male", System.StringComparison.OrdinalIgnoreCase) >= 0
            && chosen.name.IndexOf("Female", System.StringComparison.OrdinalIgnoreCase) < 0)
            return false;
        if (!femaleChar && chosen.name.IndexOf("Female", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return false;

        animator.runtimeAnimatorController = controller;
        animator.avatar = chosen;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.speed = 1f;
        animator.SetBool("IsWalking", false);

        var loco = character.GetComponent<CharacterLocomotion>();
        if (loco == null) loco = character.AddComponent<CharacterLocomotion>();
        loco.Configure(animator);

        InteriorEnvironmentSetup.FixMaterials(character.transform);
        return true;
    }

    private static bool TryPlayLegacyClip(GameObject character, string resourceName)
    {
        var clip = Resources.Load<AnimationClip>("CharacterAnims/" + resourceName);
        if (clip == null)
            clip = Resources.Load<AnimationClip>("CharacterAnims/Humanoid_Idle");
        if (clip == null) return false;

        // Désactive Animator humanoid cassé pour laisser le legacy jouer
        var animator = character.GetComponent<Animator>();
        if (animator != null)
        {
            animator.runtimeAnimatorController = null;
            animator.enabled = false;
        }

        var anim = character.GetComponent<Animation>();
        if (anim == null) anim = character.AddComponent<Animation>();
        anim.playAutomatically = true;
        anim.wrapMode = WrapMode.Loop;
        anim.cullingType = AnimationCullingType.AlwaysAnimate;
        if (anim.GetClip("Idle") == null)
            anim.AddClip(clip, "Idle");
        anim.Play("Idle");
        return true;
    }
}

/// <summary>Léger mouvement idle si pas d'animation clip.</summary>
public class CharacterIdleSway : MonoBehaviour
{
    public float swayAmount = 1.2f;
    public float breatheAmount = 0.012f;
    public float speed = 1.6f;

    private Vector3 basePos;
    private Quaternion baseRot;
    private float phase;

    private void Start()
    {
        basePos = transform.position;
        baseRot = transform.rotation;
        phase = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        phase += Time.deltaTime * speed;
        float sway = Mathf.Sin(phase) * swayAmount;
        float breath = Mathf.Sin(phase * 2.1f) * breatheAmount;
        transform.position = basePos + new Vector3(0f, breath, 0f);
        transform.rotation = baseRot * Quaternion.Euler(0f, sway, 0f);
    }

    public void Recalibrate()
    {
        basePos = transform.position;
        baseRot = transform.rotation;
    }
}
