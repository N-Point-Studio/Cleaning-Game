using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EasyTransition;
using Unity.Mathematics;

public class GamePlayManager : MonoBehaviour
{
    public static GamePlayManager Instance;

    [SerializeField] GameObject Environment;
    [SerializeField] Transform ClearInspect;
    [SerializeField] Camera ToolCamera;
    [SerializeField] ParticleSystem GlitterParticle;

    [Header("Return Transition")]
    [SerializeField] private bool useStagedReturnTransition = true;
    [SerializeField] private string returnIntermediaryScene = "TransitionScreen";
    [SerializeField] private float returnIntermediaryDelay = 0.5f;
    [SerializeField] private TransitionSettings returnTransitionSettings;

    public bool isGameFinished = false;

    private Transform initialTransfromEnvironment;
    private Vector3 environmentStartPos;
    private Quaternion environmentStartRot;
    private Vector3 FinishedPosition = new Vector3(0.0f, -5.5f, -9f);


    private Quaternion finishedRotation = Quaternion.Euler(0f, 0f, 0f);
    private Vector3 finishedObjectPosition = new Vector3(0f, 0f, 0f);

    // Cached object context for the current gameplay session
    private ObjectType sessionObjectType = ObjectType.ChinaCoin;
    private ChapterType sessionChapterType = ChapterType.China;
    private bool hasResolvedObjectContext = false;
    private bool resolvedFromSceneDetection = false;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    [SerializeField] private float debugLogInterval = 1f;
    private float nextDebugLogTime = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
        }
        Instance = this;
        initialTransfromEnvironment = Environment.transform;
        environmentStartPos = Environment.transform.position;
        environmentStartRot = Environment.transform.rotation;
        TouchManager.Instance.DisableAllTouch(false);
        ToolCamera.enabled = true;

        // Capture which object/chapter this gameplay session represents
        ResolveObjectContext();
    }

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (!IsGameplayScene())
        {
            return; // Ensure gameplay logic only runs in gameplay scenes
        }

        if (UIManager.Instance == null || AssembleManager.Instance == null)
        {
            return; // Scene unloading or managers not ready
        }

        if (enableDebugLogs && Time.time >= nextDebugLogTime)
        {
            Debug.Log($"[GamePlayManager] Progress value: {UIManager.Instance.GetAllProgressValue()}");
            nextDebugLogTime = Time.time + debugLogInterval;
        }
        FinishedGame();
    }

    private void FinishedGame()
    {
        if (UIManager.Instance.GetAllProgressValue() >= 100)
        {
            if (AssembleManager.Instance == null)
            {
                Debug.LogWarning("AssembleManager missing when checking finish state.");
                return;
            }
            if (AssembleManager.Instance.assemblyTargets.Count > 0)
            {
                Debug.Log("ASSEMBLE A - Cluster Mode");
                TouchManager.Instance.DisableAllTouch(true);
                UIManager.Instance.ShowFinishUI(true);
                UIManager.Instance.ShowFinishBackground(true);
                GlitterParticle.gameObject.SetActive(true);
                ToolCamera.enabled = false;


                Environment.transform.position = Vector3.Lerp(
                    Environment.transform.position,
                    FinishedPosition,
                    Time.deltaTime * 2f
                );

                ClusterStateMachine cluster = AssembleManager.Instance.CurrentClusterInspected;

                // ✅ NULL CHECK: Ensure cluster is not null before accessing
                if (cluster == null)
                {
                    Debug.LogError("❌ CurrentClusterInspected is NULL! Cannot finish cluster state.");
                    isGameFinished = true; // Still mark as finished to allow transition
                    return;
                }

                Debug.Log("Cluster Finished: " + cluster.name);
                cluster.SwitchState(new ClusterFinishState(cluster));
                cluster.transform.SetParent(ClearInspect, worldPositionStays: false);

                cluster.transform.localPosition = Vector3.Lerp(
                    cluster.transform.localPosition,
                    finishedObjectPosition,
                    Time.deltaTime * 2f
                );

                cluster.transform.localRotation = Quaternion.Slerp(
                    cluster.transform.localRotation,
                    finishedRotation,
                    Time.deltaTime * 2f
                );

                isGameFinished = true;
            }
            else
            {
                Debug.Log("ASSEMBLE B - Fragment Mode");
                TouchManager.Instance.DisableAllTouch(true);
                UIManager.Instance.ShowFinishUI(true);
                UIManager.Instance.ShowFinishBackground(true);
                GlitterParticle.gameObject.SetActive(true);
                ToolCamera.enabled = false;


                Environment.transform.position = Vector3.Lerp(
                    Environment.transform.position,
                    FinishedPosition,
                    Time.deltaTime * 2f
                );

                FragmentStateMachine fragment = AssembleManager.Instance.CurrentFragmentInspected;

                // ✅ NULL CHECK: Ensure fragment is not null before accessing
                if (fragment == null)
                {
                    Debug.LogError("❌ CurrentFragmentInspected is NULL! Cannot finish fragment state.");
                    Debug.LogWarning("⚠️ This might happen if:");
                    Debug.LogWarning("   1. Scene setup is incorrect (no fragment assigned)");
                    Debug.LogWarning("   2. AssembleManager not properly initialized");
                    Debug.LogWarning("   3. Fragment was destroyed before finish");
                    isGameFinished = true; // Still mark as finished to allow transition
                    return;
                }

                Debug.Log("Fragment Finished: " + fragment.name);
                fragment.SwitchState(new FragmentFinishState(fragment));

                fragment.transform.SetParent(ClearInspect, worldPositionStays: false);

                fragment.transform.localPosition = Vector3.Lerp(
                    fragment.transform.localPosition,
                    finishedObjectPosition,
                    Time.deltaTime * 2f
                );

                fragment.transform.localRotation = Quaternion.Slerp(
                    fragment.transform.localRotation,
                    finishedRotation,
                    Time.deltaTime * 2f
                );

                isGameFinished = true;
            }
        }
    }

    public bool IsGameFinished()
    {
        return isGameFinished;
    }

    /// <summary>
    /// Hard reset gameplay state when re-entering the scene.
    /// </summary>
    public void ResetSession()
    {
        isGameFinished = false;

        // Reset environment and camera/tools
        if (Environment != null)
        {
            Environment.transform.SetPositionAndRotation(environmentStartPos, environmentStartRot);
        }

        if (GlitterParticle != null)
        {
            GlitterParticle.gameObject.SetActive(false);
        }

        if (ToolCamera != null)
        {
            ToolCamera.enabled = true;
        }

        // Re-enable touch
        if (TouchManager.Instance != null)
        {
            TouchManager.Instance.DisableAllTouch(false);
        }

        Debug.Log("[GamePlayManager] Session reset.");
    }

    /// <summary>
    /// Public entry point for finish button to re-run finish logic safely.
    /// </summary>
    public void TriggerFinishButton()
    {
        Debug.Log("=== FINISH BUTTON TRIGGERED FROM UI ===");

        // Pastikan state selesai di-set
        FinishedGame();

        // ✅ FIX: SAVE OBJECT COMPLETION KETIKA GAMEPLAY SELESAI
        // CRITICAL: Save MUST complete before scene transition
        SaveObjectCompletion();

        // ✅ VERIFICATION: Print save status to confirm save succeeded
        if (SaveSystem.Instance != null)
        {
            Debug.Log("========================================");
            Debug.Log("=== VERIFYING SAVE COMPLETION ===");
            SaveSystem.Instance.PrintSaveDataInfo();
            Debug.Log("========================================");
        }

        // Lanjutkan transition ke menu
        StartSceneTransition();
    }

    public void StartSceneTransition()
    {
        // Pastikan SceneTransitionManager ada
        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogWarning("SceneTransitionManager not found, creating one...");
            var stmGO = new GameObject("SceneTransitionManager");
            stmGO.AddComponent<SceneTransitionManager>();
        }

        // Gunakan konteks objek yang sama dengan saat masuk gameplay
        ResolveObjectContext();
        ObjectType objectType = sessionObjectType;
        string targetScene = "New Start Game Sandy"; // main menu default

        Debug.Log($"Triggering transition to '{targetScene}' with ObjectType '{objectType}'");

        // Pastikan SceneTransitionManager tahu objectType yang benar untuk trigger ContentSwitcher
        SceneTransitionManager.Instance.SetObjectTypeForTransition(objectType);

        if (useStagedReturnTransition)
        {
            Debug.Log($"Using staged return transition via '{returnIntermediaryScene}' (delay {returnIntermediaryDelay}s)");
            SceneTransitionManager.Instance.MarkReturningFromGameplay();
            SceneTransitionManager.Instance.StartStagedTransition(
                returnIntermediaryScene,
                targetScene,
                returnIntermediaryDelay,
                returnTransitionSettings
            );
        }
        else
        {
            SceneTransitionManager.Instance.MarkReturningFromGameplay();
            SceneTransitionManager.Instance.TransitionToMainSceneWithContentSwitcher(objectType, targetScene, "GamePlayManager");
        }
    }

    private ObjectType DetectObjectTypeFromScene()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.ToLower();

        if (sceneName.Contains("coin") || sceneName.Contains("china coin"))
        {
            return ObjectType.ChinaCoin;
        }
        else if (sceneName.Contains("jar") || sceneName.Contains("china jar"))
        {
            return ObjectType.ChinaJar;
        }
        else if (sceneName.Contains("horse"))
        {
            return ObjectType.ChinaHorse;
        }
        else if (sceneName.Contains("kendi") || sceneName.Contains("kendin") || sceneName.Contains("indonesia"))
        {
            return ObjectType.IndonesiaKendin;
        }
        else if (sceneName.Contains("scarab") || sceneName.Contains("winged") || sceneName.Contains("mesir") || sceneName.Contains("egypt"))
        {
            return ObjectType.MesirWingedScared;
        }

        return ObjectType.ChinaCoin;
    }

    private void ResolveObjectContext()
    {
        if (hasResolvedObjectContext && (!resolvedFromSceneDetection || SceneTransitionManager.Instance == null))
        {
            return;
        }

        ObjectType detectedFromScene = DetectObjectTypeFromScene();
        ObjectType resolvedType = detectedFromScene;
        ChapterType resolvedChapter = GetChapterFromObjectType(detectedFromScene);
        bool usedSceneFallback = true;

        if (SceneTransitionManager.Instance != null)
        {
            ObjectType stmType = SceneTransitionManager.Instance.GetCurrentObjectType();
            ChapterType stmChapter = SceneTransitionManager.Instance.GetCurrentChapterType();

            resolvedType = stmType;
            resolvedChapter = stmChapter;
            usedSceneFallback = false;

            if (stmType == ObjectType.ChinaCoin && detectedFromScene != ObjectType.ChinaCoin)
            {
                resolvedType = detectedFromScene;
                resolvedChapter = GetChapterFromObjectType(resolvedType);
                usedSceneFallback = true;
            }
        }

        sessionObjectType = resolvedType;
        sessionChapterType = resolvedChapter;
        hasResolvedObjectContext = true;
        resolvedFromSceneDetection = usedSceneFallback;
    }

    private void SaveObjectCompletion()
    {
        ResolveObjectContext();

        if (SaveSystem.Instance == null)
        {
            Debug.LogWarning("SaveSystem not available - cannot save object completion");
            return;
        }

        ObjectType completedObjectType = sessionObjectType;
        ChapterType chapterType = sessionChapterType;

        Vector3 completionPosition = Vector3.zero;
        if (Environment != null)
        {
            completionPosition = Environment.transform.position;
        }
        else if (ClearInspect != null)
        {
            completionPosition = ClearInspect.position;
        }

        SaveSystem.Instance.MarkObjectCompleted(
            objectName: completedObjectType.ToString(),
            objectType: completedObjectType,
            chapterType: chapterType,
            position: completionPosition
        );
    }

    /// <summary>
    /// Get chapter type from object type
    /// </summary>
    private ChapterType GetChapterFromObjectType(ObjectType objectType)
    {
        switch (objectType)
        {
            case ObjectType.ChinaCoin:
            case ObjectType.ChinaJar:
            case ObjectType.ChinaHorse:
                return ChapterType.China;
            case ObjectType.IndonesiaKendin:
                return ChapterType.Indonesia;
            case ObjectType.MesirWingedScared:
                return ChapterType.Mesir;
            default:
                return ChapterType.China;
        }
    }


    private bool IsGameplayScene()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.ToLower();
        return sceneName.Contains("gameplay");
    }

    private void OnDisable()
    {
        // Stop any finish animation when leaving the gameplay scene
        isGameFinished = false;
        StopAllCoroutines();

        if (Environment != null)
        {
            Environment.transform.SetPositionAndRotation(environmentStartPos, environmentStartRot);
        }

        if (GlitterParticle != null)
        {
            GlitterParticle.gameObject.SetActive(false);
        }

        if (ToolCamera != null)
        {
            ToolCamera.enabled = true;
        }
    }

    public void SetFinishedRotationAndRotation(Quaternion rotationEuler, Vector3 position)
    {
        finishedRotation = rotationEuler;
        finishedObjectPosition = position;
    }
}
