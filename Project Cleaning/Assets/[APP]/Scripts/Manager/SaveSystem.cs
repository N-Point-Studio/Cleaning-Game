using System.IO;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Manages saving and loading game progress using JSON
/// Singleton pattern for easy access throughout the game
/// </summary>
public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance { get; private set; }

    [Header("Save Settings")]
    [SerializeField] private string saveFileName = "GameProgress.json";
    [SerializeField] private bool enableDebugLogs = false;
    [Header("UI Hooks")]
    [SerializeField] private UnityEngine.UI.Button resetButton; // Optional: drag a Reset button here
    [SerializeField] private bool showResetButtonInInitialModeOnly = true;
    [SerializeField] private UITransitionController uiTransitionController; // Optional: hook StartExploration event

    // Current save data
    private SaveData currentSaveData;
    private string saveFilePath;

    // Events
    public System.Action<SaveData> OnDataLoaded;
    public System.Action<SaveData> OnDataSaved;
    public System.Action OnDataReset;

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSaveSystem();
            WireResetButton();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeSaveSystem()
    {
        // Setup save file path
        saveFilePath = Path.Combine(Application.persistentDataPath, saveFileName);

        Debug.Log("========================================");
        Debug.Log("=== SAVE SYSTEM INITIALIZATION ===");
        Debug.Log($"Save File Name: {saveFileName}");
        Debug.Log($"Persistent Data Path: {Application.persistentDataPath}");
        Debug.Log($"Full Save Path: {saveFilePath}");
        Debug.Log($"File Exists: {File.Exists(saveFilePath)}");
        Debug.Log("========================================");

        // Load existing data or create new
        LoadData();
    }

    #region Save/Load Operations

    /// <summary>
    /// Load save data from JSON file
    /// </summary>
    public void LoadData()
    {
        try
        {
            Debug.Log("========================================");
            Debug.Log("=== LOADING SAVE DATA ===");
            Debug.Log($"Checking file: {saveFilePath}");
            Debug.Log($"File exists: {File.Exists(saveFilePath)}");

            if (File.Exists(saveFilePath))
            {
                string jsonContent = File.ReadAllText(saveFilePath);

                Debug.Log($"File size: {jsonContent.Length} characters");
                Debug.Log($"JSON Content:\n{jsonContent}");

                currentSaveData = JsonUtility.FromJson<SaveData>(jsonContent);

                Debug.Log($"✅ Data loaded successfully!");
                Debug.Log($"   Completed objects: {currentSaveData.GetCompletedCount()}");
                Debug.Log($"   Last save time: {currentSaveData.lastSaveTime}");

                if (currentSaveData.completedObjects.Count > 0)
                {
                    Debug.Log("   Completed objects list:");
                    foreach (var obj in currentSaveData.completedObjects)
                    {
                        Debug.Log($"      - {obj.GetInfo()}");
                    }
                }
            }
            else
            {
                // Create new save data if no file exists
                currentSaveData = new SaveData();

                Debug.LogWarning("⚠️ No save file found. Created new empty save data.");
                Debug.LogWarning("   This is normal on first run.");
            }

            Debug.Log("========================================");

            // Notify listeners
            OnDataLoaded?.Invoke(currentSaveData);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Error loading save data: {e.Message}");
            Debug.LogError($"   Stack trace: {e.StackTrace}");
            currentSaveData = new SaveData();
        }
    }

    /// <summary>
    /// Save current data to JSON file
    /// </summary>
    public void SaveData()
    {
        try
        {
            Debug.Log("========================================");
            Debug.Log("=== SAVING DATA TO DISK ===");

            if (currentSaveData == null)
            {
                Debug.LogWarning("⚠️ currentSaveData was null! Creating new SaveData.");
                currentSaveData = new SaveData();
            }

            // Update last save time
            currentSaveData.lastSaveTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            string jsonContent = JsonUtility.ToJson(currentSaveData, true);

            Debug.Log($"Saving to: {saveFilePath}");
            Debug.Log($"JSON size: {jsonContent.Length} characters");
            Debug.Log($"Completed objects: {currentSaveData.GetCompletedCount()}");

            if (currentSaveData.completedObjects.Count > 0)
            {
                Debug.Log("Objects being saved:");
                foreach (var obj in currentSaveData.completedObjects)
                {
                    Debug.Log($"   - {obj.GetInfo()}");
                }
            }

            Debug.Log($"JSON Content:\n{jsonContent}");

            // Write to file
            File.WriteAllText(saveFilePath, jsonContent);

            // Verify file was written
            bool fileExists = File.Exists(saveFilePath);
            long fileSize = fileExists ? new FileInfo(saveFilePath).Length : 0;

            Debug.Log($"✅ File written successfully!");
            Debug.Log($"   File exists: {fileExists}");
            Debug.Log($"   File size: {fileSize} bytes");
            Debug.Log("========================================");

            // Notify listeners
            OnDataSaved?.Invoke(currentSaveData);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Error saving data: {e.Message}");
            Debug.LogError($"   Stack trace: {e.StackTrace}");
        }
    }

    #endregion

    #region Data Access Methods

    /// <summary>
    /// Get current save data
    /// </summary>
    public SaveData GetSaveData()
    {
        if (currentSaveData == null)
        {
            LoadData();
        }
        return currentSaveData;
    }

    /// <summary>
    /// Check if object is already completed
    /// </summary>
    public bool IsObjectCompleted(string objectName, ObjectType objectType)
    {
        return GetSaveData().IsObjectCompleted(objectName, objectType);
    }

    /// <summary>
    /// Mark object as completed and save
    /// </summary>
    public void MarkObjectCompleted(string objectName, ObjectType objectType, ChapterType chapterType, Vector3 position)
    {
        GetSaveData().AddCompletedObject(objectName, objectType, chapterType, position);
        SaveData();

        if (enableDebugLogs)
        {
            Debug.Log($"Object marked as completed: {objectName} ({objectType})");
        }
    }

    /// <summary>
    /// Remove object from completed list
    /// </summary>
    public bool RemoveCompletedObject(string objectName, ObjectType objectType)
    {
        bool removed = GetSaveData().RemoveCompletedObject(objectName, objectType);
        if (removed)
        {
            SaveData();
            if (enableDebugLogs)
            {
                Debug.Log($"Object removed from completed list: {objectName} ({objectType})");
            }
        }
        return removed;
    }

    /// <summary>
    /// Check if intro transition has been shown before
    /// </summary>
    public bool IsIntroTransitionShown()
    {
        return GetSaveData().IsIntroTransitionShown();
    }

    /// <summary>
    /// Mark intro transition as shown and save
    /// </summary>
    public void SetIntroTransitionShown(bool shown = true)
    {
        GetSaveData().SetIntroTransitionShown(shown);
        SaveData();

        if (enableDebugLogs)
        {
            Debug.Log($"Intro transition marked as {(shown ? "shown" : "not shown")}");
        }
    }

    #endregion

    #region Reset & Testing Methods

    /// <summary>
    /// Reset all game progress (for testing)
    /// </summary>
    [ContextMenu("Reset All Progress")]
    public void ResetAllProgress()
    {
        currentSaveData = new SaveData();
        SaveData();

        Debug.Log("All game progress has been reset!");

        // Notify listeners
        OnDataReset?.Invoke();
    }

    private void WireResetButton()
    {
        if (resetButton == null)
        {
            return;
        }

        resetButton.onClick.RemoveListener(ResetAllProgress);
        resetButton.onClick.AddListener(ResetAllProgress);
        Debug.Log("[SaveSystem] Reset button wired to ResetAllProgress");

        // Optional: only visible/clickable in Initial mode
        if (showResetButtonInInitialModeOnly)
        {
            StartCoroutine(WaitForGameModeManagerThenHook());
        }

        // If a UITransitionController is assigned, also hide the reset button when its start transition begins
        if (uiTransitionController == null)
        {
            uiTransitionController = UITransitionController.Instance;
        }
        if (uiTransitionController != null)
        {
            uiTransitionController.OnStartExploration += HideResetButton;
        }
    }

    private System.Collections.IEnumerator WaitForGameModeManagerThenHook()
    {
        int attempts = 0;
        while (GameModeManager.Instance == null && attempts < 50)
        {
            yield return null;
            attempts++;
        }

        if (GameModeManager.Instance != null)
        {
            HandleModeChanged(GameModeManager.Instance.GetCurrentMode());
            GameModeManager.Instance.OnModeChanged += HandleModeChanged;
        }
        else
        {
            Debug.LogWarning("[SaveSystem] GameModeManager not found; reset button visibility will not auto-toggle.");
        }
    }

    private void HandleModeChanged(GameModeManager.GameMode mode)
    {
        if (!showResetButtonInInitialModeOnly || resetButton == null) return;
        ToggleResetButton(mode == GameModeManager.GameMode.Initial);
    }

    private void ToggleResetButton(bool visible)
    {
        if (resetButton == null) return;

        resetButton.gameObject.SetActive(visible);
        var cg = resetButton.GetComponent<CanvasGroup>() ?? resetButton.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = visible ? 1f : 0f;
        resetButton.interactable = visible;
        cg.blocksRaycasts = visible;
    }

    private void HideResetButton()
    {
        ToggleResetButton(false);
    }

    /// <summary>
    /// Reset progress then reload the current scene (for quick testing via inspector context menu).
    /// </summary>
    [ContextMenu("Reset All Progress & Reload Scene")]
    public void ResetAllProgressAndReload()
    {
        ResetAllProgress();
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        UnityEngine.SceneManagement.SceneManager.LoadScene(scene.name);
    }

    /// <summary>
    /// Delete save file completely
    /// </summary>
    [ContextMenu("Delete Save File")]
    public void DeleteSaveFile()
    {
        try
        {
            if (File.Exists(saveFilePath))
            {
                File.Delete(saveFilePath);
                Debug.Log("Save file deleted successfully!");
            }

            currentSaveData = new SaveData();
            OnDataReset?.Invoke();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error deleting save file: {e.Message}");
        }
    }

    /// <summary>
    /// Print current save data info to console
    /// </summary>
    [ContextMenu("Print Save Data Info")]
    public void PrintSaveDataInfo()
    {
        var saveData = GetSaveData();
        Debug.Log("========================================");
        Debug.Log("=== SAVE DATA INFO ===");
        Debug.Log($"Save File Path: {saveFilePath}");
        Debug.Log($"File Exists: {File.Exists(saveFilePath)}");

        if (File.Exists(saveFilePath))
        {
            long fileSize = new FileInfo(saveFilePath).Length;
            Debug.Log($"File Size: {fileSize} bytes");
            Debug.Log($"File Last Modified: {File.GetLastWriteTime(saveFilePath)}");
        }

        Debug.Log($"Save Version: {saveData.saveVersion}");
        Debug.Log($"Last Save Time: {saveData.lastSaveTime}");
        Debug.Log($"Total Completed Objects: {saveData.GetCompletedCount()}");
        Debug.Log($"Intro Transition Shown: {saveData.IsIntroTransitionShown()}");

        if (saveData.completedObjects.Count > 0)
        {
            Debug.Log("Completed Objects:");
            foreach (var obj in saveData.completedObjects)
            {
                Debug.Log($"  - {obj.GetInfo()}");
            }
        }
        else
        {
            Debug.Log("⚠️ No completed objects found.");
        }
        Debug.Log("========================================");
    }

    /// <summary>
    /// Print save file location for manual verification
    /// </summary>
    [ContextMenu("Show Save File Location")]
    public void ShowSaveFileLocation()
    {
        Debug.Log("========================================");
        Debug.Log("=== SAVE FILE LOCATION ===");
        Debug.Log($"Full Path: {saveFilePath}");
        Debug.Log($"Directory: {Path.GetDirectoryName(saveFilePath)}");
        Debug.Log($"File Name: {Path.GetFileName(saveFilePath)}");
        Debug.Log($"File Exists: {File.Exists(saveFilePath)}");

        if (File.Exists(saveFilePath))
        {
            Debug.Log("✅ File found! You can manually check this file.");
            Debug.Log($"   Last modified: {File.GetLastWriteTime(saveFilePath)}");
            Debug.Log($"   Size: {new FileInfo(saveFilePath).Length} bytes");
        }
        else
        {
            Debug.LogWarning("⚠️ File not found! Save may not have been written yet.");
        }
        Debug.Log("========================================");
    }

    #endregion

    #region Application Events

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveData();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            SaveData();
        }
    }

    private void OnApplicationQuit()
    {
        Debug.Log("========================================");
        Debug.Log("=== APPLICATION QUITTING ===");
        Debug.Log("Saving data before quit...");

        // CRITICAL: Save data before app closes
        SaveData();

        Debug.Log("Data saved. Cleaning up...");
        CleanupDOTween();
        Debug.Log("========================================");
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            if (GameModeManager.Instance != null)
            {
                GameModeManager.Instance.OnModeChanged -= HandleModeChanged;
            }
            if (uiTransitionController != null)
            {
                uiTransitionController.OnStartExploration -= HideResetButton;
            }
            Debug.Log("=== SaveSystem DESTROYING ===");
            Debug.Log("Saving data before destroy...");

            SaveData();
            CleanupDOTween();

            Debug.Log("SaveSystem destroyed.");
        }
    }

    /// <summary>
    /// Clean up DOTween instance to avoid leftover [DOTween] GameObject warnings on scene close.
    /// </summary>
    private void CleanupDOTween()
    {
        try
        {
            DOTween.KillAll();
            DOTween.Clear(true);
            var dotweenGO = GameObject.Find("[DOTween]");
            if (dotweenGO != null)
            {
                Destroy(dotweenGO);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"DOTween cleanup failed: {ex.Message}");
        }
    }

    #endregion

    #region Public Utilities

    /// <summary>
    /// Check if save file exists
    /// </summary>
    public bool SaveFileExists()
    {
        return File.Exists(saveFilePath);
    }

    /// <summary>
    /// Get save file path
    /// </summary>
    public string GetSaveFilePath()
    {
        return saveFilePath;
    }

    /// <summary>
    /// Enable or disable debug logs
    /// </summary>
    public void SetDebugMode(bool enabled)
    {
        enableDebugLogs = enabled;
    }

    #endregion
}
