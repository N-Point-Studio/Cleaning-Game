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
        DontDestroyOnLoad(gameObject);

        mainCamera = Camera.main;
        inputSystem = new InputSystem();
        inputSystem.Input.SetCallbacks(this);
    }

    void Update()
    {
        float threshold = 10f;
        Debug.Log("CURRENTPOS: " + curScreenPos);
        if (curScreenPos.x <= threshold || curScreenPos.y <= threshold)
        {
            curScreenPos = Vector3.zero;
            isInteracting = false;
        }
    }


    public bool isClickedOn
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
            isInteracting = false;
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
