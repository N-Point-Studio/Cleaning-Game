using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

/// <summary>
/// Advanced Input Manager dengan konsep:
/// 1. Button Start → Exploration Mode
/// 2. Exploration Mode → Click Object → Zoom Mode (Focus pada object)
/// 3. Zoom Mode → Camera Drag dengan click hold/swipe (hanya di mode ini)
/// 4. Mode lain tidak bisa camera drag
/// </summary>
public class AdvancedInputManager : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private LayerMask clickableLayerMask = -1;
    [SerializeField] private float maxClickDistance = 100f;

    [Header("Camera Drag Settings (Zoom Mode Only)")]
    [SerializeField] private float cameraDragSensitivity = 0.01f;
    [SerializeField] private float cameraDragSmoothing = 5f;
    [SerializeField] private float maxDragSpeed = 2f;

    [Header("UI Controls")]
    [SerializeField] private UnityEngine.UI.Image startExplorationImage;
    [SerializeField] private UnityEngine.UI.Image additionalImage1;
    [SerializeField] private UnityEngine.UI.Image additionalImage2;

    [Header("Exploration Settings")]
    [SerializeField] private Vector3 explorationPosition = new Vector3(-4.35f, 3.851f, -1.48f);
    [SerializeField] private Vector3 explorationRotation = new Vector3(90f, 0f, 0f);
    [SerializeField] private float explorationTransitionDuration = 15f;
    [SerializeField] private float returnTransitionDuration = 1.2f;

    [Header("Cozy Camera Transition")]
    [SerializeField] private float cameraDelayAfterButton = 0.2f;

    [Header("UI Transition Settings")]
    [SerializeField] private float buttonFadeDuration = 0.5f;
    [SerializeField] private float buttonScaleDuration = 0.3f;

    [Header("Swipe Detection Settings")]
    [SerializeField] private float swipeThreshold = 50f; // Minimum distance for swipe detection
    [SerializeField] private float maxSwipeTime = 1f; // Maximum time for valid swipe
    [SerializeField] private float swipeTransitionDuration = 0.6f;

    [Header("Pinch Detection Settings")]
    [SerializeField] private float pinchThreshold = 30f; // Minimum distance change for pinch
    [SerializeField] private float maxPinchTime = 2f; // Maximum time for valid pinch


    // Core components
    private Camera playerCamera;
    private TopDownCameraController cameraController;

    // Game state
    private GameMode currentGameMode = GameMode.Initial;
    private CameraDragSystem cameraDragSystem;

    // Swipe system
    private SwipeDetectionSystem swipeDetectionSystem;
    private PinchDetectionSystem pinchDetectionSystem;

    // Predefined camera positions for swipe navigation
    private readonly float[] cameraXPositions = { -4.35f, -2.5f, -0.5f };
    private int currentPositionIndex = 0; // Start at -4.35f (exploration position)

    // Exploration state
    private Vector3 originalCameraPosition;
    private Vector3 originalCameraRotation;

    // Singleton
    public static AdvancedInputManager Instance { get; private set; }

    public enum GameMode
    {
        Initial,        // Awal game, belum exploration
        Exploration,    // Mode exploration, bisa click object
        Zoom           // Mode zoom (focus), bisa camera drag
    }

    #region Unity Lifecycle

    private void Awake()
    {
        InitializeSingleton();
        InitializeComponents();
    }

    private void Start()
    {
        // Set target frame rate to 60 FPS for smooth performance
        Application.targetFrameRate = 60;

        SetupCameraController();
        SetupUI();
    }

    private void Update()
    {
        HandleInput();

        // Update camera drag system for smooth movement
        if (currentGameMode == GameMode.Zoom)
        {
            cameraDragSystem.Update(Time.deltaTime, cameraController);
        }
    }


    #endregion

    #region Initialization

    private void InitializeSingleton()
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

    private void InitializeComponents()
    {
        cameraDragSystem = new CameraDragSystem(cameraDragSensitivity, cameraDragSmoothing, maxDragSpeed);
        swipeDetectionSystem = new SwipeDetectionSystem(swipeThreshold, maxSwipeTime);
        pinchDetectionSystem = new PinchDetectionSystem(pinchThreshold, maxPinchTime);
    }

    private void SetupCameraController()
    {
        cameraController = TopDownCameraController.Instance;
        if (cameraController == null)
        {
            Debug.LogError("TopDownCameraController.Instance not found!");
        }
    }

    private void SetupUI()
    {
        if (startExplorationImage != null)
        {
            SetupImageClickDetection(startExplorationImage, StartExplorationMode);
        }

        // Setup additional images - no click detection needed, just for visual transition
        if (additionalImage1 != null)
        {
            additionalImage1.raycastTarget = false; // These are decorative only
        }
        if (additionalImage2 != null)
        {
            additionalImage2.raycastTarget = false; // These are decorative only
        }
    }

    /// <summary>
    /// Setup click detection for UI Image using EventTrigger
    /// </summary>
    private void SetupImageClickDetection(Image targetImage, System.Action onClickAction)
    {
        // Get or add EventTrigger component
        EventTrigger eventTrigger = targetImage.GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = targetImage.gameObject.AddComponent<EventTrigger>();
        }

        // Create click event
        EventTrigger.Entry clickEvent = new EventTrigger.Entry();
        clickEvent.eventID = EventTriggerType.PointerClick;
        clickEvent.callback.AddListener((data) => { onClickAction?.Invoke(); });

        // Add event to trigger
        eventTrigger.triggers.Add(clickEvent);

        // Ensure the image can receive raycast events
        targetImage.raycastTarget = true;

        Debug.Log($"Click detection setup for image: {targetImage.name}");
    }

    /// <summary>
    /// Enable/disable click detection for UI Image
    /// </summary>
    private void SetImageClickable(Image targetImage, bool clickable)
    {
        if (targetImage != null)
        {
            targetImage.raycastTarget = clickable;

            // Also enable/disable EventTrigger if it exists
            EventTrigger eventTrigger = targetImage.GetComponent<EventTrigger>();
            if (eventTrigger != null)
            {
                eventTrigger.enabled = clickable;
            }
        }
    }

    #endregion

    #region Input Handling

    private void HandleInput()
    {
        // Handle mouse input (editor)
        if (!Application.isMobilePlatform || Application.isEditor)
        {
            HandleMouseInput();
        }

        // Handle touch input (mobile)
        HandleTouchInput();
    }

    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            HandleInputDown(Input.mousePosition);
        }

        if (Input.GetMouseButton(0))
        {
            HandleInputDrag(Input.mousePosition);
        }

        if (Input.GetMouseButtonUp(0))
        {
            HandleInputUp();
        }
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount == 1)
        {
            // Single finger touch - handle swipe or drag
            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    HandleInputDown(touch.position);
                    break;

                case TouchPhase.Moved:
                    HandleInputDrag(touch.position);
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    HandleInputUp();
                    break;
            }
        }
        else if (Input.touchCount == 2)
        {
            // Two finger touch - handle pinch
            Touch touch1 = Input.GetTouch(0);
            Touch touch2 = Input.GetTouch(1);

            // Check if either finger just started
            if (touch1.phase == TouchPhase.Began || touch2.phase == TouchPhase.Began)
            {
                HandlePinchDown();
            }
            // Check if both fingers are moving
            else if ((touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved))
            {
                HandlePinchDrag();
            }
            // Check if either finger ended
            else if (touch1.phase == TouchPhase.Ended || touch2.phase == TouchPhase.Ended ||
                     touch1.phase == TouchPhase.Canceled || touch2.phase == TouchPhase.Canceled)
            {
                HandlePinchUp();
            }
        }
    }

    private void HandleInputDown(Vector2 screenPosition)
    {
        switch (currentGameMode)
        {
            case GameMode.Initial:
                // Di mode initial, tidak ada input handling
                Debug.Log("Please press Start Exploration button first");
                break;

            case GameMode.Exploration:
                // Check for object click first, if none then start swipe detection
                if (!CheckForObjectClick(screenPosition))
                {
                    swipeDetectionSystem.StartSwipe(screenPosition);
                }
                break;

            case GameMode.Zoom:
                // Di zoom mode, mulai camera drag atau click object lain
                if (!CheckForObjectClick(screenPosition))
                {
                    cameraDragSystem.StartDrag(screenPosition);
                }
                break;
        }
    }

    private void HandleInputDrag(Vector2 screenPosition)
    {
        if (currentGameMode == GameMode.Zoom)
        {
            // Hanya di zoom mode yang bisa camera drag
            cameraDragSystem.UpdateDrag(screenPosition, cameraController);
        }
        else if (currentGameMode == GameMode.Exploration)
        {
            // Update swipe detection during drag
            swipeDetectionSystem.UpdateSwipe(screenPosition);
        }
    }

    private void HandleInputUp()
    {
        if (currentGameMode == GameMode.Zoom)
        {
            cameraDragSystem.EndDrag();
        }
        else if (currentGameMode == GameMode.Exploration)
        {
            // Check if swipe was completed
            var swipeResult = swipeDetectionSystem.EndSwipe();
            if (swipeResult.IsValid)
            {
                HandleSwipeGesture(swipeResult.Direction);
            }
        }
    }

    private void HandlePinchDown()
    {
        switch (currentGameMode)
        {
            case GameMode.Initial:
                // No pinch handling in initial mode
                break;

            case GameMode.Exploration:
                // Start pinch detection for entering zoom mode
                pinchDetectionSystem.StartPinch();
                Debug.Log("Pinch started in exploration mode");
                break;

            case GameMode.Zoom:
                // Start pinch detection for exiting zoom mode
                pinchDetectionSystem.StartPinch();
                Debug.Log("Pinch started in zoom mode");
                break;
        }
    }

    private void HandlePinchDrag()
    {
        // Update pinch tracking in both exploration and zoom modes
        if (currentGameMode == GameMode.Exploration || currentGameMode == GameMode.Zoom)
        {
            pinchDetectionSystem.UpdatePinch();
        }
    }

    private void HandlePinchUp()
    {
        // Handle pinch up in both exploration and zoom modes
        if (currentGameMode == GameMode.Exploration || currentGameMode == GameMode.Zoom)
        {
            // Check if pinch was completed
            var pinchResult = pinchDetectionSystem.EndPinch();
            if (pinchResult.IsValid)
            {
                HandlePinchGesture(pinchResult.Direction);
            }
        }
    }

    private void HandlePinchGesture(PinchDirection direction)
    {
        Debug.Log($"Pinch gesture detected: {direction}");

        switch (currentGameMode)
        {
            case GameMode.Exploration:
                if (direction == PinchDirection.In)
                {
                    // Pinch in while in exploration mode - enter zoom mode
                    EnterZoomModeAtCenter();
                }
                else
                {
                    Debug.Log("Pinch out while in exploration mode ignored");
                }
                break;

            case GameMode.Zoom:
                if (direction == PinchDirection.Out)
                {
                    // Pinch out while in zoom mode - return to exploration
                    ReturnToExplorationMode();
                }
                else
                {
                    Debug.Log("Pinch in while in zoom mode ignored");
                }
                break;
        }
    }

    /// <summary>
    /// Enter zoom mode at center of current camera view (no specific object target)
    /// </summary>
    private void EnterZoomModeAtCenter()
    {
        Debug.Log("Entering Zoom Mode via pinch gesture");

        currentGameMode = GameMode.Zoom;

        // Set focus target to current camera position (center view)
        Vector3 currentPosition = cameraController.transform.position;
        Vector3 focusPosition = currentPosition + cameraController.transform.forward * 2f; // Focus slightly ahead

        // Create temporary focus target
        GameObject tempFocus = new GameObject("TempFocusTarget");
        tempFocus.transform.position = focusPosition;

        cameraController.SetFocusTarget(tempFocus.transform);
        cameraController.SwitchState(cameraController.focusState);

        // Clean up temp object after a delay
        StartCoroutine(CleanupTempFocusTarget(tempFocus, 0.1f));
    }

    private System.Collections.IEnumerator CleanupTempFocusTarget(GameObject tempTarget, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (tempTarget != null)
        {
            Destroy(tempTarget);
        }
    }

    #endregion

    #region Game Mode Management

    /// <summary>
    /// PUBLIC METHOD: Start exploration mode (dipanggil dari button)
    /// </summary>
    public void StartExplorationMode()
    {
        if (currentGameMode != GameMode.Initial) return;

        Debug.Log("Starting Exploration Mode with elegant transition");

        // Disable image click immediately to prevent multiple clicks
        if (startExplorationImage != null)
        {
            SetImageClickable(startExplorationImage, false);
        }

        // Disable additional images as well (even though they don't have click detection)
        if (additionalImage1 != null)
        {
            additionalImage1.raycastTarget = false;
        }
        if (additionalImage2 != null)
        {
            additionalImage2.raycastTarget = false;
        }

        // Start elegant button fade out and scale animation
        StartCoroutine(ElegantButtonTransition());
    }

    /// <summary>
    /// Elegant image transition with fade out and scale effect for all 3 images
    /// </summary>
    private System.Collections.IEnumerator ElegantButtonTransition()
    {
        // Create list of images to animate
        var imagesToAnimate = new System.Collections.Generic.List<UnityEngine.UI.Image>();

        if (startExplorationImage != null) imagesToAnimate.Add(startExplorationImage);
        if (additionalImage1 != null) imagesToAnimate.Add(additionalImage1);
        if (additionalImage2 != null) imagesToAnimate.Add(additionalImage2);

        // Setup canvas groups and store original scales
        var canvasGroups = new System.Collections.Generic.List<CanvasGroup>();
        var transforms = new System.Collections.Generic.List<Transform>();
        var originalScales = new System.Collections.Generic.List<Vector3>();

        foreach (var image in imagesToAnimate)
        {
            // Get or create canvas group
            CanvasGroup imageCanvasGroup = image.GetComponent<CanvasGroup>();
            if (imageCanvasGroup == null)
            {
                imageCanvasGroup = image.gameObject.AddComponent<CanvasGroup>();
            }

            canvasGroups.Add(imageCanvasGroup);
            transforms.Add(image.transform);
            originalScales.Add(image.transform.localScale);
        }

        // Create master sequence with staggered warm animations
        Sequence masterSequence = DOTween.Sequence();

        for (int i = 0; i < imagesToAnimate.Count; i++)
        {
            float delay = i * 0.15f; // Stagger each image by 0.15 seconds for cozy effect

            // Individual image animation sequence
            Sequence imageSequence = DOTween.Sequence();

            // Warm bounce down effect
            imageSequence.Append(transforms[i].DOScale(originalScales[i] * 0.85f, buttonScaleDuration * 0.6f)
                .SetEase(Ease.OutBack));
            imageSequence.Join(canvasGroups[i].DOFade(0.4f, buttonScaleDuration * 0.6f)
                .SetEase(Ease.OutSine));

            // Gentle final fade out with smooth scaling
            imageSequence.Append(transforms[i].DOScale(0f, buttonFadeDuration * 1.2f)
                .SetEase(Ease.InSine));
            imageSequence.Join(canvasGroups[i].DOFade(0f, buttonFadeDuration * 1.2f)
                .SetEase(Ease.InSine));

            // Add to master sequence with delay
            masterSequence.Insert(delay, imageSequence);
        }

        // Wait for all animations to complete
        yield return masterSequence.WaitForCompletion();

        // Hide all images completely
        foreach (var image in imagesToAnimate)
        {
            image.gameObject.SetActive(false);
        }

        // Small cozy pause for dramatic effect
        yield return new WaitForSeconds(0.3f);

        // Now start exploration mode
        BeginExplorationMode();
    }

    /// <summary>
    /// Begin exploration mode after button transition
    /// </summary>
    private void BeginExplorationMode()
    {
        currentGameMode = GameMode.Exploration;
        currentPositionIndex = 0; // Reset to exploration position (-4.35f)

        // Store original camera position
        originalCameraPosition = cameraController.transform.position;
        originalCameraRotation = cameraController.transform.rotation.eulerAngles;

        // Start elegant camera transition
        StartCoroutine(ElegantCameraTransitionToExploration());
    }

    private void EnterZoomMode(Transform targetObject)
    {
        Debug.Log($"Entering Zoom Mode - focusing on: {targetObject.name}");

        currentGameMode = GameMode.Zoom;

        // Focus camera pada object
        cameraController.SetFocusTarget(targetObject);
        cameraController.SwitchState(cameraController.focusState);
    }

    /// <summary>
    /// PUBLIC METHOD: Return to exploration mode from zoom
    /// </summary>
    public void ReturnToExplorationMode()
    {
        if (currentGameMode != GameMode.Zoom) return;

        Debug.Log($"Returning to Exploration Mode - Position Index: {currentPositionIndex}");

        currentGameMode = GameMode.Exploration;

        // Return to the last exploration position based on currentPositionIndex
        float targetX = cameraXPositions[currentPositionIndex];
        AnimateToPosition(targetX);
    }

    /// <summary>
    /// PUBLIC METHOD: Exit to initial state
    /// </summary>
    public void ExitToInitialMode()
    {
        Debug.Log("Exiting to Initial Mode");

        currentGameMode = GameMode.Initial;
        AnimateToOriginalPosition();

        // Show start button again with elegant fade in
        StartCoroutine(ShowStartButtonElegantly());
    }

    /// <summary>
    /// Show all images again with elegant fade in effect
    /// </summary>
    private System.Collections.IEnumerator ShowStartButtonElegantly()
    {
        // Wait for camera animation to near completion
        yield return new WaitForSeconds(returnTransitionDuration * 0.7f);

        // Create list of images to animate back in
        var imagesToAnimate = new System.Collections.Generic.List<UnityEngine.UI.Image>();

        if (startExplorationImage != null) imagesToAnimate.Add(startExplorationImage);
        if (additionalImage1 != null) imagesToAnimate.Add(additionalImage1);
        if (additionalImage2 != null) imagesToAnimate.Add(additionalImage2);

        // Show all images but make them invisible initially
        var canvasGroups = new System.Collections.Generic.List<CanvasGroup>();
        var transforms = new System.Collections.Generic.List<Transform>();

        foreach (var image in imagesToAnimate)
        {
            // Show image but invisible
            image.gameObject.SetActive(true);

            // Get or create canvas group
            CanvasGroup imageCanvasGroup = image.GetComponent<CanvasGroup>();
            if (imageCanvasGroup == null)
            {
                imageCanvasGroup = image.gameObject.AddComponent<CanvasGroup>();
            }

            canvasGroups.Add(imageCanvasGroup);
            transforms.Add(image.transform);

            // Set initial state (invisible and small)
            imageCanvasGroup.alpha = 0f;
            image.transform.localScale = Vector3.zero;
        }

        // Enable click interaction for main button only
        if (startExplorationImage != null)
        {
            SetImageClickable(startExplorationImage, true);
        }

        // Create master sequence for warm, cozy fade-in animations
        Sequence masterRestoreSequence = DOTween.Sequence();

        for (int i = 0; i < imagesToAnimate.Count; i++)
        {
            float delay = i * 0.2f; // Stagger each image for a warm, cozy appearance

            // Individual image restore sequence
            Sequence imageRestoreSequence = DOTween.Sequence();

            // Gentle bounce in with warm easing
            imageRestoreSequence.Append(transforms[i].DOScale(Vector3.one * 1.05f, buttonFadeDuration * 0.7f)
                .SetEase(Ease.OutBack));
            imageRestoreSequence.Join(canvasGroups[i].DOFade(1f, buttonFadeDuration * 0.7f)
                .SetEase(Ease.OutSine));

            // Settle to final size with soft bounce
            imageRestoreSequence.Append(transforms[i].DOScale(Vector3.one, buttonFadeDuration * 0.3f)
                .SetEase(Ease.OutSine));

            // Add to master sequence with delay
            masterRestoreSequence.Insert(delay, imageRestoreSequence);
        }

        yield return masterRestoreSequence.WaitForCompletion();

        Debug.Log("All images elegantly restored with warm, cozy transition");
    }



    #endregion

    #region Click Handling

    /// <summary>
    /// Handle swipe gesture in exploration mode
    /// </summary>
    private void HandleSwipeGesture(SwipeDirection direction)
    {
        Debug.Log($"Swipe gesture detected: {direction}");

        switch (direction)
        {
            case SwipeDirection.Right:
                PerformSwipeLeft(); // Swipe right gesture moves camera left (lower index)
                break;
            case SwipeDirection.Left:
                PerformSwipeRight(); // Swipe left gesture moves camera right (higher index)
                break;
            default:
                Debug.Log($"Swipe direction {direction} not handled in exploration mode");
                break;
        }
    }

    /// <summary>
    /// Perform swipe right gesture - move to next position in array (higher index)
    /// </summary>
    private void PerformSwipeRight()
    {
        Debug.Log($"PerformSwipeRight called - Current index: {currentPositionIndex}, Current X: {GetCurrentXPosition()}");

        // Check if we can move right (higher index = more to the right)
        if (currentPositionIndex < cameraXPositions.Length - 1)
        {
            currentPositionIndex++;
            float targetX = cameraXPositions[currentPositionIndex];
            AnimateToPosition(targetX);
            Debug.Log($"Swiped right - Moving to position index {currentPositionIndex}, X: {targetX}");
        }
        else
        {
            Debug.Log("Cannot swipe right - already at rightmost position");
        }
    }

    /// <summary>
    /// Perform swipe left gesture - move to previous position in array (lower index)
    /// </summary>
    private void PerformSwipeLeft()
    {
        Debug.Log($"PerformSwipeLeft called - Current index: {currentPositionIndex}, Current X: {GetCurrentXPosition()}");

        // Check if we can move left (lower index = more to the left)
        if (currentPositionIndex > 0)
        {
            currentPositionIndex--;
            float targetX = cameraXPositions[currentPositionIndex];
            AnimateToPosition(targetX);
            Debug.Log($"Swiped left - Moving to position index {currentPositionIndex}, X: {targetX}");
        }
        else
        {
            Debug.Log("Cannot swipe left - already at leftmost position");
        }
    }

    /// <summary>
    /// Animate camera to specific X position
    /// </summary>
    private void AnimateToPosition(float xPosition)
    {
        if (cameraController == null) return;

        Vector3 targetPosition = new Vector3(xPosition, explorationPosition.y, explorationPosition.z);

        Sequence sequence = DOTween.Sequence();
        sequence.Append(cameraController.transform.DOMove(targetPosition, swipeTransitionDuration).SetEase(Ease.OutCubic));

        sequence.OnComplete(() => {
            Debug.Log($"Camera moved to X position: {xPosition}");
        });
    }

    private bool CheckForObjectClick(Vector2 screenPosition)
    {
        Ray ray = playerCamera.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, maxClickDistance, clickableLayerMask))
        {
            ClickableObject clickable = hit.collider.GetComponent<ClickableObject>();
            if (clickable != null)
            {
                Debug.Log($"Object clicked: {clickable.name}");

                if (currentGameMode == GameMode.Exploration)
                {
                    // Dari exploration ke zoom mode
                    EnterZoomMode(clickable.transform);
                }
                else if (currentGameMode == GameMode.Zoom)
                {
                    // Ganti focus ke object lain
                    EnterZoomMode(clickable.transform);
                }

                // Trigger object's click method
                clickable.OnClick();
                return true;
            }
        }
        return false;
    }

    #endregion

    #region Camera Animation

    /// <summary>
    /// Cozy and warm camera transition to exploration
    /// </summary>
    private System.Collections.IEnumerator ElegantCameraTransitionToExploration()
    {
        if (cameraController == null) yield break;

        // Short cozy pause after button disappear
        yield return new WaitForSeconds(cameraDelayAfterButton);

        Transform cameraTransform = cameraController.transform;

        // Kill any existing animations
        DOTween.Kill(cameraTransform);

        // Simple, warm, and smooth movement
        Sequence cozySequence = DOTween.Sequence();

        // Gentle position movement with soft easing
        cozySequence.Append(cameraTransform.DOMove(explorationPosition, explorationTransitionDuration)
            .SetEase(Ease.OutCubic));

        // Smooth rotation with gentle timing
        cozySequence.Join(cameraTransform.DORotate(explorationRotation, explorationTransitionDuration)
            .SetEase(Ease.OutCubic));

        yield return cozySequence.WaitForCompletion();

        Debug.Log("Cozy camera transition completed - Swipe gestures now active");
    }


    private void AnimateToOriginalPosition()
    {
        if (cameraController == null) return;

        Sequence sequence = DOTween.Sequence();
        sequence.Append(cameraController.transform.DOMove(originalCameraPosition, returnTransitionDuration).SetEase(Ease.InOutQuart));
        sequence.Join(cameraController.transform.DORotate(originalCameraRotation, returnTransitionDuration).SetEase(Ease.InOutQuart));

        sequence.OnComplete(() => {
            Debug.Log("Returned to original position");
        });
    }


    #endregion

    #region Public Interface

    public GameMode GetCurrentMode() => currentGameMode;
    public bool IsInZoomMode() => currentGameMode == GameMode.Zoom;
    public bool IsInExplorationMode() => currentGameMode == GameMode.Exploration;
    public float GetCurrentXPosition() => cameraXPositions[currentPositionIndex];
    public int GetCurrentPositionIndex() => currentPositionIndex;

    #endregion
}

#region Camera Drag System

/// <summary>
/// Sistem camera drag yang smooth dan pelan dengan interpolasi
/// </summary>
public class CameraDragSystem
{
    private readonly float sensitivity;
    private readonly float smoothing;
    private readonly float maxSpeed;

    // Drag state
    private bool isDragging;
    private Vector2 lastDragPosition;

    // Smooth movement variables
    private Vector3 targetVelocity;
    private Vector3 currentVelocity;
    private Vector3 velocitySmoothing;

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

        // Reset velocities for smooth start
        targetVelocity = Vector3.zero;
        currentVelocity = Vector3.zero;

        Debug.Log("Camera drag started - smooth mode");
    }

    public void UpdateDrag(Vector2 currentPosition, TopDownCameraController cameraController)
    {
        if (!isDragging || cameraController == null) return;

        Vector2 deltaPos = currentPosition - lastDragPosition;
        lastDragPosition = currentPosition;

        // Skip very small movements
        if (deltaPos.magnitude < 0.5f)
        {
            // Slow down when not moving
            targetVelocity = Vector3.Lerp(targetVelocity, Vector3.zero, Time.deltaTime * smoothing);
            return;
        }

        // Convert screen movement to target velocity
        Vector3 inputVelocity = new Vector3(
            -deltaPos.x * sensitivity,
            0,
            -deltaPos.y * sensitivity
        );

        // Clamp velocity to max speed
        inputVelocity = Vector3.ClampMagnitude(inputVelocity, maxSpeed);

        // Set target velocity (will be smoothed in Update)
        targetVelocity = inputVelocity;
    }

    public void Update(float deltaTime, TopDownCameraController cameraController)
    {
        if (cameraController == null) return;

        // Always smooth the velocity, even when not dragging
        currentVelocity = Vector3.SmoothDamp(
            currentVelocity,
            isDragging ? targetVelocity : Vector3.zero,
            ref velocitySmoothing,
            1f / smoothing,
            Mathf.Infinity,
            deltaTime
        );

        // Apply movement if there's any velocity
        if (currentVelocity.magnitude > 0.001f)
        {
            Vector3 movement = currentVelocity * deltaTime;
            cameraController.transform.position += movement;

            // Debug info untuk velocity
            if (isDragging && currentVelocity.magnitude > 0.01f)
            {
                Debug.Log($"Smooth camera drag - Velocity: {currentVelocity.magnitude:F2}");
            }
        }
    }

    public void EndDrag()
    {
        if (isDragging)
        {
            isDragging = false;
            // Don't reset velocity immediately - let it smooth to zero naturally
            targetVelocity = Vector3.zero;
            Debug.Log("Camera drag ended - smoothing to stop");
        }
    }

    public bool IsDragging() => isDragging;

    // Debug info
    public float GetCurrentSpeed() => currentVelocity.magnitude;
    public Vector3 GetCurrentVelocity() => currentVelocity;
}

#endregion

#region Swipe Detection System

/// <summary>
/// Enum for swipe directions
/// </summary>
public enum SwipeDirection
{
    None,
    Left,
    Right,
    Up,
    Down
}

/// <summary>
/// Result of swipe detection
/// </summary>
public struct SwipeResult
{
    public bool IsValid;
    public SwipeDirection Direction;
    public Vector2 StartPosition;
    public Vector2 EndPosition;
    public float Distance;
    public float Duration;
}

/// <summary>
/// System for detecting swipe gestures
/// </summary>
public class SwipeDetectionSystem
{
    private readonly float swipeThreshold;
    private readonly float maxSwipeTime;

    // Swipe state
    private bool isTracking;
    private Vector2 startPosition;
    private float startTime;

    public SwipeDetectionSystem(float threshold, float maxTime)
    {
        swipeThreshold = threshold;
        maxSwipeTime = maxTime;
        isTracking = false;
    }

    /// <summary>
    /// Start swipe detection
    /// </summary>
    public void StartSwipe(Vector2 position)
    {
        isTracking = true;
        startPosition = position;
        startTime = Time.time;
        Debug.Log($"Swipe detection started at: {position}");
    }

    /// <summary>
    /// Update swipe tracking (optional, for continuous tracking)
    /// </summary>
    public void UpdateSwipe(Vector2 currentPosition)
    {
        if (!isTracking) return;

        // Optional: could add continuous tracking logic here
        // For now, we'll just track the duration
        float currentDuration = Time.time - startTime;
        if (currentDuration > maxSwipeTime)
        {
            // Timeout - cancel swipe
            isTracking = false;
            Debug.Log("Swipe timed out");
        }
    }

    /// <summary>
    /// End swipe detection and return result
    /// </summary>
    public SwipeResult EndSwipe()
    {
        var result = new SwipeResult();

        if (!isTracking)
        {
            result.IsValid = false;
            return result;
        }

        // Get end position from input
        Vector2 endPosition = GetCurrentInputPosition();
        float duration = Time.time - startTime;

        // Reset tracking
        isTracking = false;

        // Calculate swipe properties
        Vector2 swipeVector = endPosition - startPosition;
        float distance = swipeVector.magnitude;

        result.StartPosition = startPosition;
        result.EndPosition = endPosition;
        result.Distance = distance;
        result.Duration = duration;

        // Check if swipe meets threshold requirements
        if (distance < swipeThreshold || duration > maxSwipeTime)
        {
            result.IsValid = false;
            Debug.Log($"Swipe invalid - Distance: {distance:F1}, Duration: {duration:F2}s");
            return result;
        }

        // Determine swipe direction
        if (Mathf.Abs(swipeVector.x) > Mathf.Abs(swipeVector.y))
        {
            // Horizontal swipe - CORRECTED LOGIC
            result.Direction = swipeVector.x > 0 ? SwipeDirection.Right : SwipeDirection.Left;
        }
        else
        {
            // Vertical swipe
            result.Direction = swipeVector.y > 0 ? SwipeDirection.Up : SwipeDirection.Down;
        }

        result.IsValid = true;
        Debug.Log($"Swipe detected: {result.Direction}, Distance: {distance:F1}, Duration: {duration:F2}s");

        return result;
    }

    /// <summary>
    /// Get current input position (works for both mouse and touch)
    /// </summary>
    private Vector2 GetCurrentInputPosition()
    {
        if (Input.touchCount > 0)
        {
            return Input.GetTouch(0).position;
        }
        else
        {
            return Input.mousePosition;
        }
    }

    /// <summary>
    /// Check if currently tracking a swipe
    /// </summary>
    public bool IsTracking() => isTracking;
}

#endregion

#region Pinch Detection System

/// <summary>
/// Enum for pinch directions
/// </summary>
public enum PinchDirection
{
    None,
    In,     // Pinch in (zoom in)
    Out     // Pinch out (zoom out)
}

/// <summary>
/// Result of pinch detection
/// </summary>
public struct PinchResult
{
    public bool IsValid;
    public PinchDirection Direction;
    public float StartDistance;
    public float EndDistance;
    public float DistanceChange;
    public float Duration;
}

/// <summary>
/// System for detecting pinch gestures (mobile only)
/// </summary>
public class PinchDetectionSystem
{
    private readonly float pinchThreshold;
    private readonly float maxPinchTime;

    // Pinch state
    private bool isTracking;
    private float startDistance;
    private float startTime;

    public PinchDetectionSystem(float threshold, float maxTime)
    {
        pinchThreshold = threshold;
        maxPinchTime = maxTime;
        isTracking = false;
    }

    /// <summary>
    /// Start pinch detection (requires 2 fingers)
    /// </summary>
    public void StartPinch()
    {
        if (Input.touchCount != 2) return;

        Touch touch1 = Input.GetTouch(0);
        Touch touch2 = Input.GetTouch(1);

        isTracking = true;
        startDistance = Vector2.Distance(touch1.position, touch2.position);
        startTime = Time.time;

        Debug.Log($"Pinch detection started - Distance: {startDistance:F1}");
    }

    /// <summary>
    /// Update pinch tracking
    /// </summary>
    public void UpdatePinch()
    {
        if (!isTracking || Input.touchCount != 2)
        {
            if (isTracking && Input.touchCount != 2)
            {
                // Lost second finger - cancel pinch
                isTracking = false;
                Debug.Log("Pinch cancelled - lost second finger");
            }
            return;
        }

        // Check for timeout
        float currentDuration = Time.time - startTime;
        if (currentDuration > maxPinchTime)
        {
            isTracking = false;
            Debug.Log("Pinch timed out");
        }
    }

    /// <summary>
    /// End pinch detection and return result
    /// </summary>
    public PinchResult EndPinch()
    {
        var result = new PinchResult();

        if (!isTracking)
        {
            result.IsValid = false;
            return result;
        }

        // Reset tracking first
        isTracking = false;

        // Need 2 fingers to calculate end distance
        if (Input.touchCount != 2)
        {
            result.IsValid = false;
            Debug.Log("Pinch invalid - not enough fingers at end");
            return result;
        }

        Touch touch1 = Input.GetTouch(0);
        Touch touch2 = Input.GetTouch(1);
        float endDistance = Vector2.Distance(touch1.position, touch2.position);
        float duration = Time.time - startTime;

        // Calculate distance change
        float distanceChange = endDistance - startDistance;

        result.StartDistance = startDistance;
        result.EndDistance = endDistance;
        result.DistanceChange = distanceChange;
        result.Duration = duration;

        // Check if pinch meets threshold requirements
        if (Mathf.Abs(distanceChange) < pinchThreshold || duration > maxPinchTime)
        {
            result.IsValid = false;
            Debug.Log($"Pinch invalid - Change: {distanceChange:F1}, Duration: {duration:F2}s");
            return result;
        }

        // Determine pinch direction
        result.Direction = distanceChange > 0 ? PinchDirection.Out : PinchDirection.In;
        result.IsValid = true;

        Debug.Log($"Pinch detected: {result.Direction}, Change: {distanceChange:F1}, Duration: {duration:F2}s");
        return result;
    }

    /// <summary>
    /// Check if currently tracking a pinch
    /// </summary>
    public bool IsTracking() => isTracking;

    /// <summary>
    /// Check if device supports pinch (has touch support)
    /// </summary>
    public bool IsPinchSupported() => Input.touchSupported;
}

#endregion