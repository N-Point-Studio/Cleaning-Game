using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using DG.Tweening;

/// <summary>
/// Advanced Input Manager - Clean Version
/// Controls game modes: Initial → Exploration → Zoom
/// Handles touch/mouse input, swipe gestures, pinch gestures
/// </summary>
public class AdvancedInputManager : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private LayerMask clickableLayerMask = -1;
    [SerializeField] private float maxClickDistance = 100f;

    [Header("Camera Drag (Zoom Mode Only)")]
    [SerializeField] private float cameraDragSensitivity = 0.01f;
    [SerializeField] private float cameraDragSmoothing = 5f;
    [SerializeField] private float maxDragSpeed = 2f;

    [Header("UI Controls")]
    [SerializeField] private Image startExplorationImage;
    [SerializeField] private Image additionalImage1;
    [SerializeField] private Image additionalImage2;

    [Header("Exploration Settings")]
    [SerializeField] private Vector3 explorationPosition = new Vector3(-4.35f, 3.851f, -1.48f);
    [SerializeField] private Vector3 explorationRotation = new Vector3(90f, 0f, 0f);
    [SerializeField] private float explorationTransitionDuration = 15f;
    [SerializeField] private float returnTransitionDuration = 1.2f;

    [Header("UI Transition Settings")]
    [SerializeField] private float buttonFadeDuration = 0.5f;
    [SerializeField] private float buttonScaleDuration = 0.3f;
    [SerializeField] private float cameraDelayAfterButton = 0.2f;

    [Header("Gesture Settings")]
    [SerializeField] private float swipeThreshold = 50f;
    [SerializeField] private float maxSwipeTime = 1f;
    [SerializeField] private float swipeTransitionDuration = 0.6f;
    [SerializeField] private float pinchThreshold = 30f;
    [SerializeField] private float maxPinchTime = 2f;

    [Header("Scene Change Settings")]
    [SerializeField] private bool enableSceneChange = true;
    [SerializeField] private float sceneChangeDelay = 0.5f;

    // Core components
    private Camera playerCamera;
    private TopDownCameraController cameraController;

    // Game state
    private GameMode currentGameMode = GameMode.Initial;
    private readonly float[] cameraXPositions = { -4.35f, -2.5f, -0.5f };
    private int currentPositionIndex = 0;
    private Vector3 originalCameraPosition, originalCameraRotation;
    private bool justEnteredZoomMode = false;

    // Input systems
    private CameraDragSystem cameraDragSystem;
    private SwipeDetectionSystem swipeDetectionSystem;
    private PinchDetectionSystem pinchDetectionSystem;

    // Singleton
    public static AdvancedInputManager Instance { get; private set; }

    public enum GameMode { Initial, Exploration, Zoom }

    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            playerCamera = Camera.main;
            cameraDragSystem = new CameraDragSystem(cameraDragSensitivity, cameraDragSmoothing, maxDragSpeed);
            swipeDetectionSystem = new SwipeDetectionSystem(swipeThreshold, maxSwipeTime);
            pinchDetectionSystem = new PinchDetectionSystem(pinchThreshold, maxPinchTime);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        Application.targetFrameRate = 60;
        cameraController = TopDownCameraController.Instance;
        SetupUI();
    }

    private void Update()
    {
        HandleInput();
        if (currentGameMode == GameMode.Zoom)
        {
            cameraDragSystem.Update(Time.deltaTime, cameraController);
        }
    }
    #endregion

    #region UI Setup
    private void SetupUI()
    {
        if (startExplorationImage != null)
            SetupImageClickDetection(startExplorationImage, StartExplorationMode);

        if (additionalImage1 != null)
            additionalImage1.raycastTarget = false;

        if (additionalImage2 != null)
            additionalImage2.raycastTarget = false;
    }

    private void SetupImageClickDetection(Image targetImage, System.Action onClickAction)
    {
        if (!targetImage.TryGetComponent<EventTrigger>(out var eventTrigger))
            eventTrigger = targetImage.gameObject.AddComponent<EventTrigger>();

        var clickEvent = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        clickEvent.callback.AddListener((data) => onClickAction?.Invoke());
        eventTrigger.triggers.Add(clickEvent);
        targetImage.raycastTarget = true;
    }

    private void SetImageClickable(Image targetImage, bool clickable)
    {
        if (targetImage == null) return;

        targetImage.raycastTarget = clickable;
        if (targetImage.TryGetComponent<EventTrigger>(out var eventTrigger))
            eventTrigger.enabled = clickable;
    }
    #endregion

    #region Input Handling
    private void HandleInput()
    {
        if (!Application.isMobilePlatform || Application.isEditor)
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
        if (Input.touchCount == 1)
        {
            var touch = Input.GetTouch(0);
            switch (touch.phase)
            {
                case TouchPhase.Began: HandleInputDown(touch.position); break;
                case TouchPhase.Moved: HandleInputDrag(touch.position); break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled: HandleInputUp(); break;
            }
        }
        else if (Input.touchCount == 2)
        {
            HandlePinchInput();
        }
    }

    private void HandlePinchInput()
    {
        var touch1 = Input.GetTouch(0);
        var touch2 = Input.GetTouch(1);

        if (touch1.phase == TouchPhase.Began || touch2.phase == TouchPhase.Began)
        {
            if (currentGameMode != GameMode.Initial) pinchDetectionSystem.StartPinch();
        }
        else if (touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved)
        {
            pinchDetectionSystem.UpdatePinch();
        }
        else if (touch1.phase == TouchPhase.Ended || touch2.phase == TouchPhase.Ended ||
                 touch1.phase == TouchPhase.Canceled || touch2.phase == TouchPhase.Canceled)
        {
            var pinchResult = pinchDetectionSystem.EndPinch();
            if (pinchResult.IsValid) HandlePinchGesture(pinchResult.Direction);
        }
    }

    private void HandleInputDown(Vector2 screenPosition)
    {
        switch (currentGameMode)
        {
            case GameMode.Initial:
                // Input ignored in initial mode - user must start exploration first
                break;

            case GameMode.Exploration:
                if (!CheckForObjectClick(screenPosition))
                    swipeDetectionSystem.StartSwipe(screenPosition);
                break;

            case GameMode.Zoom:
                if (!CheckForObjectClick(screenPosition))
                    cameraDragSystem.StartDrag(screenPosition);
                break;
        }
    }

    private void HandleInputDrag(Vector2 screenPosition)
    {
        if (currentGameMode == GameMode.Zoom)
        {
            cameraDragSystem.UpdateDrag(screenPosition, cameraController);
        }
        else if (currentGameMode == GameMode.Exploration)
        {
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
            case SwipeDirection.Right: PerformSwipeLeft(); break;
            case SwipeDirection.Left: PerformSwipeRight(); break;
        }
    }

    private void HandlePinchGesture(PinchDirection direction)
    {
        if (currentGameMode == GameMode.Exploration && direction == PinchDirection.In)
        {
            EnterZoomModeAtCenter();
        }
        else if (currentGameMode == GameMode.Zoom && direction == PinchDirection.Out)
        {
            ReturnToExplorationMode();
        }
    }

    private void PerformSwipeRight()
    {
        if (currentPositionIndex < cameraXPositions.Length - 1)
        {
            currentPositionIndex++;
            AnimateToPosition(cameraXPositions[currentPositionIndex]);
        }
    }

    private void PerformSwipeLeft()
    {
        if (currentPositionIndex > 0)
        {
            currentPositionIndex--;
            AnimateToPosition(cameraXPositions[currentPositionIndex]);
        }
    }
    #endregion

    #region Game Mode Management
    public void StartExplorationMode()
    {
        if (currentGameMode != GameMode.Initial) return;

        SetImageClickable(startExplorationImage, false);
        if (additionalImage1 != null) additionalImage1.raycastTarget = false;
        if (additionalImage2 != null) additionalImage2.raycastTarget = false;

        StartCoroutine(ElegantButtonTransition());
    }

    private IEnumerator ElegantButtonTransition()
    {
        var imagesToAnimate = new System.Collections.Generic.List<Image>();
        if (startExplorationImage != null) imagesToAnimate.Add(startExplorationImage);
        if (additionalImage1 != null) imagesToAnimate.Add(additionalImage1);
        if (additionalImage2 != null) imagesToAnimate.Add(additionalImage2);

        var masterSequence = DOTween.Sequence();

        for (int i = 0; i < imagesToAnimate.Count; i++)
        {
            var image = imagesToAnimate[i];
            var canvasGroup = image.GetComponent<CanvasGroup>() ?? image.gameObject.AddComponent<CanvasGroup>();
            var transform = image.transform;
            var originalScale = transform.localScale;

            var imageSequence = DOTween.Sequence();
            imageSequence.Append(transform.DOScale(originalScale * 0.85f, buttonScaleDuration * 0.6f).SetEase(Ease.OutBack));
            imageSequence.Join(canvasGroup.DOFade(0.4f, buttonScaleDuration * 0.6f).SetEase(Ease.OutSine));
            imageSequence.Append(transform.DOScale(0f, buttonFadeDuration * 1.2f).SetEase(Ease.InSine));
            imageSequence.Join(canvasGroup.DOFade(0f, buttonFadeDuration * 1.2f).SetEase(Ease.InSine));

            masterSequence.Insert(i * 0.15f, imageSequence);
        }

        yield return masterSequence.WaitForCompletion();

        foreach (var image in imagesToAnimate) image.gameObject.SetActive(false);
        yield return new WaitForSeconds(0.3f);

        BeginExplorationMode();
    }

    private void BeginExplorationMode()
    {
        currentGameMode = GameMode.Exploration;
        currentPositionIndex = 0;
        originalCameraPosition = cameraController.transform.position;
        originalCameraRotation = cameraController.transform.rotation.eulerAngles;
        StartCoroutine(ElegantCameraTransitionToExploration());
    }

    private IEnumerator ElegantCameraTransitionToExploration()
    {
        yield return new WaitForSeconds(cameraDelayAfterButton);

        var cameraTransform = cameraController.transform;
        DOTween.Kill(cameraTransform);

        var sequence = DOTween.Sequence();
        sequence.Append(cameraTransform.DOMove(explorationPosition, explorationTransitionDuration).SetEase(Ease.OutCubic));
        sequence.Join(cameraTransform.DORotate(explorationRotation, explorationTransitionDuration).SetEase(Ease.OutCubic));

        yield return sequence.WaitForCompletion();
    }

    private void EnterZoomMode(Transform targetObject)
    {
        currentGameMode = GameMode.Zoom;
        justEnteredZoomMode = true;
        cameraController.SetFocusTarget(targetObject);
        cameraController.SwitchState(cameraController.focusState);
    }

    private void EnterZoomModeAtCenter()
    {
        currentGameMode = GameMode.Zoom;
        justEnteredZoomMode = true;

        var currentPosition = cameraController.transform.position;
        var focusPosition = currentPosition + cameraController.transform.forward * 2f;
        var tempFocus = new GameObject("TempFocusTarget") { transform = { position = focusPosition } };

        cameraController.SetFocusTarget(tempFocus.transform);
        cameraController.SwitchState(cameraController.focusState);

        StartCoroutine(CleanupTempFocusTarget(tempFocus, 0.1f));
    }

    private IEnumerator CleanupTempFocusTarget(GameObject tempTarget, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (tempTarget != null) Destroy(tempTarget);
    }

    public void ReturnToExplorationMode()
    {
        if (currentGameMode != GameMode.Zoom) return;

        // Disable all inspectable objects when leaving zoom mode
        DisableAllInspectableObjects();

        currentGameMode = GameMode.Exploration;
        justEnteredZoomMode = false;
        AnimateToPosition(cameraXPositions[currentPositionIndex]);
    }

    public void ExitToInitialMode()
    {
        // Disable all inspectable objects when exiting to initial mode
        DisableAllInspectableObjects();

        currentGameMode = GameMode.Initial;
        justEnteredZoomMode = false;
        AnimateToOriginalPosition();
        StartCoroutine(ShowStartButtonElegantly());
    }

    private IEnumerator ShowStartButtonElegantly()
    {
        yield return new WaitForSeconds(returnTransitionDuration * 0.7f);

        var imagesToAnimate = new System.Collections.Generic.List<Image>();
        if (startExplorationImage != null) imagesToAnimate.Add(startExplorationImage);
        if (additionalImage1 != null) imagesToAnimate.Add(additionalImage1);
        if (additionalImage2 != null) imagesToAnimate.Add(additionalImage2);

        foreach (var image in imagesToAnimate)
        {
            image.gameObject.SetActive(true);
            var canvasGroup = image.GetComponent<CanvasGroup>() ?? image.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            image.transform.localScale = Vector3.zero;
        }

        if (startExplorationImage != null) SetImageClickable(startExplorationImage, true);

        var masterSequence = DOTween.Sequence();
        for (int i = 0; i < imagesToAnimate.Count; i++)
        {
            var image = imagesToAnimate[i];
            var canvasGroup = image.GetComponent<CanvasGroup>();
            var transform = image.transform;

            var imageSequence = DOTween.Sequence();
            imageSequence.Append(transform.DOScale(Vector3.one * 1.05f, buttonFadeDuration * 0.7f).SetEase(Ease.OutBack));
            imageSequence.Join(canvasGroup.DOFade(1f, buttonFadeDuration * 0.7f).SetEase(Ease.OutSine));
            imageSequence.Append(transform.DOScale(Vector3.one, buttonFadeDuration * 0.3f).SetEase(Ease.OutSine));

            masterSequence.Insert(i * 0.2f, imageSequence);
        }

        yield return masterSequence.WaitForCompletion();
    }
    #endregion

    #region Camera Animation
    private void AnimateToPosition(float xPosition)
    {
        if (cameraController == null) return;

        var targetPosition = new Vector3(xPosition, explorationPosition.y, explorationPosition.z);
        cameraController.transform.DOMove(targetPosition, swipeTransitionDuration).SetEase(Ease.OutCubic);
    }

    private void AnimateToOriginalPosition()
    {
        if (cameraController == null) return;

        var sequence = DOTween.Sequence();
        sequence.Append(cameraController.transform.DOMove(originalCameraPosition, returnTransitionDuration).SetEase(Ease.InOutQuart));
        sequence.Join(cameraController.transform.DORotate(originalCameraRotation, returnTransitionDuration).SetEase(Ease.InOutQuart));
    }

    #endregion

    #region Object Interaction
    private bool CheckForObjectClick(Vector2 screenPosition)
    {
        Debug.Log("=== CheckForObjectClick called ===");

        var ray = playerCamera.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, maxClickDistance, clickableLayerMask))
        {
            Debug.Log("No object hit by raycast");
            return false;
        }

        Debug.Log($"Hit object: {hit.collider.name}");

        if (!hit.collider.TryGetComponent<ClickableObject>(out var clickable))
        {
            Debug.Log("Object has no ClickableObject component");
            return false;
        }

        Debug.Log($"Found ClickableObject on: {clickable.name}, Current mode: {currentGameMode}");

        switch (currentGameMode)
        {
            case GameMode.Exploration:
                Debug.Log("Calling HandleExplorationClick");
                HandleExplorationClick(clickable);
                break;

            case GameMode.Zoom:
                Debug.Log("Calling HandleZoomClick");
                HandleZoomClick(clickable);
                break;

            default:
                Debug.Log("Calling PlayClickFeedback (default)");
                PlayClickFeedback(clickable);
                break;
        }

        return true;
    }

    private void HandleExplorationClick(ClickableObject clickable)
    {
        // Show text popup first in exploration mode
        PlayClickFeedback(clickable);

        // Then enter zoom mode
        EnterZoomMode(clickable.transform);
    }

    private void HandleZoomClick(ClickableObject clickable)
    {
        if (justEnteredZoomMode)
        {
            justEnteredZoomMode = false;
            PlayClickFeedback(clickable);

            // Enable inspectable functionality for focused object in zoom mode
            if (clickable.IsInspectable())
            {
                clickable.SetInspectableEnabled(true);
            }
            return;
        }

        if (enableSceneChange && clickable.CanChangeScene())
            HandleSceneChange(clickable);

        PlayClickFeedback(clickable);
    }

    private void HandleSceneChange(ClickableObject clickableObject)
    {
        if (clickableObject?.CanChangeScene() != true)
            return;

        string sceneName = clickableObject.GetSceneName();
        if (string.IsNullOrEmpty(sceneName))
            return;

        // Use Easy Transitions if available and enabled
        if (clickableObject.UseTransitionAnimation() && clickableObject.GetTransitionSettings() != null)
        {
            EasyTransition.TransitionManager.Instance().Transition(sceneName, clickableObject.GetTransitionSettings(), 0f);
        }
        else
        {
            // Fallback to direct scene load
            StartCoroutine(ChangeSceneCoroutine(sceneName));
        }
    }

    private IEnumerator ChangeSceneCoroutine(string sceneName)
    {
        if (sceneChangeDelay > 0)
            yield return new WaitForSeconds(sceneChangeDelay);

        SceneManager.LoadScene(sceneName);
    }

    private void PlayClickFeedback(ClickableObject clickable)
    {
        Debug.Log("=== PlayClickFeedback called ===");

        if (clickable == null)
        {
            Debug.Log("Clickable is null!");
            return;
        }

        Debug.Log($"PlayClickFeedback for: {clickable.name}");

        // Play audio feedback
        if (clickable.ClickSound != null && clickable.TryGetComponent<AudioSource>(out var audioSource))
            audioSource.PlayOneShot(clickable.ClickSound);

        // Mark as focused and trigger events
        clickable.SetFocusState(true);
        clickable.OnObjectClicked?.Invoke();

        // Call the OnClick method to trigger popup image
        Debug.Log("Calling clickable.OnClick()");
        clickable.OnClick();
    }

    private void DisableAllInspectableObjects()
    {
        // Find all clickable objects and disable their inspectable functionality
        ClickableObject[] allClickables = FindObjectsOfType<ClickableObject>();
        foreach (var clickable in allClickables)
        {
            if (clickable.IsInspectable())
            {
                clickable.SetInspectableEnabled(false);
                clickable.SetFocusState(false);
            }
        }
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

// Supporting classes moved to bottom to reduce main class length
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
}

public class PinchDetectionSystem
{
    private readonly float pinchThreshold, maxPinchTime;
    private bool isTracking;
    private float startDistance, startTime;

    public PinchDetectionSystem(float threshold, float maxTime)
    {
        pinchThreshold = threshold;
        maxPinchTime = maxTime;
    }

    public void StartPinch()
    {
        if (Input.touchCount != 2) return;

        var touch1 = Input.GetTouch(0);
        var touch2 = Input.GetTouch(1);

        isTracking = true;
        startDistance = Vector2.Distance(touch1.position, touch2.position);
        startTime = Time.time;
    }

    public void UpdatePinch()
    {
        if (!isTracking || Input.touchCount != 2)
        {
            if (isTracking && Input.touchCount != 2) isTracking = false;
            return;
        }

        if (Time.time - startTime > maxPinchTime) isTracking = false;
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

        if (Input.touchCount != 2)
        {
            result.IsValid = false;
            return result;
        }

        var touch1 = Input.GetTouch(0);
        var touch2 = Input.GetTouch(1);
        var endDistance = Vector2.Distance(touch1.position, touch2.position);
        var duration = Time.time - startTime;
        var distanceChange = endDistance - startDistance;

        result.StartDistance = startDistance;
        result.EndDistance = endDistance;
        result.DistanceChange = distanceChange;
        result.Duration = duration;

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