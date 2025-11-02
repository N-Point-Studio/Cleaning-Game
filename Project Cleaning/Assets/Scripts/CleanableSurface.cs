using UnityEngine;

[RequireComponent(typeof(MeshCollider))]
public class CleanableSurface : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private string maskProperty = "_DirtMask";

    // Optional: keep track of a render texture mask
    public RenderTexture CleanMask { get; private set; }

    private void Awake()
    {
        if (!targetRenderer) targetRenderer = GetComponent<Renderer>();

        // Create a mask RT to feed shader
        CleanMask = new RenderTexture(512, 512, 0, RenderTextureFormat.R8);
        CleanMask.Create();
        targetRenderer.material.SetTexture(maskProperty, CleanMask);
    }

    public void CleanAtUV(Vector2 uv, Texture brush)
    {
        // Draw brush onto the render texture (mask)
        RenderTexture.active = CleanMask;
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, CleanMask.width, CleanMask.height, 0);

        // Paint brush texture onto UV space
        Rect rect = new Rect(uv.x * CleanMask.width - brush.width / 2,
                             (1 - uv.y) * CleanMask.height - brush.height / 2,
                             brush.width, brush.height);

        Graphics.DrawTexture(rect, brush);
        GL.PopMatrix();
        RenderTexture.active = null;
    }
}
