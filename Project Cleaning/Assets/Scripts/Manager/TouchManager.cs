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
    private float edgeOffset = 10f;
    private float screenWidth;
    private float screenHeight;

    //  [READ] INI UNTUK ZOOM
    public static event Action ZoomStart;
    public static event Action ZoomEnd;
    private Coroutine ZoomCoroutine;
    public Vector3 curSecondaryPos;
    public bool isRotating = false;

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
        // Debug.Log("Position primary: " + curScreenPos);
        // Debug.Log("Position secondaty: " + curSecondaryPos);
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
            // curScreenPos = new Vector3
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

    public void OnPrimaryFingerPos(InputAction.CallbackContext context)
    {
        // curSecondaryPos = context.ReadValue<Vector2>();
    }

    public void OnSecondaryFingerPos(InputAction.CallbackContext context)
    {
        curSecondaryPos = context.ReadValue<Vector2>();

    }

    public void OnSecondaryTouchContact(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            ZoomStart?.Invoke();
        }
        else if (context.canceled)
        {
            ZoomEnd?.Invoke();
        }
    }

    private void ZoomBegin()
    {
        ZoomCoroutine = StartCoroutine(ZoomDetection());
    }

    private void ZoomEnding()
    {
        StopCoroutine(ZoomCoroutine);
    }

    IEnumerator ZoomDetection()
    {
        float previousDistance = 0, distance = 0;
        while (true)
        {
            distance = Vector2.Distance(curScreenPos, curSecondaryPos);

            if (distance > previousDistance)
            {
                Vector3 targetPos = mainCamera.transform.position;
                targetPos.z -= 1;
                mainCamera.transform.position = Vector3.Slerp(mainCamera.transform.position, targetPos, Time.deltaTime * ZoomSpeed);
            }
            else if (distance < previousDistance)
            {
                Vector3 targetPos = mainCamera.transform.position;
                targetPos.z += 1;
                mainCamera.transform.position = Vector3.Slerp(mainCamera.transform.position, targetPos, Time.deltaTime * ZoomSpeed);
            }

            previousDistance = distance;
            yield return null;
        }
    }
}