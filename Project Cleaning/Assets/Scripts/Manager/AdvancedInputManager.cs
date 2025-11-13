using System.Collections;
using UnityEngine;

public class AdvancedInputManager : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private LayerMask clickableLayerMask = -1;
    [SerializeField] private float maxClickDistance = 100f;

    [Header("Tap Hold Settings")]
    [SerializeField] private float tapHoldDuration = 0.3f;
    [SerializeField] private float tapHoldMoveTolerance = 100f; // Pixels - more forgiving

    [Header("Pinch Settings")]
    [SerializeField] private float pinchThreshold = 100f; // Minimum distance change to register pinch
    [SerializeField] private float pinchSensitivity = 1f;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    private Camera playerCamera;
    private TopDownCameraController cameraController;

    // Touch tracking
    private bool isFirstFingerDown = false;
    private bool isSecondFingerDown = false;
    private Vector2 firstFingerPos;
    private Vector2 secondFingerPos;
    private Vector2 firstFingerStartPos;
    private Vector2 secondFingerStartPos;
    private float initialPinchDistance = 0f;
    private float currentPinchDistance = 0f;

    // Tap hold tracking
    private bool isTapHolding = false;
    private float tapHoldStartTime = 0f;
    private Vector2 tapHoldStartPos;
    private Vector2 lastDragPos;
    private Coroutine tapHoldCoroutine;

    // Camera exploration
    private bool isExploring = false;
    private float exploreSensitivity = 0.01f;

    // Hover tracking
    private ClickableObject hoveredObject = null;

    public static AdvancedInputManager Instance { get; private set; }

    private void Awake()
    {
        // Singleton pattern
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
        // Get camera controller after all objects are initialized
        cameraController = TopDownCameraController.Instance;
        if (cameraController == null)
        {
            Debug.LogError("TopDownCameraController.Instance not found! Make sure it's attached to the camera.");
        }
    }

    private void Update()
    {
        HandleInput();
        HandleHovering();
    }

    /// <summary>
    /// Main input handling method
    /// </summary>
    private void HandleInput()
    {
        // Handle mouse input (for testing in editor)
        if (!Application.isMobilePlatform || Application.isEditor)
        {
            HandleMouseInput();
        }

        // Handle touch input (for mobile)
        HandleTouchInput();
    }

    /// <summary>
    /// Handle mouse input for testing in editor
    /// </summary>
    private void HandleMouseInput()
    {
        // Single click
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Input.mousePosition;
            StartTapHold(mousePos);
            // Don't handle object/background clicks immediately - wait for tap hold to complete or fail
        }

        // Mouse release
        if (Input.GetMouseButtonUp(0))
        {
            StopTapHold();
        }

        // Handle exploration dragging
        if (isExploring && Input.GetMouseButton(0))
        {
            Vector2 currentMousePos = Input.mousePosition;
            HandleExplorationDrag(currentMousePos);
        }

        // Scroll wheel for pinch simulation
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f)
        {
            // Scroll up = Pinch in
            Vector2 centerPoint = Input.mousePosition;
            HandlePinchIn(centerPoint);
        }
        else if (scroll < 0f)
        {
            // Scroll down = Pinch out
            HandlePinchOut();
        }
    }

    /// <summary>
    /// Handle touch input for mobile devices
    /// </summary>
    private void HandleTouchInput()
    {
        int touchCount = Input.touchCount;

        if (touchCount == 0)
        {
            // No touches
            ResetTouchState();
            return;
        }

        if (touchCount == 1)
        {
            HandleSingleTouch(Input.GetTouch(0));
        }
        else if (touchCount >= 2)
        {
            // Multiple touches - stop exploration to avoid conflicts
            if (isExploring)
            {
                Debug.Log("Multiple touches detected - stopping exploration");
                isExploring = false;
                isTapHolding = false;
            }
            HandleMultiTouch(Input.GetTouch(0), Input.GetTouch(1));
        }
    }

    /// <summary>
    /// Handle single finger touch
    /// </summary>
    private void HandleSingleTouch(Touch touch)
    {
        switch (touch.phase)
        {
            case TouchPhase.Began:
                firstFingerPos = touch.position;
                firstFingerStartPos = touch.position;
                isFirstFingerDown = true;
                StartTapHold(touch.position);
                break;

            case TouchPhase.Moved:
                firstFingerPos = touch.position;
                CheckTapHoldMovement(touch.position);
                break;

            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                HandleSingleTouchEnd(touch);
                ResetTouchState();
                break;
        }
    }

    /// <summary>
    /// Handle single touch end (tap or failed tap hold)
    /// </summary>
    private void HandleSingleTouchEnd(Touch touch)
    {
        StopTapHold();

        // If tap hold didn't trigger, treat as normal tap
        if (!isTapHolding)
        {
            Ray ray = playerCamera.ScreenPointToRay(touch.position);
            if (Physics.Raycast(ray, out RaycastHit hit, maxClickDistance, clickableLayerMask))
            {
                ClickableObject clickable = hit.collider.GetComponent<ClickableObject>();
                if (clickable != null)
                {
                    HandleObjectClick(clickable.transform);
                }
            }
            else
            {
                HandleBackgroundClick();
            }
        }
    }

    /// <summary>
    /// Handle two finger touch (pinch gestures)
    /// </summary>
    private void HandleMultiTouch(Touch touch1, Touch touch2)
    {
        // Stop tap hold when second finger appears
        StopTapHold();

        switch (touch1.phase)
        {
            case TouchPhase.Began when touch2.phase == TouchPhase.Began:
                InitializePinch(touch1.position, touch2.position);
                break;

            case TouchPhase.Moved when isSecondFingerDown:
                UpdatePinch(touch1.position, touch2.position);
                break;

            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                ResetTouchState();
                break;
        }
    }

    /// <summary>
    /// Initialize pinch gesture
    /// </summary>
    private void InitializePinch(Vector2 pos1, Vector2 pos2)
    {
        isSecondFingerDown = true;
        firstFingerPos = pos1;
        secondFingerPos = pos2;
        firstFingerStartPos = pos1;
        secondFingerStartPos = pos2;
        initialPinchDistance = Vector2.Distance(pos1, pos2);
        currentPinchDistance = initialPinchDistance;
    }

    /// <summary>
    /// Update pinch gesture
    /// </summary>
    private void UpdatePinch(Vector2 pos1, Vector2 pos2)
    {
        firstFingerPos = pos1;
        secondFingerPos = pos2;
        float newDistance = Vector2.Distance(pos1, pos2);
        float distanceChange = newDistance - currentPinchDistance;

        if (Mathf.Abs(distanceChange) > pinchThreshold * pinchSensitivity)
        {
            Vector2 pinchCenter = (pos1 + pos2) * 0.5f;

            if (distanceChange > 0)
            {
                // Pinch out (fingers moving apart)
                HandlePinchOut();
            }
            else
            {
                // Pinch in (fingers moving together)
                HandlePinchIn(pinchCenter);
            }

            currentPinchDistance = newDistance;
        }
    }

    /// <summary>
    /// Start tap hold detection
    /// </summary>
    private void StartTapHold(Vector2 position)
    {
        Debug.Log($"StartTapHold at position {position} - Current state: {GetCurrentState()?.GetType().Name}");
        tapHoldStartPos = position;
        tapHoldStartTime = Time.time;

        if (tapHoldCoroutine != null)
        {
            StopCoroutine(tapHoldCoroutine);
        }

        tapHoldCoroutine = StartCoroutine(TapHoldCoroutine());
    }

    /// <summary>
    /// Stop tap hold detection
    /// </summary>
    private void StopTapHold()
    {
        if (tapHoldCoroutine != null)
        {
            StopCoroutine(tapHoldCoroutine);
            tapHoldCoroutine = null;

            // If tap hold was cancelled (didn't complete), handle as regular click
            if (!isTapHolding)
            {
                HandleRegularClick(tapHoldStartPos);
            }
        }

        if (isTapHolding)
        {
            // Stop exploration - camera stays where user dragged it
            if (isExploring)
            {
                Debug.Log("Exploration ended - camera stays at current position");
                isExploring = false;
            }
            isTapHolding = false;
        }
    }

    /// <summary>
    /// Check if finger moved too much during tap hold
    /// </summary>
    private void CheckTapHoldMovement(Vector2 currentPos)
    {
        // Only check movement tolerance before tap hold is activated
        if (!isTapHolding)
        {
            float moveDistance = Vector2.Distance(tapHoldStartPos, currentPos);
            if (moveDistance > tapHoldMoveTolerance)
            {
                // Finger moved too much before tap hold activated, cancel it
                StopTapHold();
            }
        }
        // If already in tap hold mode (exploring), allow unlimited movement
    }

    /// <summary>
    /// Coroutine for tap hold detection
    /// </summary>
    private IEnumerator TapHoldCoroutine()
    {
        yield return new WaitForSeconds(tapHoldDuration);

        // Tap hold completed - start exploration mode
        isTapHolding = true;
        isExploring = true;
        lastDragPos = tapHoldStartPos;

        // Start exploration mode
        if (cameraController?.currentState is FocusState)
        {
            Debug.Log("EXPLORATION MODE ACTIVATED - Drag to look around");
        }

        tapHoldCoroutine = null;
    }

    /// <summary>
    /// Reset all touch state
    /// </summary>
    private void ResetTouchState()
    {
        isFirstFingerDown = false;
        isSecondFingerDown = false;
        StopTapHold();
    }

    /// <summary>
    /// Handle mouse hover effects
    /// </summary>
    private void HandleHovering()
    {
        if (isFirstFingerDown || isSecondFingerDown) return; // Don't hover during touch

        Vector3 mousePos = Input.mousePosition;
        Ray ray = playerCamera.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit, maxClickDistance, clickableLayerMask))
        {
            ClickableObject clickable = hit.collider.GetComponent<ClickableObject>();
            HandleHoverChange(clickable);
        }
        else
        {
            HandleHoverChange(null);
        }
    }

    /// <summary>
    /// Handle hover state changes
    /// </summary>
    private void HandleHoverChange(ClickableObject newHoveredObject)
    {
        if (newHoveredObject != hoveredObject)
        {
            // Unhover previous object
            if (hoveredObject != null)
            {
                hoveredObject.OnUnhover();
            }

            // Hover new object
            hoveredObject = newHoveredObject;
            if (hoveredObject != null)
            {
                hoveredObject.OnHover();
            }
        }
    }

    /// <summary>
    /// Handle regular click (when tap hold fails)
    /// </summary>
    private void HandleRegularClick(Vector2 position)
    {
        Ray ray = playerCamera.ScreenPointToRay(position);
        if (Physics.Raycast(ray, out RaycastHit hit, maxClickDistance, clickableLayerMask))
        {
            ClickableObject clickable = hit.collider.GetComponent<ClickableObject>();
            if (clickable != null)
            {
                HandleObjectClick(clickable.transform);
            }
        }
        else
        {
            HandleBackgroundClick();
        }
    }

    /// <summary>
    /// Handle exploration dragging
    /// </summary>
    private void HandleExplorationDrag(Vector2 currentPos)
    {
        if (cameraController == null)
        {
            Debug.LogWarning("CameraController is null during exploration drag!");
            isExploring = false;
            return;
        }

        Vector2 deltaPos = currentPos - lastDragPos;
        lastDragPos = currentPos;

        // Skip very small movements
        if (deltaPos.magnitude < 1f) return;

        // Convert screen movement to world movement
        Vector3 worldDelta = new Vector3(
            -deltaPos.x * exploreSensitivity,
            0,
            -deltaPos.y * exploreSensitivity
        );

        // Move camera based on drag
        cameraController.transform.position += worldDelta;

    }


    /// <summary>
    /// Handle input from different camera states
    /// </summary>
    private void HandleObjectClick(Transform clickedObject)
    {
        var currentState = GetCurrentState();
        if (currentState is OverviewState overview)
        {
            overview.HandleObjectClick(clickedObject);
        }
        else if (currentState is FocusState focus)
        {
            focus.HandleObjectClick(clickedObject);
        }
        else if (currentState is NavigationState navigation)
        {
            navigation.HandleObjectClick(clickedObject);
        }
    }

    private void HandlePinchIn(Vector2 centerPoint)
    {
        var currentState = GetCurrentState();
        if (currentState is OverviewState overview)
        {
            overview.HandlePinchIn(centerPoint);
        }
        else if (currentState is NavigationState navigation)
        {
            navigation.HandlePinchIn(centerPoint);
        }
    }

    private void HandlePinchOut()
    {
        var currentState = GetCurrentState();
        if (currentState is FocusState focus)
        {
            focus.HandlePinchOut();
        }
        else if (currentState is NavigationState navigation)
        {
            navigation.HandlePinchOut();
        }
    }

    private void HandleTapHold(Vector2 position)
    {
        Debug.Log($"HandleTapHold called - Current state: {GetCurrentState()?.GetType().Name}");
        var currentState = GetCurrentState();
        if (currentState is FocusState focus)
        {
            Debug.Log("Entering navigation mode from focus state");
            focus.HandleTapHold(position);
        }
        else
        {
            Debug.Log($"Tap hold ignored - not in focus state, current state: {currentState?.GetType().Name}");
        }
    }

    private void HandleTapHoldRelease()
    {
        var currentState = GetCurrentState();
        if (currentState is NavigationState navigation)
        {
            navigation.HandleTapHoldRelease();
        }
    }

    private void HandleBackgroundClick()
    {
        var currentState = GetCurrentState();
        if (currentState is FocusState focus)
        {
            focus.HandleBackgroundClick();
        }
        else if (currentState is NavigationState navigation)
        {
            navigation.HandleBackgroundClick();
        }
    }

    /// <summary>
    /// Get current camera state
    /// </summary>
    private State GetCurrentState()
    {
        return cameraController?.currentState;
    }

    /// <summary>
    /// Debug GUI for testing
    /// </summary>
    private void OnGUI()
    {
        if (!showDebugInfo) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label($"Camera State: {GetCurrentState()?.GetType().Name ?? "None"}");
        GUILayout.Label($"Touch Count: {Input.touchCount}");
        GUILayout.Label($"Tap Holding: {isTapHolding}");
        GUILayout.Label($"First Finger: {isFirstFingerDown}");
        GUILayout.Label($"Second Finger: {isSecondFingerDown}");
        GUILayout.Label($"Pinch Distance: {currentPinchDistance:F1}");
        GUILayout.Label($"Focused Object: {cameraController?.currentFocusTarget?.name ?? "None"}");
        GUILayout.EndArea();
    }
}