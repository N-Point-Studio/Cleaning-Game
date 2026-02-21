using System.Collections;
using Modules;
using UnityEngine;
// Removed DG.Tweening - replaced with Lerp

/// <summary>
/// Controls camera animations and positioning for different game modes
/// </summary>
public class CameraAnimationController : MonoBehaviour
{
    [Header("Exploration Settings")]
    [SerializeField] private Vector3 explorationPosition = new Vector3(-4.35f, 3.851f, -1.48f);
    [SerializeField] private Vector3 explorationRotation = new Vector3(90f, 0f, 0f);
    [SerializeField] private float explorationTransitionDuration = 15f;
    [SerializeField] private float returnTransitionDuration = 1.2f;

    [Header("Swipe Animation Settings")]
    [SerializeField] private float swipeTransitionDuration = 0.6f;
    [SerializeField] private float fastReturnDuration = 0.15f; // Fast return for double-tap exit

    // Camera positions for swipe navigation
    private readonly float[] cameraXPositions = { -4.35f, -2.5f, -0.5f };
    private int currentPositionIndex = 0;

    // Core components
    private TopDownCameraController cameraController;
    private Vector3 originalCameraPosition, originalCameraRotation;

    // FIXED: Track UI startup state to prevent re-showing startup UI
    private bool hasShownStartupUI = false;
    private bool isReturningFromGameplay = false;

    // Lerp animation system
    private Coroutine currentSwipeAnimation;
    private Coroutine currentTransitionAnimation;
    private bool isAnimating = false;

    // Singleton
    public static CameraAnimationController Instance { get; private set; }

    #region Unity Lifecycle
    private void Awake()
    {
        AppLogger.Log($"🔍 CameraAnimationController.Awake() called on {gameObject.name}");

        if (Instance == null)
        {
            Instance = this;
            AppLogger.Log($"✅ Set as singleton instance: {gameObject.name}");
        }
        else
        {
            AppLogger.LogWarning($"⚠️ DUPLICATE CameraAnimationController found on {gameObject.name}! Destroying...");
            AppLogger.LogWarning($"   Existing instance: {Instance.gameObject.name}");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InitializeCameraController();

        // FIX 1: Catat posisi awal SETELAH kamera diinisialisasi agar tidak bernilai (0,0,0)
        if (cameraController != null)
        {
            originalCameraPosition = cameraController.transform.position;
            originalCameraRotation = cameraController.transform.rotation.eulerAngles;
        }

        // Subscribe to game mode events
        if (GameModeManager.Instance != null)
        {
            GameModeManager.Instance.OnEnterInitialMode += ExitToInitialMode;

            if (GameModeManager.Instance.GetCurrentMode() == GameModeManager.GameMode.Initial)
            {
                StartCoroutine(DelayedInitialMode());
            }
        }

        MainMenuEvents.OnNextPage += PerformSwipeRight;
        MainMenuEvents.OnPreviousPage += PerformSwipeLeft;
        MainMenuEvents.OnGoToPage += JumpToPage;
    }

    private IEnumerator DelayedInitialMode()
    {
        yield return new WaitForEndOfFrame();
        ExitToInitialMode();
    }

    /// <summary>
    /// Initialize camera controller with retry mechanism
    /// </summary>
    private void InitializeCameraController()
    {
        cameraController = TopDownCameraController.Instance;

        if (cameraController == null)
        {
            AppLogger.LogWarning("⚠️ TopDownCameraController.Instance is null in Start(), will retry in Update()");
        }
        else
        {
            AppLogger.Log("✅ CameraAnimationController successfully linked to TopDownCameraController in Start()");
        }
    }

    private void Update()
    {
        // AUTO-REINITIALIZE: If cameraController becomes null, try to get it again
        if (cameraController == null && TopDownCameraController.Instance != null)
        {
            AppLogger.Log("🔄 Auto-reinitializing cameraController in Update()");
            cameraController = TopDownCameraController.Instance;
        }
    }
    #endregion

    #region Camera Transitions
    public void BeginExplorationMode()
    {
        originalCameraPosition = cameraController.transform.position;
        originalCameraRotation = cameraController.transform.rotation.eulerAngles;

        AppLogger.Log("=== EXPLORATION MODE STARTED - Camera animation ready ===");

        StartCoroutine(ElegantCameraTransitionToExploration());
    }

    /// <summary>
    /// Resets the exploration slide index to the beginning.
    /// </summary>
    public void ResetSlideIndex()
    {
        currentPositionIndex = 0;
        MainMenuEvents.OnPageChanged?.Invoke(currentPositionIndex);
    }

    private IEnumerator ElegantCameraTransitionToExploration()
    {
        var cameraDelayAfterButton = UITransitionController.Instance?.GetCameraDelayAfterButton() ?? 0.2f;
        yield return new WaitForSeconds(cameraDelayAfterButton);

        var cameraTransform = cameraController.transform;

        // Stop any existing animations
        if (currentTransitionAnimation != null)
        {
            StopCoroutine(currentTransitionAnimation);
        }

        // Start Lerp-based transition
        currentTransitionAnimation = StartCoroutine(LerpCameraTransition(
            cameraTransform.position,
            cameraTransform.rotation.eulerAngles,
            explorationPosition,
            explorationRotation,
            explorationTransitionDuration
        ));

        yield return currentTransitionAnimation;
        currentTransitionAnimation = null;
    }

    public void EnterZoomMode(Transform targetObject)
    {
        if (cameraController == null) return;

        cameraController.SetFocusTarget(targetObject);
        cameraController.SwitchState(cameraController.focusState);
    }

    public void EnterZoomModeAtCenter()
    {
        // Keep the old method for backward compatibility, but use screen center
        var screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        EnterZoomModeAtScreenPosition(screenCenter);
    }

    public void EnterZoomModeAtScreenPosition(Vector2 screenPosition)
    {
        AppLogger.Log($"=== ENTERING ZOOM MODE - Screen: {screenPosition}, CameraController: {(cameraController != null ? "Ready" : "NULL")} ===");

        // Safeguard: Ensure camera controller is ready
        if (cameraController == null)
        {
            AppLogger.LogWarning("=== CAMERA CONTROLLER NOT READY - Cannot enter zoom mode ===");
            return;
        }

        // Convert screen position to world position using ObjectInteractionHandler
        Vector3 focusPosition = ObjectInteractionHandler.Instance?.CalculateWorldPositionFromScreen(screenPosition, explorationPosition) ?? Vector3.zero;

        var tempFocus = new GameObject("TempFocusTarget") { transform = { position = focusPosition } };

        cameraController.SetFocusTarget(tempFocus.transform);
        cameraController.SwitchState(cameraController.focusState);

        StartCoroutine(CleanupTempFocusTarget(tempFocus, 0.1f));

        AppLogger.Log($"=== ZOOM TO SCREEN POSITION: {screenPosition} -> WORLD: {focusPosition} ===");
    }

    private IEnumerator CleanupTempFocusTarget(GameObject tempTarget, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (tempTarget != null) Destroy(tempTarget);
    }

    public void ReturnToExplorationMode()
    {
        ReturnToExplorationModeWithSpeed(false);
    }

    public void ReturnToExplorationModeFast()
    {
        ReturnToExplorationModeWithSpeed(true);
    }

    private void ReturnToExplorationModeWithSpeed(bool useFastTransition)
    {
        // Disable all inspectable objects when leaving zoom mode
        ObjectInteractionHandler.Instance?.ResetAllObjectStates();

        // Stop any ongoing animations to prevent conflicts
        if (currentSwipeAnimation != null)
        {
            StopCoroutine(currentSwipeAnimation);
            currentSwipeAnimation = null;
            AppLogger.Log("🛑 Stopped existing swipe animation for return to exploration");
        }

        if (currentTransitionAnimation != null)
        {
            StopCoroutine(currentTransitionAnimation);
            currentTransitionAnimation = null;
            AppLogger.Log("🛑 Stopped existing transition animation for return to exploration");
        }

        // Use fast or normal transition
        float duration = useFastTransition ? fastReturnDuration : swipeTransitionDuration;
        AnimateToPositionWithDuration(cameraXPositions[currentPositionIndex], duration);
    }

    private void ExitToInitialMode()
    {
        float timestamp = Time.time;
        AppLogger.Log($"=== EXITTOINITIALMODE CALLED at {timestamp} ===");
        AppLogger.Log($"Current state - hasShownStartupUI: {hasShownStartupUI}, isReturningFromGameplay: {isReturningFromGameplay}");
        AppLogger.Log($"Instance check - CameraAnimationController.Instance: {(Instance == this ? "THIS" : "OTHER")}");

        // Disable all inspectable objects when exiting to initial mode
        ObjectInteractionHandler.Instance?.ResetAllObjectStates();

        // FIXED: Check if CameraStateManager has a valid state to restore
        // If so, let CameraStateManager handle camera positioning instead of AnimateToOriginalPosition
        var cameraStateManager = FindObjectOfType<CameraStateManager>();
        bool letCameraStateManagerHandle = false;

        if (cameraStateManager != null && cameraStateManager.HasValidStateToRestore())
        {
            letCameraStateManagerHandle = true;
            AppLogger.Log("✅ CameraStateManager has valid state - letting it handle camera restoration instead of AnimateToOriginalPosition");
        }
        else
        {
            AppLogger.Log("📝 No valid camera state to restore - using default AnimateToOriginalPosition");
        }

        // Only animate to original position if CameraStateManager isn't handling it
        if (!letCameraStateManagerHandle)
        {
            AnimateToOriginalPosition();
        }

        // FIXED: Use SaveSystem to check if intro transition should be shown
        bool shouldShowIntroTransition = !hasShownStartupUI && !isReturningFromGameplay;
        AppLogger.Log($"🔄 Intro decision - hasShownStartupUI={hasShownStartupUI}, isReturningFromGameplay={isReturningFromGameplay}, shouldShowIntroTransition={shouldShowIntroTransition}");

        if (shouldShowIntroTransition)
        {
            AppLogger.Log($"🎬 Showing startup UI for first time at {timestamp}");
            hasShownStartupUI = true;
            StartCoroutine(ShowStartButtonElegantly());
        }
        else
        {
            AppLogger.Log($"🔄 Skipping startup UI - already shown or returning from gameplay at {timestamp}");
            isReturningFromGameplay = false; // Reset the flag for next time
        }
        AppLogger.Log($"=== EXITTOINITIALMODE FINISHED at {timestamp} ===");
    }

    private IEnumerator ShowStartButtonElegantly()
    {
        float timestamp = Time.time;
        AppLogger.Log($"🚨 ShowStartButtonElegantly COROUTINE STARTED at {timestamp}");
        AppLogger.Log($"🚨 About to call UITransitionController.ShowStartButtonElegantly...");
        yield return UITransitionController.Instance?.ShowStartButtonElegantly(returnTransitionDuration);
        AppLogger.Log($"🚨 ShowStartButtonElegantly COROUTINE FINISHED at {Time.time}");
    }
    #endregion

    #region Camera Movement
    public void AnimateToPosition(float xPosition)
    {
        AnimateToPositionWithDuration(xPosition, swipeTransitionDuration);
    }

    public void AnimateToPositionWithDuration(float xPosition, float duration)
    {
        if (cameraController == null)
        {
            AppLogger.LogError("❌ cameraController is null in AnimateToPositionWithDuration");
            return;
        }

        var targetPosition = new Vector3(xPosition, explorationPosition.y, explorationPosition.z);
        var currentPosition = cameraController.transform.position;

        AppLogger.Log($"🎯 AnimateToPositionWithDuration (Lerp):");
        AppLogger.Log($"   Current Position: {currentPosition}");
        AppLogger.Log($"   Target Position: {targetPosition}");
        AppLogger.Log($"   Duration: {duration}s");
        AppLogger.Log($"   Distance: {Vector3.Distance(currentPosition, targetPosition):F2}");

        // Stop any existing swipe animation
        if (currentSwipeAnimation != null)
        {
            StopCoroutine(currentSwipeAnimation);
            AppLogger.Log("🛑 Stopped existing swipe animation");
        }

        // Start new Lerp-based animation
        isAnimating = true;
        currentSwipeAnimation = StartCoroutine(LerpCameraPosition(currentPosition, targetPosition, duration));
    }

    /// <summary>
    /// Lerp-based camera position animation - replaces DOTween
    /// </summary>
    private IEnumerator LerpCameraPosition(Vector3 startPos, Vector3 targetPos, float duration)
    {
        AppLogger.Log($"🎬 Lerp animation STARTED - moving to {targetPos}");

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            // Check if cameraController still exists
            if (cameraController == null)
            {
                AppLogger.LogError("❌ cameraController became null during Lerp animation");
                break;
            }

            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;

            // Apply easing (OutCubic equivalent)
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);

            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, easedProgress);
            cameraController.transform.position = currentPos;

            yield return null;
        }

        // Ensure we end at exact target position
        if (cameraController != null)
        {
            cameraController.transform.position = targetPos;
            AppLogger.Log($"✅ Lerp animation COMPLETED - final position: {cameraController.transform.position}");
        }

        isAnimating = false;
        currentSwipeAnimation = null;
    }

    /// <summary>
    /// Lerp-based camera transition with position and rotation - replaces DOTween Sequence
    /// </summary>
    private IEnumerator LerpCameraTransition(Vector3 startPos, Vector3 startRot, Vector3 targetPos, Vector3 targetRot, float duration)
    {
        AppLogger.Log($"🎬 Lerp transition STARTED - moving to {targetPos}, rotating to {targetRot}");

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            // Check if cameraController still exists
            if (cameraController == null)
            {
                AppLogger.LogError("❌ cameraController became null during Lerp transition");
                break;
            }

            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;

            // Apply easing (InOutQuart equivalent for transitions)
            float easedProgress;
            if (progress < 0.5f)
            {
                easedProgress = 8f * progress * progress * progress * progress;
            }
            else
            {
                float f = progress - 1f;
                easedProgress = 1f - 8f * f * f * f * f;
            }

            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, easedProgress);
            Vector3 currentRot = Vector3.Lerp(startRot, targetRot, easedProgress);

            cameraController.transform.position = currentPos;
            cameraController.transform.rotation = Quaternion.Euler(currentRot);

            yield return null;
        }

        // Ensure we end at exact target position and rotation
        if (cameraController != null)
        {
            cameraController.transform.position = targetPos;
            cameraController.transform.rotation = Quaternion.Euler(targetRot);
            AppLogger.Log($"✅ Lerp transition COMPLETED - final position: {cameraController.transform.position}, rotation: {targetRot}");
        }

        isAnimating = false;
        currentTransitionAnimation = null;
    }

    private void AnimateToOriginalPosition()
    {
        if (cameraController == null) return;

        // Stop any existing transition animation
        if (currentTransitionAnimation != null)
        {
            StopCoroutine(currentTransitionAnimation);
        }

        // Start Lerp-based return animation
        currentTransitionAnimation = StartCoroutine(LerpCameraTransition(
            cameraController.transform.position,
            cameraController.transform.rotation.eulerAngles,
            originalCameraPosition,
            originalCameraRotation,
            returnTransitionDuration
        ));
    }

    public void PerformSwipeRight()
    {
        AppLogger.Log($"🎯 PerformSwipeRight called - currentPositionIndex: {currentPositionIndex}/{cameraXPositions.Length - 1}");

        // ENHANCED DEFENSIVE: Ensure cameraController is valid with gentle reinitialization
        if (cameraController == null)
        {
            AppLogger.Log("🔄 cameraController is null in PerformSwipeRight, reinitializing...");
            cameraController = TopDownCameraController.Instance;

            if (cameraController == null)
            {
                AppLogger.LogError("❌ TopDownCameraController.Instance is also null - cannot perform swipe");
                return;
            }
            AppLogger.Log("✅ cameraController reinitialized successfully for swipe");
        }

        if (currentPositionIndex < cameraXPositions.Length - 1)
        {
            currentPositionIndex++;
            MainMenuEvents.OnPageChanged?.Invoke(currentPositionIndex);
            AppLogger.Log($"🎯 Moving to position index {currentPositionIndex} (X: {cameraXPositions[currentPositionIndex]})");
            AnimateToPosition(cameraXPositions[currentPositionIndex]);
        }
        else
        {
            AppLogger.Log("🚫 Already at rightmost position, cannot swipe right further");
        }
    }

    public void PerformSwipeLeft()
    {
        AppLogger.Log($"🎯 PerformSwipeLeft called - currentPositionIndex: {currentPositionIndex}/{cameraXPositions.Length - 1}");

        // ENHANCED DEFENSIVE: Ensure cameraController is valid with gentle reinitialization
        if (cameraController == null)
        {
            AppLogger.Log("🔄 cameraController is null in PerformSwipeLeft, reinitializing...");
            cameraController = TopDownCameraController.Instance;

            if (cameraController == null)
            {
                AppLogger.LogError("❌ TopDownCameraController.Instance is also null - cannot perform swipe");
                return;
            }
            AppLogger.Log("✅ cameraController reinitialized successfully for swipe");
        }

        if (currentPositionIndex > 0)
        {
            currentPositionIndex--;
            MainMenuEvents.OnPageChanged?.Invoke(currentPositionIndex);
            AppLogger.Log($"🎯 Moving to position index {currentPositionIndex} (X: {cameraXPositions[currentPositionIndex]})");
            AnimateToPosition(cameraXPositions[currentPositionIndex]);
        }
        else
        {
            AppLogger.Log("🚫 Already at leftmost position, cannot swipe left further");
        }
    }

    public void JumpToPage(int pageIndex)
    {
        if (cameraController == null)
        {
            cameraController = TopDownCameraController.Instance;
            if (cameraController == null) return;
        }

        if (pageIndex >= 0 && pageIndex < cameraXPositions.Length)
        {
            currentPositionIndex = pageIndex;
            
            MainMenuEvents.OnPageChanged?.Invoke(currentPositionIndex);
            AnimateToPosition(cameraXPositions[currentPositionIndex]);
        }
    }
    #endregion

    #region Public Interface
    public float GetCurrentXPosition() => cameraXPositions[currentPositionIndex];
    public int GetCurrentPositionIndex() => currentPositionIndex;
    public Vector3 GetExplorationPosition() => explorationPosition;
    public bool IsReturningFromGameplay() => isReturningFromGameplay;

    /// <summary>
    /// Call this method when returning from gameplay to prevent startup UI from showing again
    /// </summary>
    public void NotifyReturningFromGameplay()
    {
        float timestamp = Time.time;
        isReturningFromGameplay = true;
        AppLogger.Log($"🔄 CameraAnimationController notified: returning from gameplay at {timestamp}");
        AppLogger.Log($"🔄 Current state - hasShownStartupUI: {hasShownStartupUI}, isReturningFromGameplay: {isReturningFromGameplay}");
        AppLogger.Log($"🔄 This is instance: {(Instance == this ? "SINGLETON" : "NOT_SINGLETON")}");
    }

    /// <summary>
    /// Force reset startup UI state (for debugging or special cases)
    /// </summary>
    [ContextMenu("Reset Startup UI State")]
    public void ResetStartupUIState()
    {
        hasShownStartupUI = false;
        isReturningFromGameplay = false;
        AppLogger.Log("🔄 Startup UI state reset - will show again on next ExitToInitialMode");
    }

    /// <summary>
    /// Check if startup UI has been shown (for debugging)
    /// </summary>
    public bool HasShownStartupUI() => hasShownStartupUI;
    #endregion

    #region Unity Lifecycle Cleanup
    private void OnDestroy()
    {
        AppLogger.Log($"🔍 CameraAnimationController.OnDestroy() called on {gameObject.name}");

        // Stop all running animations
        if (currentSwipeAnimation != null)
        {
            StopCoroutine(currentSwipeAnimation);
            currentSwipeAnimation = null;
            AppLogger.Log("🛑 Stopped swipe animation in OnDestroy");
        }

        if (currentTransitionAnimation != null)
        {
            StopCoroutine(currentTransitionAnimation);
            currentTransitionAnimation = null;
            AppLogger.Log("🛑 Stopped transition animation in OnDestroy");
        }

        // Clear singleton instance if this is the current instance
        if (Instance == this)
        {
            Instance = null;
            AppLogger.Log("✅ CameraAnimationController singleton instance cleared");
        }

        MainMenuEvents.OnNextPage -= PerformSwipeRight;
        MainMenuEvents.OnPreviousPage -= PerformSwipeLeft;
        MainMenuEvents.OnGoToPage -= JumpToPage;
    }
    #endregion

    public int GetTotalPages() => cameraXPositions.Length;
}

