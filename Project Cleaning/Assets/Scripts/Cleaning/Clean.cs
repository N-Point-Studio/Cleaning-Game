using System;
using UnityEngine;

public class Clean : MonoBehaviour
{
    public enum CollisionToolsType
    {
        Texture,
        Mesh,
    }
    public Texture2D _dirtMaskBase;
    public Material _material;
    // public CollisionToolsType type = CollisionToolsType.Mesh;
    private Texture2D _templateDirtMask;

    private void Start()
    {
        CreateTexture();
    }

    public bool CleanAt(Vector2 textureCoord, Texture2D brush)
    {
        Debug.Log("Clean at " + textureCoord);

        bool didCleanAnything = false;

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


                    if (pixelDirtMask.g > 0.01f && pixelDirt.g < 1.0f)
                    {
                        _templateDirtMask.SetPixel(px, py, new Color(0, pixelDirtMask.g * pixelDirt.g, 0));
                        didCleanAnything = true;
                    }
                }
            }
        }

        // BARU: Hanya panggil Apply() jika ada yang berubah (ini optimasi besar!)
        if (didCleanAnything)
        {
            _templateDirtMask.Apply();
        }

        return didCleanAnything;
    }

    public bool CleanAt(Vector2 uv, Texture2D brush, float brushScale, float surfaceRotation)
    {
        bool didCleanAnything = false;

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

                // --- Rotasi UV brush berdasarkan rotasi permukaan ---
                Vector2 rotated = RotateUV(u, v, surfaceRotation);

                // Hindari sampling di luar brush
                if (rotated.x < 0 || rotated.x > 1 || rotated.y < 0 || rotated.y > 1)
                    continue;

                Color brushPixel = brush.GetPixelBilinear(rotated.x, rotated.y);
                Color dirtPixel = _templateDirtMask.GetPixel(px, py);

                if (dirtPixel.g > 0.01f && brushPixel.g < 1.0f)
                {
                    float newGreen = dirtPixel.g * brushPixel.g;
                    _templateDirtMask.SetPixel(px, py, new Color(0, newGreen, 0));
                    didCleanAnything = true;
                }
            }
        }

        if (didCleanAnything)
            _templateDirtMask.Apply();

        return didCleanAnything;
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

    public bool DestroyMesh()
    {
        Destroy(gameObject);
        // Setiap kali dipanggil, kita anggap "berhasil"
        return true;
    }

    private Vector2 RotateUV(float u, float v, float angleDeg)
    {
        float angle = angleDeg * Mathf.Deg2Rad;

        float cos = Mathf.Cos(angle);
        float sin = Mathf.Sin(angle);

        // Geser sehingga titik pusat (0.5,0.5)
        float cx = u - 0.5f;
        float cy = v - 0.5f;

        // Rotasi
        float rx = cx * cos - cy * sin;
        float ry = cx * sin + cy * cos;

        // Kembalikan ke posisi uv 0..1
        return new Vector2(rx + 0.5f, ry + 0.5f);
    }

}
