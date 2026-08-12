using UnityEngine;

/// <summary>Employé PNJ derrière le comptoir : coupe, sert, bouge un peu.</summary>
public class EmployeeNPC : MonoBehaviour
{
    public float workAnimSpeed = 3f;
    public bool isDeclared = true;

    private Vector3 basePos;
    private float phase;

    public void Init(Transform slot, bool declared, string employeeName)
    {
        isDeclared = declared;
        basePos = slot != null ? slot.position : transform.position;
        transform.position = basePos;
        phase = Random.Range(0f, Mathf.PI * 2f);
        transform.rotation = Quaternion.Euler(0f, 180f, 0f);
    }

    private void Update()
    {
        phase += Time.deltaTime * workAnimSpeed;
        float bob = Mathf.Sin(phase) * 0.04f;
        transform.position = basePos + new Vector3(0f, bob, 0f);
        transform.localRotation = Quaternion.Euler(0f, 180f + Mathf.Sin(phase * 0.7f) * 6f, 0f);
    }
}
