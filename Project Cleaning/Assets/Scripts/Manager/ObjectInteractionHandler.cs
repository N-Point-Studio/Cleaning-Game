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

    [Header("ContentSwitcher Integration")]
    [SerializeField] private bool enableContentSwitcherTrigger = true;
    [SerializeField] private bool onlyTriggerInZoomMode = false; // Only trigger when in zoom mode

    // Core components
    private Camera playerCamera;
    private bool justEnteredZoomMode = false;
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
            GameModeManager.Instance.OnEnterZoomMode += () => {
                justEnteredZoomMode = true;
                lastModeChangeTime = Time.time;
                Debug.Log("=== OBJECT HANDLER - Zoom mode entered ===");
            };
            GameModeManager.Instance.OnEnterExplorationMode += () => {
                justEnteredZoomMode = false;
                lastModeChangeTime = Time.time;
                Debug.Log("=== OBJECT HANDLER - Exploration mode entered ===");
            };
            GameModeManager.Instance.OnEnterInitialMode += () => {
                justEnteredZoomMode = false;
                lastModeChangeTime = Time.time;
                Debug.Log("=== OBJECT HANDLER - Initial mode entered ===");
            };
        }
    }
    #endregion

    #region Object Interaction
    public bool CheckForObjectClick(Vector2 screenPosition)
    {
        Debug.Log("=== CheckForObjectClick called ===");

        // STABILITY CHECK: Wait a bit after mode changes to ensure state is stable
        if (Time.time - lastModeChangeTime < clickValidationDelay)
        {
            Debug.Log($"=== CLICK BLOCKED - Mode change too recent ({Time.time - lastModeChangeTime:F2}s ago) ===");
            return false;
        }

        var ray = playerCamera.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, maxClickDistance, clickableLayerMask))
        {
            Debug.Log("No object hit by raycast");
            return false;
        }

        Debug.Log($"Hit object: {hit.collider.name}");

        if (!hit.collider.TryGetComponent<ClickableObject>(out var clickable))
        {
            Debug.Log("Object has no ClickableObject component");
            return false;
        }

        var currentMode = GameModeManager.Instance?.GetCurrentMode() ?? GameModeManager.GameMode.Initial;
        Debug.Log($"Found ClickableObject on: {clickable.name}, Current mode: {currentMode}, Time since last mode change: {Time.time - lastModeChangeTime:F2}s");

        switch (currentMode)
        {
            case GameModeManager.GameMode.Exploration:
                Debug.Log("=== EXPLORATION CLICK - Entering zoom mode ===");
                HandleExplorationClick(clickable);
                break;

            case GameModeManager.GameMode.Zoom:
                Debug.Log("=== ZOOM CLICK - Handling zoom interaction ===");
                HandleZoomClick(clickable);
                break;

            default:
                Debug.Log("=== DEFAULT CLICK - Playing feedback only ===");
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
            Debug.Log("=== CLICK IGNORED - Transition in progress ===");
            return;
        }
        AdvancedInputManager.StartTransitionLock(); // Acquire the lock

        Debug.Log($"=== HANDLE EXPLORATION CLICK START: {clickable.name} ===");
        Debug.Log($"=== CURRENT MODE BEFORE CHANGE: {GameModeManager.Instance?.GetCurrentMode()} ===");

        // Show text popup first in exploration mode
        PlayClickFeedback(clickable);

        // STEP 1: Change mode state first
        Debug.Log("=== STEP 1: Changing to Zoom mode ===");

        if (GameModeManager.Instance == null)
        {
            Debug.LogError("=== ERROR: GameModeManager.Instance is NULL! ===");
            AdvancedInputManager.EndTransitionLock(); // Release lock on error
            return;
        }

        GameModeManager.Instance.EnterZoomMode();

        // Immediate check after mode change
        Debug.Log($"=== MODE AFTER CHANGE ATTEMPT: {GameModeManager.Instance.GetCurrentMode()} ===");

        // EMERGENCY: If mode change failed, try force method immediately
        if (GameModeManager.Instance.GetCurrentMode() != GameModeManager.GameMode.Zoom)
        {
            Debug.LogError($"=== MODE CHANGE FAILED! Trying force method ===");
            GameModeManager.Instance.ForceEnterZoomMode();
            Debug.Log($"=== MODE AFTER FORCE: {GameModeManager.Instance.GetCurrentMode()} ===");
        }

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
            Debug.LogWarning($"=== ERROR: Expected Zoom mode but got {currentMode}, using force method ===");
            // Force mode change if needed
            GameModeManager.Instance?.ForceEnterZoomMode();

            // Wait another frame after force mode change
            yield return null;

            // Verify again
            currentMode = GameModeManager.Instance?.GetCurrentMode() ?? GameModeManager.GameMode.Initial;
            Debug.Log($"=== FORCE RESULT: Now in {currentMode} mode ===");
        }

        // Set focus target for camera
        var cameraController = CameraAnimationController.Instance;
        if (cameraController != null)
        {
            Debug.Log("=== Setting camera controller zoom target ===");
            cameraController.EnterZoomMode(target);
        }
        else
        {
            Debug.LogError("=== CameraAnimationController is null! ===");
        }
    }

    private void HandleZoomClick(ClickableObject clickable)
    {
        if (justEnteredZoomMode)
        {
            justEnteredZoomMode = false;
            PlayClickFeedback(clickable);

            // Enable inspectable functionality for focused object in zoom mode
            if (clickable.IsInspectable())
            {
                clickable.SetInspectableEnabled(true);
            }
            return;
        }

        if (enableSceneChange && clickable.CanChangeScene())
            HandleSceneChange(clickable);

        PlayClickFeedback(clickable);
    }

    private void HandleSceneChange(ClickableObject clickableObject)
    {
        if (clickableObject?.CanChangeScene() != true)
            return;

        string sceneName = clickableObject.GetSceneName();
        if (string.IsNullOrEmpty(sceneName))
            return;

        // NEW: Store clicked object info for later use when returning from gameplay
        StoreClickedObjectInfo(clickableObject);

        // Use Easy Transitions if available and enabled
        if (clickableObject.UseTransitionAnimation() && clickableObject.GetTransitionSettings() != null)
        {
            EasyTransition.TransitionManager.Instance().Transition(sceneName, clickableObject.GetTransitionSettings(), 0f);
        }
        else
        {
            // Fallback to direct scene load
            StartCoroutine(ChangeSceneCoroutine(sceneName));
        }
    }

    /// <summary>
    /// Store clicked object info for updating it after gameplay completion
    /// </summary>
    private void StoreClickedObjectInfo(ClickableObject clickedObject)
    {
        if (clickedObject == null) return;

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
            Debug.LogWarning("SceneTransitionManager not found! Clicked object info not stored.");
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

        // Play audio feedback
        if (clickable.ClickSound != null && clickable.TryGetComponent<AudioSource>(out var audioSource))
            audioSource.PlayOneShot(clickable.ClickSound);

        // Mark as focused and trigger events
        clickable.SetFocusState(true);
        clickable.OnObjectClicked?.Invoke();

        // Call the OnClick method to trigger popup image
        Debug.Log("Calling clickable.OnClick()");
        clickable.OnClick();

        // DISABLED: Don't trigger ContentSwitcher on click - only trigger after finish game
        // TriggerContentSwitcherForObject(clickable);
    }

    public void DisableAllInspectableObjects()
    {
        // Find all clickable objects and disable their inspectable functionality
        ClickableObject[] allClickables = FindObjectsOfType<ClickableObject>();
        foreach (var clickable in allClickables)
        {
            if (clickable.IsInspectable())
            {
                clickable.SetInspectableEnabled(false);
                clickable.SetFocusState(false);
            }
        }
    }

    public Vector3 CalculateWorldPositionFromScreen(Vector2 screenPosition, Vector3 explorationPosition)
    {
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
    /// Helper method to get ChapterType from ObjectType
    /// </summary>
    private ChapterType GetChapterFromObjectType(ObjectType objectType)
    {
        switch (objectType)
        {
            case ObjectType.ChinaCoin:
            case ObjectType.ChinaJar:
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