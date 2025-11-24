using UnityEngine;
using System.Collections;

/// <summary>
/// Detects double tap gestures and triggers appropriate actions
/// </summary>
public class DoubleTapDetector : MonoBehaviour
{
    [Header("Double Tap Settings")]
    [SerializeField] private float doubleTapTimeWindow = 0.3f; // Maximum time between two taps (increased for better detection)
    [SerializeField] private float doubleTapDistanceThreshold = 50f; // Maximum distance between taps
    [SerializeField] private bool enableDoubleTapZoom = true; // Enable double tap as backup to pinch
    [SerializeField] private bool instantSingleTapReturn = false; // Single tap in zoom mode instantly returns (no double tap needed)
    [SerializeField] private float gestureCooldown = 0.5f; // Increased cooldown to prevent conflicts
    [SerializeField] private float zoomExitCooldownBypass = 0.15f; // Allow early exit double-tap even if global cooldown is active

    // Double tap detection state
    private float lastTapTime = 0f;
    private Vector2 lastTapPosition = Vector2.zero;
    private bool isWaitingForSecondTap = false;
    private float lastGestureTime = 0f; // Prevent rapid gesture conflicts

    // Singleton
    public static DoubleTapDetector Instance { get; private set; }

    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion

    #region Double Tap Detection
    public bool CheckForDoubleTap(Vector2 currentTapPosition)
    {
        if (!enableDoubleTapZoom) return false;

        float currentTime = Time.time;
        float timeSinceLastGesture = currentTime - lastGestureTime;
        bool inZoomMode = GameModeManager.Instance?.GetCurrentMode() == GameModeManager.GameMode.Zoom;

        var currentMode = GameModeManager.Instance?.GetCurrentMode() ?? GameModeManager.GameMode.Initial;

        // ENHANCED COOLDOWN: Block rapid taps more strictly to prevent conflicts
        if (timeSinceLastGesture < gestureCooldown)
        {
            // Allow a quick exit from zoom even if we're still inside the global cooldown window.
            if (inZoomMode && timeSinceLastGesture >= zoomExitCooldownBypass)
            {
                Debug.Log($"=== COOLDOWN BYPASS FOR ZOOM EXIT: {timeSinceLastGesture:F2}s since last gesture (bypass after {zoomExitCooldownBypass:F2}s) ===");
            }
            else
            {
                Debug.Log($"=== GESTURE COOLDOWN - Time since last gesture: {timeSinceLastGesture:F2}s, Mode: {currentMode} ===");
                Debug.Log("=== GESTURE BLOCKED - Too rapid, waiting for cooldown ===");
                return false;
            }
        }

        // INSTANT SINGLE TAP RETURN: If in zoom mode and instant return is enabled, return immediately
        if (instantSingleTapReturn && currentMode == GameModeManager.GameMode.Zoom)
        {
            Debug.Log("=== INSTANT SINGLE TAP RETURN - No delay ===");
            lastGestureTime = currentTime; // Record gesture time
            return true; // Treat single tap as immediate return gesture
        }

        // Check if we're waiting for a second tap
        if (isWaitingForSecondTap)
        {
            // Check if within time window
            if (currentTime - lastTapTime <= doubleTapTimeWindow)
            {
                // Check if within distance threshold
                float tapDistance = Vector2.Distance(currentTapPosition, lastTapPosition);
                if (tapDistance <= doubleTapDistanceThreshold)
                {
                    Debug.Log($"=== DOUBLE TAP DETECTED: Time={currentTime - lastTapTime:F2}s, Distance={tapDistance:F1}px ===");
                    isWaitingForSecondTap = false;
                    lastGestureTime = currentTime; // Record gesture time
                    return true; // Valid double tap
                }
            }
            // Time window expired or distance too far - reset
            isWaitingForSecondTap = false;
        }

        // This is first tap or reset - start waiting for second tap
        lastTapTime = currentTime;
        lastTapPosition = currentTapPosition;
        isWaitingForSecondTap = true;
        Debug.Log($"=== FIRST TAP DETECTED: Waiting for second tap within {doubleTapTimeWindow}s ===");

        return false; // Not a double tap (yet)
    }

    public void HandleDoubleTap(Vector2 tapPosition)
    {
        // Do not start a new transition if one is already happening
        if (AdvancedInputManager.IsInTransition)
        {
            Debug.Log("=== DOUBLE TAP IGNORED - Transition in progress ===");
            return;
        }

        var currentMode = GameModeManager.Instance?.GetCurrentMode() ?? GameModeManager.GameMode.Initial;

        // Block input for a short duration to prevent the second tap from being processed as a new click
        if (AdvancedInputManager.Instance != null)
        {
            // Use a longer delay when exiting zoom, as requested by the user
            if (currentMode == GameModeManager.GameMode.Zoom)
            {
                AdvancedInputManager.Instance.BlockInputFor(1.0f);
            }
            else
            {
                AdvancedInputManager.Instance.BlockInputFor(0.3f);
            }
        }
        
        Debug.Log($"=== HANDLE DOUBLE TAP: Mode={currentMode}, Position={tapPosition} ===");

        // Acquire the lock before starting a transition
        AdvancedInputManager.StartTransitionLock();

        switch (currentMode)
        {
            case GameModeManager.GameMode.Exploration:
                // Double tap in exploration mode = Enter zoom mode at tap position
                Debug.Log("=== DOUBLE TAP TO ZOOM IN ===");
                StartCoroutine(SynchronizedEnterZoom(tapPosition));
                break;

            case GameModeManager.GameMode.Zoom:
                // Double tap in zoom mode = Return to exploration mode with fast animation
                Debug.Log("=== DOUBLE TAP TO ZOOM OUT ===");
                StartCoroutine(SynchronizedReturnToExploration());
                break;

            case GameModeManager.GameMode.Initial:
                // Double tap ignored in initial mode
                Debug.Log("=== DOUBLE TAP IGNORED - Still in Initial mode ===");
                AdvancedInputManager.EndTransitionLock(); // Release lock if no transition happens
                break;
        }
    }

    public void ResetDoubleTapState()
    {
        isWaitingForSecondTap = false;
        lastTapTime = 0f;
    }

    public void ResetGestureCooldown()
    {
        lastGestureTime = 0f;
        // Also reset double tap state to prevent false detections
        ResetDoubleTapState();
        Debug.Log("=== GESTURE COOLDOWN AND DOUBLE TAP STATE RESET ===");
    }

    // Add complete gesture reset for mode transitions
    public void CompleteGestureReset()
    {
        lastGestureTime = 0f;
        lastTapTime = 0f;
        isWaitingForSecondTap = false;
        lastTapPosition = Vector2.zero;
        Debug.Log("=== COMPLETE GESTURE RESET - All states cleared ===");
    }

    // Synchronized enter zoom mode
    private System.Collections.IEnumerator SynchronizedEnterZoom(Vector2 tapPosition)
    {
        var cameraController = TopDownCameraController.Instance;
        if (cameraController == null)
        {
            Debug.LogError("TopDownCameraController.Instance is NULL!");
            AdvancedInputManager.EndTransitionLock(); // Release lock on error
            yield break;
        }

        // Set focus target to closest object to tap position
        Transform closestObject = cameraController.FindClosestObjectToScreenPoint(tapPosition);
        if (closestObject != null)
        {
            cameraController.SetFocusTarget(closestObject);
        }
        else
        {
            Debug.LogWarning("No closest object found for double tap zoom. Using current camera position as focus.");
        }

        // Temporarily set a faster transition duration for double tap zoom
        float fastDoubleTapTransitionDuration = 0.2f; // Define a fast duration
        cameraController.SetTransitionDuration(fastDoubleTapTransitionDuration);

        // Perform all state changes synchronously
        cameraController.SwitchState(cameraController.focusState);
        GameModeManager.Instance?.EnterZoomMode();
        
        // Reset duration after the transition is configured
        cameraController.ResetTransitionDuration();

        Debug.Log($"=== MODE AFTER SYNCHRONIZED ZOOM: {GameModeManager.Instance?.GetCurrentMode()} ===");
        yield break; // Coroutine is done, the lock will be released by the camera tween's OnComplete
    }

    // Synchronized return to exploration mode
    private System.Collections.IEnumerator SynchronizedReturnToExploration()
    {
        // Ensure all objects lose focus and stop shaking before we transition.
        ObjectInteractionHandler.Instance?.ResetAllObjectStates();

        var cameraController = TopDownCameraController.Instance;
        if (cameraController == null)
        {
            Debug.LogError("TopDownCameraController.Instance is NULL!");
            AdvancedInputManager.EndTransitionLock(); // Release lock on error
            yield break;
        }

        // Temporarily set a slower transition duration for zoom out, as requested
        float exitTransitionDuration = 1.0f;
        cameraController.SetTransitionDuration(exitTransitionDuration);

        // Perform all state changes synchronously
        cameraController.SwitchState(cameraController.overviewState);
        GameModeManager.Instance?.ReturnToExplorationMode();

        // Reset duration after the transition is configured
        cameraController.ResetTransitionDuration();

        Debug.Log($"=== MODE AFTER SYNCHRONIZED RETURN: {GameModeManager.Instance?.GetCurrentMode()} ===");
        yield break; // Coroutine is done, the lock will be released by the camera tween's OnComplete
    }
    #endregion
}
