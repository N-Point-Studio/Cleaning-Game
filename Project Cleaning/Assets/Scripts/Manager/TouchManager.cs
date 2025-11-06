using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class TouchManager : MonoBehaviour, InputSystem.IInputActions
{
    public static TouchManager Instance { get; private set; }
    public InputSystem inputSystem;
    public Vector3 curScreenPos;
    private Camera mainCamera;
    public bool isInteracting = false;

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
        inputSystem = new InputSystem();
        inputSystem.Input.SetCallbacks(this);

        screenWidth = Screen.width;
        screenHeight = Screen.height;
    }

    void Update()
    {
        Debug.Log("Position: " + curScreenPos);
        if (curScreenPos == Vector3.zero) return;
        // ScreenSafeArea();
    }

    public bool isClickedOn = false;

    public void ScreenSafeArea()
    {
        bool isOutOfBonds =
            curScreenPos.x <= edgeOffset ||
            curScreenPos.y <= edgeOffset ||
            curScreenPos.x >= screenWidth - edgeOffset ||
            curScreenPos.y >= screenHeight - edgeOffset;

        if (isOutOfBonds || !isInteracting)
        {
            curScreenPos = Vector3.zero;
        }
    }

    void OnEnable()
    {
        inputSystem.Input.Enable();
    }

    void OnDisable()
    {
        inputSystem.Input.Disable();
    }

    public void OnPress(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Debug.Log("clicked: tap");
            isClickedOn = true;
        }
        else if (context.canceled)
        {
            curScreenPos = Vector3.zero;
            isInteracting = false;
            isClickedOn = false;
            Debug.Log("clicked: release");
        }
    }

    public void OnScreenPos(InputAction.CallbackContext context)
    {
        if (isClickedOn)
        {
            curScreenPos = context.ReadValue<Vector2>();
        }
    }


    public void TouchUsed(bool isUsed)
    {
        isInteracting = isUsed;
    }
}
