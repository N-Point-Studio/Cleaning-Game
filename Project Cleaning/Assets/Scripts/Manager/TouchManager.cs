using System;
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
    public bool isDragging = false;
    private float edgeOffset = 10f;
    private float screenWidth;
    private float screenHeight;

    //  [READ] INI UNTUK ZOOM
    public static event Action ZoomStart;
    public static event Action ZoomEnd;
    private Coroutine ZoomCoroutine;
    public Vector3 curSecondaryPos;
    public bool isRotating = false;
    public bool isZooming = false;
    public bool isTapped = false;
    public static event Action OnTapped;
    public static event Action OnTapReleased;
    public Vector2 tapPosition;

    public static event Action OnHoldPerformed;
    public static event Action OnHoldReleased;

    float touchDownTime;
    [SerializeField] private float ZoomSpeed;

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
        if (curScreenPos == Vector3.zero) return;
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
            isClickedOn = true;
        }
        else if (context.canceled)
        {
            curScreenPos = Vector3.zero;
            isInteracting = false;
            isClickedOn = false;
            isTapped = false;
        }
    }

    public void OnScreenPos(InputAction.CallbackContext context)
    {
        if (isClickedOn)
        {
            curScreenPos = context.ReadValue<Vector2>();
            tapPosition = context.ReadValue<Vector2>();
        }
    }


    public void TouchUsed(bool isUsed)
    {
        isInteracting = isUsed;
    }

    public void IsRotate(bool isRotate)
    {
        isRotating = isRotate;
    }

    public void IsZoom(bool status)
    {
        isZooming = status;
    }

    public void SetIsDrag(bool status)
    {
        isDragging = status;
    }

    public void OnSecondaryFingerPos(InputAction.CallbackContext context)
    {
        curSecondaryPos = context.ReadValue<Vector2>();

    }

    public void OnSecondaryTouchContact(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Debug.Log("secondary performed");
            ZoomStart?.Invoke();
        }
        else if (context.canceled)
        {
            Debug.Log("secondary canceled");
            ZoomEnd?.Invoke();
        }
    }

    public void OnTap(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            isTapped = true;
            OnTapped?.Invoke();
        }
        else if (context.canceled)
        {
            isTapped = false;
            Debug.Log("tap release");
            OnTapReleased?.Invoke();
        }
    }

    public void OnHold(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnHoldPerformed?.Invoke();
        }
        else if (context.canceled)
        {
            OnHoldReleased?.Invoke();
        }
    }
    private bool isTouchHittingObject = false;
    private RaycastHit hitInfo;

    // private void OnDrawGizmos()
    // {
    //     if (mainCamera == null) mainCamera = Camera.main;
    //     if (mainCamera == null || curScreenPos == Vector3.zero) return;

    //     // Konversi screen → world (z diatur agar terlihat di depan kamera)
    //     Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(curScreenPos.x, curScreenPos.y, 20f));
    //     Vector3 worldPosOffset = mainCamera.ScreenToWorldPoint(new Vector3(curScreenPos.x, curScreenPos.y + 150, 20f));

    //     Gizmos.color = Color.yellow;
    //     Gizmos.DrawSphere(worldPos, 0.1f);
    //     Gizmos.color = Color.cyan;
    //     Gizmos.DrawLine(mainCamera.transform.position, worldPos);
    //     Gizmos.color = Color.blue;
    //     Gizmos.DrawLine(mainCamera.transform.position, worldPosOffset);
    // }
}