using Unity.VisualScripting;
using UnityEngine;

enum CollisionToolsType
{
    Texture,
    Mesh,
}

public class ToolColision : MonoBehaviour
{
    [SerializeField] private CollisionToolsType toolType = CollisionToolsType.Mesh;
    [SerializeField] private string removableTag = "Dirts";
    [SerializeField] private Texture2D brush;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(removableTag))
        {
            switch (toolType)
            {
                case CollisionToolsType.Mesh:
                    DestroyMeshDirts(collision.gameObject);
                    break;
                case CollisionToolsType.Texture:
                    TryCleanTexture(collision);
                    break;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(removableTag))
        {
            switch (toolType)
            {
                case CollisionToolsType.Mesh:
                    DestroyMeshDirts(other.gameObject);
                    break;
                case CollisionToolsType.Texture:
                    TryCleanTexture(other);
                    break;
            }
        }
    }

    private void DestroyMeshDirts(GameObject gameObject)
    {
        Destroy(gameObject);
        Debug.Log($"Removed object: {gameObject.name}");
    }

    private void CleanTexture()
    {

    }

    private void TryCleanTexture(Collision collision)
    {
        var renderer = collision.gameObject.GetComponent<Renderer>();
        if (renderer == null) return;

        var clean = renderer.GetComponent<Clean>();
        if (clean == null) return;

        Vector3 hitPoint = collision.contacts[0].point;
        CleanAtPoint(clean, renderer, hitPoint);
    }

    private void TryCleanTexture(Collider collider)
    {
        var renderer = collider.GetComponent<Renderer>();
        if (renderer == null) return;

        var clean = renderer.GetComponent<Clean>();
        if (clean == null) return;

        Vector3 hitPoint = collider.ClosestPoint(transform.position);
        CleanAtPoint(clean, renderer, hitPoint);
    }

    private void CleanAtPoint(Clean clean, Renderer renderer, Vector3 hitPoint)
    {
        Vector3 dir = (hitPoint - transform.position).normalized;
        Ray ray = new Ray(transform.position, dir);
        float maxDistance = 5f;

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
        {
            if (hit.collider != null && hit.collider.gameObject == renderer.gameObject)
            {
                clean.CleanAt(hit.textureCoord, brush);
            }
        }
    }

}
