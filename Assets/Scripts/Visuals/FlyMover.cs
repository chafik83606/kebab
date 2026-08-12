using UnityEngine;

/// <summary>
/// Animation simple de va-et-vient pour les mouches individuelles.
/// À placer sur chaque Fly.prefab.
/// </summary>
public class FlyMover : MonoBehaviour
{
    [SerializeField] private float speed = 2f;
    [SerializeField] private float radius = 0.4f;
    [SerializeField] private float heightBob = 0.15f;

    private Vector3 origin;
    private float seed;

    private void OnEnable()
    {
        origin = transform.localPosition;
        seed = Random.Range(0f, 100f);
    }

    private void Update()
    {
        float t = Time.time * speed + seed;
        transform.localPosition = origin + new Vector3(
            Mathf.Sin(t) * radius,
            Mathf.Sin(t * 1.7f) * heightBob,
            Mathf.Cos(t * 0.8f) * radius
        );
    }
}
