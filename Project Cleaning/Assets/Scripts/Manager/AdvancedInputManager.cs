using System.Collections;
using UnityEngine;
using UnityEngine.UI;
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
    [SerializeField] private UnityEngine.UI.Button startExplorationButton;
    [SerializeField] private UnityEngine.UI.Button swipeRightButton;

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

    [Header("Swipe Right Settings")]
    [SerializeField] private Vector3 swipeRightPosition = new Vector3(-2.50f, 3.851f, -1.48f);
    [SerializeField] private float swipeTransitionDuration = 0.6f;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    // Core components
    private Camera playerCamera;
    private TopDownCameraController cameraController;

    // Game state
    private GameMode currentGameMode = GameMode.Initial;
    private CameraDragSystem cameraDragSystem;

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

    private void OnGUI()
    {
        if (showDebugInfo) DrawDebugInfo();
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
        if (startExplorationButton != null)
        {
            startExplorationButton.onClick.AddListener(StartExplorationMode);
        }

        if (swipeRightButton != null)
        {
            swipeRightButton.onClick.AddListener(SwipeRight);
            // Hide swipe right button initially
            swipeRightButton.gameObject.SetActive(false);
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
                // Di exploration mode, hanya bisa click object
                HandleExplorationModeClick(screenPosition);
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
    }

    private void HandleInputUp()
    {
        if (currentGameMode == GameMode.Zoom)
        {
            cameraDragSystem.EndDrag();
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

        // Disable button immediately to prevent multiple clicks
        if (startExplorationButton != null)
        {
            startExplorationButton.interactable = false;
        }

        // Start elegant button fade out and scale animation
        StartCoroutine(ElegantButtonTransition());
    }

    /// <summary>
    /// Elegant button transition with fade out and scale effect
    /// </summary>
    private System.Collections.IEnumerator ElegantButtonTransition()
    {
        if (startExplorationButton != null)
        {
            // Get button components
            CanvasGroup buttonCanvasGroup = startExplorationButton.GetComponent<CanvasGroup>();
            if (buttonCanvasGroup == null)
            {
                buttonCanvasGroup = startExplorationButton.gameObject.AddComponent<CanvasGroup>();
            }

            Transform buttonTransform = startExplorationButton.transform;
            Vector3 originalScale = buttonTransform.localScale;

            // Create elegant fade out and scale animation
            Sequence buttonSequence = DOTween.Sequence();

            // Scale down with bounce effect
            buttonSequence.Append(buttonTransform.DOScale(originalScale * 0.8f, buttonScaleDuration * 0.5f).SetEase(Ease.OutBack));
            buttonSequence.Join(buttonCanvasGroup.DOFade(0.3f, buttonScaleDuration * 0.5f));

            // Final fade out and scale to zero
            buttonSequence.Append(buttonTransform.DOScale(0f, buttonFadeDuration).SetEase(Ease.InBack));
            buttonSequence.Join(buttonCanvasGroup.DOFade(0f, buttonFadeDuration));

            // Wait for button animation to complete
            yield return buttonSequence.WaitForCompletion();

            // Hide button completely
            startExplorationButton.gameObject.SetActive(false);
        }

        // Small delay for dramatic effect
        yield return new WaitForSeconds(0.2f);

        // Now start exploration mode
        BeginExplorationMode();
    }

    /// <summary>
    /// Begin exploration mode after button transition
    /// </summary>
    private void BeginExplorationMode()
    {
        currentGameMode = GameMode.Exploration;

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

        Debug.Log("Returning to Exploration Mode");

        currentGameMode = GameMode.Exploration;
        AnimateToExplorationPosition();
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
    /// Show start button again with elegant fade in effect
    /// </summary>
    private System.Collections.IEnumerator ShowStartButtonElegantly()
    {
        // Hide swipe right button first
        if (swipeRightButton != null)
        {
            swipeRightButton.gameObject.SetActive(false);
        }

        // Wait for camera animation to near completion
        yield return new WaitForSeconds(returnTransitionDuration * 0.7f);

        if (startExplorationButton != null)
        {
            // Show button but invisible
            startExplorationButton.gameObject.SetActive(true);

            // Get or create canvas group
            CanvasGroup buttonCanvasGroup = startExplorationButton.GetComponent<CanvasGroup>();
            if (buttonCanvasGroup == null)
            {
                buttonCanvasGroup = startExplorationButton.gameObject.AddComponent<CanvasGroup>();
            }

            Transform buttonTransform = startExplorationButton.transform;

            // Set initial state (invisible and small)
            buttonCanvasGroup.alpha = 0f;
            buttonTransform.localScale = Vector3.zero;

            // Enable button interaction
            startExplorationButton.interactable = true;

            // Create elegant fade in and scale animation
            Sequence showButtonSequence = DOTween.Sequence();

            // Scale up with bounce effect and fade in
            showButtonSequence.Append(buttonTransform.DOScale(Vector3.one, buttonFadeDuration).SetEase(Ease.OutBack));
            showButtonSequence.Join(buttonCanvasGroup.DOFade(1f, buttonFadeDuration));

            yield return showButtonSequence.WaitForCompletion();

            Debug.Log("Start button elegantly restored");
        }
    }

    /// <summary>
    /// Show swipe right button with elegant entrance
    /// </summary>
    private System.Collections.IEnumerator ShowSwipeRightButtonElegantly()
    {
        if (swipeRightButton == null) yield break;

        // Small delay for better timing
        yield return new WaitForSeconds(0.3f);

        // Show button but invisible
        swipeRightButton.gameObject.SetActive(true);

        // Get or create canvas group
        CanvasGroup buttonCanvasGroup = swipeRightButton.GetComponent<CanvasGroup>();
        if (buttonCanvasGroup == null)
        {
            buttonCanvasGroup = swipeRightButton.gameObject.AddComponent<CanvasGroup>();
        }

        Transform buttonTransform = swipeRightButton.transform;

        // Set initial state (invisible and small)
        buttonCanvasGroup.alpha = 0f;
        buttonTransform.localScale = Vector3.zero;

        // Enable button interaction
        swipeRightButton.interactable = true;

        // Create elegant slide in from right animation
        Vector3 originalPosition = buttonTransform.localPosition;
        Vector3 startPosition = originalPosition + new Vector3(200f, 0, 0); // Slide from right
        buttonTransform.localPosition = startPosition;

        // Create elegant entrance sequence
        Sequence showSwipeSequence = DOTween.Sequence();

        // Slide in and scale up simultaneously
        showSwipeSequence.Append(buttonTransform.DOLocalMove(originalPosition, buttonFadeDuration).SetEase(Ease.OutBack));
        showSwipeSequence.Join(buttonTransform.DOScale(Vector3.one, buttonFadeDuration).SetEase(Ease.OutBack));
        showSwipeSequence.Join(buttonCanvasGroup.DOFade(1f, buttonFadeDuration));

        yield return showSwipeSequence.WaitForCompletion();

        Debug.Log("Swipe right button elegantly shown");
    }

    /// <summary>
    /// PUBLIC METHOD: Swipe camera to right position (dipanggil dari button)
    /// </summary>
    public void SwipeRight()
    {
        if (currentGameMode != GameMode.Exploration)
        {
            Debug.Log("Swipe right only available in Exploration mode");
            return;
        }

        Debug.Log("Swiping camera to right position");
        AnimateToSwipeRightPosition();
    }

    #endregion

    #region Click Handling

    private void HandleExplorationModeClick(Vector2 screenPosition)
    {
        if (CheckForObjectClick(screenPosition))
        {
            // Object clicked, sudah handled di CheckForObjectClick
            return;
        }

        Debug.Log("Background clicked in exploration mode - no action");
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

        // Show swipe right button after smooth transition
        StartCoroutine(ShowSwipeRightButtonElegantly());

        Debug.Log("Cozy camera transition completed");
    }

    private void AnimateToExplorationPosition()
    {
        // Legacy method - now just calls the elegant version
        StartCoroutine(ElegantCameraTransitionToExploration());
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

    private void AnimateToSwipeRightPosition()
    {
        if (cameraController == null) return;

        Sequence sequence = DOTween.Sequence();
        sequence.Append(cameraController.transform.DOMove(swipeRightPosition, swipeTransitionDuration).SetEase(Ease.OutCubic));
        sequence.Join(cameraController.transform.DORotate(explorationRotation, swipeTransitionDuration).SetEase(Ease.OutCubic));

        sequence.OnComplete(() => {
            Debug.Log("Swipe right position reached - camera moved to the right");
        });
    }

    #endregion

    #region Public Interface

    public GameMode GetCurrentMode() => currentGameMode;
    public bool IsInZoomMode() => currentGameMode == GameMode.Zoom;
    public bool IsInExplorationMode() => currentGameMode == GameMode.Exploration;

    // Swipe camera methods
    public void SwipeRightCamera() => SwipeRight();

    #endregion

    #region Debug

    private void DrawDebugInfo()
    {
        GUILayout.BeginArea(new Rect(10, 10, 350, 200));

        GUILayout.Label($"Game Mode: {currentGameMode}");
        GUILayout.Label($"Camera State: {cameraController?.currentState?.GetType().Name ?? "None"}");
        GUILayout.Label($"Camera Drag: {(currentGameMode == GameMode.Zoom ? "ENABLED" : "DISABLED")}");
        GUILayout.Label($"Drag Speed: {cameraDragSystem.GetCurrentSpeed():F2}");
        GUILayout.Label($"Focused Object: {cameraController?.currentFocusTarget?.name ?? "None"}");

        GUILayout.Space(10);

        // Control buttons
        if (currentGameMode == GameMode.Initial)
        {
            if (GUILayout.Button("Start Exploration"))
            {
                StartExplorationMode();
            }
        }
        else if (currentGameMode == GameMode.Exploration)
        {
            if (GUILayout.Button("Swipe Right"))
            {
                SwipeRight();
            }
            if (GUILayout.Button("Exit to Initial"))
            {
                ExitToInitialMode();
            }
        }
        else if (currentGameMode == GameMode.Zoom)
        {
            if (GUILayout.Button("Return to Exploration"))
            {
                ReturnToExplorationMode();
            }
            if (GUILayout.Button("Exit to Initial"))
            {
                ExitToInitialMode();
            }
        }

        GUILayout.EndArea();
    }

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