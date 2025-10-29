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

        // Handle mouse input based on current state
        if (Input.GetMouseButtonDown(0))
        {
            if (hasObjectInCloseUp)
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
        if (hasObjectInCloseUp) return;

        currentCloseUpObject = targetObject;
        hasObjectInCloseUp = true;

        Debug.Log($"Bringing {targetObject.name} to close-up view");

        // Check if it's a jar piece being assembled
        if (IsJarPieceBeingAssembled(targetObject))
        {
            Debug.Log("JAR PIECE - Click and hold to rotate, release and drag to assemble");
        }
        else
        {
            Debug.Log("OBJECT READY - Click and hold to rotate");
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