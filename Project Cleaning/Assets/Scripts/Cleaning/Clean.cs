using System;
using UnityEngine;

public class Clean : MonoBehaviour
{
    public enum CollisionToolsType
    {
        Texture,
        Mesh,
    }
    [SerializeField] private Texture2D _dirtMaskBase;
    [SerializeField] private Material _material;
    [SerializeField] private CollisionToolsType type = CollisionToolsType.Mesh;
    private Texture2D _templateDirtMask;

    private void Awake()
    {
        CreateTexture();
    }

    public void CleanAt(Vector2 textureCoord, Texture2D brush)
    {
        Debug.Log("Clean at " + textureCoord);
        int pixelX = (int)(textureCoord.x * _templateDirtMask.width);
        int pixelY = (int)(textureCoord.y * _templateDirtMask.height);
        for (int x = 0; x < brush.width; x++)
        {
            for (int y = 0; y < brush.height; y++)
            {
                int px = pixelX + x;
                int py = pixelY + y;

                if (px >= 0 && px < _templateDirtMask.width && py >= 0 && py < _templateDirtMask.height)
                {
                    Color pixelDirt = brush.GetPixel(x, y);
                    Color pixelDirtMask = _templateDirtMask.GetPixel(px, py);

                    _templateDirtMask.SetPixel(px, py, new Color(0, pixelDirtMask.g * pixelDirt.g, 0));
                }
            }
        }

        _templateDirtMask.Apply();
    }
    public void CleanAt(Vector2 uv, Texture2D brush, float brushScale)
    {
        int centerX = (int)(uv.x * _templateDirtMask.width);
        int centerY = (int)(uv.y * _templateDirtMask.height);

        int radius = Mathf.RoundToInt((brush.width * brushScale) * 0.5f);

        for (int x = -radius; x < radius; x++)
        {
            for (int y = -radius; y < radius; y++)
            {
                int px = centerX + x;
                int py = centerY + y;

                if (px < 0 || px >= _templateDirtMask.width || py < 0 || py >= _templateDirtMask.height)
                    continue;

                float u = (float)(x + radius) / (radius * 2);
                float v = (float)(y + radius) / (radius * 2);

                Color brushPixel = brush.GetPixelBilinear(u, v);
                Color dirtPixel = _templateDirtMask.GetPixel(px, py);

                _templateDirtMask.SetPixel(px, py, new Color(0, dirtPixel.g * brushPixel.g, 0));
            }
        }

        _templateDirtMask.Apply();
    }

    private void CreateTexture()
    {
        _templateDirtMask = new Texture2D(_dirtMaskBase.width, _dirtMaskBase.height);
        _templateDirtMask.SetPixels(_dirtMaskBase.GetPixels());
        _templateDirtMask.Apply();
        var renderer = GetComponent<Renderer>();
        _material = renderer.material;

        _material.SetTexture("_DirtMask", _templateDirtMask);
    }

    public void DestroyMesh()
    {
        Destroy(gameObject);
    }
}
