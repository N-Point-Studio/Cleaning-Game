using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Clean : MonoBehaviour
{
    public Texture2D _dirtMaskBase;
    public Material _material;
    private Texture2D _templateDirtMask;

    private float dirtAmountTotal;
    private float dirtAmount;

    private void Start()
    {
        CleanManager.Instance.Register(this);
        CreateTexture();
    }


    private void Update()
    {
        Debug.Log(name + " progress is: " + GetDirtAmount());
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

                if (px < 0 || px >= _templateDirtMask.width ||
                    py < 0 || py >= _templateDirtMask.height)
                    continue;

                float u = (x + radius) / (radius * 2f);
                float v = (y + radius) / (radius * 2f);

                Vector2 rotated = RotateUV(u, v, surfaceRotation);

                if (rotated.x < 0 || rotated.x > 1 ||
                    rotated.y < 0 || rotated.y > 1)
                    continue;

                Color brushPixel = brush.GetPixelBilinear(rotated.x, rotated.y);
                Color dirtPixel = _templateDirtMask.GetPixel(px, py);

                // Pixel tidak menghapus apa-apa
                if (brushPixel.g >= 1f)
                    continue;

                // Sudah bersih
                if (dirtPixel.g <= 0.01f)
                    continue;

                // Hitung berapa yang hilang
                float newGreen = dirtPixel.g * brushPixel.g;
                float removed = dirtPixel.g - newGreen;

                // Update progress
                dirtAmount -= removed;

                // Update pixel
                _templateDirtMask.SetPixel(px, py, new Color(0, newGreen, 0));
                didCleanAnything = true;
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

        _material = GetComponent<Renderer>().material;
        _material.SetTexture("_DirtMask", _templateDirtMask);

        // Hitung total dirt
        dirtAmountTotal = 0f;
        for (int x = 0; x < _dirtMaskBase.width; x++)
        {
            for (int y = 0; y < _dirtMaskBase.height; y++)
            {
                dirtAmountTotal += _dirtMaskBase.GetPixel(x, y).g;
            }
        }

        dirtAmount = dirtAmountTotal;
    }

    private Vector2 RotateUV(float u, float v, float angleDeg)
    {
        float angle = angleDeg * Mathf.Deg2Rad;
        float cos = Mathf.Cos(angle);
        float sin = Mathf.Sin(angle);

        float cx = u - 0.5f;
        float cy = v - 0.5f;

        float rx = cx * cos - cy * sin;
        float ry = cx * sin + cy * cos;

        return new Vector2(rx + 0.5f, ry + 0.5f);
    }

    public float GetDirtAmount()
    {
        // Lindungi dari NaN
        if (dirtAmountTotal <= 0)
            return 0f;

        return dirtAmount / dirtAmountTotal;
    }
}
