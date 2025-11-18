using UnityEngine;

/// <summary>
/// Detects double tap gestures and triggers appropriate actions
/// </summary>
public class DoubleTapDetector : MonoBehaviour
{
    [Header("Double Tap Settings")]
    [SerializeField] private float doubleTapTimeWindow = 0.2f; // Maximum time between two taps (faster response)
    [SerializeField] private float doubleTapDistanceThreshold = 50f; // Maximum distance between taps
    [SerializeField] private bool enableDoubleTapZoom = true; // Enable double tap as backup to pinch
    [SerializeField] private bool instantSingleTapReturn = false; // Single tap in zoom mode instantly returns (no double tap needed)
    [SerializeField] private float gestureCooldown = 0.3f; // Prevent rapid gesture conflicts

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

        // SMART COOLDOWN: Only block rapid taps in the same mode to prevent conflicts
        if (currentTime - lastGestureTime < gestureCooldown)
        {
            // Only block if we're in the same mode (to prevent rapid mode switching)
            // But allow object clicks after mode changes
            Debug.Log($"=== GESTURE COOLDOWN - Time since last gesture: {currentTime - lastGestureTime:F2}s, Mode: {currentMode} ===");

            // Allow gestures if enough time has passed OR if we're doing a valid mode transition
            if (currentTime - lastGestureTime < gestureCooldown * 0.5f) // Reduce cooldown for object interactions
            {
                Debug.Log("=== GESTURE BLOCKED - Too rapid ===");
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
        var currentMode = GameModeManager.Instance?.GetCurrentMode() ?? GameModeManager.GameMode.Initial;
        Debug.Log($"=== HANDLE DOUBLE TAP: Mode={currentMode}, Position={tapPosition} ===");

        switch (currentMode)
        {
            case GameModeManager.GameMode.Exploration:
                // Double tap in exploration mode = Enter zoom mode at tap position
                Debug.Log("=== DOUBLE TAP TO ZOOM IN ===");
                GameModeManager.Instance?.EnterZoomMode();
                CameraAnimationController.Instance?.EnterZoomModeAtScreenPosition(tapPosition);
                break;

            case GameModeManager.GameMode.Zoom:
                // Double tap in zoom mode = Return to exploration mode with fast animation
                Debug.Log("=== DOUBLE TAP TO ZOOM OUT ===");
                GameModeManager.Instance?.ReturnToExplorationMode();
                CameraAnimationController.Instance?.ReturnToExplorationModeFast(); // Use fast return
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
        Debug.Log("=== GESTURE COOLDOWN RESET ===");
    }
    #endregion
}