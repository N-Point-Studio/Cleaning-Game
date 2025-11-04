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
    public float animationSpeed = 0.25f;
    [Tooltip("Darken background while an object is inspected")]
    public bool useBackgroundDimming = true;
    public CanvasGroup dimmingOverlay;
    public float dimmingTargetAlpha = 0.45f;
    public float dimmingSpeed = 6f;

    [Header("Dynamic Distance Settings")]
    [Tooltip("Automatically adjust distance based on object size")]
    public bool useDynamicDistance = true;
    [Tooltip("Minimum distance multiplier for very small objects")]
    public float minDistanceMultiplier = 1.5f;
    [Tooltip("Maximum distance multiplier for very large objects")]
    public float maxDistanceMultiplier = 4f;
    [Tooltip("Base size reference for distance calculation")]
    public float baseSizeReference = 1f;

    [Header("Assembled Jar Override Settings")]
    [Tooltip("Use different settings specifically for assembled jar")]
    public bool useAssembledJarOverride = true;
    [Tooltip("Fixed distance for assembled jar (overrides dynamic calculation)")]
    public float assembledJarDistance = 3.5f;
    [Tooltip("Position offset specifically for assembled jar")]
    public Vector3 assembledJarOffset = Vector3.zero;
    [Tooltip("Scale for assembled jar")]
    public float assembledJarScale = 1.0f;

    [Header("Object Selection")]
    public LayerMask clickableObjects = -1;
    public KeyCode exitCloseUpKey = KeyCode.Escape;

    [Header("References")]
    public SmoothObjectRotator smoothRotator;
    public ObjectAnimationHandler animationHandler;
    public ObjectSelectionHandler selectionHandler;

    // Current state
    private Transform currentCloseUpObject;
    private bool hasObjectInCloseUp = false;
    private bool isObjectTransitioning = false; // Prevents rotation input while tweens are running

    // Original behavior restored

    // Components will auto-initialize if not assigned
    public SmoothObjectRotator SmoothRotator => smoothRotator;
    public ObjectAnimationHandler AnimationHandler => animationHandler;
    public ObjectSelectionHandler SelectionHandler => selectionHandler;
    public bool IsTransitioning => isObjectTransitioning;

    void Awake()
    {
        InitializeComponents();
        UpdateDimmingOverlay(false, true);
    }

    void Start()
    {
        SetupComponents();
    }

    void OnEnable()
    {
        TouchManager.OnMouseDown += HandleMouseDown;
        TouchManager.OnMouseUp += HandleMouseUp;
    }

    void OnDisable()
    {
        TouchManager.OnMouseDown -= HandleMouseDown;
        TouchManager.OnMouseUp -= HandleMouseUp;
    }

    void Update()
    {
        HandleKeyboardInput();
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
        // Ensure components are properly initialized
        if (selectionHandler == null || animationHandler == null || smoothRotator == null)
        {
            Debug.LogError("ObjectCloseUpManager: Required components are missing! Re-initializing...");
            InitializeComponents();
        }

        // Configure selection handler
        if (selectionHandler != null)
        {
            selectionHandler.Setup(clickableObjects, this);
            Debug.Log($"Selection handler configured with layer mask: {clickableObjects}");
        }

        // Configure animation handler
        if (animationHandler != null)
        {
            animationHandler.Setup(distanceFromCamera, positionOffset, objectScale, animationSpeed);
            Debug.Log("Animation handler configured");
        }

        // Configure smooth rotator
        if (smoothRotator != null)
        {
            Debug.Log("Smooth rotator ready");
        }
    }

    /// <summary>
    /// Handle keyboard input only
    /// </summary>
    void HandleKeyboardInput()
    {
        // Exit close-up view
        if (hasObjectInCloseUp && Input.GetKeyDown(exitCloseUpKey))
        {
            ExitCloseUp();
        }
    }

    /// <summary>
    /// Handle mouse down from TouchManager
    /// </summary>
    void HandleMouseDown(Vector2 screenPos)
    {
        if (isObjectTransitioning)
        {
            Debug.Log("⏳ Close-up transition active - ignoring click input");
            return;
        }

        Debug.Log($"🖱️ Mouse clicked - hasObjectInCloseUp: {hasObjectInCloseUp}");

        if (hasObjectInCloseUp)
        {
            Debug.Log("🔄 Object in close-up - requesting rotation start");

            var mouseDownJarPiece = currentCloseUpObject != null ? currentCloseUpObject.GetComponent<JarAutoAssembly>() : null;
            if (mouseDownJarPiece != null)
            {
                if (mouseDownJarPiece.IsReassemblyHoldLocked)
                {
                    Debug.Log("♻️ Reassembly hold active - skipping rotation start");
                    return;
                }

                Transform rotationTarget = mouseDownJarPiece.EnsureRotationTarget(this, false);
                if (rotationTarget != null && rotationTarget != currentCloseUpObject)
                {
                    Debug.Log($"🔁 Switching rotation target to {rotationTarget.name} for partial assembly");
                    SetCurrentObject(rotationTarget);
                }
            }

            if (isObjectTransitioning)
            {
                Debug.Log("⏳ Close-up transition still running - deferring rotation start");
                return;
            }

            if (ShouldAllowRotation(currentCloseUpObject))
            {
                // Update the rotator's fixed position before starting rotation
                UpdateRotatorFixedPosition(currentCloseUpObject);
                smoothRotator.StartRotating(currentCloseUpObject);
            }
            else
            {
                Debug.Log("🚫 Rotation blocked: individual piece after completion");
            }
        }
        else
        {
            Debug.Log("🎯 No object in close-up - trying to select object...");

            // Debug: Check what objects are under the mouse before trying selection
            DebugObjectsUnderMouse(screenPos);

            // No object in close-up - try to select one
            selectionHandler.TrySelectObjectAtScreenPosition(screenPos);
        }
    }

    /// <summary>
    /// Handle mouse up from TouchManager
    /// </summary>
    void HandleMouseUp(Vector2 screenPos)
    {
        if (isObjectTransitioning)
        {
            Debug.Log("⏳ Close-up transition active - ignoring mouse up");
            return;
        }

        if (hasObjectInCloseUp)
        {
            // Stop rotation when mouse is released
            smoothRotator.StopRotating();
        }
    }

    /// <summary>
    /// Bring an object close to camera for inspection
    /// </summary>
    public void BringObjectToCloseUp(Transform targetObject)
    {
        Debug.Log($"🎯 BringObjectToCloseUp called with: {targetObject?.name ?? "null"}");
        Debug.Log($"    Current hasObjectInCloseUp: {hasObjectInCloseUp}");
        Debug.Log($"    Current object: {currentCloseUpObject?.name ?? "null"}");

        if (targetObject == null)
        {
            Debug.LogError("❌ BringObjectToCloseUp: targetObject is null!");
            return;
        }

        // If there's already an object in close-up, switch to the new one
        if (hasObjectInCloseUp)
        {
            if (currentCloseUpObject == targetObject)
            {
                Debug.Log($"🔁 {targetObject.name} is already the active close-up object - ignoring duplicate request");
                return;
            }

            Debug.Log($"🔄 Switching from {currentCloseUpObject?.name ?? "null"} to {targetObject.name}");
            SwitchToNewObject(targetObject);
            return;
        }

        Debug.Log($"🆕 No object in close-up, bringing {targetObject.name} directly");
        SetCurrentObject(targetObject);
        LogInspectionPositionDetails(targetObject);

        // Start animation
        Debug.Log($"🎬 Starting animation for {targetObject.name}");
        isObjectTransitioning = true;
        animationHandler.AnimateToCloseUp(targetObject, OnCloseUpAnimationComplete);
    }

    public void SetCurrentObject(Transform targetObject)
    {
        // FIXED: Use proper Unity null checks to prevent MissingReferenceException
        string targetName = "null";
        if (targetObject != null && targetObject.gameObject != null)
        {
            targetName = targetObject.name;
        }

        string previousName = "null";
        if (currentCloseUpObject != null && currentCloseUpObject.gameObject != null)
        {
            previousName = currentCloseUpObject.name;
        }

        Debug.Log($"🔄 SetCurrentObject called with: {targetName}");
        Debug.Log($"    Previous object: {previousName}");

        currentCloseUpObject = targetObject;
        hasObjectInCloseUp = targetObject != null;

        UpdateDimmingOverlay(hasObjectInCloseUp);

        if (targetObject == null)
        {
            Debug.Log("✅ Current object set to null, HasObjectInCloseUp = false");
            return;
        }

        // Additional check to ensure object wasn't destroyed
        if (targetObject.gameObject == null)
        {
            Debug.LogWarning("⚠️ Target object was destroyed, setting to null");
            currentCloseUpObject = null;
            hasObjectInCloseUp = false;
            return;
        }

        Debug.Log($"✅ Bringing {targetObject.name} to close-up view");
        Debug.Log($"    HasObjectInCloseUp = {hasObjectInCloseUp}");

        // Check if it's a jar piece
        var jarComponent = targetObject.GetComponent<JarAutoAssembly>();
        if (jarComponent != null)
        {
            Debug.Log($"🏺 JAR PIECE ({jarComponent.pieceType}) - Click to rotate, drag to assemble (assembled: {jarComponent.isAssembled})");

            // Check if this is an assembled jar root
            if (jarComponent.IsAssembledJarRoot(targetObject))
            {
                Debug.Log($"👑 This jar piece IS the assembled jar root");
            }
            else
            {
                Debug.Log($"🧩 This is an individual jar piece, not the assembled root");
            }
        }
        else
        {
            Debug.Log("🎯 OBJECT READY - Click to rotate and inspect (no JarAutoAssembly component)");

            // Check if this is recognized as an assembled jar by other pieces
            bool isRecognizedAsAssembledJar = IsAssembledJar(targetObject);
            Debug.Log($"    Recognized as assembled jar by other pieces: {isRecognizedAsAssembledJar}");
        }
    }

    public void ClearCurrentObject()
    {
        if (hasObjectInCloseUp)
        {
            Debug.Log($"Clearing current close-up object: {currentCloseUpObject?.name ?? "null"}");
        }

        currentCloseUpObject = null;
        hasObjectInCloseUp = false;
    }

    /// <summary>
    /// Return object to its original position
    /// </summary>
    public void ExitCloseUp(bool instant = false)
    {
        if (!hasObjectInCloseUp || currentCloseUpObject == null) return;

        Debug.Log($"Returning {currentCloseUpObject.name} from close-up view");

        // Stop smooth rotation FIRST
        if (smoothRotator != null)
        {
            smoothRotator.StopRotating();
            Debug.Log("Stopped rotation for object returning from close-up");
        }

        // Notify inspectable component that inspection is ending
        var inspectable = currentCloseUpObject.GetComponent<IInspectable>();
        inspectable?.OnInspectionEnd();

        if (instant)
        {
            isObjectTransitioning = true;
            animationHandler.ReturnToOriginalInstant(currentCloseUpObject, OnReturnAnimationComplete);
        }
        else
        {
            // Start return animation
            isObjectTransitioning = true;
            animationHandler.AnimateFromCloseUp(currentCloseUpObject, OnReturnAnimationComplete);
        }
    }

    /// <summary>
    /// Called when close-up animation finishes
    /// </summary>
    void OnCloseUpAnimationComplete()
    {
        Debug.Log("Close-up animation completed - object ready for interaction");
        isObjectTransitioning = false;

        // Auto-start rotation for single-click inspection
        if (currentCloseUpObject != null)
        {
            Debug.Log("Auto-starting rotation for single-click inspection");

            var autoRotationJarPiece = currentCloseUpObject.GetComponent<JarAutoAssembly>();
            if (autoRotationJarPiece != null)
            {
                Transform rotationTarget = autoRotationJarPiece.EnsureRotationTarget(this, false);
                if (rotationTarget != null && rotationTarget != currentCloseUpObject)
                {
                    Debug.Log($"🔁 Auto-rotation switching target to {rotationTarget.name} for partial assembly");
                    SetCurrentObject(rotationTarget);
                }
            }

            bool shouldRotate = ShouldAllowRotation(currentCloseUpObject);
            if (shouldRotate)
            {
                // Update the rotator's fixed position to the current inspection position
                // This prevents position drift during rotation
                UpdateRotatorFixedPosition(currentCloseUpObject);
                smoothRotator.StartRotating(currentCloseUpObject);
            }
            else
            {
                Debug.Log("Auto rotation skipped: individual piece after completion");
            }

            JarAutoAssembly jarPiece = currentCloseUpObject.GetComponent<JarAutoAssembly>();
            if (jarPiece != null)
            {
                Vector3 basePosition = jarPiece.GetBaseCorrectWorldPosition();
                Quaternion baseRotation = jarPiece.GetBaseCorrectWorldRotation();

                Vector3 offset = currentCloseUpObject.position - basePosition;
                Quaternion rotationOffset = currentCloseUpObject.rotation * Quaternion.Inverse(baseRotation);

                jarPiece.CacheInspectionOffsets(offset, rotationOffset);
            }
        }
    }

    /// <summary>
    /// Called when return animation finishes
    /// </summary>
    void OnReturnAnimationComplete()
    {
        currentCloseUpObject = null;
        hasObjectInCloseUp = false;
        isObjectTransitioning = false;
        UpdateDimmingOverlay(false);
        Debug.Log("Object returned to original position");
    }

    void UpdateDimmingOverlay(bool enable, bool instant = false)
    {
        if (!useBackgroundDimming || dimmingOverlay == null)
            return;

        float targetAlpha = enable ? dimmingTargetAlpha : 0f;
        if (Mathf.Approximately(dimmingOverlay.alpha, targetAlpha))
            return;

        dimmingOverlay.DOKill();

        if (instant)
        {
            dimmingOverlay.alpha = targetAlpha;
            return;
        }

        float distance = Mathf.Abs(dimmingOverlay.alpha - targetAlpha);
        float duration = (dimmingSpeed <= 0f) ? 0.2f : Mathf.Clamp(distance / dimmingSpeed, 0.05f, 0.35f);
        dimmingOverlay.DOFade(targetAlpha, duration).SetEase(Ease.OutSine);
    }

    /// <summary>
    /// Get the position where objects should appear for close-up
    /// </summary>
    public Vector3 GetCloseUpPosition()
    {
        return GetCloseUpPosition(currentCloseUpObject);
    }

    /// <summary>
    /// Get the position where a specific object should appear for close-up
    /// </summary>
    public Vector3 GetCloseUpPosition(Transform targetObject)
    {
        Camera cam = Camera.main;
        float distance = distanceFromCamera;
        Vector3 offset = positionOffset;

        if (targetObject != null)
        {
            // Check if this is an assembled jar and use override settings
            if (useAssembledJarOverride && IsAssembledJar(targetObject))
            {
                distance = assembledJarDistance;
                offset = assembledJarOffset;
                // Debug.Log($"Using assembled jar settings - distance: {distance}, offset: {offset}");
            }
            // Use dynamic distance calculation for individual pieces
            else if (useDynamicDistance)
            {
                distance = CalculateDynamicDistance(targetObject);
            }
        }

        return cam.transform.position +
               cam.transform.forward * distance +
               offset;
    }

    /// <summary>
    /// Calculate appropriate distance based on object size
    /// </summary>
    private float CalculateDynamicDistance(Transform targetObject)
    {
        if (targetObject == null) return distanceFromCamera;

        // Get object bounds
        Bounds objectBounds = GetObjectBounds(targetObject);
        float objectSize = Mathf.Max(objectBounds.size.x, objectBounds.size.y, objectBounds.size.z);

        // Calculate distance multiplier based on size
        float sizeRatio = objectSize / baseSizeReference;
        float distanceMultiplier = Mathf.Clamp(sizeRatio, minDistanceMultiplier, maxDistanceMultiplier);

        return distanceFromCamera * distanceMultiplier;
    }

    /// <summary>
    /// Get combined bounds of object and all its children
    /// </summary>
    private Bounds GetObjectBounds(Transform targetObject)
    {
        Renderer[] renderers = targetObject.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            // Fallback to collider bounds if no renderers
            Collider collider = targetObject.GetComponent<Collider>();
            return collider != null ? collider.bounds : new Bounds(targetObject.position, Vector3.one);
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        return bounds;
    }

    /// <summary>
    /// Check if the target object is an assembled jar
    /// </summary>
    private bool IsAssembledJar(Transform targetObject)
    {
        if (targetObject == null) return false;

        // Check if this transform is referenced as an assembled jar root
        JarAutoAssembly[] allJarPieces = FindObjectsOfType<JarAutoAssembly>();
        foreach (var piece in allJarPieces)
        {
            if (piece.IsAssembledJarRoot(targetObject))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Switch from current object to a new object for inspection
    /// </summary>
    private void SwitchToNewObject(Transform newObject)
    {
        if (currentCloseUpObject == null) return;

        Transform previousObject = currentCloseUpObject;

        Debug.Log($"Switching inspection from {previousObject.name} to {newObject.name}");

        // Stop rotation on current object
        if (smoothRotator != null)
        {
            smoothRotator.StopRotating();
            Debug.Log("Stopped rotation on previous object");
        }

        // End inspection on current object
        var currentInspectable = previousObject.GetComponent<IInspectable>();
        currentInspectable?.OnInspectionEnd();

        // Clear active state while the previous object returns
        currentCloseUpObject = null;
        hasObjectInCloseUp = false;

        // Return current object to original position immediately (no animation)
        isObjectTransitioning = true;
        animationHandler.AnimateFromCloseUp(previousObject, () => {
            // After current object returns, bring new object to close-up
            Debug.Log($"Previous object returned, now bringing {newObject.name} to close-up");
            SetCurrentObject(newObject);
            LogInspectionPositionDetails(newObject);

            // Start inspection on new object
            var newInspectable = newObject.GetComponent<IInspectable>();
            newInspectable?.OnInspectionStart();

            // Animate new object to close-up
            isObjectTransitioning = true;
            animationHandler.AnimateToCloseUp(newObject, OnCloseUpAnimationComplete);
        });
    }

    /// <summary>
    /// Pause rotation on currently inspected object (without exiting inspection)
    /// </summary>
    public void PauseRotation()
    {
        if (smoothRotator != null && smoothRotator.IsRotating)
        {
            smoothRotator.StopRotating();
            Debug.Log("Paused rotation on inspected object");
        }
    }

    /// <summary>
    /// Update the rotator's fixed position to prevent position drift during rotation
    /// </summary>
    private void UpdateRotatorFixedPosition(Transform targetObject)
    {
        if (targetObject == null || smoothRotator == null) return;

        // Update the fixed position to the current object position
        // This ensures rotation happens around the inspection position, not the original position
        smoothRotator.UpdateFixedPosition(targetObject.position);
        Debug.Log($"Updated rotator fixed position for {targetObject.name} at {targetObject.position}");
    }

    bool ShouldAllowRotation(Transform target)
    {
        if (target == null)
            return false;

        // If jar is not fully assembled, allow rotation of any object
        JarAutoAssembly[] allPieces = FindObjectsOfType<JarAutoAssembly>();
        bool anyNotAssembled = false;
        foreach (var piece in allPieces)
        {
            if (piece != null && piece.isAssembled)
            {
                continue;
            }

            anyNotAssembled = true;
            break;
        }

        if (anyNotAssembled)
            return true;

        // Jar is fully assembled - check what type of object we're trying to rotate
        var jarComponent = target.GetComponent<JarAutoAssembly>();

        if (jarComponent != null)
        {
            // This target has JarAutoAssembly component (it's a jar piece)
            // Only allow rotation if it's the assembled jar root
            bool isAssembledRoot = jarComponent.IsAssembledJarRoot(target);
            Debug.Log($"Jar piece rotation check: {target.name}, IsAssembledRoot: {isAssembledRoot}");
            return isAssembledRoot;
        }
        else
        {
            // This target doesn't have JarAutoAssembly component
            // Check if any jar pieces recognize this as their assembled jar root
            bool isRecognizedAsAssembledJar = IsAssembledJar(target);
            Debug.Log($"Non-jar-piece rotation check: {target.name}, IsRecognizedAsAssembledJar: {isRecognizedAsAssembledJar}");
            return isRecognizedAsAssembledJar;
        }
    }

    /// <summary>
    /// Resume rotation on currently inspected object
    /// </summary>
    public void ResumeRotation()
    {
        if (hasObjectInCloseUp && currentCloseUpObject != null && smoothRotator != null)
        {
            smoothRotator.StartRotating(currentCloseUpObject);
            Debug.Log("Resumed rotation on inspected object");
        }
    }

    

    /// <summary>
    /// Update close-up settings at runtime
    /// </summary>
    

    

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

    // Public properties
    public bool HasObjectInCloseUp => hasObjectInCloseUp;
    public Transform CurrentCloseUpObject => currentCloseUpObject;

    /// <summary>
    /// Debug method to check what objects are under the mouse cursor
    /// </summary>
    void DebugObjectsUnderMouse(Vector2 screenPos)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(screenPos);
        Debug.Log($"🔍 DEBUGGING OBJECTS UNDER MOUSE:");
        Debug.Log($"    Mouse Position: {screenPos}");
        Debug.Log($"    Ray: {ray.origin} -> {ray.direction}");

        // Test with all layers first
        RaycastHit[] allHits = Physics.RaycastAll(ray, Mathf.Infinity);
        Debug.Log($"    Total objects hit (all layers): {allHits.Length}");

        if (allHits.Length == 0)
        {
            Debug.Log($"❌ NO OBJECTS HIT BY RAYCAST - Check object colliders and positions");
            return;
        }

        foreach (RaycastHit hit in allHits)
        {
            GameObject hitObj = hit.transform.gameObject;
            JarAutoAssembly jarComponent = hitObj.GetComponent<JarAutoAssembly>();
            bool hasJarComponent = jarComponent != null;
            bool isAssembled = hasJarComponent && jarComponent.isAssembled;

            Debug.Log($"    Hit: {hitObj.name} (Layer: {hitObj.layer}, JarComponent: {hasJarComponent}, Assembled: {isAssembled})");

            if (hasJarComponent)
            {
                Debug.Log($"      -> Jar piece type: {jarComponent.pieceType}");
                Debug.Log($"      -> Position: {hitObj.transform.position}");
                Debug.Log($"      -> Collider enabled: {hitObj.GetComponent<Collider>()?.enabled}");
            }
        }

        // Test with clickable layer mask
        Debug.Log($"    Clickable layer mask: {clickableObjects} (binary: {System.Convert.ToString(clickableObjects, 2)})");

        if (Physics.Raycast(ray, out RaycastHit clickableHit, Mathf.Infinity, clickableObjects))
        {
            Debug.Log($"✅ Clickable object hit: {clickableHit.transform.name}");
        }
        else
        {
            Debug.Log($"❌ No clickable objects hit with current layer mask");
        }
    }

    /// <summary>
    /// Output detailed positional data for the object being inspected
    /// </summary>
    void LogInspectionPositionDetails(Transform targetObject)
    {
        if (targetObject == null)
        {
            Debug.LogWarning("LogInspectionPositionDetails called with null target");
            return;
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("Cannot log inspection details - Camera.main is missing");
            return;
        }

        string parentName = targetObject.parent != null ? targetObject.parent.name : "<None>";
        Debug.Log($"📐 Inspection data for {targetObject.name}");
        Debug.Log($"    Parent: {parentName}");
        Debug.Log($"    World Position: {targetObject.position}");
        Debug.Log($"    Local Position: {targetObject.localPosition}");
        Debug.Log($"    World Rotation (Euler): {targetObject.rotation.eulerAngles}");
        Debug.Log($"    Local Rotation (Euler): {targetObject.localEulerAngles}");
        Debug.Log($"    Local Scale: {targetObject.localScale}");
        Debug.Log($"    Lossy Scale: {targetObject.lossyScale}");

        bool usedAssembledOverride = false;
        bool usedDynamicDistance = false;

        float selectedDistance = distanceFromCamera;
        Vector3 selectedOffset = positionOffset;

        if (useAssembledJarOverride && IsAssembledJar(targetObject))
        {
            usedAssembledOverride = true;
            selectedDistance = assembledJarDistance;
            selectedOffset = assembledJarOffset;
        }
        else if (useDynamicDistance)
        {
            usedDynamicDistance = true;

            Renderer[] renderers = targetObject.GetComponentsInChildren<Renderer>();
            bool boundsFromRenderers = renderers.Length > 0;
            Bounds bounds = GetObjectBounds(targetObject);
            float largestDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            float safeBaseReference = Mathf.Approximately(baseSizeReference, 0f) ? 1f : baseSizeReference;
            float rawMultiplier = largestDimension / safeBaseReference;
            float clampedMultiplier = Mathf.Clamp(rawMultiplier, minDistanceMultiplier, maxDistanceMultiplier);

            selectedDistance = distanceFromCamera * clampedMultiplier;

            Debug.Log($"    Bounds Centre: {bounds.center}");
            Debug.Log($"    Bounds Size: {bounds.size}");
            Debug.Log($"    Bounds Source: {(boundsFromRenderers ? "Renderers" : "Collider/Fallback")}");
            Debug.Log($"    Dynamic Distance -> Raw Multiplier: {rawMultiplier:F3}, Clamped: {clampedMultiplier:F3}");
        }

        Vector3 closeUpTarget = cam.transform.position + cam.transform.forward * selectedDistance + selectedOffset;
        Debug.Log($"    Close-Up Target Position: {closeUpTarget}");
        Debug.Log($"    Distance Mode: {(usedAssembledOverride ? "Assembled Jar Override" : (usedDynamicDistance ? "Dynamic" : "Fixed"))}");
        Debug.Log($"    Distance Used: {selectedDistance:F3} (Base: {distanceFromCamera:F3})");
        Debug.Log($"    Offset Applied: {selectedOffset}");

        JarAutoAssembly jarComponent = targetObject.GetComponent<JarAutoAssembly>();
        if (jarComponent != null)
        {
            bool isRoot = jarComponent.IsAssembledJarRoot(targetObject);
            Debug.Log($"    Jar Piece Type: {jarComponent.pieceType}, Is Assembled: {jarComponent.isAssembled}, Is Root: {isRoot}");

            Vector3 baseCorrectPosition = jarComponent.GetBaseCorrectWorldPosition();
            Quaternion baseCorrectRotation = jarComponent.GetBaseCorrectWorldRotation();
            Debug.Log($"    Jar Base Position: {baseCorrectPosition}");
            Debug.Log($"    Jar Base Rotation (Euler): {baseCorrectRotation.eulerAngles}");
        }

        var inspectable = targetObject.GetComponent<IInspectable>();
        if (inspectable != null)
        {
            float idealScale = inspectable.GetIdealInspectionScale();
            Transform inspectionTarget = inspectable.GetInspectionTarget();
            Debug.Log($"    Inspectable Ideal Scale: {idealScale}");
            Debug.Log($"    Inspectable Target: {(inspectionTarget != null ? inspectionTarget.name : "<null>")}");
        }

        Debug.Log($"    Camera Position: {cam.transform.position}");
        Debug.Log($"    Camera Forward: {cam.transform.forward}");
    }

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
