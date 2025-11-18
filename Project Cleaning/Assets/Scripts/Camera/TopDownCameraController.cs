using System.Collections;
using UnityEngine;
using DG.Tweening;

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
        currentFocusTarget = target;
    }

    /// <summary>
    /// Transition to overview state
    /// </summary>
    public void TransitionToOverview()
    {
        // Get the correct exploration position from the animation controller
        var camAnimController = CameraAnimationController.Instance;
        if (camAnimController == null)
        {
            // Fallback to old logic if controller not found
            Vector3 targetPosFallback = overviewPosition;
            Vector3 targetRotFallback = overviewRotation;
            float targetFOVFallback = overviewFOV;
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

        StartTransition(targetPos, targetRot, targetFOV);
    }

    /// <summary>
    /// Transition to focus state
    /// </summary>
    public void TransitionToFocus()
    {
        if (currentFocusTarget == null) return;

        Vector3 targetPos = CalculateFocusPosition(currentFocusTarget);
        Vector3 targetRot = new Vector3(90f, 0f, 0f); // FOCUS MODE specific rotation (top-down)
        float targetFOV = focusFOV;


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
    /// Start smooth transition between camera states using DoTween
    /// </summary>
    private void StartTransition(Vector3 targetPos, Vector3 targetRot, float targetFOV)
    {
        // Kill any existing transition and force it to completion to ensure OnComplete callbacks are fired
        DOTween.Kill(transform, true);
        DOTween.Kill(cam, true);

        // Set transitioning state
        isTransitioning = true;

        // Create smooth transition sequence with DoTween
        Sequence transitionSequence = DOTween.Sequence();
        transitionSequence.Append(transform.DOMove(targetPos, transitionDuration).SetEase(Ease.OutQuart));
        transitionSequence.Join(transform.DORotate(targetRot, transitionDuration).SetEase(Ease.OutQuart));
        transitionSequence.Join(DOTween.To(() => cam.fieldOfView, x => cam.fieldOfView = x, targetFOV, transitionDuration).SetEase(Ease.OutQuart));

        // Set completion callback
        transitionSequence.OnComplete(() => {
            isTransitioning = false;
            AdvancedInputManager.EndTransitionLock(); // Release the global lock
        });
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

        // Kill any existing transitions
        DOTween.Kill(transform);
        DOTween.Kill(cam);

        Vector3 targetPosition = CalculateFocusPosition(target);
        Vector3 targetRotation = new Vector3(90f, 0f, 0f); // Top-down view

        transform.position = targetPosition;
        transform.rotation = Quaternion.Euler(targetRotation);
        cam.fieldOfView = focusFOV;

        currentFocusTarget = target;
        isTransitioning = false;
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
}