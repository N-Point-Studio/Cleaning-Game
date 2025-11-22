using System.Collections;
using UnityEngine;
using DG.Tweening;

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

    // Singleton
    public static CameraAnimationController Instance { get; private set; }

    #region Unity Lifecycle
    private void Awake()
    {
        Debug.Log($"🔍 CameraAnimationController.Awake() called on {gameObject.name}");

        if (Instance == null)
        {
            Instance = this;
            Debug.Log($"✅ Set as singleton instance: {gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"⚠️ DUPLICATE CameraAnimationController found on {gameObject.name}! Destroying...");
            Debug.LogWarning($"   Existing instance: {Instance.gameObject.name}");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        cameraController = TopDownCameraController.Instance;

        // Subscribe to game mode events
        if (GameModeManager.Instance != null)
        {
            // GameModeManager.Instance.OnEnterExplorationMode += BeginExplorationMode; // This is the line that causes the bug
            GameModeManager.Instance.OnEnterInitialMode += ExitToInitialMode;
            GameModeManager.Instance.OnEnterZoomMode += () => { }; // Zoom handling is done through EnterZoomMode method
        }
    }
    #endregion

    #region Camera Transitions
    public void BeginExplorationMode()
    {
        originalCameraPosition = cameraController.transform.position;
        originalCameraRotation = cameraController.transform.rotation.eulerAngles;

        Debug.Log("=== EXPLORATION MODE STARTED - Camera animation ready ===");

        StartCoroutine(ElegantCameraTransitionToExploration());
    }

    /// <summary>
    /// Resets the exploration slide index to the beginning.
    /// </summary>
    public void ResetSlideIndex()
    {
        currentPositionIndex = 0;
    }

    private IEnumerator ElegantCameraTransitionToExploration()
    {
        var cameraDelayAfterButton = UITransitionController.Instance?.GetCameraDelayAfterButton() ?? 0.2f;
        yield return new WaitForSeconds(cameraDelayAfterButton);

        var cameraTransform = cameraController.transform;
        DOTween.Kill(cameraTransform);

        var sequence = DOTween.Sequence();
        sequence.Append(cameraTransform.DOMove(explorationPosition, explorationTransitionDuration).SetEase(Ease.OutCubic));
        sequence.Join(cameraTransform.DORotate(explorationRotation, explorationTransitionDuration).SetEase(Ease.OutCubic));

        yield return sequence.WaitForCompletion();
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
        Debug.Log($"=== ENTERING ZOOM MODE - Screen: {screenPosition}, CameraController: {(cameraController != null ? "Ready" : "NULL")} ===");

        // Safeguard: Ensure camera controller is ready
        if (cameraController == null)
        {
            Debug.LogWarning("=== CAMERA CONTROLLER NOT READY - Cannot enter zoom mode ===");
            return;
        }

        // Convert screen position to world position using ObjectInteractionHandler
        Vector3 focusPosition = ObjectInteractionHandler.Instance?.CalculateWorldPositionFromScreen(screenPosition, explorationPosition) ?? Vector3.zero;

        var tempFocus = new GameObject("TempFocusTarget") { transform = { position = focusPosition } };

        cameraController.SetFocusTarget(tempFocus.transform);
        cameraController.SwitchState(cameraController.focusState);

        StartCoroutine(CleanupTempFocusTarget(tempFocus, 0.1f));

        Debug.Log($"=== ZOOM TO SCREEN POSITION: {screenPosition} -> WORLD: {focusPosition} ===");
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
        ObjectInteractionHandler.Instance?.DisableAllInspectableObjects();

        // Kill any ongoing animations to prevent conflicts
        if (cameraController != null)
        {
            DOTween.Kill(cameraController.transform);
        }

        // Use fast or normal transition
        float duration = useFastTransition ? fastReturnDuration : swipeTransitionDuration;
        AnimateToPositionWithDuration(cameraXPositions[currentPositionIndex], duration);
    }

    private void ExitToInitialMode()
    {
        float timestamp = Time.time;
        Debug.Log($"=== EXITTOINITIALMODE CALLED at {timestamp} ===");
        Debug.Log($"Current state - hasShownStartupUI: {hasShownStartupUI}, isReturningFromGameplay: {isReturningFromGameplay}");
        Debug.Log($"Instance check - CameraAnimationController.Instance: {(Instance == this ? "THIS" : "OTHER")}");

        // Disable all inspectable objects when exiting to initial mode
        ObjectInteractionHandler.Instance?.DisableAllInspectableObjects();

        AnimateToOriginalPosition();

        // FIXED: Use SaveSystem to check if intro transition should be shown
        bool shouldShowIntroTransition = true;

        // Check SaveSystem first
        if (SaveSystem.Instance != null)
        {
            bool introAlreadyShown = SaveSystem.Instance.IsIntroTransitionShown();
            shouldShowIntroTransition = !introAlreadyShown && !isReturningFromGameplay;
            Debug.Log($"💾 SaveSystem check: introAlreadyShown={introAlreadyShown}, isReturningFromGameplay={isReturningFromGameplay}");
        }
        else
        {
            // Fallback to local flags if SaveSystem not available
            shouldShowIntroTransition = !hasShownStartupUI && !isReturningFromGameplay;
            Debug.Log($"🔄 Using local flags: hasShownStartupUI={hasShownStartupUI}, isReturningFromGameplay={isReturningFromGameplay}");
        }

        if (shouldShowIntroTransition)
        {
            Debug.Log($"🎬 Showing startup UI for first time at {timestamp}");
            hasShownStartupUI = true;
            StartCoroutine(ShowStartButtonElegantly());
        }
        else
        {
            Debug.Log($"🔄 Skipping startup UI - already shown or returning from gameplay at {timestamp}");
            isReturningFromGameplay = false; // Reset the flag for next time
        }
        Debug.Log($"=== EXITTOINITIALMODE FINISHED at {timestamp} ===");
    }

    private IEnumerator ShowStartButtonElegantly()
    {
        float timestamp = Time.time;
        Debug.Log($"🚨 ShowStartButtonElegantly COROUTINE STARTED at {timestamp}");
        Debug.Log($"🚨 About to call UITransitionController.ShowStartButtonElegantly...");
        yield return UITransitionController.Instance?.ShowStartButtonElegantly(returnTransitionDuration);
        Debug.Log($"🚨 ShowStartButtonElegantly COROUTINE FINISHED at {Time.time}");
    }
    #endregion

    #region Camera Movement
    public void AnimateToPosition(float xPosition)
    {
        AnimateToPositionWithDuration(xPosition, swipeTransitionDuration);
    }

    public void AnimateToPositionWithDuration(float xPosition, float duration)
    {
        if (cameraController == null) return;

        var targetPosition = new Vector3(xPosition, explorationPosition.y, explorationPosition.z);
        cameraController.transform.DOMove(targetPosition, duration).SetEase(Ease.OutCubic);
    }

    private void AnimateToOriginalPosition()
    {
        if (cameraController == null) return;

        var sequence = DOTween.Sequence();
        sequence.Append(cameraController.transform.DOMove(originalCameraPosition, returnTransitionDuration).SetEase(Ease.InOutQuart));
        sequence.Join(cameraController.transform.DORotate(originalCameraRotation, returnTransitionDuration).SetEase(Ease.InOutQuart));
    }

    public void PerformSwipeRight()
    {
        if (currentPositionIndex < cameraXPositions.Length - 1)
        {
            currentPositionIndex++;
            AnimateToPosition(cameraXPositions[currentPositionIndex]);
        }
    }

    public void PerformSwipeLeft()
    {
        if (currentPositionIndex > 0)
        {
            currentPositionIndex--;
            AnimateToPosition(cameraXPositions[currentPositionIndex]);
        }
    }
    #endregion

    #region Public Interface
    public float GetCurrentXPosition() => cameraXPositions[currentPositionIndex];
    public int GetCurrentPositionIndex() => currentPositionIndex;
    public Vector3 GetExplorationPosition() => explorationPosition;

    /// <summary>
    /// Call this method when returning from gameplay to prevent startup UI from showing again
    /// </summary>
    public void NotifyReturningFromGameplay()
    {
        float timestamp = Time.time;
        isReturningFromGameplay = true;
        Debug.Log($"🔄 CameraAnimationController notified: returning from gameplay at {timestamp}");
        Debug.Log($"🔄 Current state - hasShownStartupUI: {hasShownStartupUI}, isReturningFromGameplay: {isReturningFromGameplay}");
        Debug.Log($"🔄 This is instance: {(Instance == this ? "SINGLETON" : "NOT_SINGLETON")}");
    }

    /// <summary>
    /// Force reset startup UI state (for debugging or special cases)
    /// </summary>
    [ContextMenu("Reset Startup UI State")]
    public void ResetStartupUIState()
    {
        hasShownStartupUI = false;
        isReturningFromGameplay = false;
        Debug.Log("🔄 Startup UI state reset - will show again on next ExitToInitialMode");
    }

    /// <summary>
    /// Check if startup UI has been shown (for debugging)
    /// </summary>
    public bool HasShownStartupUI() => hasShownStartupUI;
    #endregion
}