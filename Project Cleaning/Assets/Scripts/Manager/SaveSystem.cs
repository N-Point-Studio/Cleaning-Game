using System.IO;
using UnityEngine;

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
        // Singleton setup
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
        // Setup save file path
        saveFilePath = Path.Combine(Application.persistentDataPath, saveFileName);

        if (enableDebugLogs)
        {
            Debug.Log($"SaveSystem initialized. Save path: {saveFilePath}");
        }

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
            if (File.Exists(saveFilePath))
            {
                string jsonContent = File.ReadAllText(saveFilePath);
                currentSaveData = JsonUtility.FromJson<SaveData>(jsonContent);

                if (enableDebugLogs)
                {
                    Debug.Log($"Data loaded successfully. Completed objects: {currentSaveData.GetCompletedCount()}");
                }
            }
            else
            {
                // Create new save data if no file exists
                currentSaveData = new SaveData();

                if (enableDebugLogs)
                {
                    Debug.Log("No save file found. Created new save data.");
                }
            }

            // Notify listeners
            OnDataLoaded?.Invoke(currentSaveData);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading save data: {e.Message}");
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
            if (currentSaveData == null)
            {
                currentSaveData = new SaveData();
            }

            string jsonContent = JsonUtility.ToJson(currentSaveData, true);
            File.WriteAllText(saveFilePath, jsonContent);

            if (enableDebugLogs)
            {
                Debug.Log($"Data saved successfully. Completed objects: {currentSaveData.GetCompletedCount()}");
            }

            // Notify listeners
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
        Debug.Log("=== SAVE DATA INFO ===");
        Debug.Log($"Save File Path: {saveFilePath}");
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
            Debug.Log("No completed objects found.");
        }
        Debug.Log("====================");
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

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SaveData();
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