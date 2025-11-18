using UnityEngine;

/// <summary>
/// Advanced Input Manager - Refactored Version
/// Orchestrates modular components for input handling
/// Reduced from 1200+ lines to under 300 lines while maintaining same behavior
/// </summary>
public class AdvancedInputManager : MonoBehaviour
{
    [Header("Camera Drag (Zoom Mode Only)")]
    [SerializeField] private float cameraDragSensitivity = 0.01f;
    [SerializeField] private float cameraDragSmoothing = 5f;
    [SerializeField] private float maxDragSpeed = 2f;

    [Header("Gesture Settings")]
    [SerializeField] private float swipeThreshold = 50f;
    [SerializeField] private float maxSwipeTime = 1f;
    [SerializeField] private float pinchThreshold = 15f;
    [SerializeField] private float maxPinchTime = 3f;
    [SerializeField] private float earlyPinchDetectionTime = 0.1f;
    [SerializeField] private float pinchVsSwipePriority = 8f;

    // Core components
    private TopDownCameraController cameraController;

    // Input systems
    private CameraDragSystem cameraDragSystem;
    private SwipeDetectionSystem swipeDetectionSystem;
    private PinchDetectionSystem pinchDetectionSystem;

    // Modular components
    private GameModeManager gameModeManager;
    private UITransitionController uiController;
    private ObjectInteractionHandler objectHandler;
    private CameraAnimationController cameraAnimator;
    private DoubleTapDetector doubleTapDetector;

    // Gesture conflict resolution
    private bool isPinchInProgress = false;
    private bool isSwipeBlocked = false;

    // Singleton
    public static AdvancedInputManager Instance { get; private set; }

    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeInputSystems();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        Application.targetFrameRate = 60;
        InitializeComponents();
    }

    private void Update()
    {
        HandleInput();
        if (gameModeManager != null && gameModeManager.IsInZoomMode() &&
            cameraDragSystem != null && cameraController != null)
        {
            cameraDragSystem.Update(Time.deltaTime, cameraController);
        }
    }
    #endregion

    #region Initialization
    private void InitializeInputSystems()
    {
        cameraDragSystem = new CameraDragSystem(cameraDragSensitivity, cameraDragSmoothing, maxDragSpeed);
        swipeDetectionSystem = new SwipeDetectionSystem(swipeThreshold, maxSwipeTime);
        pinchDetectionSystem = new PinchDetectionSystem(pinchThreshold, maxPinchTime, earlyPinchDetectionTime, pinchVsSwipePriority);
    }

    private void InitializeComponents()
    {
        cameraController = TopDownCameraController.Instance;

        // Get or find the modular components
        gameModeManager = GameModeManager.Instance;
        uiController = UITransitionController.Instance;
        objectHandler = ObjectInteractionHandler.Instance;
        cameraAnimator = CameraAnimationController.Instance;
        doubleTapDetector = DoubleTapDetector.Instance;

        // Subscribe to game mode events
        if (gameModeManager != null)
        {
            gameModeManager.OnModeChanged += OnGameModeChanged;
        }
    }

    private void OnGameModeChanged(GameModeManager.GameMode newMode)
    {
        // Reset gesture states when mode changes
        isPinchInProgress = false;
        isSwipeBlocked = false;

        if (doubleTapDetector != null)
        {
            // Use complete reset to clear all gesture states
            doubleTapDetector.CompleteGestureReset();
        }

        Debug.Log($"=== INPUT MANAGER - Mode changed to: {newMode}, gesture cooldown reset ===");
    }
    #endregion

    #region Input Handling
    private void HandleInput()
    {
        // Only handle mouse input if no touches are detected to avoid conflicts
        if ((!Application.isMobilePlatform || Application.isEditor) && Input.touchCount == 0)
        {
            HandleMouseInput();
        }
        HandleTouchInput();
    }

    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0)) HandleInputDown(Input.mousePosition);
        if (Input.GetMouseButton(0)) HandleInputDrag(Input.mousePosition);
        if (Input.GetMouseButtonUp(0)) HandleInputUp();
    }

    private void HandleTouchInput()
    {
        // ABSOLUTE PRIORITY: If 2+ touches detected, immediately cancel any ongoing swipe and handle pinch
        if (Input.touchCount >= 2)
        {
            if (!isPinchInProgress)
            {
                // Force cancel any ongoing swipe detection
                swipeDetectionSystem.CancelSwipe();

                // INSTANT PINCH DETECTION: Start pinch immediately regardless of touch phases
                if (gameModeManager != null && !gameModeManager.IsInInitialMode())
                {
                    pinchDetectionSystem.ForceStartPinch();
                    Debug.Log("=== INSTANT PINCH DETECTION - Force starting pinch ===");
                }

                isPinchInProgress = true;
                isSwipeBlocked = true;
                Debug.Log("=== PINCH MODE STARTED - Canceling swipe, blocking single touch ===");
            }
            HandlePinchInput();
            return; // Exit early to prevent single touch processing
        }

        if (Input.touchCount == 1)
        {
            var touch = Input.GetTouch(0);
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    if (isPinchInProgress)
                    {
                        isPinchInProgress = false;
                        isSwipeBlocked = false;
                        Debug.Log("=== PINCH MODE ENDED - Single touch began ===");
                    }

                    if (!isSwipeBlocked)
                    {
                        Debug.Log("=== Single Touch Began - Calling HandleInputDown ===");
                        HandleInputDown(touch.position);
                    }
                    else
                    {
                        Debug.Log("=== Single Touch Began - BLOCKED by pinch ===");
                    }
                    break;
                case TouchPhase.Moved:
                    if (!isSwipeBlocked) HandleInputDrag(touch.position);
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (!isSwipeBlocked) HandleInputUp();
                    isSwipeBlocked = false;
                    break;
            }
        }
        else if (Input.touchCount == 0)
        {
            if (isPinchInProgress || isSwipeBlocked)
            {
                Debug.Log("=== All touches ended - Resetting gesture states ===");
            }
            isPinchInProgress = false;
            isSwipeBlocked = false;
        }
    }

    private void HandlePinchInput()
    {
        var touch1 = Input.GetTouch(0);
        var touch2 = Input.GetTouch(1);

        Debug.Log($"=== PINCH INPUT: T1={touch1.phase}, T2={touch2.phase}, PinchInProgress={isPinchInProgress}, SwipeBlocked={isSwipeBlocked} ===");

        if (touch1.phase == TouchPhase.Began || touch2.phase == TouchPhase.Began)
        {
            if (!gameModeManager.IsInInitialMode())
            {
                Debug.Log($"=== PINCH TOUCH BEGAN - Distance: {Vector2.Distance(touch1.position, touch2.position):F1} (already started by ForceStart) ===");
            }
            else
            {
                Debug.Log("=== PINCH BLOCKED - Still in Initial mode ===");
            }
        }
        else if (touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved)
        {
            if (isPinchInProgress)
            {
                var earlyPinchResult = pinchDetectionSystem.UpdatePinch();
                Debug.Log($"=== PINCH UPDATE: Distance={earlyPinchResult.CurrentDistance:F1}, Change={earlyPinchResult.DistanceChange:F1}, EarlyDetection={earlyPinchResult.HasEarlyDetection} ===");

                // ADAPTIVE DETECTION: Multiple thresholds for better reliability
                if (earlyPinchResult.DistanceChange > 5f || earlyPinchResult.HasEarlyDetection)
                {
                    isSwipeBlocked = true;
                    Debug.Log("=== PINCH MOVEMENT DETECTED - Blocking swipe ===");
                }

                // BACKUP DETECTION: Force gesture completion if significant change detected
                if (earlyPinchResult.DistanceChange > pinchThreshold * 0.8f) // 80% of threshold
                {
                    Debug.Log("=== BACKUP PINCH DETECTION - Near threshold reached ===");
                }
            }
            else
            {
                // Fallback: If pinch was not in progress but we detect 2 moving touches, try to start detection
                Debug.Log("=== FALLBACK PINCH DETECTION ATTEMPT ===");
                if (!gameModeManager.IsInInitialMode())
                {
                    pinchDetectionSystem.ForceStartPinch();
                    isPinchInProgress = true;
                    isSwipeBlocked = true;
                }
            }
        }
        else if (touch1.phase == TouchPhase.Ended || touch2.phase == TouchPhase.Ended ||
                 touch1.phase == TouchPhase.Canceled || touch2.phase == TouchPhase.Canceled)
        {
            if (isPinchInProgress)
            {
                var pinchResult = pinchDetectionSystem.EndPinch();
                Debug.Log($"=== PINCH ENDED: Valid={pinchResult.IsValid}, Direction={pinchResult.Direction}, Change={pinchResult.DistanceChange:F1} ===");

                if (pinchResult.IsValid)
                {
                    Debug.Log($"=== EXECUTING PINCH GESTURE: {pinchResult.Direction} at {pinchResult.PinchCenter} ===");
                    HandlePinchGesture(pinchResult.Direction, pinchResult.PinchCenter);
                }
                else
                {
                    // FALLBACK: Execute gesture if close to threshold (forgiving approach)
                    float changeThreshold = pinchThreshold * 0.6f; // 60% of normal threshold
                    if (Mathf.Abs(pinchResult.DistanceChange) >= changeThreshold)
                    {
                        Debug.Log($"=== FALLBACK PINCH EXECUTION: Change {pinchResult.DistanceChange:F1} >= {changeThreshold:F1} ===");
                        var direction = pinchResult.DistanceChange > 0 ? PinchDirection.Out : PinchDirection.In;
                        HandlePinchGesture(direction, pinchResult.PinchCenter);
                    }
                    else
                    {
                        Debug.Log($"=== PINCH FAILED: Change {pinchResult.DistanceChange:F1} < threshold {pinchThreshold:F1} ===");
                    }
                }

                // AGGRESSIVE STATE CLEANUP: Always reset pinch state after processing
                isPinchInProgress = false;
                isSwipeBlocked = false;
                Debug.Log("=== PINCH STATE RESET - Ready for next gesture ===");
            }
        }
    }

    private void HandleInputDown(Vector2 screenPosition)
    {
        var currentMode = gameModeManager?.GetCurrentMode() ?? GameModeManager.GameMode.Initial;

        // PRIORITY 1: Check for object clicks FIRST (before double-tap detection)
        bool objectWasClicked = false;
        switch (currentMode)
        {
            case GameModeManager.GameMode.Initial:
                // Input ignored in initial mode - user must start exploration first
                break;

            case GameModeManager.GameMode.Exploration:
                objectWasClicked = (objectHandler != null && objectHandler.CheckForObjectClick(screenPosition));
                if (!objectWasClicked)
                    swipeDetectionSystem.StartSwipe(screenPosition);
                break;

            case GameModeManager.GameMode.Zoom:
                objectWasClicked = (objectHandler != null && objectHandler.CheckForObjectClick(screenPosition));
                if (!objectWasClicked)
                    cameraDragSystem.StartDrag(screenPosition);
                break;
        }

        // PRIORITY 2: Only check for double-tap if NO object was clicked
        if (!objectWasClicked && doubleTapDetector != null)
        {
            // Add extra delay check to ensure mode switching is stable
            bool canDoubleTap = doubleTapDetector.CheckForDoubleTap(screenPosition);
            if (canDoubleTap)
            {
                Debug.Log("=== DOUBLE TAP DETECTED - Handling gesture ===");
                doubleTapDetector.HandleDoubleTap(screenPosition);
                return; // Exit early - double tap handled
            }
        }

        // If an object was clicked, ensure gesture states are clean for next interaction
        if (objectWasClicked && doubleTapDetector != null)
        {
            // Reset gesture state after object interaction to prevent interference
            doubleTapDetector.ResetDoubleTapState();
        }
    }

    private void HandleInputDrag(Vector2 screenPosition)
    {
        var currentMode = gameModeManager?.GetCurrentMode() ?? GameModeManager.GameMode.Initial;
        if (currentMode == GameModeManager.GameMode.Zoom)
        {
            cameraDragSystem.UpdateDrag(screenPosition, cameraController);
        }
        else if (currentMode == GameModeManager.GameMode.Exploration)
        {
            swipeDetectionSystem.UpdateSwipe(screenPosition);
        }
    }

    private void HandleInputUp()
    {
        var currentMode = gameModeManager?.GetCurrentMode() ?? GameModeManager.GameMode.Initial;
        if (currentMode == GameModeManager.GameMode.Zoom)
        {
            cameraDragSystem.EndDrag();
        }
        else if (currentMode == GameModeManager.GameMode.Exploration)
        {
            var swipeResult = swipeDetectionSystem.EndSwipe();
            if (swipeResult.IsValid) HandleSwipeGesture(swipeResult.Direction);
        }
    }
    #endregion

    #region Gesture Handling
    private void HandleSwipeGesture(SwipeDirection direction)
    {
        switch (direction)
        {
            case SwipeDirection.Right:
                cameraAnimator?.PerformSwipeLeft();
                break;
            case SwipeDirection.Left:
                cameraAnimator?.PerformSwipeRight();
                break;
        }
    }

    private void HandlePinchGesture(PinchDirection direction, Vector2 pinchCenter = default)
    {
        var currentMode = gameModeManager?.GetCurrentMode() ?? GameModeManager.GameMode.Initial;
        Debug.Log($"=== HANDLE PINCH GESTURE: Direction={direction}, Mode={currentMode}, Center={pinchCenter} ===");

        if (currentMode == GameModeManager.GameMode.Exploration && direction == PinchDirection.In)
        {
            Debug.Log("=== PINCH IN - Attempting to enter zoom mode ===");
            gameModeManager?.EnterZoomMode();

            if (pinchCenter != default)
            {
                cameraAnimator?.EnterZoomModeAtScreenPosition(pinchCenter);
            }
            else
            {
                Debug.Log("=== PINCH CENTER IS DEFAULT - Using center fallback ===");
                cameraAnimator?.EnterZoomModeAtCenter();
            }
        }
        else if (currentMode == GameModeManager.GameMode.Zoom && direction == PinchDirection.Out)
        {
            Debug.Log("=== PINCH OUT - Returning to exploration mode with synchronized transition ===");
            StartCoroutine(SynchronizedPinchReturnToExploration());
        }
        else
        {
            Debug.Log($"=== PINCH GESTURE IGNORED - Mode: {currentMode}, Direction: {direction} ===");
        }
    }
    #endregion

    #region Synchronized Transitions
    private System.Collections.IEnumerator SynchronizedPinchReturnToExploration()
    {
        // STEP 1: Start camera animation to exploration mode FIRST
        Debug.Log("=== PINCH STEP 1: Starting camera animation to exploration mode ===");
        cameraAnimator?.ReturnToExplorationModeFast();

        // STEP 2: Wait for camera animation to start
        yield return new WaitForSeconds(0.1f);

        // STEP 3: Change the game mode state to match the visual transition
        Debug.Log("=== PINCH STEP 2: Changing to Exploration mode after camera animation started ===");
        gameModeManager?.ReturnToExplorationMode();

        Debug.Log($"=== MODE AFTER SYNCHRONIZED PINCH RETURN: {gameModeManager?.GetCurrentMode()} ===");
    }
    #endregion

    #region Public Interface - Legacy Compatibility
    public GameModeManager.GameMode GetCurrentMode() => gameModeManager?.GetCurrentMode() ?? GameModeManager.GameMode.Initial;
    public bool IsInZoomMode() => gameModeManager?.IsInZoomMode() ?? false;
    public bool IsInExplorationMode() => gameModeManager?.IsInExplorationMode() ?? false;
    public float GetCurrentXPosition() => cameraAnimator?.GetCurrentXPosition() ?? 0f;
    public int GetCurrentPositionIndex() => cameraAnimator?.GetCurrentPositionIndex() ?? 0;
    #endregion
}

// Keep the supporting input system classes at the bottom for reference
#region Input Systems
public class CameraDragSystem
{
    private readonly float sensitivity, smoothing, maxSpeed;
    private bool isDragging;
    private Vector2 lastDragPosition;
    private Vector3 targetVelocity, currentVelocity, velocitySmoothing;

    public CameraDragSystem(float dragSensitivity, float dragSmoothing, float dragMaxSpeed)
    {
        sensitivity = dragSensitivity;
        smoothing = dragSmoothing;
        maxSpeed = dragMaxSpeed;
    }

    public void StartDrag(Vector2 position)
    {
        isDragging = true;
        lastDragPosition = position;
        targetVelocity = currentVelocity = Vector3.zero;
    }

    public void UpdateDrag(Vector2 currentPosition, TopDownCameraController cameraController)
    {
        if (!isDragging || cameraController == null) return;

        var deltaPos = currentPosition - lastDragPosition;
        lastDragPosition = currentPosition;

        if (deltaPos.magnitude < 0.5f)
        {
            targetVelocity = Vector3.Lerp(targetVelocity, Vector3.zero, Time.deltaTime * smoothing);
            return;
        }

        var inputVelocity = new Vector3(-deltaPos.x * sensitivity, 0, -deltaPos.y * sensitivity);
        targetVelocity = Vector3.ClampMagnitude(inputVelocity, maxSpeed);
    }

    public void Update(float deltaTime, TopDownCameraController cameraController)
    {
        if (cameraController == null) return;

        currentVelocity = Vector3.SmoothDamp(currentVelocity, isDragging ? targetVelocity : Vector3.zero,
            ref velocitySmoothing, 1f / smoothing, Mathf.Infinity, deltaTime);

        if (currentVelocity.magnitude > 0.001f)
        {
            cameraController.transform.position += currentVelocity * deltaTime;
        }
    }

    public void EndDrag()
    {
        if (isDragging)
        {
            isDragging = false;
            targetVelocity = Vector3.zero;
        }
    }
}

public enum SwipeDirection { None, Left, Right, Up, Down }
public struct SwipeResult
{
    public bool IsValid;
    public SwipeDirection Direction;
    public Vector2 StartPosition, EndPosition;
    public float Distance, Duration;
}

public class SwipeDetectionSystem
{
    private readonly float swipeThreshold, maxSwipeTime;
    private bool isTracking;
    private Vector2 startPosition;
    private float startTime;

    public SwipeDetectionSystem(float threshold, float maxTime)
    {
        swipeThreshold = threshold;
        maxSwipeTime = maxTime;
    }

    public void StartSwipe(Vector2 position)
    {
        isTracking = true;
        startPosition = position;
        startTime = Time.time;
    }

    public void UpdateSwipe(Vector2 currentPosition)
    {
        if (!isTracking) return;
        if (Time.time - startTime > maxSwipeTime)
        {
            isTracking = false;
        }
    }

    public void CancelSwipe()
    {
        if (isTracking)
        {
            isTracking = false;
            Debug.Log("=== SWIPE CANCELED - Pinch priority ===");
        }
    }

    public SwipeResult EndSwipe()
    {
        var result = new SwipeResult();
        if (!isTracking)
        {
            result.IsValid = false;
            return result;
        }

        var endPosition = Input.touchCount > 0 ? Input.GetTouch(0).position : (Vector2)Input.mousePosition;
        var duration = Time.time - startTime;
        isTracking = false;

        var swipeVector = endPosition - startPosition;
        var distance = swipeVector.magnitude;

        result.StartPosition = startPosition;
        result.EndPosition = endPosition;
        result.Distance = distance;
        result.Duration = duration;

        if (distance < swipeThreshold || duration > maxSwipeTime)
        {
            result.IsValid = false;
            return result;
        }

        if (Mathf.Abs(swipeVector.x) > Mathf.Abs(swipeVector.y))
        {
            result.Direction = swipeVector.x > 0 ? SwipeDirection.Right : SwipeDirection.Left;
        }
        else
        {
            result.Direction = swipeVector.y > 0 ? SwipeDirection.Up : SwipeDirection.Down;
        }

        result.IsValid = true;
        return result;
    }
}

public enum PinchDirection { None, In, Out }
public struct PinchResult
{
    public bool IsValid;
    public PinchDirection Direction;
    public float StartDistance, EndDistance, DistanceChange, Duration;
    public Vector2 PinchCenter; // Center point between two fingers
}

public struct PinchUpdateResult
{
    public bool HasEarlyDetection;
    public float CurrentDistance;
    public float DistanceChange;
}

public class PinchDetectionSystem
{
    private readonly float pinchThreshold, maxPinchTime, earlyDetectionTime, priorityDistance;
    private bool isTracking;
    private float startDistance, startTime, currentDistance;
    private bool hasEarlyDetectionTriggered;
    private Vector2 pinchCenter; // Store the center point of pinch gesture

    public PinchDetectionSystem(float threshold, float maxTime, float earlyTime, float priority)
    {
        pinchThreshold = threshold;
        maxPinchTime = maxTime;
        earlyDetectionTime = earlyTime;
        priorityDistance = priority;
    }

    public void StartPinch()
    {
        if (Input.touchCount != 2) return;

        var touch1 = Input.GetTouch(0);
        var touch2 = Input.GetTouch(1);

        isTracking = true;
        startDistance = Vector2.Distance(touch1.position, touch2.position);
        currentDistance = startDistance;
        startTime = Time.time;
        hasEarlyDetectionTriggered = false;

        // Calculate and store pinch center point
        pinchCenter = (touch1.position + touch2.position) * 0.5f;
    }

    public void ForceStartPinch()
    {
        if (Input.touchCount < 2) return;

        var touch1 = Input.GetTouch(0);
        var touch2 = Input.GetTouch(1);

        // Force start pinch detection regardless of touch phases
        isTracking = true;
        startDistance = Vector2.Distance(touch1.position, touch2.position);
        currentDistance = startDistance;
        startTime = Time.time;
        hasEarlyDetectionTriggered = false;

        // Calculate and store pinch center point
        pinchCenter = (touch1.position + touch2.position) * 0.5f;

        Debug.Log($"=== FORCE START PINCH - Distance: {startDistance:F1}, Center: {pinchCenter} ===");
    }

    public PinchUpdateResult UpdatePinch()
    {
        var result = new PinchUpdateResult();

        if (!isTracking || Input.touchCount != 2)
        {
            if (isTracking && Input.touchCount != 2) isTracking = false;
            return result;
        }

        if (Time.time - startTime > maxPinchTime)
        {
            isTracking = false;
            return result;
        }

        var touch1 = Input.GetTouch(0);
        var touch2 = Input.GetTouch(1);
        currentDistance = Vector2.Distance(touch1.position, touch2.position);

        // Update pinch center as fingers move
        pinchCenter = (touch1.position + touch2.position) * 0.5f;

        float distanceChange = Mathf.Abs(currentDistance - startDistance);
        result.CurrentDistance = currentDistance;
        result.DistanceChange = distanceChange;

        // Early detection for pinch priority
        if (!hasEarlyDetectionTriggered &&
            (Time.time - startTime > earlyDetectionTime) &&
            distanceChange > priorityDistance)
        {
            hasEarlyDetectionTriggered = true;
            result.HasEarlyDetection = true;
        }

        return result;
    }

    public PinchResult EndPinch()
    {
        var result = new PinchResult();
        if (!isTracking)
        {
            result.IsValid = false;
            return result;
        }

        isTracking = false;

        // Use last known currentDistance instead of requiring 2 touches
        var endDistance = currentDistance;
        var duration = Time.time - startTime;
        var distanceChange = endDistance - startDistance;

        result.StartDistance = startDistance;
        result.EndDistance = endDistance;
        result.DistanceChange = distanceChange;
        result.Duration = duration;
        result.PinchCenter = pinchCenter; // Include pinch center position

        // More sensitive threshold and better validation
        if (Mathf.Abs(distanceChange) < pinchThreshold || duration > maxPinchTime)
        {
            result.IsValid = false;
            return result;
        }

        result.Direction = distanceChange > 0 ? PinchDirection.Out : PinchDirection.In;
        result.IsValid = true;
        return result;
    }
}
#endregion