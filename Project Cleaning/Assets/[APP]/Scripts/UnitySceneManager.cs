using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UnitySceneManager : MonoBehaviour
{
    [Header("Scene Management Settings")]
    [SerializeField] private bool useLoadingScreen = true;
    [SerializeField] private float minimumLoadingTime = 1f;

    [Header("Scene List")]
    [SerializeField] private List<GameSceneData> scenes = new List<GameSceneData>();

    [Header("Loading Screen (Optional)")]
    [SerializeField] private GameObject loadingScreenPrefab;
    [SerializeField] private Canvas loadingCanvas;
    [SerializeField] private Slider progressBar;
    [SerializeField] private TMPro.TextMeshProUGUI loadingText;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    // Static instance for easy access
    public static UnitySceneManager Instance { get; private set; }

    // Events
    public System.Action<string> OnSceneLoadStarted;
    public System.Action<string> OnSceneLoadCompleted;
    public System.Action<float> OnSceneLoadProgress;

    // Current scene info
    private string currentSceneName;
    private List<string> sceneHistory = new List<string>();

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Initialize()
    {
        currentSceneName = SceneManager.GetActiveScene().name;

        // Setup loading screen canvas if exists
        if (loadingCanvas != null)
        {
            DontDestroyOnLoad(loadingCanvas.gameObject);
            loadingCanvas.gameObject.SetActive(false);
        }

        Log($"UnitySceneManager initialized. Current scene: {currentSceneName}");
    }

    #region Public Methods

    /// <summary>
    /// Load scene by name
    /// </summary>
    public void LoadScene(string sceneName)
    {
        var sceneData = GetSceneData(sceneName);
        if (sceneData == null)
        {
            LogWarning($"Scene '{sceneName}' not found in scene list!");
            return;
        }

        StartCoroutine(LoadSceneAsync(sceneData));
    }

    /// <summary>
    /// Load scene with history for back navigation
    /// </summary>
    public void NavigateToScene(string sceneName)
    {
        // Add current scene to history
        if (!string.IsNullOrEmpty(currentSceneName) && currentSceneName != sceneName)
        {
            sceneHistory.Add(currentSceneName);
        }

        LoadScene(sceneName);
    }

    /// <summary>
    /// Go back to previous scene
    /// </summary>
    public void GoBackToPreviousScene()
    {
        if (sceneHistory.Count > 0)
        {
            string previousScene = sceneHistory[sceneHistory.Count - 1];
            sceneHistory.RemoveAt(sceneHistory.Count - 1);
            LoadScene(previousScene);
        }
        else
        {
            LogWarning("No previous scene to go back to!");
        }
    }

    /// <summary>
    /// Reload current scene
    /// </summary>
    public void ReloadCurrentScene()
    {
        LoadScene(currentSceneName);
    }

    /// <summary>
    /// Load scene additively (keeps current scene loaded)
    /// </summary>
    public void LoadSceneAdditive(string sceneName)
    {
        var sceneData = GetSceneData(sceneName);
        if (sceneData == null)
        {
            LogWarning($"Scene '{sceneName}' not found!");
            return;
        }

        StartCoroutine(LoadSceneAdditiveAsync(sceneData));
    }

    /// <summary>
    /// Unload scene (for additive scenes)
    /// </summary>
    public void UnloadScene(string sceneName)
    {
        StartCoroutine(UnloadSceneAsync(sceneName));
    }

    /// <summary>
    /// Get current scene name
    /// </summary>
    public string GetCurrentSceneName()
    {
        return currentSceneName;
    }

    /// <summary>
    /// Check if scene exists in build settings
    /// </summary>
    public bool DoesSceneExist(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneNameInBuild = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (sceneNameInBuild == sceneName)
                return true;
        }
        return false;
    }

    #endregion

    #region Coroutines

    private IEnumerator LoadSceneAsync(GameSceneData sceneData)
    {
        OnSceneLoadStarted?.Invoke(sceneData.sceneName);
        Log($"Loading scene: {sceneData.sceneName}");

        // Show loading screen
        if (useLoadingScreen)
        {
            ShowLoadingScreen(sceneData);
        }

        // Start loading
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneData.sceneName);
        loadOperation.allowSceneActivation = false;

        float startTime = Time.time;

        // Wait for loading to complete
        while (!loadOperation.isDone)
        {
            float progress = Mathf.Clamp01(loadOperation.progress / 0.9f);
            UpdateLoadingProgress(progress);
            OnSceneLoadProgress?.Invoke(progress);

            // Allow activation when ready and minimum time passed
            if (loadOperation.progress >= 0.9f &&
                Time.time - startTime >= minimumLoadingTime)
            {
                loadOperation.allowSceneActivation = true;
            }

            yield return null;
        }

        // Scene loaded
        currentSceneName = sceneData.sceneName;
        UpdateLoadingProgress(1f);

        // Hide loading screen
        if (useLoadingScreen)
        {
            yield return new WaitForSeconds(0.5f); // Brief pause to show 100%
            HideLoadingScreen();
        }

        OnSceneLoadCompleted?.Invoke(sceneData.sceneName);
        Log($"Scene loaded: {sceneData.sceneName}");
    }

    private IEnumerator LoadSceneAdditiveAsync(GameSceneData sceneData)
    {
        Log($"Loading scene additively: {sceneData.sceneName}");

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneData.sceneName, LoadSceneMode.Additive);

        while (!loadOperation.isDone)
        {
            yield return null;
        }

        Log($"Scene loaded additively: {sceneData.sceneName}");
    }

    private IEnumerator UnloadSceneAsync(string sceneName)
    {
        Log($"Unloading scene: {sceneName}");

        AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(sceneName);

        while (!unloadOperation.isDone)
        {
            yield return null;
        }

        Log($"Scene unloaded: {sceneName}");
    }

    #endregion

    #region Loading Screen

    private void ShowLoadingScreen(GameSceneData sceneData)
    {
        if (loadingCanvas != null)
        {
            loadingCanvas.gameObject.SetActive(true);

            if (loadingText != null)
            {
                loadingText.text = string.IsNullOrEmpty(sceneData.loadingText)
                    ? $"Loading {sceneData.sceneName}..."
                    : sceneData.loadingText;
            }

            UpdateLoadingProgress(0f);
        }
        else if (loadingScreenPrefab != null)
        {
            GameObject loadingScreen = Instantiate(loadingScreenPrefab);
            DontDestroyOnLoad(loadingScreen);
            // Auto-destroy after use
            StartCoroutine(DestroyLoadingScreenAfterDelay(loadingScreen, 3f));
        }
    }

    private void HideLoadingScreen()
    {
        if (loadingCanvas != null)
        {
            loadingCanvas.gameObject.SetActive(false);
        }
    }

    private void UpdateLoadingProgress(float progress)
    {
        if (progressBar != null)
        {
            progressBar.value = progress;
        }
    }

    private IEnumerator DestroyLoadingScreenAfterDelay(GameObject loadingScreen, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (loadingScreen != null)
        {
            Destroy(loadingScreen);
        }
    }

    #endregion

    #region Helper Methods

    private GameSceneData GetSceneData(string sceneName)
    {
        return scenes.Find(s => s.sceneName == sceneName);
    }

    private void Log(string message)
    {
        if (showDebugLogs)
            Debug.Log($"[UnitySceneManager] {message}");
    }

    private void LogWarning(string message)
    {
        if (showDebugLogs)
            Debug.LogWarning($"[UnitySceneManager] {message}");
    }

    #endregion

    #region Button Helper Methods (For UI)

    /// <summary>
    /// Helper for button OnClick events
    /// </summary>
    public void LoadSceneButton(string sceneName)
    {
        LoadScene(sceneName);
    }

    /// <summary>
    /// Helper for button OnClick events
    /// </summary>
    public void NavigateToSceneButton(string sceneName)
    {
        NavigateToScene(sceneName);
    }

    /// <summary>
    /// Helper for button OnClick events
    /// </summary>
    public void GoBackButton()
    {
        GoBackToPreviousScene();
    }

    /// <summary>
    /// Helper for button OnClick events
    /// </summary>
    public void ReloadSceneButton()
    {
        ReloadCurrentScene();
    }

    /// <summary>
    /// Helper for button OnClick events
    /// </summary>
    public void QuitGameButton()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    #endregion
}

[System.Serializable]
public class GameSceneData
{
    [Header("Scene Info")]
    public string sceneName;

    [Header("Display")]
    public string displayName;
    public string loadingText = "Loading...";

    [Header("Settings")]
    [Tooltip("Scene description for documentation")]
    public string description;
}