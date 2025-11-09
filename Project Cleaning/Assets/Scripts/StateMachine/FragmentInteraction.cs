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
    public float zoomSpeed = 0.01f;

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

    private Vector2 previousTouchPos;

    private Coroutine zoomRoutine;

    void Awake()
    {
        cam = Camera.main;
        initialPosition = transform.position;
    }

    private void OnEnable()
    {
        TouchManager.OnTapped += HandleTap;
        TouchManager.OnHoldPerformed += HandleHold;

        if (isZoomAvailable)
        {
            TouchManager.ZoomStart += StartZoom;
            TouchManager.ZoomEnd += StopZoom;
        }
    }

    private void OnDisable()
    {
        TouchManager.OnTapped -= HandleTap;
        TouchManager.OnHoldPerformed -= HandleHold;

        if (isZoomAvailable)
        {
            TouchManager.ZoomStart -= StartZoom;
            TouchManager.ZoomEnd -= StopZoom;
        }
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
            Debug.Log($"{name} TAP triggered.");
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
            Debug.Log($"{name} HOLD triggered.");
            // Example:
            // GetComponent<FragmentController>().SetMoveStateReset();
        }
    }

    // -----------------------------------------------------
    // DRAG
    // -----------------------------------------------------
    void HandleDrag()
    {
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
        }
        // Drag in progress
        else if (isDragging)
        {
            MoveOnScreen(TouchManager.Instance.curScreenPos, dragSpeed);
        }

        // Release drag
        if (!TouchManager.Instance.isInteracting && isDragging)
        {
            isDragging = false;
            isReturning = true;
            TouchManager.Instance.TouchUsed(false);
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
        }
    }

    private void MoveOnScreen(Vector2 screenPos, float speed)
    {
        if (TouchManager.Instance.curScreenPos != Vector3.zero)
        {
            Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(
                screenPos.x,
                screenPos.y,
                cam.WorldToScreenPoint(initialPosition).z
            ));

            Vector3 target = new Vector3(worldPos.x, initialPosition.y, worldPos.z);
            transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * speed);
        }
        else
        {
            MoveBack(speed);
        }
    }

    private void MoveBack(float speed)
    {
        transform.position = Vector3.Lerp(transform.position, initialPosition, Time.deltaTime * speed);
    }



    // -----------------------------------------------------
    // ROTATE
    // -----------------------------------------------------
    void HandleRotate()
    {
        if (!TouchManager.Instance.isClickedOn) { isRotating = false; return; }

        Vector2 cur = TouchManager.Instance.curScreenPos;

        if (!isRotating)
        {
            isRotating = true;
            previousTouchPos = cur;
            return;
        }

        Vector2 delta = cur - previousTouchPos;
        transform.Rotate(-delta.y * rotateSpeed, delta.x * rotateSpeed, 0, Space.World);

        previousTouchPos = cur;
    }

    // -----------------------------------------------------
    // ZOOM
    // -----------------------------------------------------
    void StartZoom()
    {
        if (!isZoomAvailable) return;
        zoomRoutine = StartCoroutine(ZoomRoutine());
    }

    void StopZoom()
    {
        if (zoomRoutine != null) StopCoroutine(zoomRoutine);
    }

    IEnumerator ZoomRoutine()
    {
        float prevDist = 0;
        while (true)
        {
            float dist = Vector2.Distance(TouchManager.Instance.curScreenPos, TouchManager.Instance.curSecondaryPos);
            float delta = dist - prevDist;
            transform.position += transform.forward * (delta * zoomSpeed);
            prevDist = dist;
            yield return null;
        }
    }
}
