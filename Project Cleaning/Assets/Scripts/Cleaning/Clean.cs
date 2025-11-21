using System.Collections;
using UnityEngine;
using Unity.Collections; // Diperlukan untuk NativeArray

public class Clean : MonoBehaviour
{
    [Header("Textures")]
    public Texture2D _dirtMaskBase;
    public Material _material;
    
    private Texture2D _templateDirtMask;
    private NativeArray<Color32> _rawTextureContainer; // Akses memori langsung (Sangat Cepat)
    private int _textureWidth;
    private int _textureHeight;

    [Header("Progress (0 = kotor, 1 = bersih)")]
    [Range(0, 1f)]
    [SerializeField] private float progress = 0f;

    private float[] _brushAlphaCache; 
    private int _brushWidth;
    private int _brushHeight;

    private float dirtAmountTotal = 0f;
    private float dirtAmount = 0f;
    
    // OPTIMASI: Dirty Flag
    private bool _isDirty = false; // Penanda apakah tekstur berubah frame ini?

    private void Start()
    {
        CreateTexture();
        StartCoroutine(CalculateDirtTotalAsync());
    }

    private void OnDestroy()
    {
        // Penting: NativeArray harus dibuang manual untuk mencegah memory leak
        if (_rawTextureContainer.IsCreated)
        {
            _rawTextureContainer.Dispose();
        }
    }
    
    // OPTIMASI: LateUpdate hanya memanggil Apply() 1x per frame, bukan 1000x.
    private void LateUpdate()
    {
        if (_isDirty)
        {
            _templateDirtMask.Apply();
            UpdateProgress();
            _isDirty = false; // Reset flag
        }
    }

    private IEnumerator CalculateDirtTotalAsync()
    {
        dirtAmountTotal = 0f;
        
        // Optimasi: Menggunakan raw data lebih cepat daripada GetPixels()
        // Namun karena ini cuma start, GetPixels masih oke.
        Color[] pixels = _dirtMaskBase.GetPixels(); 
        const int batchSize = 10000;

        for (int i = 0; i < pixels.Length; i++)
        {
            dirtAmountTotal += pixels[i].g;
            if (i % batchSize == 0) yield return null;
        }

        dirtAmount = dirtAmountTotal;
        CleanManager.Instance.Register(this);
        Debug.Log($"{name} Dirt Total Selesai: {dirtAmountTotal}");
    }

    public bool CleanAt(Vector2 uv, Texture2D brush, float brushScale, float surfaceRotation)
    {
        // OPTIMASI: Cek apakah cache brush sudah ada? Jika belum, buat.
        if (_brushAlphaCache == null || _brushWidth != brush.width)
        {
            PrepareBrushCache(brush);
        }

        // Konversi UV (0-1) ke Koordinat Pixel
        int centerX = (int)(uv.x * _textureWidth);
        int centerY = (int)(uv.y * _textureHeight);
        
        // Hitung radius kuadrat untuk optimasi jarak (menghindari akar kuadrat di loop)
        int radius = Mathf.RoundToInt((brush.width * brushScale) * 0.5f);
        int radiusSq = radius * radius; 

        bool didCleanAnything = false;

        // Loop area kuas
        for (int y = -radius; y < radius; y += 2)
        {
            int py = centerY + y;
            if (py < 0 || py >= _textureHeight) continue; // Cek batas Y lebih awal

            for (int x = -radius; x < radius; x += 2)
            {
                int px = centerX + x;
                if (px < 0 || px >= _textureWidth) continue; // Cek batas X

                // Optimasi Circle Check sederhana sebelum rotasi mahal
                if (x*x + y*y > radiusSq) continue;

                // ROTASI UV KUAS
                // Kita perlu memetakan koordinat (x,y) relatif terhadap pusat kuas ke UV kuas (0-1)
                // Lalu dari UV kuas ke koordinat pixel kuas untuk ambil nilai dari Cache.

                // Rotasi UV untuk kuas
                float u = (x + radius) / (radius * 2f);
                float v = (y + radius) / (radius * 2f);
                Vector2 rotated = RotateUV(u, v, surfaceRotation);

                if (rotated.x < 0 || rotated.x >= 1 || rotated.y < 0 || rotated.y >= 1)
                    continue;

                // 4. Ambil nilai Alpha dari CACHE Array (Bukan GetPixelBilinear)
                // Kita konversi UV rotasi ke koordinat pixel brush
                int brushPx = (int)(rotated.x * (_brushWidth - 1));
                int brushPy = (int)(rotated.y * (_brushHeight - 1));

                // Akses array 1D: index = y * width + x
                float brushAlpha = _brushAlphaCache[brushPy * _brushWidth + brushPx];

                if (brushAlpha >= 1f) continue; // Anggap 1f di mask brush = transparan/tidak kena

                // OPTIMASI: Akses array langsung via index (Sangat Cepat)
                int pixelIndex = py * _textureWidth + px;
                Color32 currentPixel = _rawTextureContainer[pixelIndex];

                // Logika pembersihan (menggunakan byte 0-255 lebih cepat dari float)
                if (currentPixel.g <= 2) continue; // Sudah bersih (epsilon byte)

                // Konversi float math ke byte math
                byte removeAmount = (byte)((currentPixel.g * brushAlpha)); 
                
                // Update Dirt Amount Global
                // Kita konversi balik ke float (0-1) untuk tracking progress akurat
                float removedFloat = (currentPixel.g - removeAmount) / 255f; 
                dirtAmount -= removedFloat;

                // Set Pixel Baru
                currentPixel.g = removeAmount;
                _rawTextureContainer[pixelIndex] = currentPixel;
                
                didCleanAnything = true;
            }
        }

        if (didCleanAnything)
        {
            _isDirty = true; // Tandai untuk di-Apply nanti di LateUpdate
        }

        return didCleanAnything;
    }

    // Fungsi baru untuk mengubah Texture Brush jadi Array float agar akses super cepat
    private void PrepareBrushCache(Texture2D brush)
    {
        _brushWidth = brush.width;
        _brushHeight = brush.height;
        _brushAlphaCache = new float[_brushWidth * _brushHeight];

        // Kita ambil pixel sekali saja
        Color[] brushPixels = brush.GetPixels();

        for (int i = 0; i < brushPixels.Length; i++)
        {
            // Simpan channel Green (atau Alpha) ke array float sederhana
            _brushAlphaCache[i] = brushPixels[i].g; 
        }
        
        Debug.Log($"Brush Cache Created: {_brushWidth}x{_brushHeight}");
    }

    private void CreateTexture()
    {
        _textureWidth = _dirtMaskBase.width;
        _textureHeight = _dirtMaskBase.height;

        // Format RGBA32 penting agar memory layout konsisten
        _templateDirtMask = new Texture2D(_textureWidth, _textureHeight, TextureFormat.RGBA32, false);
        _templateDirtMask.SetPixels32(_dirtMaskBase.GetPixels32());
        _templateDirtMask.Apply();

        // Simpan referensi ke Raw Data untuk akses super cepat
        _rawTextureContainer = _templateDirtMask.GetRawTextureData<Color32>();

        _material = GetComponent<Renderer>().material;
        _material.SetTexture("_DirtMask", _templateDirtMask);
    }

    private void UpdateProgress()
    {
        if (dirtAmountTotal <= 0)
            progress = 0f;
        else
            progress = Mathf.Clamp01(1f - (dirtAmount / dirtAmountTotal));
    }

    // Helper Rotation (Tetap sama, cuma dioptimalkan math-nya sedikit)
    private Vector2 RotateUV(float u, float v, float angleDeg)
    {
        float angle = angleDeg * Mathf.Deg2Rad;
        float cos = Mathf.Cos(angle);
        float sin = Mathf.Sin(angle);
        float cx = u - 0.5f;
        float cy = v - 0.5f;
        return new Vector2((cx * cos - cy * sin) + 0.5f, (cx * sin + cy * cos) + 0.5f);
    }
    
    public float GetDirtAmount() => progress;
}