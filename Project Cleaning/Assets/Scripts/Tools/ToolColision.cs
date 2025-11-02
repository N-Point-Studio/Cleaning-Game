using UnityEngine;

public class ToolCollision : MonoBehaviour
{
    public enum CollisionToolsType
    {
        Texture,
        Mesh,
    }

    [SerializeField] private CollisionToolsType toolType = CollisionToolsType.Texture;
    [SerializeField] private string removableTag = "Dirts";
    [SerializeField] private Texture2D brush;
    [SerializeField] private Transform tipPointPos;
    [SerializeField] private float rayDistance = 10f;

    private void Update()
    {
        if (toolType != CollisionToolsType.Texture || tipPointPos == null) return;
        Ray ray = new Ray(tipPointPos.position, tipPointPos.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance))
        {
            var targetRenderer = hit.collider.GetComponent<Renderer>();
            if (targetRenderer == null) return;

            if (hit.collider.CompareTag(removableTag))
            {
                var clean = hit.collider.GetComponent<Clean>();
                if (clean != null)
                {
                    clean.CleanAt(hit.textureCoord, brush);
                    Vector2 uv = hit.textureCoord;
                    Debug.Log($"[TipPoint Raycast] Cleaned {hit.collider.name}, UV: {uv.x:F3}, {uv.y:F3}");
                }
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleCollision(collision.gameObject, collision.contacts);
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleCollision(other.gameObject, null);
    }

    private void HandleCollision(GameObject target, ContactPoint[] contacts)
    {
        if (!target.CompareTag(removableTag)) return;

        switch (toolType)
        {
            case CollisionToolsType.Mesh:
                Destroy(target);
                Debug.Log($"Removed object: {target.name}");
                break;

            case CollisionToolsType.Texture:
                var clean = target.GetComponent<Clean>();
                if (clean == null) return;

                // Jika ada contact points (dari OnCollision), gunakan itu
                if (contacts != null && contacts.Length > 0)
                {
                    foreach (var contact in contacts)
                    {
                        TryCleanAtContact(clean, contact);
                    }
                }
                else
                {
                    // Jika OnTrigger, lakukan raycast dari tipPoint ke target
                    TryCleanAtTrigger(clean, target);
                }
                break;
        }
    }

    private void TryCleanAtContact(Clean clean, ContactPoint contact)
    {
        var meshCollider = contact.thisCollider as MeshCollider;
        if (meshCollider == null || meshCollider.convex) return;

        Ray ray = new Ray(contact.point + contact.normal * 0.001f, -contact.normal);
        if (meshCollider.Raycast(ray, out RaycastHit hit, 0.01f))
        {
            clean.CleanAt(hit.textureCoord, brush);
            Debug.Log("Cleaned at UV: " + hit.textureCoord);
        }
    }

    private void TryCleanAtTrigger(Clean clean, GameObject target)
    {
        Vector3 dir = (target.transform.position - tipPointPos.position).normalized;
        Ray ray = new Ray(tipPointPos.position, dir);
        float maxDistance = 5f;

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
        {
            if (hit.collider != null && hit.collider.gameObject == target)
            {
                clean.CleanAt(hit.textureCoord, brush);
                Debug.Log("Cleaned at trigger UV: " + hit.textureCoord);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (tipPointPos == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(tipPointPos.position, tipPointPos.position + tipPointPos.forward * 10f);
    }
}
