using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [System.Serializable]
    public class TutorialStep
    {
        public string stepName;         // Nama langkah (misal: "Inspect")
        public GameObject uiGuideline;  // Canvas/Panel UI tutorial
        public bool isCompleted;        // Status selesai
    }

    // URUTAN INDEX PENTING:
    // 0 = Inspect (Drag item)
    // 1 = Clean (Drag tool)
    // 2 = Rotate
    // 3 = Zoom
    // 4 = Assemble
    // 5 = Disassemble
    public List<TutorialStep> steps;

    [Header("Settings")]
    [Tooltip("Waktu tunggu sebelum instruksi muncul (detik)")]
    public float delayTime = 5f;

    private int currentStepIndex = 0;
    private Coroutine currentTimer; // Untuk menyimpan timer yang sedang berjalan

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {   // SAFETY CHECK: Pastikan steps tidak kosong
        if (steps == null || steps.Count == 0) {
            Debug.LogError("[TutorialManager] Error: List 'Steps' kosong! Isi di Inspector.");
            return;
        }
        // Matikan semua UI saat awal
        foreach (var step in steps)
        {
            if (step.uiGuideline != null) step.uiGuideline.SetActive(false);
        }

        // Mulai timer langkah pertama dengan aman
        StartStepTimer(0);
    }

    private void Update()
    {
        if (currentStepIndex >= steps.Count) return;
        CheckLogic();
    }

    private void CheckLogic()
    {
        switch (currentStepIndex)
        {
            case 0: // Inspect
                // Safety check agar tidak error jika AssembleManager belum siap
                if (AssembleManager.Instance != null) 
                {
                    if (AssembleManager.Instance.CurrentClusterInspected != null || 
                        AssembleManager.Instance.CurrentFragmentInspected != null)
                    {
                        CompleteStep(0);
                    }
                }
                break;

            case 1: // --- 2. CARA CLEANING ---
                // Logika ini dipanggil dari SurfaceDetection.cs (lihat langkah integrasi di bawah)
                break;

            case 2: // --- 3. CARA ROTASI ---
                // Cek variabel isRotating di TouchManager
                if (TouchManager.Instance != null && TouchManager.Instance.isRotating)
                    CompleteStep(2);
                break;

            case 3: // --- 4. CARA ZOOM ---
                // Cek variabel isZooming di TouchManager
                if (TouchManager.Instance != null && TouchManager.Instance.isZooming)
                    CompleteStep(3);
                break;
            
            case 4: // --- 5. CARA ASSEMBLE ---
                // Logika dipanggil dari FragmentAttachedState.cs
                break;

            case 5: // --- 6. CARA DISASSEMBLE ---
                // Logika dipanggil dari FragmentUnassembleState.cs
                break;
        }
    }

    // Fungsi Helper baru untuk mengatur timer dengan rapi
    private void StartStepTimer(int index)
    {
        // Hentikan timer sebelumnya jika ada (spesifik, tidak mematikan coroutine lain)
        if (currentTimer != null) StopCoroutine(currentTimer);

        // Mulai timer baru dan simpan referensinya
        currentTimer = StartCoroutine(WaitAndShowStep(index));
    }

    public void CompleteStep(int index)
    {
        // Validasi index
        if (index >= steps.Count) return;

        // Hanya selesaikan jika index sesuai dan belum kelar
        if (index == currentStepIndex && !steps[index].isCompleted)
        {
            steps[index].isCompleted = true;
            Debug.Log($"Tutorial: Langkah {steps[index].stepName} Selesai!");
            
            // Matikan UI langkah ini
            if (steps[index].uiGuideline != null)
                steps[index].uiGuideline.SetActive(false);

            // 1. Hentikan timer yang sedang berjalan untuk langkah ini
            if (currentTimer != null) StopCoroutine(currentTimer);

            // 2. Pindah index
            currentStepIndex++;

            // 3. Mulai timer langkah berikutnya (jika ada)
            if (currentStepIndex < steps.Count)
            {
                StartStepTimer(currentStepIndex);
            }
        }
    }

    private IEnumerator WaitAndShowStep(int stepIndex)
    {
        // Tunggu
        yield return new WaitForSeconds(delayTime);

        // Cek kondisi: Masih di step yang sama DAN belum selesai?
        if (currentStepIndex == stepIndex && !steps[stepIndex].isCompleted)
        {
            Debug.Log($"[Tutorial] Waktu habis! Menampilkan UI: {steps[stepIndex].stepName}");
            
            if (steps[stepIndex].uiGuideline != null)
                steps[stepIndex].uiGuideline.SetActive(true);
        }
    }
}