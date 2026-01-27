using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles object interaction and clickable object management
/// </summary>
public class ObjectInteractionHandler : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private LayerMask clickableLayerMask = -1;
    [SerializeField] private float maxClickDistance = 100f;

    [Header("Scene Change Settings")]
    [SerializeField] private bool enableSceneChange = true;
    [SerializeField] private float sceneChangeDelay = 0.5f;
    [SerializeField] private float clickValidationDelay = 0.1f; // Delay to ensure mode state is stable
    [SerializeField] private string transitionScreenName = "TransitionScreen";

    [Header("ContentSwitcher Integration")]
    [SerializeField] private bool enableContentSwitcherTrigger = true;
    [SerializeField] private bool onlyTriggerInZoomMode = false; // Only trigger when in zoom mode

    // Core components
    private Camera playerCamera;
    private float lastModeChangeTime = 0f;

    // Singleton
    public static ObjectInteractionHandler Instance { get; private set; }

    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            playerCamera = Camera.main;

            // Pastikan SceneTransitionManager sudah ada sejak awal supaya data klik tersimpan (hindari fallback ke Coin).
            if (SceneTransitionManager.Instance == null)
            {
                var stmGO = new GameObject("SceneTransitionManager");
                stmGO.AddComponent<SceneTransitionManager>();
                Debug.Log("[ObjectInteractionHandler] Bootstrap SceneTransitionManager at startup");
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Subscribe to game mode events
        if (GameModeManager.Instance != null)
        {
            GameModeManager.Instance.OnEnterZoomMode += () =>
            {
                lastModeChangeTime = Time.time;
                Debug.Log("=== OBJECT HANDLER - Zoom mode entered ===");
            };
            GameModeManager.Instance.OnEnterExplorationMode += () =>
            {
                lastModeChangeTime = Time.time;
                Debug.Log("=== OBJECT HANDLER - Exploration mode entered ===");
            };
            GameModeManager.Instance.OnEnterInitialMode += () =>
            {
                lastModeChangeTime = Time.time;
                Debug.Log("=== OBJECT HANDLER - Initial mode entered ===");
            };
        }

        // Subscribe to scene loaded events to refresh camera reference
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Refresh camera reference when scene loads
        RefreshCameraReference();

        // Fail-safe: make sure transition lock is cleared when a scene finishes loading
        AdvancedInputManager.EndTransitionLock();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
    #endregion

    #region Object Interaction
    public bool CheckForObjectClick(Vector2 screenPosition)
    {
        // NULL CHECK: Ensure camera is valid before using it
        if (playerCamera == null)
        {
            Debug.LogError("=== CAMERA NULL - Finding new camera ===");
            playerCamera = Camera.main;

            if (playerCamera == null)
            {
                Debug.LogError("=== NO MAIN CAMERA FOUND - Cannot process click ===");
                return false;
            }
        }

        // STABILITY CHECK: Wait a bit after mode changes to ensure state is stable
        if (Time.time - lastModeChangeTime < clickValidationDelay)
        {
            return false;
        }

        var ray = playerCamera.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, maxClickDistance, clickableLayerMask))
        {
            return false;
        }

        if (!hit.collider.TryGetComponent<ClickableObject>(out var clickable))
        {
            return false;
        }

        var currentMode = GameModeManager.Instance?.GetCurrentMode() ?? GameModeManager.GameMode.Initial;
        switch (currentMode)
        {
            case GameModeManager.GameMode.Exploration:
                HandleExplorationClick(clickable);
                break;

            case GameModeManager.GameMode.Zoom:
                HandleZoomClick(clickable);
                break;

            default:
                PlayClickFeedback(clickable);
                break;
        }

        return true;
    }

    private void HandleExplorationClick(ClickableObject clickable)
    {
        // Do not start a new transition if one is already happening
        if (AdvancedInputManager.IsInTransition)
        {
            return;
        }
        AdvancedInputManager.StartTransitionLock(); // Acquire the lock

        // Show text popup first in exploration mode
        PlayClickFeedback(clickable);

        // STEP 1: Change mode state first

        if (GameModeManager.Instance == null)
        {
            Debug.LogError("=== ERROR: GameModeManager.Instance is NULL! ===");
            AdvancedInputManager.EndTransitionLock(); // Release lock on error
            return;
        }

        GameModeManager.Instance.EnterZoomMode();

        // Immediate check after mode change
        // STEP 2: Wait a frame to ensure mode change is processed, then set camera focus
        StartCoroutine(SetCameraFocusDelayed(clickable.transform));
    }

    private System.Collections.IEnumerator SetCameraFocusDelayed(Transform target)
    {
        // Wait a frame to ensure GameModeManager state change is fully processed
        yield return null;

        Debug.Log($"=== STEP 2: Setting camera focus to: {target.name} ===");

        // Verify we're actually in zoom mode now
        var currentMode = GameModeManager.Instance?.GetCurrentMode() ?? GameModeManager.GameMode.Initial;
        if (currentMode != GameModeManager.GameMode.Zoom)
        {
            // Force mode change if needed
            GameModeManager.Instance?.ForceEnterZoomMode();

            // Wait another frame after force mode change
            yield return null;

            // Verify again
            currentMode = GameModeManager.Instance?.GetCurrentMode() ?? GameModeManager.GameMode.Initial;
        }

        // Set focus target for camera
        var cameraController = CameraAnimationController.Instance;
        if (cameraController != null)
        {
            cameraController.EnterZoomMode(target);
        }
        else
        {
            Debug.LogError("=== CameraAnimationController is null! ===");
        }
    }

    private void HandleZoomClick(ClickableObject clickable)
    {
        if (enableSceneChange && clickable.CanChangeScene())
        {
            PlayClickFeedback(clickable); // Play feedback before changing scene.
            HandleSceneChange(clickable);
        }
        else
        {
            PlayClickFeedback(clickable);
        }
    }

    private void HandleSceneChange(ClickableObject clickableObject)
    {
        if (clickableObject?.CanChangeScene() != true)
            return;

        string finalSceneName = clickableObject.GetSceneName();
        if (string.IsNullOrEmpty(finalSceneName))
            return;

        // Always store clicked object info so gameplay knows which artefact to load.
        StoreClickedObjectInfo(clickableObject);

        // --- New Self-Contained Transition Logic ---

        // If a staged transition is needed, use the self-contained runner.
        if (clickableObject.UseStagedTransition())
        {
            Debug.Log("=== Starting Staged Scene Transition (Standalone) ===");
            GameObject transitionerGO = new GameObject("StagedTransitionRunner");
            DontDestroyOnLoad(transitionerGO);
            var runner = transitionerGO.AddComponent<StagedTransitionRunner>();
            runner.StartTransition(
                clickableObject.GetIntermediarySceneName(),
                finalSceneName,
                clickableObject.GetIntermediaryDelay()
            );
            return;
        }
        // Otherwise, perform a simple, direct scene change.
        else
        {
            // Prefer global SceneTransitionManager with TransitionScreen intermediary when available
            if (SceneTransitionManager.Instance != null)
            {
                Debug.Log("=== Using TransitionScreen via SceneTransitionManager ===");
                SceneTransitionManager.Instance.SetTransitionDirectionToGameplay();
                SceneTransitionManager.Instance.SetObjectTypeForTransition(clickableObject.GetObjectType());
                SceneTransitionManager.Instance.StartStagedTransition(
                    transitionScreenName,
                    finalSceneName,
                    0f,
                    null
                );
            }
            else
            {
                Debug.Log("=== Using Direct Scene Transition (Standalone) ===");
                StartCoroutine(ChangeSceneCoroutine(finalSceneName));
            }
        }
    }

    /// <summary>
    /// Store clicked object info for updating it after gameplay completion
    /// </summary>
    private void StoreClickedObjectInfo(ClickableObject clickedObject)
    {
        if (clickedObject == null) return;

        // Ensure SceneTransitionManager exists so data is available in gameplay scene
        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogWarning("SceneTransitionManager not found, creating one to store clicked object info...");
            var stmGO = new GameObject("SceneTransitionManager");
            stmGO.AddComponent<SceneTransitionManager>();
        }

        // Get object info
        string objectName = clickedObject.name;
        Vector3 objectPosition = clickedObject.transform.position;
        ObjectType objectType = clickedObject.GetObjectType();

        Debug.Log($"=== STORING CLICKED OBJECT INFO ===");
        Debug.Log($"Object Name: {objectName}");
        Debug.Log($"Object Position: {objectPosition}");
        Debug.Log($"Object Type: {objectType}");

        // Store in SceneTransitionManager
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.SetObjectTypeForTransitionWithClickedObject(
                objectType,
                objectName,
                objectPosition
            );
        }
        else
        {
            Debug.LogError("SceneTransitionManager creation failed - clicked object info not stored!");
        }
    }

    private IEnumerator ChangeSceneCoroutine(string sceneName)
    {
        if (sceneChangeDelay > 0)
            yield return new WaitForSeconds(sceneChangeDelay);

        SceneManager.LoadScene(sceneName);
    }

    private void PlayClickFeedback(ClickableObject clickable)
    {
        Debug.Log("=== PlayClickFeedback called ===");

        if (clickable == null)
        {
            Debug.Log("Clickable is null!");
            return;
        }

        Debug.Log($"PlayClickFeedback for: {clickable.name}");

        // Play audio feedback (global click + object-specific if any)
        MenuSfxManager.Instance?.PlayClick();

        AudioSource audioSource = null;
        if (!clickable.TryGetComponent<AudioSource>(out audioSource))
        {
            audioSource = clickable.gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }
        if (clickable.ClickSound != null)
        {
            audioSource.PlayOneShot(clickable.ClickSound);
        }

        // Mark as focused and trigger events
        clickable.SetFocusState(true);
        clickable.OnObjectClicked?.Invoke();

        // Call the OnClick method to trigger popup image
        Debug.Log("Calling clickable.OnClick()");
        clickable.OnClick();

        // DISABLED: Don't trigger ContentSwitcher on click - only trigger after finish game
        // TriggerContentSwitcherForObject(clickable);
    }

    public void ResetAllObjectStates()
    {
        // Find all clickable objects and reset their focus state
        ClickableObject[] allClickables = FindObjectsOfType<ClickableObject>();
        foreach (var clickable in allClickables)
        {
            clickable.SetFocusState(false);
        }
    }

    public Vector3 CalculateWorldPositionFromScreen(Vector2 screenPosition, Vector3 explorationPosition)
    {
        // NULL CHECK: Ensure camera is valid
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                Debug.LogError("No camera available for position calculation");
                return explorationPosition;
            }
        }

        // Create a ray from camera through screen position
        var ray = playerCamera.ScreenPointToRay(screenPosition);

        // Try to hit something in the world
        if (Physics.Raycast(ray, out RaycastHit hit, maxClickDistance, clickableLayerMask))
        {
            // If we hit something, zoom to that point
            return hit.point;
        }

        // If no hit, calculate position on current camera plane
        var planeDistance = 2f; // Distance from current camera position
        var focusPosition = ray.origin + ray.direction * planeDistance;

        // Clamp the Y position to be at the same level as exploration mode
        focusPosition.y = explorationPosition.y;

        return focusPosition;
    }

    /// <summary>
    /// Trigger ContentSwitcher based on ClickableObject's ObjectType
    /// </summary>
    private void TriggerContentSwitcherForObject(ClickableObject clickableObject)
    {
        if (clickableObject == null) return;

        // Check if ContentSwitcher trigger is enabled
        if (!enableContentSwitcherTrigger)
        {
            Debug.Log("ContentSwitcher trigger disabled");
            return;
        }

        // Check if we should only trigger in zoom mode
        if (onlyTriggerInZoomMode)
        {
            var currentMode = GameModeManager.Instance?.GetCurrentMode() ?? GameModeManager.GameMode.Initial;
            if (currentMode != GameModeManager.GameMode.Zoom)
            {
                Debug.Log($"ContentSwitcher trigger skipped - not in zoom mode (current: {currentMode})");
                return;
            }
        }

        // Get ObjectType from ClickableObject
        ObjectType objectType = clickableObject.GetObjectType();
        ChapterType chapterType = clickableObject.GetChapterFromObjectType();

        Debug.Log($"=== TRIGGERING CONTENT SWITCHER ===");
        Debug.Log($"Object: {clickableObject.name}");
        Debug.Log($"ObjectType: {objectType}");
        Debug.Log($"ChapterType: {chapterType}");

        // Find ContentSwitcher in scene
        ContentSwitcher[] contentSwitchers = FindObjectsOfType<ContentSwitcher>();

        if (contentSwitchers.Length == 0)
        {
            Debug.LogWarning("No ContentSwitcher found in scene!");
            return;
        }

        // Find ContentSwitcher that matches our ChapterType
        ContentSwitcher targetSwitcher = null;
        foreach (ContentSwitcher switcher in contentSwitchers)
        {
            if (switcher.GetChapterType() == chapterType)
            {
                targetSwitcher = switcher;
                break;
            }
        }

        // If no exact match, use first available ContentSwitcher
        if (targetSwitcher == null)
        {
            targetSwitcher = contentSwitchers[0];
            Debug.Log($"No exact ChapterType match, using first ContentSwitcher: {targetSwitcher.name}");

            // Set the ChapterType and ObjectType to match our clicked object
            targetSwitcher.SetChapterType(chapterType);
        }

        // Set ObjectType and trigger ContentSwitcher
        targetSwitcher.SetObjectType(objectType);

        Debug.Log($"Triggering ContentSwitcher: {targetSwitcher.name}");
        Debug.Log($"Set to ChapterType: {chapterType}, ObjectType: {objectType}");

        // Trigger the ContentSwitcher
        targetSwitcher.OnButtonClicked();

        Debug.Log("=== CONTENT SWITCHER TRIGGERED ===");
    }

    /// <summary>
    /// Public method to manually trigger ContentSwitcher for specific ObjectType
    /// </summary>
    public void ManualTriggerContentSwitcher(ObjectType objectType)
    {
        Debug.Log($"=== MANUAL TRIGGER CONTENT SWITCHER ===");
        Debug.Log($"Requested ObjectType: {objectType}");

        // Create a temporary object info for the trigger
        ChapterType chapterType = GetChapterFromObjectType(objectType);

        // Find and trigger ContentSwitcher
        ContentSwitcher[] contentSwitchers = FindObjectsOfType<ContentSwitcher>();

        if (contentSwitchers.Length == 0)
        {
            Debug.LogWarning("No ContentSwitcher found in scene for manual trigger!");
            return;
        }

        ContentSwitcher targetSwitcher = null;
        foreach (ContentSwitcher switcher in contentSwitchers)
        {
            if (switcher.GetChapterType() == chapterType)
            {
                targetSwitcher = switcher;
                break;
            }
        }

        if (targetSwitcher == null)
        {
            targetSwitcher = contentSwitchers[0];
            targetSwitcher.SetChapterType(chapterType);
        }

        targetSwitcher.SetObjectType(objectType);
        targetSwitcher.OnButtonClicked();

        Debug.Log($"Manual trigger completed for ObjectType: {objectType}");
    }

    /// <summary>
    /// Refresh camera reference (useful when scene changes)
    /// </summary>
    public void RefreshCameraReference()
    {
        Camera newCamera = Camera.main;
        if (newCamera != null)
        {
            playerCamera = newCamera;
            Debug.Log($"Camera reference updated to: {playerCamera.name}");
        }
        else
        {
            Debug.LogWarning("No main camera found for refresh");
        }
    }

    /// <summary>
    /// Manually set camera reference (for debugging)
    /// </summary>
    [ContextMenu("Set Camera to Main Camera")]
    public void SetCameraToMain()
    {
        playerCamera = Camera.main;
        Debug.Log($"Camera manually set to: {(playerCamera != null ? playerCamera.name : "NULL")}");
    }

    /// <summary>
    /// Check camera status (for debugging)
    /// </summary>
    [ContextMenu("Check Camera Status")]
    public void CheckCameraStatus()
    {
        Debug.Log("=== CAMERA STATUS ===");
        Debug.Log($"Player Camera: {(playerCamera != null ? playerCamera.name : "NULL")}");
        Debug.Log($"Camera.main: {(Camera.main != null ? Camera.main.name : "NULL")}");
        Debug.Log($"Cameras in scene: {Camera.allCamerasCount}");

        if (Camera.allCamerasCount > 0)
        {
            Debug.Log("Available cameras:");
            foreach (Camera cam in Camera.allCameras)
            {
                Debug.Log($"  - {cam.name} (Active: {cam.gameObject.activeInHierarchy})");
            }
        }
        Debug.Log("====================");
    }

    /// <summary>
    /// Helper method to get ChapterType from ObjectType
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

    /// <summary>
    /// Enable or disable ContentSwitcher trigger functionality
    /// </summary>
    public void SetContentSwitcherTriggerEnabled(bool enabled)
    {
        enableContentSwitcherTrigger = enabled;
        Debug.Log($"ContentSwitcher trigger {(enabled ? "enabled" : "disabled")}");
    }

    /// <summary>
    /// Test method to find and list all ContentSwitchers in scene
    /// </summary>
    [System.Obsolete("For testing only")]
    public void DebugListContentSwitchers()
    {
        ContentSwitcher[] contentSwitchers = FindObjectsOfType<ContentSwitcher>();
        Debug.Log($"=== CONTENT SWITCHER DEBUG ===");
        Debug.Log($"Found {contentSwitchers.Length} ContentSwitcher(s) in scene:");

        for (int i = 0; i < contentSwitchers.Length; i++)
        {
            var switcher = contentSwitchers[i];
            Debug.Log($"{i + 1}. Name: {switcher.name}");
            Debug.Log($"   ChapterType: {switcher.GetChapterType()}");
            Debug.Log($"   ObjectType: {switcher.GetObjectType()}");
        }
    }
    #endregion
}
