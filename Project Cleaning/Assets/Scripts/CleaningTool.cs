using UnityEngine;

public class CleaningTool : MonoBehaviour
{
    [SerializeField] private float rayDistance = 2f;
    [SerializeField] private LayerMask cleanableLayer;
    [SerializeField] private Texture2D brush;

    private Camera cam;

    void Start() => cam = Camera.main;

    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, cleanableLayer))
            {
                transform.position = hit.point;
                transform.rotation = Quaternion.LookRotation(-hit.normal);

                CleanableSurface surface = hit.collider.GetComponent<CleanableSurface>();
                if (surface != null)
                    surface.CleanAtUV(hit.textureCoord, brush);
            }
        }
    }
}
