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

    private AudioSource cleaningAudioSource;
    [SerializeField] private ParticleSystem cleaningVFX;
    private bool isActivelyCleaning = false;

    private Vector3 lastFramePosition;
    private bool isMoving = false;
    private float vfxEmissionRate;
    [SerializeField] private float movementThreshold = 0.01f; // 1 milimeter


    void Awake()
    {
        // Ambil komponen AudioSource yang ada di objek ini
        cleaningAudioSource = GetComponent<AudioSource>();
        if (surface == null)
        {
            Debug.LogError("SurfaceDetection belum di-assign di ToolCleaningSurface!");
        }

        // --- BARU: Cek null untuk VFX ---
        if (cleaningVFX == null)
        {
            Debug.LogError("CleaningVFX belum di-assign di Inspector!");
        }
        else
        {
            // --- LOGIKA AWAKE BARU ---
            // 1. Simpan rate emisi (misal: 20) yang Anda atur di Inspector
            vfxEmissionRate = cleaningVFX.emission.rateOverTime.constant;

            // 2. Set rate emisi ke 0 saat game dimulai
            // Ini adalah cara yang BENAR untuk mengubah struct:
            var emissionModule = cleaningVFX.emission;
            emissionModule.rateOverTime = 0f;
        }

        // --- BARU: Inisialisasi posisi awal ---
        lastFramePosition = transform.position;
    }

    void Update()
    {
        if (surface == null) return;
        RaycastCleaningSurface();
        HandleEffects();
        CheckForMovement();
    }

    private void CheckForMovement()
    {
        // Hitung jarak alat bergerak sejak frame terakhir
        float distanceMoved = Vector3.Distance(transform.position, lastFramePosition);

        // Setel 'isMoving' HANYA jika gerakan melebihi batas threshold
        isMoving = distanceMoved > movementThreshold;

        // Simpan posisi saat ini untuk pengecekan di frame berikutnya
        lastFramePosition = transform.position;
    }

    private void RaycastCleaningSurface()
    {
        switch (toolType)
        {
            case CollisionToolsType.Mesh:
                if (surface.MudObject != null)
                {
                    TryDestroyMesh(surface.MudObject);
                }
                break;
            case CollisionToolsType.Texture:
                if (surface.CleaningSurface != null)
                {
                    TryClean(surface.CleaningSurface, surface.TextureSurface);
                }
                break;
        }
    }

    private void TryClean(Clean clean, Vector2 textureCoord)
    {
        // clean.CleanAt(textureCoord, brush);
        clean.CleanAt(textureCoord, brush, brushSize);
    }

    private void TryDestroyMesh(CleanMesh obj)
    {
        obj.DestroyMesh();
    }

    private void HandleEffects()
    {
        // Debug.Log("HandleAudio Check: isActivelyCleaning = " + isActivelyCleaning + " | isMoving = " + isMoving);

        // Kondisi utama kita
        bool conditionsMet = isActivelyCleaning && isMoving;

        // Kita hanya perlu mengecek kondisi "PLAY"
        if (conditionsMet)
        {
            // Jika membersihkan DAN bergerak, DAN suaranya TIDAK sedang diputar...
            if (!cleaningAudioSource.isPlaying)
            {
                // ...maka putar suaranya SATU KALI.
                // Karena 'Loop' sudah mati, ini akan memainkan klip sampai selesai.
                cleaningAudioSource.Play();
            }
        }

        if (cleaningVFX != null)
        {
            // Kita HARUS menyimpan struct ke variabel lokal dulu
            var emissionModule = cleaningVFX.emission;

            if (conditionsMet)
            {
                // Jika kondisi terpenuhi, set rate ke nilai aslinya (misal: 20)
                emissionModule.rateOverTime = vfxEmissionRate;
            }
            else
            {
                // Jika kondisi TIDAK terpenuhi, set rate ke 0
                emissionModule.rateOverTime = 0f;
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.black;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * raycastRange);
    }
}
