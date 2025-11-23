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
        LogDebug("🔄 Starting focus restoration coroutine");

        // Wait for scene to be fully loaded
        yield return new WaitForSeconds(0.5f);

        // Find the target object
        GameObject targetObject = FindTargetObject();
        if (targetObject == null)
        {
            LogDebug($"❌ Could not find target object: {savedFocusData.objectName}");
            yield break;
        }

        LogDebug($"✅ Found target object: {targetObject.name}");

        // Wait for TopDownCameraController to be ready
        TopDownCameraController cameraController = null;
        int attempts = 0;
        while (attempts < 20) // 2 seconds max
        {
            try
            {
                cameraController = TopDownCameraController.Instance;
                if (cameraController != null && cameraController.gameObject != null && cameraController.enabled)
                {
                    break;
                }
            }
            catch (System.Exception ex)
            {
                LogDebug($"⚠️ Waiting for camera controller (attempt {attempts + 1}): {ex.Message}");
            }

            yield return new WaitForSeconds(0.1f);
            attempts++;
        }

        if (cameraController == null)
        {
            LogDebug("❌ TopDownCameraController not available after waiting");
            yield break;
        }

        LogDebug("✅ TopDownCameraController ready, proceeding with focus");

        // DIRECT ZOOM RESTORATION - Mimic TopDownCameraController click behavior
        LogDebug("🎯 MIMICKING TOPDOWNCAMERACONTROLLER CLICK BEHAVIOR");

        bool restorationSuccess = false;
        try
        {
            // Step 1: Set focus target (same as TopDownCameraController.SetFocusTarget)
            cameraController.SetFocusTarget(targetObject.transform);
            LogDebug("✅ Focus target set");

            // Step 2: IMMEDIATELY call TransitionToFocus (same as ClickableObject click)
            cameraController.TransitionToFocus();
            LogDebug("✅ TransitionToFocus called - zoom animation should start");

            restorationSuccess = true;
        }
        catch (System.Exception ex)
        {
            LogDebug($"❌ Error during zoom restoration: {ex.Message}");
        }

        if (restorationSuccess)
        {
            // Wait for zoom transition to begin
            yield return new WaitForSeconds(0.2f);

            // Step 3: Ensure GameModeManager is in zoom mode
            try
            {
                if (GameModeManager.Instance != null)
                {
                    GameModeManager.Instance.ForceEnterZoomMode();
                    LogDebug("✅ GameModeManager set to zoom mode");
                }
            }
            catch (System.Exception ex)
            {
                LogDebug($"❌ Error setting game mode: {ex.Message}");
            }

            // Step 4: Update clickable object state
            try
            {
                var clickableComponent = targetObject.GetComponent<ClickableObject>();
                if (clickableComponent != null)
                {
                    clickableComponent.SetFocusState(true);
                    LogDebug($"✅ Updated clickable object focus state");
                }
            }
            catch (System.Exception ex)
            {
                LogDebug($"❌ Error updating clickable object: {ex.Message}");
            }

            LogDebug("🎯 ZOOM RESTORATION COMPLETED - CAMERA SHOULD BE ZOOMING TO TARGET");
        }
    }

    private GameObject FindTargetObject()
    {
        LogDebug($"🔍 Searching for object: {savedFocusData.objectName}");

        // Method 1: Direct GameObject.Find
        GameObject directFind = GameObject.Find(savedFocusData.objectName);
        if (directFind != null)
        {
            LogDebug($"✅ Found by direct search: {directFind.name}");
            return directFind;
        }

        // Method 2: Search through all ClickableObjects
        ClickableObject[] clickableObjects = FindObjectsOfType<ClickableObject>();
        LogDebug($"🔍 Searching {clickableObjects.Length} ClickableObjects");

        // Priority 1: Exact name match
        foreach (var clickable in clickableObjects)
        {
            if (clickable.name.Equals(savedFocusData.objectName, System.StringComparison.OrdinalIgnoreCase))
            {
                LogDebug($"✅ Found by exact name: {clickable.name}");
                return clickable.gameObject;
            }
        }

        // Priority 2: Contains match
        foreach (var clickable in clickableObjects)
        {
            if (clickable.name.Contains(savedFocusData.objectName) ||
                savedFocusData.objectName.Contains(clickable.name))
            {
                LogDebug($"✅ Found by partial name: {clickable.name}");
                return clickable.gameObject;
            }
        }

        // Priority 3: Position match
        if (savedFocusData.objectPosition != Vector3.zero)
        {
            GameObject closest = null;
            float closestDistance = float.MaxValue;

            foreach (var clickable in clickableObjects)
            {
                float distance = Vector3.Distance(clickable.transform.position, savedFocusData.objectPosition);
                if (distance < 1f && distance < closestDistance)
                {
                    closest = clickable.gameObject;
                    closestDistance = distance;
                }
            }

            if (closest != null)
            {
                LogDebug($"✅ Found by position: {closest.name} (distance: {closestDistance:F2})");
                return closest;
            }
        }

        LogDebug($"❌ Object not found: {savedFocusData.objectName}");
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