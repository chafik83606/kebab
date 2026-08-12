using UnityEngine;

/// <summary>Client PNJ : entre, fait la queue, commande, paie, sort.</summary>
public class CustomerNPC : MonoBehaviour
{
    public float walkSpeed = 1.4f;
    public float orderDuration = 3.5f;

    private Transform[] path;
    private int pathIndex;
    private float orderTimer;
    private bool ordering;
    private bool leaving;
    private Transform exitPoint;
    private System.Action onFinished;
    private CharacterLocomotion locomotion;
    private bool wasMoving;

    public void Init(Transform[] queuePath, Transform exit, Color clothing, System.Action finished)
    {
        path = queuePath;
        pathIndex = 0;
        exitPoint = exit;
        onFinished = finished;
        ordering = false;
        leaving = false;
        orderTimer = 0f;

        if (path != null && path.Length > 0)
            transform.position = path[0].position;

        locomotion = GetComponent<CharacterLocomotion>();
    }

    private void Update()
    {
        if (leaving)
        {
            SetMoving(true);
            MoveTowards(exitPoint != null ? exitPoint.position : transform.position + Vector3.right * 5f);
            if (exitPoint != null && Vector3.Distance(transform.position, exitPoint.position) < 0.4f)
            {
                onFinished?.Invoke();
                Destroy(gameObject);
            }
            return;
        }

        if (ordering)
        {
            SetMoving(false);
            orderTimer -= Time.deltaTime;
            // Petite animation d'attente
            transform.position += Vector3.up * (Mathf.Sin(Time.time * 6f) * 0.002f);
            if (orderTimer <= 0f)
            {
                ordering = false;
                leaving = true;
            }
            return;
        }

        if (path == null || pathIndex >= path.Length)
        {
            ordering = true;
            orderTimer = orderDuration;
            return;
        }

        Transform target = path[pathIndex];
        if (target == null)
        {
            pathIndex++;
            return;
        }

        MoveTowards(target.position);
        SetMoving(true);
        if (Vector3.Distance(transform.position, target.position) < 0.25f)
            pathIndex++;
    }

    private void SetMoving(bool moving)
    {
        if (locomotion == null) return;
        if (wasMoving == moving) return;
        wasMoving = moving;
        locomotion.SetWalking(moving, moving ? walkSpeed : 0f);
    }

    private void MoveTowards(Vector3 target)
    {
        Vector3 dir = target - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        Vector3 next = transform.position + dir.normalized * walkSpeed * Time.deltaTime;
        next.y = transform.position.y; // pas de flottement
        transform.position = next;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
    }
}
