using System.IO;
using UnityEngine;
using DG.Tweening;
using UnityEngine.SceneManagement;
using System;

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

    // Current save data
    private SaveData currentSaveData;
    private string saveFilePath;

    // Events
    public System.Action<SaveData> OnDataLoaded;
    public System.Action<SaveData> OnDataSaved;
    public System.Action OnDataReset;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSaveSystem();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeSaveSystem()
    {
        saveFilePath = Path.Combine(Application.persistentDataPath, saveFileName);
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
            if (File.Exists(saveFilePath))
            {
                string jsonContent = File.ReadAllText(saveFilePath);
                currentSaveData = JsonUtility.FromJson<SaveData>(jsonContent);
            }
            else
            {
                currentSaveData = new SaveData();
            }
            OnDataLoaded?.Invoke(currentSaveData);
        }
        catch (System.Exception)
        {
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
            if (currentSaveData == null) currentSaveData = new SaveData();
            currentSaveData.lastSaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string jsonContent = JsonUtility.ToJson(currentSaveData, true);
            File.WriteAllText(saveFilePath, jsonContent);
            OnDataSaved?.Invoke(currentSaveData);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error saving data: {e.Message}");
        }
    }

    #endregion

    #region Data Access Methods

    /// <summary>
    /// Get current save data
    /// </summary>
    public SaveData GetSaveData()
    {
        if (currentSaveData == null) LoadData();
        return currentSaveData;
    }

    /// <summary>
    /// Check if object is already completed
    /// </summary>
    public bool IsObjectCompleted(string objectName, ObjectType objectType) => GetSaveData().IsObjectCompleted(objectName, objectType);

    /// <summary>
    /// Mark object as completed and save
    /// </summary>
    public void MarkObjectCompleted(string objectName, ObjectType objectType, ChapterType chapterType, Vector3 position)
    {
        GetSaveData().AddCompletedObject(objectName, objectType, chapterType, position);
        SaveData();
    }

    /// <summary>
    /// Remove object from completed list
    /// </summary>
    public bool RemoveCompletedObject(string objectName, ObjectType objectType)
    {
        bool removed = GetSaveData().RemoveCompletedObject(objectName, objectType);
        if (removed) SaveData();
        return removed;
    }

    /// <summary>
    /// Check if intro transition has been shown before
    /// </summary>
    public bool IsIntroTransitionShown() => GetSaveData().IsIntroTransitionShown();

    /// <summary>
    /// Mark intro transition as shown and save
    /// </summary>
    public void SetIntroTransitionShown(bool shown = true)
    {
        GetSaveData().SetIntroTransitionShown(shown);
        SaveData();
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

        PlayerPrefs.DeleteKey("TUTORIAL_COMPLETED");
        PlayerPrefs.Save();

        if (SceneTransitionManager.Instance != null) SceneTransitionManager.Instance.ResetTransitionData();
        if (SimpleCameraFocusRestore.Instance != null) SimpleCameraFocusRestore.Instance.ClearFocusData();
        if (CameraStateManager.Instance != null) CameraStateManager.Instance.ClearSavedState();
        if (CameraAnimationController.Instance != null) CameraAnimationController.Instance.ResetStartupUIState();
        if (GameModeManager.Instance != null) GameModeManager.Instance.ExitToInitialMode();
        
        ResetAllProgressAndReload();
    }

    /// <summary>
    /// Reset progress then reload the current scene (for quick testing via inspector context menu).
    /// </summary>
    [ContextMenu("Reset All Progress & Reload Scene")]
    public void ResetAllProgressAndReload()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        if (CameraAnimationController.Instance != null) CameraAnimationController.Instance.StopAllCoroutines();
        if (UITransitionController.Instance != null) UITransitionController.Instance.StopAllCoroutines();

        AdvancedInputManager.EndTransitionLock();
        DG.Tweening.DOTween.KillAll();

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.ResetTransitionData();

            SceneTransitionManager.Instance.StartStagedTransition(
                "TransitionScreen",
                currentScene,
                0.5f,
                null,
                false,
                true,
                false
            );
        }
        else
        {
            SceneManager.LoadScene(currentScene);
        }
    }

    /// <summary>
    /// Delete save file completely
    /// </summary>
    [ContextMenu("Delete Save File")]
    public void DeleteSaveFile()
    {
        if (File.Exists(saveFilePath)) File.Delete(saveFilePath);
        currentSaveData = new SaveData();
        PlayerPrefs.DeleteKey("TUTORIAL_COMPLETED");
        PlayerPrefs.Save();
        OnDataReset?.Invoke();
    }

    /// <summary>
    /// Print current save data info to console
    /// </summary>
    [ContextMenu("Print Save Data Info")]
    public void PrintSaveDataInfo()
    {
        var saveData = GetSaveData();
        if (enableDebugLogs)
        {
            Debug.Log("========================================");
            Debug.Log("=== SAVE DATA INFO ===");
            Debug.Log($"Save File Path: {saveFilePath}");
            Debug.Log($"File Exists: {File.Exists(saveFilePath)}");
        }

        if (File.Exists(saveFilePath))
        {
            long fileSize = new FileInfo(saveFilePath).Length;
            if (enableDebugLogs)
            {
                Debug.Log($"File Last Modified: {File.GetLastWriteTime(saveFilePath)}");
                Debug.Log($"File Size: {fileSize} bytes");
            }
        }

        if (enableDebugLogs)
        {
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
    }

    /// <summary>
    /// Print save file location for manual verification
    /// </summary>
    [ContextMenu("Show Save File Location")]
    public void ShowSaveFileLocation()
    {
        if (enableDebugLogs)
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
        if (enableDebugLogs)
        {
            Debug.Log("========================================");
            Debug.Log("=== APPLICATION QUITTING ===");
            Debug.Log("Saving data before quit...");
        }
    
        SaveData();

        if (enableDebugLogs)
        {
            // CleanupDOTween();
            Debug.Log("Data saved. Cleaning up...");
            Debug.Log("========================================");
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            if (enableDebugLogs)
            {
                Debug.Log("=== SaveSystem DESTROYING ===");
                Debug.Log("Saving data before destroy...");
            }

            SaveData();
            // CleanupDOTween();
            
            if (enableDebugLogs)
                Debug.Log("SaveSystem destroyed.");
        }
    }

    /// <summary>
    /// Clean up DOTween instance to avoid leftover [DOTween] GameObject warnings on scene close.
    /// </summary>
    // private void CleanupDOTween()
    // {
    //     try
    //     {
    //         DOTween.KillAll();
    //         DOTween.Clear(true);
    //         var dotweenGO = GameObject.Find("[DOTween]");
    //         if (dotweenGO != null)
    //         {
    //             Destroy(dotweenGO);
    //         }
    //     }
    //     catch (System.Exception ex)
    //     {
    //         Debug.LogWarning($"DOTween cleanup failed: {ex.Message}");
    //     }
    // }

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
