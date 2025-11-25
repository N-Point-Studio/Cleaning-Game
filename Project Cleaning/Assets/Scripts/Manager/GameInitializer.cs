using UnityEngine;

/// <summary>
/// ALTERNATIVE SAFER VERSION: Initializes core game systems at startup
/// Ensures SaveSystem and other essential systems are available
///
/// FIXES:
/// - Uses Start() instead of Awake() to avoid TouchManager conflicts
/// - Only creates systems on MAIN SCENE (not on gameplay scenes)
/// - Has safety checks for existing instances
/// - Won't interfere with touch input system
///
/// USAGE:
/// - Place this script ONLY on main menu scene
/// - For gameplay scenes, systems will auto-create when needed
/// </summary>
public class GameInitializer : MonoBehaviour
{
    [Header("System Creation")]
    [SerializeField] private bool autoCreateSystems = true;
    [SerializeField] private bool enableDebugLogs = false;
    [SerializeField] private bool onlyCreateOnMainScene = true;

    [Header("Scene Detection")]
    [SerializeField] private string[] mainSceneNames = { "New Start Game Sandy", "Main Menu", "StartScene" };

    private bool hasInitialized = false;

    private void Start()
    {
        // Use Start() instead of Awake() to avoid conflicts with TouchManager
        if (autoCreateSystems && !hasInitialized)
        {
            StartCoroutine(InitializeSystemsDelayed());
        }
    }

    /// <summary>
    /// Initialize systems with safety checks and scene detection
    /// </summary>
    private System.Collections.IEnumerator InitializeSystemsDelayed()
    {
        // Wait for TouchManager and other essential systems to be ready
        yield return new WaitForSeconds(0.5f);

        // Check if we should only create systems on main scenes
        if (onlyCreateOnMainScene && !IsMainScene())
        {
            if (enableDebugLogs)
            {
                Debug.Log($"Skipping system initialization - not on main scene. Current scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
            }
            yield break;
        }

        if (enableDebugLogs)
        {
            Debug.Log("Starting safe system initialization...");
        }

        hasInitialized = true;
        InitializeSystems();
    }

    /// <summary>
    /// Check if current scene is a main scene
    /// </summary>
    private bool IsMainScene()
    {
        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        foreach (string mainSceneName in mainSceneNames)
        {
            if (currentSceneName.Contains(mainSceneName))
            {
                return true;
            }
        }

        return false;
    }

    private void InitializeSystems()
    {
        if (enableDebugLogs)
        {
            Debug.Log("=== SAFE SYSTEM INITIALIZATION START ===");
        }

        int systemsCreated = 0;

        // 1. Create SaveSystem if it doesn't exist (safe check)
        try
        {
            if (SaveSystem.Instance == null)
            {
                GameObject saveSystemGO = new GameObject("SaveSystem");
                saveSystemGO.AddComponent<SaveSystem>();
                DontDestroyOnLoad(saveSystemGO);
                systemsCreated++;

                if (enableDebugLogs)
                {
                    Debug.Log("✅ SaveSystem created successfully");
                }
            }
            else if (enableDebugLogs)
            {
                Debug.Log("ℹ️ SaveSystem already exists - skipping creation");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Failed to create SaveSystem: {ex.Message}");
        }

        // 2. Create SceneTransitionManager if it doesn't exist (safe check)
        try
        {
            if (SceneTransitionManager.Instance == null)
            {
                GameObject transitionManagerGO = new GameObject("SceneTransitionManager");
                transitionManagerGO.AddComponent<SceneTransitionManager>();
                DontDestroyOnLoad(transitionManagerGO);
                systemsCreated++;

                if (enableDebugLogs)
                {
                    Debug.Log("✅ SceneTransitionManager created successfully");
                }
            }
            else if (enableDebugLogs)
            {
                Debug.Log("ℹ️ SceneTransitionManager already exists - skipping creation");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Failed to create SceneTransitionManager: {ex.Message}");
        }

        // 3. Verify TouchManager doesn't conflict
        if (TouchManager.Instance == null)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning("⚠️ TouchManager not found - this might cause touch input issues");
            }
        }
        else if (enableDebugLogs)
        {
            Debug.Log("✅ TouchManager found and ready");
        }

        if (enableDebugLogs)
        {
            Debug.Log($"=== SAFE SYSTEM INITIALIZATION COMPLETED ===");
            Debug.Log($"Systems created: {systemsCreated}");
            Debug.Log($"SaveSystem ready: {SaveSystem.Instance != null}");
            Debug.Log($"SceneTransitionManager ready: {SceneTransitionManager.Instance != null}");
            Debug.Log($"TouchManager ready: {TouchManager.Instance != null}");
        }
    }

    /// <summary>
    /// Manual system initialization for testing
    /// </summary>
    [ContextMenu("Initialize Systems")]
    public void ManualInitializeSystems()
    {
        InitializeSystems();
        Debug.Log("Systems manually initialized");
    }

    /// <summary>
    /// Check system status
    /// </summary>
    [ContextMenu("Check System Status")]
    public void CheckSystemStatus()
    {
        Debug.Log("=== SYSTEM STATUS ===");
        Debug.Log($"SaveSystem: {(SaveSystem.Instance != null ? "✅ Active" : "❌ Missing")}");
        Debug.Log($"SceneTransitionManager: {(SceneTransitionManager.Instance != null ? "✅ Active" : "❌ Missing")}");

        if (SaveSystem.Instance != null)
        {
            var saveData = SaveSystem.Instance.GetSaveData();
            Debug.Log($"SaveSystem - Completed Objects: {saveData.GetCompletedCount()}");
            Debug.Log($"SaveSystem - Save File: {(SaveSystem.Instance.SaveFileExists() ? "✅ Exists" : "❌ Not Found")}");
        }

        Debug.Log("====================");
    }
}