using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FragmentInteraction : MonoBehaviour
{
    [Header("Interaction Availability")]
    public bool isTapAvailable = false;
    public bool isDragAvailable = false;
    public bool isHoldAvailable = false;
    public bool isRotateAvailable = false;
    public bool isZoomAvailable = false;

    [Header("Movement Settings")]
    public float dragSpeed = 10f;
    public float rotateSpeed = 0.08f;
    public float zoomSpeed = 5f;

    private float returnSpeed = 10f;

    private Camera cam;
    private Vector3 initialPosition;
    private float initialScreenZ;

    public bool isTapping = false;
    public bool isDragging = false;
    public bool isHolding = false;
    public bool isReturning = false;
    public bool isRotating = false;
    public bool isZooming = false;

    [Header("Rotate Settings")]
    [SerializeField] private float rotationRate = .08f;
    [SerializeField] private bool xRotation = true;
    [SerializeField] private bool yRotation = true;
    [SerializeField] private bool invertX = false;
    [SerializeField] private bool invertY = true;
    [SerializeField] private float dragThreshold = 5f;
    private bool fingerOnObject = false;
    private float previousX;
    private float previousY;

    private Vector2 previousTouchPos;

    private Coroutine zoomRoutine;
    private float minZoom = 4.4f;
    private float maxZoom = 6.5f;

    void Awake()
    {
        cam = Camera.main;
        initialPosition = transform.position;
    }

    private void OnEnable()
    {
        TouchManager.OnTapped += HandleTap;
        TouchManager.OnHoldPerformed += HandleHold;
        TouchManager.OnHoldReleased += HandleHoldRelease;

        // if (isZoomAvailable)
        // {
        TouchManager.ZoomStart += StartZoom;
        TouchManager.ZoomEnd += StopZoom;
        // }
    }

    private void OnDisable()
    {
        TouchManager.OnTapped -= HandleTap;
        TouchManager.OnHoldPerformed -= HandleHold;
        TouchManager.OnHoldReleased -= HandleHoldRelease;

        TouchManager.ZoomStart -= StartZoom;
        TouchManager.ZoomEnd -= StopZoom;
    }

    private void Start()
    {
        initialPosition = transform.position;
    }

    void Update()
    {
        if (isDragAvailable) HandleDrag();
        if (isRotateAvailable) HandleRotate();
    }

    // -----------------------------------------------------
    // TAP
    // -----------------------------------------------------
    void HandleTap()
    {
        if (!isTapAvailable) return;

        Ray ray = cam.ScreenPointToRay(TouchManager.Instance.tapPosition);
        if (Physics.Raycast(ray, out RaycastHit hit) && hit.transform == transform)
        {
            isTapping = true;
            // Debug.Log($"{name} TAP triggered.");
            // Call inspect / select logic here
            // Example:
            // AssembleManager.Instance.InspectFragment(GetComponent<FragmentController>());
        }
    }
    public void ResetTap() => isTapping = false;

    // -----------------------------------------------------
    // HOLD
    // -----------------------------------------------------
    void HandleHold()
    {
        if (!isHoldAvailable) return;

        Ray ray = cam.ScreenPointToRay(TouchManager.Instance.tapPosition);
        if (Physics.Raycast(ray, out RaycastHit hit) && hit.transform == transform)
        {
            isHolding = true;
            // Debug.Log($"{name} HOLD triggered.");

            // Example:
            // GetComponent<FragmentController>().SetMoveStateReset();
        }
    }

    void HandleHoldRelease()
    {
        isHolding = false;
    }

    // -----------------------------------------------------
    // DRAG
    // -----------------------------------------------------
    void HandleDrag()
    {
        // if (isZoomAvailable) return;
        if (isZooming) return;
        Debug.Log("pos handle drag: ");
        // Start Drag
        if (!TouchManager.Instance.isInteracting && !isDragging)
        {
            Ray ray = cam.ScreenPointToRay(TouchManager.Instance.curScreenPos);
            if (Physics.Raycast(ray, out RaycastHit hit) && hit.transform == transform)
            {
                isDragging = true;
                isReturning = false;
                TouchManager.Instance.TouchUsed(true);
            }
            Debug.Log("Target dragged start" + " " + name);
        }
        // Drag in progress
        else if (isDragging)
        {
            MoveOnScreen(TouchManager.Instance.curScreenPos, dragSpeed);
            Debug.Log("Target dragging" + " " + name);

        }

        // Release drag
        if (!TouchManager.Instance.isInteracting && isDragging)
        {
            isDragging = false;
            isReturning = true;
            TouchManager.Instance.TouchUsed(false);
            Debug.Log("Target release dragged" + " " + name);

        }

        // Return to initial position when released
        if (isReturning)
        {
            MoveBack(returnSpeed);
            if (Vector3.Distance(transform.position, initialPosition) < 0.01f)
            {
                isReturning = false;
                isDragging = false;
            }
            Debug.Log("Target dragged return" + " " + name);

        }
    }

    private void MoveOnScreen(Vector2 screenPos, float speed)
    {
        // if (isZoomAvailable) return;
        if (TouchManager.Instance.curScreenPos != Vector3.zero)
        {
            Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(
                screenPos.x,
                screenPos.y,
                cam.WorldToScreenPoint(initialPosition).z
            ));

            // Vector3 target = new Vector3(worldPos.x, initialPosition.y, worldPos.z);
            Vector3 target = new Vector3(worldPos.x, transform.position.y, worldPos.z);

            transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * speed);
        }
        // else
        // {
        //     MoveBack(speed);
        // }
    }

    private void MoveBack(float speed)
    {
        if (!isReturning) return;
        transform.position = Vector3.Lerp(transform.position, initialPosition, Time.deltaTime * speed);
    }

    // -----------------------------------------------------
    // ROTATE
    // -----------------------------------------------------
    public void HandleRotate()
    {
        // Rotate hanya boleh jalan saat state Inspect
        if (!isRotateAvailable) return;

        // Kalau tidak ada input tekan -> reset rotate state
        if (!TouchManager.Instance.isClickedOn)
        {
            isRotating = false;
            fingerOnObject = false;
            TouchManager.Instance.IsRotate(false);
            return;
        }

        Vector2 curPos = TouchManager.Instance.curScreenPos;
        Ray ray = cam.ScreenPointToRay(curPos);

        // Check apakah user menyentuh object
        if (!fingerOnObject)
        {
            if (Physics.Raycast(ray, out RaycastHit hit) && hit.transform == transform)
            {
                fingerOnObject = true;
                previousX = curPos.x;
                previousY = curPos.y;
                return;
            }
            return;
        }

        // Tentukan kapan rotate dimulai (setelah dragThreshold)
        float moveDist = Vector2.Distance(new Vector2(previousX, previousY), curPos);
        if (moveDist > dragThreshold)
        {
            isRotating = true;
            TouchManager.Instance.IsRotate(true);
        }

        if (isRotating)
            ApplyRotation(curPos);
    }

    private void ApplyRotation(Vector2 touchPos)
    {
        float deltaX = -(touchPos.y - previousY) * rotationRate;
        float deltaY = -(touchPos.x - previousX) * rotationRate;

        if (!yRotation) deltaX = 0;
        if (!xRotation) deltaY = 0;
        if (invertX) deltaY *= -1;
        if (invertY) deltaX *= -1;

        transform.Rotate(deltaX, 0, deltaY, Space.World);

        previousX = touchPos.x;
        previousY = touchPos.y;
    }

    // -----------------------------------------------------
    // ZOOM
    // -----------------------------------------------------
    void StartZoom()
    {
        if (!isZoomAvailable) return;
        isZooming = true;
        isDragging = false;
        isReturning = false;
        Debug.Log("Is zooming? " + isZooming + " " + name);
        zoomRoutine = StartCoroutine(ZoomRoutine());
    }

    void StopZoom()
    {
        isZooming = false;
        Debug.Log("Is zooming? " + isZooming);
        if (zoomRoutine != null) StopCoroutine(zoomRoutine);
    }

    IEnumerator ZoomRoutine()
    {
        float previousDistance = 0f, distance;

        while (true)
        {
            distance = Vector2.Distance(TouchManager.Instance.curScreenPos, TouchManager.Instance.curSecondaryPos);
            Debug.Log("Is Zooming Distance " + distance);

            Vector3 targetPos = transform.position;

            if (distance > previousDistance)
                targetPos.y += 1f;
            else if (distance < previousDistance)
                targetPos.y -= 1f;

            targetPos.y = Mathf.Clamp(targetPos.y, minZoom, maxZoom);
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * zoomSpeed);

            Debug.Log("position of: " + name + "is " + transform.position);
            Debug.Log("Target pos: " + targetPos.y);

            previousDistance = distance;
            yield return null;
        }
    }

    public void HandleZoom()
    {
        if (!isZoomAvailable || !isZooming) return;
    }
}
