using UnityEngine;

/// <summary>
/// Bascule Idle / Walk + synchronise la vitesse d'anim avec le déplacement.
/// </summary>
[RequireComponent(typeof(Animator))]
public class CharacterLocomotion : MonoBehaviour
{
    [Tooltip("Vitesse monde attendue pour l'anim Walk à speed=1")]
    public float referenceWalkSpeed = 1.35f;

    private Animator animator;
    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
    private bool walking;

    public void Configure(Animator anim)
    {
        animator = anim;
        if (animator != null)
            animator.applyRootMotion = false;
    }

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    public void SetWalking(bool isWalking, float moveSpeed = -1f)
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        if (animator == null || !animator.isActiveAndEnabled) return;

        walking = isWalking;
        animator.SetBool(IsWalkingHash, isWalking);

        if (isWalking && moveSpeed > 0f && referenceWalkSpeed > 0.01f)
            animator.speed = Mathf.Clamp(moveSpeed / referenceWalkSpeed, 0.7f, 1.6f);
        else
            animator.speed = 1f;
    }

    public bool IsWalking => walking;
}
