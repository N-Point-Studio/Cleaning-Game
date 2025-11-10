using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ToolCleaningSurface : MonoBehaviour
{
    public enum CollisionToolsType
    {
        Texture,
        Mesh,
    }

    [Header("Tool Settings")]
    [SerializeField] private CollisionToolsType toolType = CollisionToolsType.Texture;
    [SerializeField] float raycastRange = 5f;
    [SerializeField] private LayerMask dirtsLayerMask;
    [SerializeField] private Texture2D brush;
    [SerializeField] private SurfaceDetection surface;
    [SerializeField] private float brushSize;

    [Header("Audio Settings")]
    [Tooltip("Seberapa jauh alat harus bergerak (dalam meter) dalam satu frame untuk dianggap 'bergerak' dan membunyikan audio.")]
    [SerializeField] private float movementThreshold = 0.01f; // 1 milimeter

    public Vector3 RaycastTipPos { get; private set; }
    public Vector3 RaycastTipNormal { get; private set; }

    // BARU: Variabel untuk menyimpan komponen Audio Source
    private AudioSource cleaningAudioSource;

    // NAMA VARIABEL DIGANTI agar lebih jelas
    private bool isActivelyCleaning = false; // Melacak status pembersihan AKTIF

    // --- BARU: Variabel untuk melacak gerakan ---
    private Vector3 lastFramePosition;
    private bool isMoving = false;

    // BARU: Awake dipanggil sebelum Start
    void Awake()
    {
        // Ambil komponen AudioSource yang ada di objek ini
        cleaningAudioSource = GetComponent<AudioSource>();
        if (surface == null)
        {
            Debug.LogError("SurfaceDetection belum di-assign di ToolCleaningSurface!");
        }

        // --- BARU: Inisialisasi posisi awal ---
        lastFramePosition = transform.position;
    }

    void Update()
    {
        if (surface == null) return;

        CheckForMovement();
        RaycastCleaningSurface();
        HandleAudio();
    }

    // --- FUNGSI BARU ---
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
        // Setel ulang status ke false setiap frame
        isActivelyCleaning = false;

        switch (toolType)
        {
            case CollisionToolsType.Mesh:
                if (surface.MudObject != null)
                {
                    isActivelyCleaning = TryDestroyMesh(surface.MudObject);;
                }
                break;
            case CollisionToolsType.Texture:
                if (surface.CleaningSurface != null)
                {
                    isActivelyCleaning = TryClean(surface.CleaningSurface, surface.TextureSurface);;
                }
                break;
        }
    }

    private bool TryClean(Clean clean, Vector2 textureCoord)
    {
        // clean.CleanAt(textureCoord, brush);
        return clean.CleanAt(textureCoord, brush, brushSize);
    }

    private bool TryDestroyMesh(CleanMesh obj)
    {
        return obj.DestroyMesh();
    }

// Logika audio dipindah ke fungsi terpisah
    private void HandleAudio()
    {
        Debug.Log("HandleAudio Check: isActivelyCleaning = " + isActivelyCleaning + " | isMoving = " + isMoving);
        
        // Kita hanya perlu mengecek kondisi "PLAY"
        if (isActivelyCleaning && isMoving)
        {
            // Jika membersihkan DAN bergerak, DAN suaranya TIDAK sedang diputar...
            if (!cleaningAudioSource.isPlaying)
            {
                // ...maka putar suaranya SATU KALI.
                // Karena 'Loop' sudah mati, ini akan memainkan klip sampai selesai.
                cleaningAudioSource.Play();
            }
        }
    }
    void OnDrawGizmos()
    {
        Gizmos.color = Color.black;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * raycastRange);
    }
}
