using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FragmentInteraction : MonoBehaviour
{
    [Header("Interaction Availability")]
    public bool isTapAvailable = false;
    public bool isDragAvailable = false;
    public bool isHoldAvailable = false;

    [Header("Movement Settings")]
    public float dragSpeed = 10f;
    public float zoomSpeed = 5f;

    private float returnSpeed = 10f;

    private Camera cam;
    private Vector3 initialPosition;

    public bool isTapping = false;
    public bool isDragging = false;
    public bool isHolding = false;
    public bool isReturning = false;
    public bool isRotating = false;

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
    }

    private void OnDisable()
    {
        TouchManager.OnTapped -= HandleTap;
        TouchManager.OnHoldPerformed -= HandleHold;
        TouchManager.OnHoldReleased -= HandleHoldRelease;
    }

    private void Start()
    {
        initialPosition = transform.position;
    }

    private void FixedUpdate()
    {
        if (isDragAvailable) HandleDrag();
    }

    void HandleTap()
    {
        if (!isTapAvailable) return;

        Ray ray = cam.ScreenPointToRay(TouchManager.Instance.tapPosition);
        if (Physics.Raycast(ray, out RaycastHit hit) && hit.transform == transform)
        {
            isTapping = true;
        }
    }
    public void ResetTap()
    {
        TouchManager.Instance.isTapped = false;
        isTapping = false;
    }

    void HandleHold()
    {
        if (TouchManager.Instance.isRotating || TouchManager.Instance.isDragging) return;
        if (!isHoldAvailable) return;
        Ray ray = cam.ScreenPointToRay(TouchManager.Instance.tapPosition);
        if (Physics.Raycast(ray, out RaycastHit hit) && hit.transform == transform)
        {
            isHolding = true;
        }
    }

    void HandleHoldRelease()
    {
        isHolding = false;
    }

    public void SetInitialPos(Vector3 pos)
    {
        initialPosition = pos;
    }

    void HandleDrag()
    {
        if (TouchManager.Instance.isRotating || TouchManager.Instance.isZooming) return;

        if (!TouchManager.Instance.isInteracting && !isDragging)
        {
            Ray ray = cam.ScreenPointToRay(TouchManager.Instance.curScreenPos);
            if (Physics.Raycast(ray, out RaycastHit hit) && hit.transform == transform)
            {
                isDragging = true;
                isReturning = false;
                TouchManager.Instance.TouchUsed(true);
                TouchManager.Instance.SetIsDrag(true);
            }
        }

        else if (isDragging)
        {
            MoveOnScreen(TouchManager.Instance.curScreenPos, dragSpeed);
        }

        if (!TouchManager.Instance.isInteracting && isDragging)
        {
            isDragging = false;
            isReturning = true;
            TouchManager.Instance.TouchUsed(false);
            TouchManager.Instance.SetIsDrag(false);
        }

        if (isReturning)
        {
            MoveBack(returnSpeed);
            if (Vector3.Distance(transform.position, initialPosition) < 0.01f)
            {
                isReturning = false;
                isDragging = false;
                TouchManager.Instance.SetIsDrag(false);
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
            Vector3 target = new Vector3(worldPos.x, transform.position.y, worldPos.z);
            transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * speed);
        }
    }

    private void MoveBack(float speed)
    {
        if (!isReturning) return;
        transform.position = Vector3.Lerp(transform.position, initialPosition, Time.deltaTime * speed);
    }

    internal void DisableAllInteraction()
    {
        isTapAvailable = false;
        isDragAvailable = false;
        isHoldAvailable = false;
    }
}
