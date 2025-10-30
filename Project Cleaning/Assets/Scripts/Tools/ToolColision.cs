using UnityEngine;

public class ToolColision : MonoBehaviour
{
    [SerializeField] private string removableTag = "Dirts";

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("ADA");
        if (collision.gameObject.CompareTag(removableTag))
        {
            Destroy(collision.gameObject);
            Debug.Log($"Removed object: {collision.gameObject.name}");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("ADA");
        if (other.CompareTag(removableTag))
        {
            Destroy(other.gameObject);
            Debug.Log($"Removed object: {other.gameObject.name}");
        }
    }
}
