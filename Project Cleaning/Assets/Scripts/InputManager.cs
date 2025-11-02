using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class TouchManager : MonoBehaviour, InputSystem.IInputActions
{
    public static TouchManager Instance { get; private set; }  // Singleton instance

    public InputSystem inputSystem;
    public Vector3 curScreenPos;
    private Camera mainCamera;
    public bool isDragging;
    public bool isInteracting = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Optional: persists across scenes

        mainCamera = Camera.main;
        inputSystem = new InputSystem();
        inputSystem.Input.SetCallbacks(this);
    }

    private bool isClickedOn
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
            Debug.Log("Press performed");
            if (isClickedOn) { }
        }
        else if (context.canceled)
        {
            Debug.Log("Press canceled");
            curScreenPos = Vector3.zero;
            TouchUsed(false);
        }
    }

    public void OnScreenPos(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        Debug.Log("Screen Pos performed");
        curScreenPos = context.ReadValue<Vector2>();
    }

    public void TouchUsed(bool isUsed)
    {
        isInteracting = isUsed;
    }
}
