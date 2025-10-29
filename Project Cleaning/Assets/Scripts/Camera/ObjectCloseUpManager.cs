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
    /// Handle user input - centralized input management
    /// </summary>
    void HandleInput()
    {
        // Exit close-up view
        if (hasObjectInCloseUp && Input.GetKeyDown(exitCloseUpKey))
        {
            ExitCloseUp();
        }

        // Handle mouse input based on current state
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log($"Mouse clicked - hasObjectInCloseUp: {hasObjectInCloseUp}");

            if (hasObjectInCloseUp)
            {
                Debug.Log("Object in close-up - starting rotation");
                // All objects in close-up can be rotated for inspection
                // Jar pieces can still be assembled via drag when needed
                smoothRotator.StartRotating(currentCloseUpObject);
            }
            else
            {
                Debug.Log("No object in close-up - trying to select object...");
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
    /// Bring an object close to camera for inspection
    /// </summary>
    public void BringObjectToCloseUp(Transform targetObject)
    {
        // If there's already an object in close-up, switch to the new one
        if (hasObjectInCloseUp)
        {
            Debug.Log($"Switching from {currentCloseUpObject.name} to {targetObject.name}");
            SwitchToNewObject(targetObject);
            return;
        }

        currentCloseUpObject = targetObject;
        hasObjectInCloseUp = true;

        Debug.Log($"Bringing {targetObject.name} to close-up view");

        // Check if it's a jar piece
        var jarComponent = targetObject.GetComponent<JarAutoAssembly>();
        if (jarComponent != null)
        {
            Debug.Log($"JAR PIECE ({jarComponent.pieceType}) - Click to rotate, drag to assemble (assembled: {jarComponent.isAssembled})");
        }
        else
        {
            Debug.Log("OBJECT READY - Click to rotate and inspect");
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

        // Stop smooth rotation FIRST
        if (smoothRotator != null)
        {
            smoothRotator.StopRotating();
            Debug.Log("Stopped rotation for object returning from close-up");
        }

        // Notify inspectable component that inspection is ending
        var inspectable = currentCloseUpObject.GetComponent<IInspectable>();
        inspectable?.OnInspectionEnd();

        // Start return animation
        animationHandler.AnimateFromCloseUp(currentCloseUpObject, OnReturnAnimationComplete);
    }

    /// <summary>
    /// Called when close-up animation finishes
    /// </summary>
    void OnCloseUpAnimationComplete()
    {
        Debug.Log("Close-up animation completed - object ready for interaction");

        // Auto-start rotation for single-click inspection
        if (currentCloseUpObject != null)
        {
            Debug.Log("Auto-starting rotation for single-click inspection");
            smoothRotator.StartRotating(currentCloseUpObject);
        }
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
    /// Switch from current object to a new object for inspection
    /// </summary>
    private void SwitchToNewObject(Transform newObject)
    {
        if (currentCloseUpObject == null) return;

        Debug.Log($"Switching inspection from {currentCloseUpObject.name} to {newObject.name}");

        // Stop rotation on current object
        if (smoothRotator != null)
        {
            smoothRotator.StopRotating();
            Debug.Log("Stopped rotation on previous object");
        }

        // End inspection on current object
        var currentInspectable = currentCloseUpObject.GetComponent<IInspectable>();
        currentInspectable?.OnInspectionEnd();

        // Return current object to original position immediately (no animation)
        animationHandler.AnimateFromCloseUp(currentCloseUpObject, () => {
            // After current object returns, bring new object to close-up
            Debug.Log($"Previous object returned, now bringing {newObject.name} to close-up");
            currentCloseUpObject = newObject;
            hasObjectInCloseUp = true;

            // Start inspection on new object
            var newInspectable = newObject.GetComponent<IInspectable>();
            newInspectable?.OnInspectionStart();

            // Animate new object to close-up
            animationHandler.AnimateToCloseUp(newObject, OnCloseUpAnimationComplete);
        });
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