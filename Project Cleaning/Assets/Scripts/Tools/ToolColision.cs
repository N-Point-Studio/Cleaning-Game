using UnityEngine;
using System.Collections.Generic;

public class ToolCollision : MonoBehaviour
{
    public enum CollisionToolsType
    {
        Texture,
        Mesh,
    }

    [Header("Settings")]
    [SerializeField] private CollisionToolsType toolType = CollisionToolsType.Texture;
    [SerializeField] private string removableTag = "Dirts";
    [SerializeField] private Texture2D brush;
    [SerializeField] private Transform tipPointPos;
    [SerializeField] private float rayDistance = 10f;
    [SerializeField] private float brushWidth = 0.05f; // radius around tip
    [SerializeField] private int pointsPerStroke = 5; // number of points per frame

    private Vector3 lastTipPos;
    private List<Vector3> strokePoints = new List<Vector3>();

    private void Update()
    {
        if (toolType != CollisionToolsType.Texture || tipPointPos == null) return;

        Vector3 currentTip = tipPointPos.position;

        // Only continue if tip moved
        if (Vector3.Distance(lastTipPos, currentTip) > 0.001f)
        {
            // Generate points along the movement for smoother strokes
            strokePoints.Clear();
            for (int i = 0; i < pointsPerStroke; i++)
            {
                float t = i / (float)pointsPerStroke;
                Vector3 point = Vector3.Lerp(lastTipPos, currentTip, t);

                // Optional: randomize within brush width to simulate area
                point += tipPointPos.right * Random.Range(-brushWidth, brushWidth);
                point += tipPointPos.up * Random.Range(-brushWidth, brushWidth);

                strokePoints.Add(point);
            }

            foreach (var point in strokePoints)
            {
                Ray ray = new Ray(point, tipPointPos.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, rayDistance))
                {
                    if (hit.collider.CompareTag(removableTag))
                    {
                        var clean = hit.collider.GetComponent<Clean>();
                        if (clean != null)
                        {
                            clean.CleanAt(hit.textureCoord, brush);
                        }
                    }
                }
            }

            lastTipPos = currentTip;
        }
    }

    private void OnDrawGizmos()
    {
        if (tipPointPos == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(tipPointPos.position, tipPointPos.position + tipPointPos.forward * rayDistance);
    }
}
