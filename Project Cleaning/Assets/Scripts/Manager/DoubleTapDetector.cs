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

        var currentMode = GameModeManager.Instance?.GetCurrentMode() ?? GameModeManager.GameMode.Initial;

        // ENHANCED COOLDOWN: Block rapid taps more strictly to prevent conflicts
        if (currentTime - lastGestureTime < gestureCooldown)
        {
            Debug.Log($"=== GESTURE COOLDOWN - Time since last gesture: {currentTime - lastGestureTime:F2}s, Mode: {currentMode} ===");

            // Always respect full cooldown period to prevent timing conflicts
            Debug.Log("=== GESTURE BLOCKED - Too rapid, waiting for cooldown ===");
            return false;
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
        var currentMode = GameModeManager.Instance?.GetCurrentMode() ?? GameModeManager.GameMode.Initial;
        Debug.Log($"=== HANDLE DOUBLE TAP: Mode={currentMode}, Position={tapPosition} ===");

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
        // STEP 1: Start camera animation to zoom mode FIRST
        Debug.Log("=== DOUBLE TAP STEP 1: Starting camera animation to zoom mode ===");
        CameraAnimationController.Instance?.EnterZoomModeAtScreenPosition(tapPosition);

        // STEP 2: Wait for camera animation to start
        yield return new WaitForSeconds(0.1f);

        // STEP 3: Change the game mode state to match the visual transition
        Debug.Log("=== DOUBLE TAP STEP 2: Changing to Zoom mode after camera animation started ===");
        GameModeManager.Instance?.EnterZoomMode();

        // STEP 4: CRITICAL FIX - Ensure camera state machine is also synchronized
        var cameraController = TopDownCameraController.Instance;
        if (cameraController != null)
        {
            Debug.Log("=== DOUBLE TAP STEP 3: Synchronizing camera state machine to focus ===");
            // Set focus target to closest object to tap position
            Transform closestObject = cameraController.FindClosestObjectToScreenPoint(tapPosition);
            if (closestObject != null)
            {
                cameraController.SetFocusTarget(closestObject);
            }
            cameraController.SwitchState(cameraController.focusState);
        }

        Debug.Log($"=== MODE AFTER SYNCHRONIZED ZOOM: {GameModeManager.Instance?.GetCurrentMode()} ===");
    }

    // Synchronized return to exploration mode
    private System.Collections.IEnumerator SynchronizedReturnToExploration()
    {
        // STEP 1: Start camera animation to exploration mode FIRST
        Debug.Log("=== DOUBLE TAP STEP 1: Starting camera animation to exploration mode ===");
        CameraAnimationController.Instance?.ReturnToExplorationModeFast();

        // STEP 2: Wait for camera animation to start
        yield return new WaitForSeconds(0.1f);

        // STEP 3: Change the game mode state to match the visual transition
        Debug.Log("=== DOUBLE TAP STEP 2: Changing to Exploration mode after camera animation started ===");
        GameModeManager.Instance?.ReturnToExplorationMode();

        Debug.Log($"=== MODE AFTER SYNCHRONIZED RETURN: {GameModeManager.Instance?.GetCurrentMode()} ===");
    }
    #endregion
}