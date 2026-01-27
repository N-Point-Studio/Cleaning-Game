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
    public Vector3 curSecondaryPos;
    public bool isRotating = false;
    public bool isZooming = false;
    public bool isTapped = false;
    public static event Action OnTapped;
    public static event Action OnTapReleased;
    public Vector2 tapPosition;

    public static event Action OnHoldPerformed;
    public static event Action OnHoldReleased;

    [SerializeField] private float ZoomSpeed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
        }
        Instance = this;

        mainCamera = Camera.main;
        inputSystem = new InputSystem();
        inputSystem.Input.SetCallbacks(this);

        screenWidth = Screen.width;
        screenHeight = Screen.height;

        // ✅ FIX: Reset all touch states when entering new gameplay scene
        ResetTouchState();
    }

    /// <summary>
    /// Reset all touch input states to ensure clean state for new gameplay session
    /// </summary>
    public void ResetTouchState()
    {
        curScreenPos = Vector3.zero;
        curSecondaryPos = Vector3.zero;
        isInteracting = false;
        isDragging = false;
        isRotating = false;
        isZooming = false;
        isTapped = false;
        isClickedOn = false;

        Debug.Log("[TouchManager] Touch state reset for new session");
    }

    void Update()
    {
        if (curScreenPos == Vector3.zero) return;
    }

    public bool isClickedOn = false;

    void OnEnable()
    {
        if (inputSystem != null)
            inputSystem.Input.Enable();
    }

    void OnDisable()
    {
        // Add null check to prevent NullReferenceException during scene cleanup
        if (inputSystem != null)
            inputSystem.Input.Disable();
    }

    private void OnDestroy()
    {
        // Clear static listeners so zoom/tap handlers don't linger into the next scene
        ZoomStart = null;
        ZoomEnd = null;
        OnTapped = null;
        OnTapReleased = null;
        OnHoldPerformed = null;
        OnHoldReleased = null;

        if (Instance == this)
        {
            Instance = null;
        }
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
            Debug.Log("[TouchManager] secondary touch performed - ZOOM START");
            // --- PERBAIKAN: Set status zooming jadi TRUE ---
            isZooming = true;
            // -----------------------------------------------

            // ✅ DEBUG: Log event subscribers
            if (ZoomStart != null)
            {
                var subscriberCount = ZoomStart.GetInvocationList().Length;
                Debug.Log($"[TouchManager] ✅ Invoking ZoomStart event ({subscriberCount} subscribers)");
            }
            else
            {
                Debug.LogWarning("[TouchManager] ⚠️ ZoomStart event has NO subscribers!");
            }

            ZoomStart?.Invoke();
        }
        else if (context.canceled)
        {
            Debug.Log("[TouchManager] secondary touch canceled - ZOOM END");
            // --- PERBAIKAN: Set status zooming jadi FALSE ---
            isZooming = false;
            // ------------------------------------------------

            // ✅ DEBUG: Log event subscribers
            if (ZoomEnd != null)
            {
                var subscriberCount = ZoomEnd.GetInvocationList().Length;
                Debug.Log($"[TouchManager] ✅ Invoking ZoomEnd event ({subscriberCount} subscribers)");
            }
            else
            {
                Debug.LogWarning("[TouchManager] ⚠️ ZoomEnd event has NO subscribers!");
            }

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

    public void DisableAllTouch(bool status)
    {
        if (status)
        {
            inputSystem.Input.Disable();

            curScreenPos = Vector3.zero;
            curSecondaryPos = Vector3.zero;

            isInteracting = false;
            isDragging = false;
            isRotating = false;
            isZooming = false;
            isTapped = false;
            isClickedOn = false;

            OnTapReleased?.Invoke();
            OnHoldReleased?.Invoke();

            Debug.Log("All Touch Disabled");
        }
        else
        {
            inputSystem.Input.Enable();
            Debug.Log("All Touch Enabled");
        }
    }

}
