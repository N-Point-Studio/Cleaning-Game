using UnityEngine;
using DG.Tweening;

/// <summary>
/// Main manager for bringing objects close to camera for inspection
/// Camera never moves - only objects come to camera
/// </summary>
public class ObjectCloseUpManager : MonoBehaviour
{
    [Header("Close-Up Settings")]
    public float distanceFromCamera = 2f;
    public Vector3 positionOffset = Vector3.zero;
    public float objectScale = 1.0f;
    public float animationSpeed = 1f;

    [Header("Zoom Settings")]
    public float minZoomScale = 0.5f;
    public float maxZoomScale = 3.0f;
    public float zoomSpeed = 0.5f;
    public float zoomSmoothness = 5f;
    public float pinchSensitivity = 2.0f;

    [Header("Double-Click Zoom")]
    public bool enableDoubleClickZoom = true;
    public float doubleClickTimeWindow = 0.3f;
    public float[] zoomPresets = {1.0f, 1.5f, 2.5f}; // Cycle through these zoom levels

    [Header("Object Selection")]
    public LayerMask clickableObjects = -1;
    public KeyCode exitCloseUpKey = KeyCode.Escape;

    [Header("Zoom Hotkeys")]
    public KeyCode zoomInKey = KeyCode.Plus;
    public KeyCode zoomOutKey = KeyCode.Minus;
    public KeyCode resetZoomKey = KeyCode.R;

    [Header("References")]
    public SmoothObjectRotator smoothRotator;
    public ObjectAnimationHandler animationHandler;
    public ObjectSelectionHandler selectionHandler;

    // Current state
    private Transform currentCloseUpObject;
    private bool hasObjectInCloseUp = false;

    // Zoom state
    private float currentZoomScale = 1.0f;
    private Vector3 originalObjectScale;

    // Touch input state
    private float lastPinchDistance = 0f;
    private bool isPinching = false;

    // Double-click state
    private float lastClickTime = 0f;
    private int currentZoomPresetIndex = 0;

    // Components will auto-initialize if not assigned
    public SmoothObjectRotator SmoothRotator => smoothRotator;
    public ObjectAnimationHandler AnimationHandler => animationHandler;
    public ObjectSelectionHandler SelectionHandler => selectionHandler;

    void Awake()
    {
        InitializeComponents();
    }

    void Start()
    {
        SetupComponents();
    }

    void Update()
    {
        HandleInput();
    }

    /// <summary>
    /// Initialize all required components
    /// </summary>
    void InitializeComponents()
    {
        // Auto-create components if not assigned
        if (smoothRotator == null)
            smoothRotator = GetComponent<SmoothObjectRotator>() ?? gameObject.AddComponent<SmoothObjectRotator>();

        if (animationHandler == null)
            animationHandler = GetComponent<ObjectAnimationHandler>() ?? gameObject.AddComponent<ObjectAnimationHandler>();

        if (selectionHandler == null)
            selectionHandler = GetComponent<ObjectSelectionHandler>() ?? gameObject.AddComponent<ObjectSelectionHandler>();
    }

    /// <summary>
    /// Setup component dependencies
    /// </summary>
    void SetupComponents()
    {
        // Configure selection handler
        selectionHandler.Setup(clickableObjects, this);

        // Configure animation handler
        animationHandler.Setup(distanceFromCamera, positionOffset, objectScale, animationSpeed);

        // Configure smooth rotator
        // smoothRotator.Setup(); // No setup needed for smooth rotator
    }

    /// <summary>
    /// Handle user input - centralized input management
    /// </summary>
    void HandleInput()
    {
        // Exit close-up view
        if (hasObjectInCloseUp && Input.GetKeyDown(exitCloseUpKey))
        {
            ExitCloseUp();
        }

        // Handle zoom input when object is in close-up
        if (hasObjectInCloseUp)
        {
            HandleZoomInput();
            HandleKeyboardZoom();
        }

        // Handle mouse input based on current state
        if (Input.GetMouseButtonDown(0))
        {
            if (hasObjectInCloseUp)
            {
                // Check for double-click first
                if (enableDoubleClickZoom && HandleDoubleClick())
                {
                    // Double-click detected - don't start rotation
                    return;
                }

                // Check if the mouse is actually over the current close-up object
                if (IsMouseOverCurrentObject())
                {
                    // For jar pieces, only allow rotation if they're NOT being assembled
                    if (IsJarPieceBeingAssembled(currentCloseUpObject))
                    {
                        Debug.Log("Jar piece assembly mode - rotation disabled during assembly");
                        // Don't start rotation for jar pieces that are being assembled
                    }
                    else
                    {
                        Debug.Log("Click and hold to rotate object");
                        // Start rotation for objects that can be rotated
                        smoothRotator.StartRotating(currentCloseUpObject);
                    }
                }
                else
                {
                    Debug.Log("Click detected on different object - not the current close-up object");
                }
            }
            else
            {
                // No object in close-up - try to select one (but jar pieces will be filtered out)
                selectionHandler.TrySelectObjectAtMousePosition();
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            if (hasObjectInCloseUp)
            {
                // Stop rotation when mouse is released
                smoothRotator.StopRotating();
            }
        }
    }

    /// <summary>
    /// Handle double-click detection for zoom cycling
    /// </summary>
    bool HandleDoubleClick()
    {
        float currentTime = Time.time;

        if (currentTime - lastClickTime <= doubleClickTimeWindow)
        {
            // Double-click detected!
            CycleZoomPreset();
            lastClickTime = 0f; // Reset to prevent triple-click
            return true;
        }
        else
        {
            // Single click - update time for potential double-click
            lastClickTime = currentTime;
            return false;
        }
    }

    /// <summary>
    /// Cycle through predefined zoom levels
    /// </summary>
    void CycleZoomPreset()
    {
        if (zoomPresets == null || zoomPresets.Length == 0) return;

        // Move to next zoom preset
        currentZoomPresetIndex = (currentZoomPresetIndex + 1) % zoomPresets.Length;
        float targetZoom = zoomPresets[currentZoomPresetIndex];

        // Clamp to valid range
        targetZoom = Mathf.Clamp(targetZoom, minZoomScale, maxZoomScale);

        currentZoomScale = targetZoom;
        ApplyZoom();

        Debug.Log($"Double-click zoom: {currentZoomScale:F1}x (Preset {currentZoomPresetIndex + 1}/{zoomPresets.Length})");
    }

    /// <summary>
    /// Handle zoom input with mouse wheel and pinch-to-zoom
    /// </summary>
    void HandleZoomInput()
    {
        if (currentCloseUpObject == null) return;

        // Handle mouse wheel zoom (for desktop)
        HandleMouseWheelZoom();

        // Handle pinch-to-zoom (for Mac trackpad and iOS)
        HandlePinchZoom();
    }

    /// <summary>
    /// Handle mouse wheel zoom input
    /// </summary>
    void HandleMouseWheelZoom()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            // Calculate new zoom scale
            float zoomDirection = scrollInput > 0 ? 1f : -1f;
            float targetZoomScale = currentZoomScale + (zoomDirection * zoomSpeed);

            // Clamp zoom scale
            targetZoomScale = Mathf.Clamp(targetZoomScale, minZoomScale, maxZoomScale);

            if (Mathf.Abs(targetZoomScale - currentZoomScale) > 0.01f)
            {
                currentZoomScale = targetZoomScale;
                ApplyZoom();

                Debug.Log($"Zoom: {currentZoomScale:F1}x (Mouse wheel: {(scrollInput > 0 ? "Up" : "Down")})");
            }
        }
    }

    /// <summary>
    /// Handle pinch-to-zoom for Mac trackpad and iOS devices
    /// </summary>
    void HandlePinchZoom()
    {
        // Check for two-finger touch (iOS/mobile)
        if (Input.touchCount == 2)
        {
            Touch touch1 = Input.GetTouch(0);
            Touch touch2 = Input.GetTouch(1);

            // Calculate distance between fingers
            float currentPinchDistance = Vector2.Distance(touch1.position, touch2.position);

            if (!isPinching)
            {
                // Start pinching
                isPinching = true;
                lastPinchDistance = currentPinchDistance;
            }
            else
            {
                // Calculate pinch delta
                float pinchDelta = currentPinchDistance - lastPinchDistance;

                if (Mathf.Abs(pinchDelta) > 5f) // Minimum threshold to prevent jitter
                {
                    // Convert pinch distance to zoom scale
                    float zoomDelta = (pinchDelta / Screen.width) * pinchSensitivity;
                    float targetZoomScale = currentZoomScale + zoomDelta;

                    // Clamp zoom scale
                    targetZoomScale = Mathf.Clamp(targetZoomScale, minZoomScale, maxZoomScale);

                    if (Mathf.Abs(targetZoomScale - currentZoomScale) > 0.01f)
                    {
                        currentZoomScale = targetZoomScale;
                        ApplyZoom();

                        Debug.Log($"Zoom: {currentZoomScale:F1}x (Pinch: {(pinchDelta > 0 ? "Out" : "In")})");
                    }

                    lastPinchDistance = currentPinchDistance;
                }
            }
        }
        else
        {
            // No longer pinching
            isPinching = false;
        }

        // Handle Mac trackpad magnification gesture
        #if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        HandleMacTrackpadGestures();
        #endif
    }

    /// <summary>
    /// Handle Mac trackpad gestures (magnification)
    /// </summary>
    #if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
    void HandleMacTrackpadGestures()
    {
        // On Mac, trackpad pinch is often captured as mouse wheel with modifier keys
        // or through specialized input systems

        // Check for Command+scroll (common Mac zoom gesture)
        if ((Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand)) &&
            Mathf.Abs(Input.GetAxis("Mouse ScrollWheel")) > 0.01f)
        {
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");
            float zoomDirection = scrollInput > 0 ? 1f : -1f;
            float targetZoomScale = currentZoomScale + (zoomDirection * zoomSpeed * 2f); // Faster zoom with Command

            targetZoomScale = Mathf.Clamp(targetZoomScale, minZoomScale, maxZoomScale);

            if (Mathf.Abs(targetZoomScale - currentZoomScale) > 0.01f)
            {
                currentZoomScale = targetZoomScale;
                ApplyZoom();
                Debug.Log($"Zoom: {currentZoomScale:F1}x (Mac trackpad: Cmd+{(scrollInput > 0 ? "Up" : "Down")})");
            }
        }

        // Note: True Mac trackpad pinch detection requires native plugins
        // For now, users can use:
        // 1. Mouse wheel scrolling (works on trackpad)
        // 2. Command + scroll for faster zoom
        // 3. Keyboard shortcuts (+, -, R)
    }
    #endif

    /// <summary>
    /// Handle keyboard zoom controls
    /// </summary>
    void HandleKeyboardZoom()
    {
        if (Input.GetKeyDown(zoomInKey))
        {
            ZoomIn();
        }
        else if (Input.GetKeyDown(zoomOutKey))
        {
            ZoomOut();
        }
        else if (Input.GetKeyDown(resetZoomKey))
        {
            ZoomToFit();
        }
    }

    /// <summary>
    /// Apply zoom using DOTween for smooth scaling
    /// </summary>
    void ApplyZoom()
    {
        if (currentCloseUpObject == null) return;

        Vector3 targetScale = originalObjectScale * currentZoomScale;

        // Kill any existing scale animations
        currentCloseUpObject.DOKill();

        // Smooth zoom animation
        currentCloseUpObject.DOScale(targetScale, 1f / zoomSmoothness)
            .SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// Reset zoom to default scale
    /// </summary>
    void ResetZoom()
    {
        currentZoomScale = 1.0f;
        if (currentCloseUpObject != null)
        {
            ApplyZoom();
        }
    }

    /// <summary>
    /// Bring an object close to camera for inspection
    /// </summary>
    public void BringObjectToCloseUp(Transform targetObject)
    {
        // If there's already an object in close-up, switch to the new one
        if (hasObjectInCloseUp)
        {
            // Stop any rotation on the current object
            smoothRotator.StopRotating();

            // Return current object to its position first
            animationHandler.AnimateFromCloseUp(currentCloseUpObject, null);

            Debug.Log($"Switching from {currentCloseUpObject.name} to {targetObject.name}");
        }

        currentCloseUpObject = targetObject;
        hasObjectInCloseUp = true;

        // Initialize zoom state
        originalObjectScale = targetObject.localScale;
        currentZoomScale = 1.0f;

        // Reset double-click state
        lastClickTime = 0f;
        currentZoomPresetIndex = 0;

        Debug.Log($"Bringing {targetObject.name} to close-up view");

        // Check if it's a jar piece being assembled
        if (IsJarPieceBeingAssembled(targetObject))
        {
            Debug.Log("JAR PIECE - Click and hold to rotate, double-click to zoom, mouse wheel/pinch to zoom");
        }
        else
        {
            Debug.Log("OBJECT READY - Click and hold to rotate, double-click to zoom, mouse wheel/pinch to zoom");
        }

        // Start animation
        animationHandler.AnimateToCloseUp(targetObject, OnCloseUpAnimationComplete);
    }

    /// <summary>
    /// Return object to its original position
    /// </summary>
    public void ExitCloseUp()
    {
        if (!hasObjectInCloseUp || currentCloseUpObject == null) return;

        Debug.Log($"Returning {currentCloseUpObject.name} from close-up view");

        // Stop smooth rotation
        smoothRotator.StopRotating();

        // Reset zoom and pinch state
        currentZoomScale = 1.0f;
        isPinching = false;
        lastPinchDistance = 0f;

        // Reset double-click state
        lastClickTime = 0f;
        currentZoomPresetIndex = 0;

        // Start return animation
        animationHandler.AnimateFromCloseUp(currentCloseUpObject, OnReturnAnimationComplete);
    }

    /// <summary>
    /// Called when close-up animation finishes
    /// </summary>
    void OnCloseUpAnimationComplete()
    {
        Debug.Log("Close-up animation completed - object ready for interaction");
    }

    /// <summary>
    /// Called when return animation finishes
    /// </summary>
    void OnReturnAnimationComplete()
    {
        currentCloseUpObject = null;
        hasObjectInCloseUp = false;
        Debug.Log("Object returned to original position");
    }

    /// <summary>
    /// Get the position where objects should appear for close-up
    /// </summary>
    public Vector3 GetCloseUpPosition()
    {
        Camera cam = Camera.main;
        return cam.transform.position +
               cam.transform.forward * distanceFromCamera +
               positionOffset;
    }

    /// <summary>
    /// Update close-up settings at runtime
    /// </summary>
    public void UpdateCloseUpSettings(float distance, Vector3 offset, float scale)
    {
        distanceFromCamera = distance;
        positionOffset = offset;
        objectScale = scale;

        animationHandler?.UpdateSettings(distance, offset, scale);
    }

    /// <summary>
    /// Check if the mouse cursor is over the current close-up object
    /// </summary>
    private bool IsMouseOverCurrentObject()
    {
        if (currentCloseUpObject == null) return false;

        Camera cam = Camera.main;
        if (cam == null) return false;

        // Cast a ray from the camera through the mouse position
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            // Check if the hit object is the current close-up object or one of its children
            Transform hitTransform = hit.transform;

            // Check if it's the exact object or a child of the current object
            while (hitTransform != null)
            {
                if (hitTransform == currentCloseUpObject)
                {
                    return true;
                }
                hitTransform = hitTransform.parent;
            }
        }

        return false;
    }

    /// <summary>
    /// Check if the target object is a jar piece that's currently being assembled
    /// </summary>
    private bool IsJarPieceBeingAssembled(Transform targetObject)
    {
        if (targetObject == null) return false;

        // Check if this object has a JarAutoAssembly component
        JarAutoAssembly jarComponent = targetObject.GetComponent<JarAutoAssembly>();
        if (jarComponent != null)
        {
            // If it's a jar piece and not fully assembled, it's being assembled
            return !jarComponent.isAssembled;
        }

        return false;
    }

    /// <summary>
    /// Set zoom level programmatically
    /// </summary>
    public void SetZoomLevel(float zoomLevel)
    {
        if (!hasObjectInCloseUp) return;

        currentZoomScale = Mathf.Clamp(zoomLevel, minZoomScale, maxZoomScale);
        ApplyZoom();
        Debug.Log($"Zoom set to: {currentZoomScale:F1}x");
    }

    /// <summary>
    /// Quick zoom presets
    /// </summary>
    public void ZoomToFit() => SetZoomLevel(1.0f);
    public void ZoomIn() => SetZoomLevel(currentZoomScale + 0.5f);
    public void ZoomOut() => SetZoomLevel(currentZoomScale - 0.5f);

    /// <summary>
    /// Enable or disable double-click zoom at runtime
    /// </summary>
    public void SetDoubleClickZoomEnabled(bool enabled)
    {
        enableDoubleClickZoom = enabled;
        Debug.Log($"Double-click zoom: {(enabled ? "Enabled" : "Disabled")}");
    }

    // Public properties
    public bool HasObjectInCloseUp => hasObjectInCloseUp;
    public Transform CurrentCloseUpObject => currentCloseUpObject;
    public float CurrentZoomLevel => currentZoomScale;
    public int CurrentZoomPresetIndex => currentZoomPresetIndex;
    public bool DoubleClickZoomEnabled => enableDoubleClickZoom;

    void OnDrawGizmosSelected()
    {
        // Draw close-up position
        Vector3 closeUpPos = GetCloseUpPosition();

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(closeUpPos, 0.3f);

        // Draw line from camera to close-up position
        Camera cam = Camera.main;
        if (cam != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(cam.transform.position, closeUpPos);
        }
    }
}