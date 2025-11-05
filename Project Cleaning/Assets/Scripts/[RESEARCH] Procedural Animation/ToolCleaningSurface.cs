using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolCleaningSurface : MonoBehaviour
{
    public enum CollisionToolsType
    {
        Texture,
        Mesh,
    }

    [SerializeField] private CollisionToolsType toolType = CollisionToolsType.Texture;
    [SerializeField] float raycastRange = 5f;
    [SerializeField] private LayerMask dirtsLayerMask;
    [SerializeField] private Texture2D brush;
    [SerializeField] private SurfaceDetection surface;
    [SerializeField] private float brushSize;

    public Vector3 RaycastTipPos { get; private set; }
    public Vector3 RaycastTipNormal { get; private set; }

    void Update()
    {
        RaycastCleaningSurface();
    }

    private void RaycastCleaningSurface()
    {
        if (surface.CleaningSurface != null)
        {
            TryClean(surface.CleaningSurface, surface.TextureSurface);
        }
    }

    private void TryClean(Clean clean, Vector2 textureCoord)
    {
        // clean.CleanAt(textureCoord, brush);
        clean.CleanAt(textureCoord, brush, brushSize);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.black;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * raycastRange);
    }
}
