using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class TouchManager : MonoBehaviour, InputSystem.IInputActions
{
    public static TouchManager Instance { get; private set; }
    // Events for other scripts to listen to (replaces OnMouseDown/Up/Drag)
    public static event Action<Vector2> OnMouseDown;
    public static event Action<Vector2> OnMouseUp;
    public static event Action<Vector2> OnMouseDrag;

    public Vector3 curScreenPos;
    private Camera mainCamera;
    public bool isInteracting = false;

    // New Input System
    private InputSystem inputSystem;
    private bool isPressed = false;
    private bool wasPressed = false;

    private float edgeOffset = 10f;
    private float screenWidth;
    private float screenHeight;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        mainCamera = Camera.main;

        // Initialize new Input System
        inputSystem = new InputSystem();
        inputSystem.Input.SetCallbacks(this);

        screenWidth = Screen.width;
        screenHeight = Screen.height;
    }

    private void OnEnable()
    {
        inputSystem?.Input.Enable();
    }

    private void OnDisable()
    {
        inputSystem?.Input.Disable();
    }

    // New Input System callbacks
    public void OnPress(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Vector2 latestPointerPos = ReadPointerScreenPosition();
            if (latestPointerPos != Vector2.zero)
            {
                curScreenPos = latestPointerPos;
            }

            isPressed = true;
            if (!wasPressed)
            {
                wasPressed = true;
                OnMouseDown?.Invoke(curScreenPos);
            }
        }
        else if (context.canceled)
        {
            isPressed = false;
            if (wasPressed)
            {
                wasPressed = false;
                OnMouseUp?.Invoke(curScreenPos);
            }
            isDragging = false;
        }
    }

    public void OnScreenPos(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        curScreenPos = context.ReadValue<Vector2>();

        // Handle threshold
        float threshold = 10f;
        if (curScreenPos.x <= threshold || curScreenPos.y <= threshold)
        {
            curScreenPos = Vector3.zero;
            isInteracting = false;
        }

        // If pressed and moving, it's a drag
        if (isPressed && wasPressed)
        {
            isDragging = true;
            OnMouseDrag?.Invoke(curScreenPos);
        }
    }

    public bool IsClickedOn
    {
        get
        {
            Ray ray = mainCamera.ScreenPointToRay(curScreenPos);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                return hit.transform == transform;
            }
            return false;
        }
    }

    public void TouchUsed(bool isUsed)
    {
        isInteracting = isUsed;
    }

    // Static properties to maintain compatibility
    public static Vector2 MousePosition => Instance != null ? Instance.curScreenPos : Vector2.zero;
    public static bool IsPressed => Instance != null && Instance.isPressed;

    Vector2 ReadPointerScreenPosition()
    {
        if (Pointer.current != null)
        {
            return Pointer.current.position.ReadValue();
        }

        if (Mouse.current != null)
        {
            return Mouse.current.position.ReadValue();
        }

        return curScreenPos;
    }
}
