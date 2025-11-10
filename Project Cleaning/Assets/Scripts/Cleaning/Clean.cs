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

    public bool CleanAt(Vector2 uv, Texture2D brush, float brushScale)
    {
        // BARU: Tambahkan flag untuk melacak perubahan
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

                Color brushPixel = brush.GetPixelBilinear(u, v);
                Color dirtPixel = _templateDirtMask.GetPixel(px, py);

                if (x == 0 && y == 0)
            {
                Debug.Log("DEBUG: Nilai G Kotoran (dirtPixel.g) = " + dirtPixel.g);
            }
                
                // BARU: Cek apakah piksel ini kotor DAN kuas mencoba membersihkannya
                if (dirtPixel.g > 0.01f && brushPixel.g < 1.0f)
                {
                float newGreen = dirtPixel.g * brushPixel.g;
                _templateDirtMask.SetPixel(px, py, new Color(0, newGreen, 0));
                didCleanAnything = true;

                if (x == 0 && y == 0)
                {
                    Debug.LogWarning("--- MEMBERSIHKAN PIKSEL TENGAH! ---");
                }
                }
            }
        }

        // BARU: Hanya panggil Apply() jika ada yang berubah
        if (didCleanAnything)
        {
            _templateDirtMask.Apply();
        }
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
}
