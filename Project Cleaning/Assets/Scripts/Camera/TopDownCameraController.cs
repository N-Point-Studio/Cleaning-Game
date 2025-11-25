using System.Collections;
using UnityEngine;
// Removed DG.Tweening - replaced with Lerp

public class TopDownCameraController : StateMachine
{
    [Header("Overview Settings")]
    [SerializeField] private float overviewFOV = 60f;

    [Header("Focus Settings")]
    [SerializeField] private float focusHeight = 8f;
    [SerializeField] private float focusFOV = 30f;

    [Header("Navigation Settings")]
    [SerializeField] private float navigationHeight = 20f; // Much higher for clear difference
    [SerializeField] private float navigationFOV = 50f; // Wider view for exploration

    [Header("Transition Settings")]
    [SerializeField] private float transitionDuration = 0.5f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Runtime Info")]
    [SerializeField] private bool isTransitioning = false;

    // Public access for states
    public Transform currentFocusTarget { get; private set; }
    public State currentState { get; private set; }

    // States - public so input manager can access them
    public OverviewState overviewState;
    public FocusState focusState;
    public NavigationState navigationState;

    private Camera cam;
    private float defaultTransitionDuration; // Store default duration

    // Camera state tracking
    private Vector3 overviewPosition;
    private Vector3 overviewRotation;

    // Lerp animation system - replaces DOTween
    private Coroutine currentTransition;
    private bool isTransitioningLerp = false;

    public static TopDownCameraController Instance { get; private set; }

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            cam = GetComponent<Camera>();

            // Initialize states
            overviewState = new OverviewState(this);
            focusState = new FocusState(this);
            navigationState = new NavigationState(this);

            // Store default transition duration
            defaultTransitionDuration = transitionDuration;

            // Store current transform as overview position
            StoreCurrentAsOverview();

            // Start in overview state
            SwitchState(overviewState);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Override SwitchState to track current state
    /// </summary>
    public new void SwitchState(State newState)
    {
        currentState = newState;
        base.SwitchState(newState);
    }

    /// <summary>
    /// Store current camera transform as the overview position
    /// </summary>
    private void StoreCurrentAsOverview()
    {
        overviewPosition = transform.position;
        overviewRotation = transform.rotation.eulerAngles; // Keep the actual current rotation
        currentFocusTarget = null;
    }

    /// <summary>
    /// Set camera to overview position immediately
    /// </summary>
    private void SetToOverviewImmediate()
    {
        transform.position = overviewPosition;
        transform.rotation = Quaternion.Euler(overviewRotation);
        cam.fieldOfView = overviewFOV;
        currentFocusTarget = null;
    }

    /// <summary>
    /// Set the focus target
    /// </summary>
    public void SetFocusTarget(Transform target)
    {
        Debug.Log($"=== SetFocusTarget called ===");
        Debug.Log($"Previous target: {(currentFocusTarget != null ? currentFocusTarget.name : "NULL")}");
        Debug.Log($"New target: {(target != null ? target.name : "NULL")}");

        currentFocusTarget = target;

        Debug.Log($"✅ Focus target set to: {(currentFocusTarget != null ? currentFocusTarget.name : "NULL")}");

        // REMOVED AUTO-SAVE: This was causing "destroyed object" errors during scene transitions
        // CameraStateManager will save state at appropriate times (before scene transitions)
        // No need for immediate auto-save on every SetFocusTarget call
    }

    /// <summary>
    /// Transition to overview state
    /// </summary>
    public void TransitionToOverview()
    {
        Debug.Log("🔧 TransitionToOverview called - ensuring exploration mode is properly initialized");

        // CRITICAL FIX: Ensure exploration mode is properly set up first
        if (GameModeManager.Instance != null && !GameModeManager.Instance.IsInExplorationMode())
        {
            Debug.Log("🔄 Setting GameModeManager to exploration mode");
            GameModeManager.Instance.ReturnToExplorationMode();
        }

        // Get the correct exploration position from the animation controller
        var camAnimController = CameraAnimationController.Instance;
        if (camAnimController == null)
        {
            // Fallback to old logic if controller not found
            Vector3 targetPosFallback = overviewPosition;
            Vector3 targetRotFallback = overviewRotation;
            float targetFOVFallback = overviewFOV;

            Debug.Log("⚠️ CameraAnimationController not found, using fallback overview position");
            StartTransition(targetPosFallback, targetRotFallback, targetFOVFallback);
            return;
        }

        // Calculate the correct target position based on the current exploration state
        float targetX = camAnimController.GetCurrentXPosition();
        Vector3 explorationBasePos = camAnimController.GetExplorationPosition();
        Vector3 targetPos = new Vector3(targetX, explorationBasePos.y, explorationBasePos.z);

        // Use the standard exploration rotation
        Vector3 targetRot = new Vector3(90f, 0f, 0f);
        float targetFOV = overviewFOV;

        Debug.Log($"✅ Transitioning to overview - X: {targetX}, Position: {targetPos}");
        StartTransition(targetPos, targetRot, targetFOV);
    }

    /// <summary>
    /// NEW: Transition to the appropriate state based on stored focus target
    /// This method should be called when returning from gameplay to restore the correct camera state
    /// </summary>
    public void TransitionToStoredState()
    {
        Debug.Log($"TransitionToStoredState called - currentFocusTarget: {(currentFocusTarget != null ? currentFocusTarget.name : "NULL")}");

        // If we have a focus target, return to focus mode
        if (currentFocusTarget != null)
        {
            Debug.Log($"Restoring focus on target: {currentFocusTarget.name}");
            TransitionToFocus();

            // Notify the target object that it's focused again
            ClickableObject clickable = currentFocusTarget.GetComponent<ClickableObject>();
            if (clickable != null)
            {
                clickable.SetFocusState(true);
            }
        }
        else
        {
            Debug.Log("No focus target stored, transitioning to overview");
            TransitionToOverview();
        }
    }

    /// <summary>
    /// Transition to focus state
    /// </summary>
    public void TransitionToFocus()
    {
        if (currentFocusTarget == null)
        {
            Debug.LogWarning("❌ TransitionToFocus called but currentFocusTarget is null");
            return;
        }

        Debug.Log($"🎯 TransitionToFocus called for target: {currentFocusTarget.name}");

        Vector3 targetPos = CalculateFocusPosition(currentFocusTarget);
        Vector3 targetRot = new Vector3(90f, 0f, 0f); // FOCUS MODE specific rotation (top-down)
        float targetFOV = focusFOV;

        // Notify the target object that it's being focused
        ClickableObject clickable = currentFocusTarget.GetComponent<ClickableObject>();
        if (clickable != null)
        {
            Debug.Log($"📍 Setting focus state on target object: {currentFocusTarget.name}");
            clickable.SetFocusState(true);
        }

        StartTransition(targetPos, targetRot, targetFOV);
    }

    /// <summary>
    /// Transition to navigation state
    /// </summary>
    public void TransitionToNavigation()
    {
        Debug.Log("TransitionToNavigation called");
        if (currentFocusTarget == null)
        {
            Debug.Log("No focus target - going to overview");
            TransitionToOverview();
            return;
        }

        Vector3 targetPos = CalculateNavigationPosition(currentFocusTarget);
        Vector3 targetRot = overviewRotation;
        float targetFOV = navigationFOV;
        Debug.Log($"Navigation transition: pos={targetPos}, FOV={targetFOV}");
        StartTransition(targetPos, targetRot, targetFOV);
    }

    /// <summary>
    /// Calculate optimal focus position for target object
    /// </summary>
    private Vector3 CalculateFocusPosition(Transform target)
    {
        // Calculate focus position based on target object position
        Vector3 targetPos = target.position;

        // Focus height and offset from object
        Vector3 focusPosition = new Vector3(
            targetPos.x,           // Same X as object
            focusHeight,           // Use focus height setting
            targetPos.z            // Same Z as object
        );

        return focusPosition;
    }

    /// <summary>
    /// Calculate navigation position (higher than focus)
    /// </summary>
    private Vector3 CalculateNavigationPosition(Transform target)
    {
        Vector3 targetPos = target.position;
        return new Vector3(targetPos.x, navigationHeight, targetPos.z);
    }

    /// <summary>
    /// Find closest clickable object to a screen point
    /// </summary>
    public Transform FindClosestObjectToScreenPoint(Vector2 screenPoint)
    {
        ClickableObject[] allClickables = FindObjectsOfType<ClickableObject>();
        Transform closest = null;
        float closestDistance = float.MaxValue;

        foreach (ClickableObject clickable in allClickables)
        {
            Vector2 objectScreenPos = cam.WorldToScreenPoint(clickable.transform.position);
            float distance = Vector2.Distance(screenPoint, objectScreenPos);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = clickable.transform;
            }
        }

        return closest;
    }

    /// <summary>
    /// Start smooth transition between camera states using Lerp
    /// </summary>
    private void StartTransition(Vector3 targetPos, Vector3 targetRot, float targetFOV)
    {
        // Stop any existing transition
        if (currentTransition != null)
        {
            StopCoroutine(currentTransition);
            Debug.Log("🛑 Stopped existing transition");
        }

        // Set transitioning state
        isTransitioning = true;
        isTransitioningLerp = true;

        // Start Lerp-based transition
        currentTransition = StartCoroutine(LerpCameraTransition(
            transform.position,
            transform.rotation.eulerAngles,
            cam.fieldOfView,
            targetPos,
            targetRot,
            targetFOV,
            transitionDuration
        ));
    }

    /// <summary>
    /// Lerp-based camera transition - replaces DOTween
    /// </summary>
    private IEnumerator LerpCameraTransition(Vector3 startPos, Vector3 startRot, float startFOV,
                                           Vector3 targetPos, Vector3 targetRot, float targetFOV,
                                           float duration)
    {
        Debug.Log($"🎬 TopDown Lerp transition STARTED - pos: {targetPos}, rot: {targetRot}, FOV: {targetFOV}");

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;

            // Apply easing (OutQuart equivalent)
            float easedProgress = 1f - Mathf.Pow(1f - progress, 4f);

            // Lerp position, rotation, and FOV
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, easedProgress);
            Vector3 currentRot = Vector3.Lerp(startRot, targetRot, easedProgress);
            float currentFOV = Mathf.Lerp(startFOV, targetFOV, easedProgress);

            // Apply to transform and camera
            transform.position = currentPos;
            transform.rotation = Quaternion.Euler(currentRot);
            cam.fieldOfView = currentFOV;

            yield return null;
        }

        // Ensure exact final values
        transform.position = targetPos;
        transform.rotation = Quaternion.Euler(targetRot);
        cam.fieldOfView = targetFOV;

        // Clear transitioning state
        isTransitioning = false;
        isTransitioningLerp = false;
        currentTransition = null;

        // Release the global transition lock
        AdvancedInputManager.EndTransitionLock();

        Debug.Log($"✅ TopDown Lerp transition COMPLETED - final pos: {transform.position}, rot: {targetRot}, FOV: {cam.fieldOfView}");
    }

    /// <summary>
    /// Check if camera is currently focused on an object
    /// </summary>
    public bool IsFocused()
    {
        return currentFocusTarget != null;
    }

    /// <summary>
    /// Get currently focused object
    /// </summary>
    public Transform GetCurrentFocus()
    {
        Debug.Log($"=== GetCurrentFocus called ===");
        Debug.Log($"Current focus target: {(currentFocusTarget != null ? currentFocusTarget.name : "NULL")}");
        if (currentFocusTarget != null && currentFocusTarget.gameObject != null)
        {
            Debug.Log($"Target is valid and active: {currentFocusTarget.gameObject.activeInHierarchy}");
        }
        return currentFocusTarget;
    }

    /// <summary>
    /// Check if camera is currently transitioning
    /// </summary>
    public bool IsTransitioning()
    {
        return isTransitioning;
    }

    /// <summary>
    /// Force immediate transition (no animation)
    /// </summary>
    public void FocusOnObjectImmediate(Transform target)
    {
        if (target == null) return;

        // Stop any existing transitions
        if (currentTransition != null)
        {
            StopCoroutine(currentTransition);
            currentTransition = null;
            Debug.Log("🛑 Stopped transition for immediate focus");
        }

        Vector3 targetPosition = CalculateFocusPosition(target);
        Vector3 targetRotation = new Vector3(90f, 0f, 0f); // Top-down view

        transform.position = targetPosition;
        transform.rotation = Quaternion.Euler(targetRotation);
        cam.fieldOfView = focusFOV;

        currentFocusTarget = target;
        isTransitioning = false;
        isTransitioningLerp = false;

        // CRITICAL FIX: Switch the state machine to focusState to prevent other states from overriding the position.
        SwitchState(focusState);
        Debug.Log($"[ImmediateFocus] Switched camera state to FocusState for target {target.name}");
    }

    /// <summary>
    /// Public method to adjust transition speed at runtime
    /// </summary>
    public void SetTransitionDuration(float newDuration)
    {
        transitionDuration = Mathf.Clamp(newDuration, 0.1f, 3f);
    }

    /// <summary>
    /// Public method to reset transition speed to default
    /// </summary>
    public void ResetTransitionDuration()
    {
        transitionDuration = defaultTransitionDuration;
    }

    /// <summary>
    /// NEW: Force the camera to a hardcoded position and rotation, then enter focus state.
    /// This is used when returning from gameplay to a specific, non-object-based focus view.
    /// </summary>
    public void SetHardcodedFocusPosition(Vector3 position, Quaternion rotation)
    {
        Debug.Log($"[HARDCODE] Forcing camera to Pos: {position}, Rot: {rotation.eulerAngles}");

        // Stop any existing transitions to prevent conflicts
        if (currentTransition != null)
        {
            StopCoroutine(currentTransition);
            currentTransition = null;
        }

        // Set state immediately, without animation
        transform.position = position;
        transform.rotation = rotation;
        cam.fieldOfView = focusFOV;
        
        // We are not focused on a specific object
        currentFocusTarget = null;
        
        // Ensure the camera is in the focus state for correct controls and behavior
        SwitchState(focusState);

        isTransitioning = false;
        isTransitioningLerp = false;
        AdvancedInputManager.EndTransitionLock();

        Debug.Log("[HARDCODE] Camera position forced and switched to focus state.");
    }

    /// <summary>
    /// Get current transition speed
    /// </summary>
    public float GetTransitionSpeed()
    {
        return transitionDuration;
    }

    /// <summary>
    /// Debug: Draw gizmos in scene view
    /// </summary>
    private void OnDrawGizmos()
    {
        // Draw overview position (if initialized)
        if (overviewPosition != Vector3.zero)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(overviewPosition, Vector3.one * 2f);
        }

        // Draw focus position if target exists
        if (currentFocusTarget != null)
        {
            Gizmos.color = Color.red;
            Vector3 focusPos = CalculateFocusPosition(currentFocusTarget);
            Gizmos.DrawWireCube(focusPos, Vector3.one);
            Gizmos.DrawLine(focusPos, currentFocusTarget.position);
        }
    }

    /// <summary>
    /// Cleanup when TopDownCameraController is destroyed
    /// </summary>
    private void OnDestroy()
    {
        Debug.Log($"🔍 TopDownCameraController.OnDestroy() called");

        // Stop any running transitions
        if (currentTransition != null)
        {
            StopCoroutine(currentTransition);
            currentTransition = null;
            Debug.Log("🛑 Stopped transition in OnDestroy");
        }

        // Clear singleton instance if this is the current instance
        if (Instance == this)
        {
            Instance = null;
            Debug.Log("✅ TopDownCameraController singleton instance cleared");
        }
    }
}