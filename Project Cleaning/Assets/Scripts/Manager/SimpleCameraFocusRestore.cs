using System.Collections;
using UnityEngine;

/// <summary>
/// SIMPLE APPROACH: Direct camera focus restoration without complex systems
/// Integrates directly with existing SceneTransitionManager workflow
/// </summary>
public class SimpleCameraFocusRestore : MonoBehaviour
{
    [System.Serializable]
    public class FocusData
    {
        public string objectName;
        public Vector3 objectPosition;
        public ObjectType objectType;
        public bool isValid;

        public void Clear()
        {
            objectName = "";
            objectPosition = Vector3.zero;
            objectType = ObjectType.ChinaCoin;
            isValid = false;
        }

        public override string ToString()
        {
            return $"FocusData(Object: {objectName}, Type: {objectType}, Valid: {isValid})";
        }
    }

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    // Static data persists across scenes
    private static FocusData savedFocusData = new FocusData();

    // Singleton pattern
    public static SimpleCameraFocusRestore Instance { get; private set; }

    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LogDebug("✅ SimpleCameraFocusRestore initialized");
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion

    #region Public Interface
    /// <summary>
    /// Save current camera focus - called by SceneTransitionManager
    /// </summary>
    public void SaveCurrentFocus()
    {
        LogDebug("=== SAVING FOCUS DATA ===");

        // Clear previous data
        savedFocusData.Clear();

        try
        {
            var topDownCamera = TopDownCameraController.Instance;
            if (topDownCamera == null || topDownCamera.gameObject == null)
            {
                LogDebug("⚠️ TopDownCameraController not available");
                return;
            }

            var currentFocus = topDownCamera.GetCurrentFocus();
            if (currentFocus != null && currentFocus.gameObject != null)
            {
                savedFocusData.objectName = currentFocus.name;
                savedFocusData.objectPosition = currentFocus.position;
                savedFocusData.isValid = true;

                // Get ObjectType if available
                var clickableObject = currentFocus.GetComponent<ClickableObject>();
                if (clickableObject != null)
                {
                    savedFocusData.objectType = clickableObject.GetObjectType();
                }

                LogDebug($"✅ Focus saved: {savedFocusData}");
            }
            else
            {
                LogDebug("📝 No focus to save");
            }
        }
        catch (System.Exception ex)
        {
            LogDebug($"❌ Error saving focus: {ex.Message}");
        }
    }

    /// <summary>
    /// Restore camera focus - called by SceneTransitionManager after delay
    /// </summary>
    public void RestoreFocus()
    {
        if (!savedFocusData.isValid)
        {
            LogDebug("📝 No valid focus data to restore");
            return;
        }

        LogDebug($"🔄 Starting focus restoration: {savedFocusData}");
        StartCoroutine(RestoreFocusCoroutine());
    }

    /// <summary>
    /// Check if there's valid focus data to restore
    /// </summary>
    public bool HasValidFocusData()
    {
        return savedFocusData.isValid;
    }

    /// <summary>
    /// Get saved focus data for debugging
    /// </summary>
    public FocusData GetSavedFocusData()
    {
        return savedFocusData;
    }

    /// <summary>
    /// Clear saved focus data
    /// </summary>
    public void ClearFocusData()
    {
        savedFocusData.Clear();
        LogDebug("🧹 Focus data cleared");
    }
    #endregion

    #region Internal Implementation
    private IEnumerator RestoreFocusCoroutine()
    {
        LogDebug("🔄 [Robust] Starting focus restoration coroutine...");

        // 1. Wait for TopDownCameraController to be ready to prevent null references.
        TopDownCameraController cameraController = null;
        int attempts = 0;
        while (attempts < 20) // 2 seconds max
        {
            cameraController = TopDownCameraController.Instance;
            if (cameraController != null && cameraController.enabled)
            {
                break;
            }
            yield return new WaitForSeconds(0.1f);
            attempts++;
        }

        if (cameraController == null)
        {
            Debug.LogError("[SimpleCameraFocusRestore] ABORT: TopDownCameraController not found after waiting. Cannot restore focus.");
            yield break;
        }
        LogDebug("✅ [Robust] TopDownCameraController is ready.");

        // 2. Find the target object using the robust search method.
        GameObject targetObject = FindTargetObject();

        // 3. CRITICAL: Handle the case where the object is not found.
        if (targetObject == null)
        {
            // Log a clear error message for debugging.
            Debug.LogError($"[SimpleCameraFocusRestore] RESTORATION FAILED: Could not find target object '{savedFocusData.objectName}' in the current scene. Defaulting to overview mode.");
            
            // Explicitly fall back to the overview state. This makes failure predictable.
            cameraController.TransitionToOverview();

            // Clean up the invalid data.
            ClearFocusData();
            yield break;
        }

        LogDebug($"✅ [Robust] Found target object: {targetObject.name}. Proceeding with zoom.");

        // 4. If object is found, proceed with the zoom restoration.
        try
        {
            GameModeManager.Instance?.ForceEnterZoomMode();
            cameraController.SetFocusTarget(targetObject.transform);
            cameraController.TransitionToFocus();

            var clickableComponent = targetObject.GetComponent<ClickableObject>();
            clickableComponent?.SetFocusState(true);

            LogDebug($"✅ [Robust] Focus restoration commands sent successfully for {targetObject.name}.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SimpleCameraFocusRestore] An exception occurred during the focus restoration process for '{targetObject.name}': {ex.Message}");
            cameraController.TransitionToOverview(); // Fallback on error
        }
        
        // 5. Clean up the focus data now that it has been used.
        ClearFocusData();
    }

    private GameObject FindTargetObject()
    {
        LogDebug($"🔍 [Robust] Searching for '{savedFocusData.objectName}' (Pos: {savedFocusData.objectPosition}, Type: {savedFocusData.objectType})");

        // Method 1: Direct name search (fastest)
        GameObject directFind = GameObject.Find(savedFocusData.objectName);
        if (directFind != null)
        {
            LogDebug($"✅ [Robust] Found via direct GameObject.Find: {directFind.name}");
            return directFind;
        }

        // Method 2: Search all ClickableObjects (more reliable)
        ClickableObject[] clickableObjects = FindObjectsOfType<ClickableObject>();
        LogDebug($"🔍 [Robust] Searching through {clickableObjects.Length} ClickableObjects.");

        // Priority 1: Exact name match
        foreach (var clickable in clickableObjects)
        {
            if (clickable.name.Equals(savedFocusData.objectName, System.StringComparison.Ordinal))
            {
                LogDebug($"✅ [Robust] Found by EXACT name match: {clickable.name}");
                return clickable.gameObject;
            }
        }

        // Priority 2: Position-based search (very reliable if position is saved)
        if (savedFocusData.objectPosition != Vector3.zero)
        {
            GameObject closestMatch = null;
            float closestDistance = float.MaxValue;
            const float searchRadius = 1.0f; 

            foreach (var clickable in clickableObjects)
            {
                float distance = Vector3.Distance(clickable.transform.position, savedFocusData.objectPosition);
                if (distance < searchRadius && distance < closestDistance)
                {
                    closestMatch = clickable.gameObject;
                    closestDistance = distance;
                }
            }

            if (closestMatch != null)
            {
                LogDebug($"✅ [Robust] Found by POSITION match: {closestMatch.name} (Distance: {closestDistance:F2})");
                return closestMatch;
            }
        }

        // Priority 3: ObjectType match (last resort, can be inaccurate)
        foreach (var clickable in clickableObjects)
        {
            if (clickable.GetObjectType() == savedFocusData.objectType)
            {
                LogDebug($"⚠️ [Robust] Found by ObjectType match (LAST RESORT): {clickable.name}. This may not be the correct object.");
                return clickable.gameObject;
            }
        }

        LogDebug($"❌ [Robust] Target object '{savedFocusData.objectName}' not found in scene after all search methods.");
        return null;
    }

    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[SimpleCameraFocusRestore] {message}");
        }
    }
    #endregion
}