using System.Collections;
using UnityEngine;

/// <summary>
/// Centralized camera state persistence manager
/// Handles camera state saving and restoration across scene transitions
/// </summary>
public class CameraStateManager : MonoBehaviour
{
    [System.Serializable]
    public class CameraState
    {
        public string focusedObjectName;
        public Vector3 focusedObjectPosition;
        public ObjectType objectType;
        public bool hasFocus;
        public float timestamp;

        public CameraState()
        {
            focusedObjectName = "";
            focusedObjectPosition = Vector3.zero;
            objectType = ObjectType.ChinaCoin;
            hasFocus = false;
            timestamp = 0f;
        }

        public bool IsValid()
        {
            return hasFocus && !string.IsNullOrEmpty(focusedObjectName);
        }

        public override string ToString()
        {
            return $"CameraState(Object: {focusedObjectName}, Type: {objectType}, HasFocus: {hasFocus})";
        }
    }

    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogs = true;

    // Singleton pattern
    public static CameraStateManager Instance { get; private set; }

    // Persistent camera state
    private static CameraState persistentState = new CameraState();

    // Restoration control
    private bool isRestorationInProgress = false;
    private Coroutine restorationCoroutine;

    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LogDebug("🔧 CameraStateManager initialized and persisted across scenes");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Subscribe to scene loaded events for automatic restoration
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
    #endregion

    #region Public Interface

    /// <summary>
    /// Save current camera focus state
    /// Call this before scene transition
    /// </summary>
    public void SaveCurrentCameraState()
    {
        LogDebug("=== SAVING CAMERA STATE ===");

        // SAFE ACCESS: Use try-catch to prevent destroyed object errors
        try
        {
            var topDownCamera = TopDownCameraController.Instance;
            if (topDownCamera == null || topDownCamera.gameObject == null)
            {
                LogDebug("⚠️ TopDownCameraController not available - cannot save state");
                return;
            }

            Transform focusTarget = null;

            // SAFE ACCESS: Wrap in try-catch since object might be destroyed
            try
            {
                focusTarget = topDownCamera.GetCurrentFocus();
            }
            catch (System.Exception ex)
            {
                LogDebug($"⚠️ Error accessing focus target: {ex.Message}");
                return;
            }

            persistentState.timestamp = Time.time;

            if (focusTarget != null && focusTarget.gameObject != null)
            {
                persistentState.focusedObjectName = focusTarget.name;
                persistentState.focusedObjectPosition = focusTarget.position;
                persistentState.hasFocus = true;

                // Try to get ObjectType from ClickableObject component
                try
                {
                    var clickableObject = focusTarget.GetComponent<ClickableObject>();
                    if (clickableObject != null)
                    {
                        persistentState.objectType = clickableObject.GetObjectType();
                    }
                }
                catch (System.Exception ex)
                {
                    LogDebug($"⚠️ Error accessing ClickableObject: {ex.Message}");
                }

                LogDebug($"✅ Camera state saved: {persistentState}");
            }
            else
            {
                persistentState.hasFocus = false;
                LogDebug("📝 No focus target - saved empty state");
            }
        }
        catch (System.Exception ex)
        {
            LogDebug($"❌ Error saving camera state: {ex.Message}");
        }
    }

    /// <summary>
    /// Restore camera state in current scene
    /// Call this after scene is loaded
    /// </summary>
    public void RestoreCameraState()
    {
        if (isRestorationInProgress)
        {
            LogDebug("⚠️ Restoration already in progress - skipping");
            return;
        }

        if (!persistentState.IsValid())
        {
            LogDebug("📝 No valid camera state to restore");
            return;
        }

        LogDebug($"🔄 Starting camera state restoration: {persistentState}");

        if (restorationCoroutine != null)
        {
            StopCoroutine(restorationCoroutine);
        }

        restorationCoroutine = StartCoroutine(RestoreCameraStateCoroutine());
    }

    /// <summary>
    /// Clear saved state (useful for reset scenarios)
    /// </summary>
    public void ClearSavedState()
    {
        persistentState = new CameraState();
        LogDebug("🧹 Camera state cleared");
    }

    /// <summary>
    /// Get current saved state for debugging
    /// </summary>
    public CameraState GetSavedState()
    {
        return persistentState;
    }

    /// <summary>
    /// Check if there's a valid state to restore
    /// </summary>
    public bool HasValidStateToRestore()
    {
        return persistentState.IsValid();
    }

    #endregion

    #region Internal Methods

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        LogDebug($"🔄 Scene loaded: {scene.name}");

        // Only auto-restore for menu scenes (where camera should return to focus)
        if (IsMenuScene(scene.name))
        {
            // Delay restoration to ensure all systems are initialized
            // StartCoroutine(DelayedAutoRestore());
        }
    }

    private bool IsMenuScene(string sceneName)
    {
        return sceneName.Contains("New Start Game Sandy") ||
               sceneName.Contains("Menu") ||
               sceneName.Contains("Main");
    }

    private IEnumerator DelayedAutoRestore()
    {
        // Wait for basic systems to initialize
        yield return new WaitForSeconds(0.2f);

        // PRIORITY: Restore camera state IMMEDIATELY to prevent other systems from overriding
        LogDebug("🚀 PRIORITY: Auto-triggering camera state restoration with high priority");
        RestoreCameraState();

        // Additional wait for ContentSwitcher completion (after camera restoration)
        yield return new WaitForSeconds(1f);

        LogDebug("✅ Camera state restoration completed, other systems can proceed");
    }

    private IEnumerator RestoreCameraStateCoroutine()
    {
        isRestorationInProgress = true;
        LogDebug($"=== STARTING CAMERA RESTORATION PROCESS ===");

        // Step 1: Wait for camera controller to be available with safe checks
        TopDownCameraController topDownCamera = null;
        int waitAttempts = 0;
        const int maxWaitAttempts = 50; // 5 seconds max

        while (waitAttempts < maxWaitAttempts)
        {
            try
            {
                topDownCamera = TopDownCameraController.Instance;

                // Additional safety check: ensure the gameObject is not destroyed
                if (topDownCamera != null && topDownCamera.gameObject != null && topDownCamera.enabled)
                {
                    break; // Found valid camera controller
                }

                topDownCamera = null; // Reset if not valid
            }
            catch (System.Exception ex)
            {
                LogDebug($"⚠️ Error accessing TopDownCameraController (attempt {waitAttempts + 1}): {ex.Message}");
                topDownCamera = null;
            }

            yield return new WaitForSeconds(0.1f);
            waitAttempts++;
        }

        if (topDownCamera == null)
        {
            LogDebug("❌ TopDownCameraController not available after waiting - aborting restoration");
            isRestorationInProgress = false;
            yield break;
        }

        LogDebug("✅ TopDownCameraController found, proceeding with restoration");

        // Step 2: Find the target object in current scene
        GameObject targetObject = FindTargetObjectInScene();

        if (targetObject == null)
        {
            LogDebug($"❌ Could not find target object '{persistentState.focusedObjectName}' - falling back to exploration mode");
            yield return RestoreToExplorationMode(topDownCamera);
            isRestorationInProgress = false;
            yield break;
        }

        LogDebug($"✅ Found target object: {targetObject.name}");

        // Step 3: Set up game mode and camera
        yield return SetupGameModeForRestore();

        // Step 4: Restore camera focus
        yield return RestoreCameraFocus(topDownCamera, targetObject);

        LogDebug("✅ Camera state restoration completed successfully");
        isRestorationInProgress = false;
    }

    private GameObject FindTargetObjectInScene()
    {
        LogDebug($"🔍 Searching for target object: '{persistentState.focusedObjectName}' (Type: {persistentState.objectType})");

        // Method 1: Direct name search with exact match priority
        GameObject directFind = GameObject.Find(persistentState.focusedObjectName);
        if (directFind != null)
        {
            LogDebug($"✅ Found by direct search: {directFind.name}");
            return directFind;
        }

        // Method 2: Search through ClickableObjects with improved logic
        ClickableObject[] clickableObjects = FindObjectsOfType<ClickableObject>();
        LogDebug($"🔍 Searching through {clickableObjects.Length} ClickableObjects");

        // PRIORITY 1: Exact name match
        foreach (var clickable in clickableObjects)
        {
            if (clickable.name.Equals(persistentState.focusedObjectName))
            {
                LogDebug($"✅ Found by EXACT name match: {clickable.name}");
                return clickable.gameObject;
            }
        }

        // PRIORITY 2: Name contains (partial match)
        foreach (var clickable in clickableObjects)
        {
            if (clickable.name.Contains(persistentState.focusedObjectName) ||
                persistentState.focusedObjectName.Contains(clickable.name))
            {
                LogDebug($"✅ Found by PARTIAL name match: {clickable.name} (searching for: {persistentState.focusedObjectName})");
                return clickable.gameObject;
            }
        }

        // PRIORITY 3: Position-based search (most reliable)
        if (persistentState.focusedObjectPosition != Vector3.zero)
        {
            GameObject closestMatch = null;
            float closestDistance = float.MaxValue;

            foreach (var clickable in clickableObjects)
            {
                float distance = Vector3.Distance(clickable.transform.position, persistentState.focusedObjectPosition);
                if (distance < 1f && distance < closestDistance) // Within 1 unit and closest
                {
                    closestMatch = clickable.gameObject;
                    closestDistance = distance;
                }
            }

            if (closestMatch != null)
            {
                LogDebug($"✅ Found by POSITION match: {closestMatch.name} (distance: {closestDistance:F2})");
                return closestMatch;
            }
        }

        // PRIORITY 4: ObjectType match (LAST RESORT - less reliable)
        LogDebug($"⚠️ Using ObjectType match as LAST RESORT for type: {persistentState.objectType}");
        foreach (var clickable in clickableObjects)
        {
            if (clickable.GetObjectType() == persistentState.objectType)
            {
                LogDebug($"⚠️ Found by ObjectType match (last resort): {clickable.name} (Type: {persistentState.objectType})");
                LogDebug($"   WARNING: This might not be the exact object that was clicked!");
                return clickable.gameObject;
            }
        }

        LogDebug($"❌ Target object '{persistentState.focusedObjectName}' not found in scene");
        return null;
    }

    private IEnumerator SetupGameModeForRestore()
    {
        LogDebug("🔧 Setting up game mode for camera restoration");

        // Ensure GameModeManager is in proper state
        if (GameModeManager.Instance != null)
        {
            // Force to exploration mode first, then to zoom mode
            if (!GameModeManager.Instance.IsInExplorationMode())
            {
                LogDebug("🔄 Setting GameModeManager to exploration mode");
                GameModeManager.Instance.StartExplorationMode();
                yield return new WaitForSeconds(0.1f);
            }

            LogDebug("🔄 Setting GameModeManager to zoom mode");
            GameModeManager.Instance.EnterZoomMode();
        }

        // Ensure CameraAnimationController is ready
        if (CameraAnimationController.Instance != null)
        {
            LogDebug("🔄 Initializing CameraAnimationController exploration mode");
            CameraAnimationController.Instance.BeginExplorationMode();
        }

        yield return new WaitForSeconds(0.2f);
    }

    private IEnumerator RestoreCameraFocus(TopDownCameraController cameraController, GameObject targetObject)
    {
        LogDebug($"🎯 Restoring camera focus to: {targetObject.name}");

        // SAFE ACCESS: Check controller is still valid
        if (cameraController == null || cameraController.gameObject == null)
        {
            LogDebug("❌ CameraController became invalid during restoration");
            yield break;
        }

        // Set focus target with error handling
        bool setFocusSuccess = false;
        try
        {
            cameraController.SetFocusTarget(targetObject.transform);
            setFocusSuccess = true;
            LogDebug("✅ Focus target set successfully");
        }
        catch (System.Exception ex)
        {
            LogDebug($"❌ Error setting focus target: {ex.Message}");
            yield break;
        }

        // Wait a frame for target to be set
        yield return null;

        // SAFE ACCESS: Check controller is still valid before transition
        if (cameraController != null && cameraController.gameObject != null && setFocusSuccess)
        {
            bool transitionSuccess = false;
            try
            {
                // FORCE transition to focus mode with extra verification
                cameraController.TransitionToFocus();
                LogDebug("✅ Camera transition to focus initiated");
                transitionSuccess = true;
            }
            catch (System.Exception ex)
            {
                LogDebug($"❌ Error during camera transition: {ex.Message}");
            }

            // EXTRA: Force switch to focus state to ensure it's applied (outside try-catch)
            if (transitionSuccess)
            {
                yield return new WaitForSeconds(0.1f);

                try
                {
                    cameraController.SwitchState(cameraController.focusState);
                    LogDebug("✅ FORCED focus state switch for extra reliability");
                }
                catch (System.Exception ex)
                {
                    LogDebug($"❌ Error during focus state switch: {ex.Message}");
                }
            }
        }

        // Update clickable object state
        if (targetObject != null)
        {
            try
            {
                var clickableComponent = targetObject.GetComponent<ClickableObject>();
                if (clickableComponent != null)
                {
                    clickableComponent.SetFocusState(true);
                    LogDebug($"✅ Updated focus state for: {targetObject.name}");
                }
            }
            catch (System.Exception ex)
            {
                LogDebug($"❌ Error updating clickable object state: {ex.Message}");
            }
        }

        LogDebug("✅ Camera focus restoration completed");
    }

    private IEnumerator RestoreToExplorationMode(TopDownCameraController cameraController)
    {
        LogDebug("🔧 Falling back to exploration mode");

        // Set GameModeManager to exploration mode
        if (GameModeManager.Instance != null)
        {
            GameModeManager.Instance.ReturnToExplorationMode();
        }

        // Transition camera to overview
        cameraController.TransitionToOverview();

        yield return null;
    }

    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[CameraStateManager] {message}");
        }
    }

    #endregion

    #region Context Menu Debug Methods

    [ContextMenu("Debug: Print Current State")]
    public void DebugPrintCurrentState()
    {
        Debug.Log($"=== CAMERA STATE DEBUG ===");
        Debug.Log($"Persistent State: {persistentState}");
        Debug.Log($"Is Valid: {persistentState.IsValid()}");
        Debug.Log($"Restoration In Progress: {isRestorationInProgress}");

        var topDownCamera = TopDownCameraController.Instance;
        if (topDownCamera != null)
        {
            var currentFocus = topDownCamera.GetCurrentFocus();
            Debug.Log($"Current Camera Focus: {(currentFocus != null ? currentFocus.name : "NULL")}");
        }
        else
        {
            Debug.Log("TopDownCameraController: NULL");
        }
    }

    [ContextMenu("Debug: Save Current State")]
    public void DebugSaveCurrentState()
    {
        SaveCurrentCameraState();
        DebugPrintCurrentState();
    }

    [ContextMenu("Debug: Restore State")]
    public void DebugRestoreState()
    {
        RestoreCameraState();
    }

    [ContextMenu("Debug: Clear State")]
    public void DebugClearState()
    {
        ClearSavedState();
        DebugPrintCurrentState();
    }

    #endregion
}