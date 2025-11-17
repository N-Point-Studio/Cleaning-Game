using System.Collections;
using UnityEngine;

public class InspectableJar : MonoBehaviour
{
    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float minZoom = 4.4f;
    [SerializeField] private float maxZoom = 6.5f;

    [Header("Rotate Settings")]
    [SerializeField] private float rotationRate = .08f;
    [SerializeField] private bool xRotation = true;
    [SerializeField] private bool yRotation = true;
    [SerializeField] private bool invertX = false;
    [SerializeField] private bool invertY = true;

    public bool isRotating = false;
    private float previousX;
    private float previousZ;
    private bool hasInitializedTouch = false;
    private float dragThreshold = 5f;
    public bool fingerOnObject = false;
    private Coroutine zoomRoutine;
    private Camera cam;

    private void Awake()
    {
        cam = Camera.main;
    }

    private void OnEnable()
    {
        TouchManager.ZoomStart += StartZoom;
        TouchManager.ZoomEnd += StopZoom;
    }

    private void OnDisable()
    {
        TouchManager.ZoomStart -= StartZoom;
        TouchManager.ZoomEnd -= StopZoom;
    }

    private void FixedUpdate()
    {
        if (TouchManager.Instance.isInteracting) return;

        if (!TouchManager.Instance.isClickedOn)
        {
            isRotating = false;
            hasInitializedTouch = false;
            TouchManager.Instance.IsRotate(false);
            return;
        }

        Vector2 curPos = TouchManager.Instance.curScreenPos;

        if (!hasInitializedTouch)
        {
            previousX = curPos.x;
            previousZ = curPos.y;
            hasInitializedTouch = true;
            return;
        }

        float moveDist = Vector2.Distance(new Vector2(previousX, previousZ), curPos);
        if (moveDist > dragThreshold)
        {
            isRotating = true;
            TouchManager.Instance.IsRotate(true);
        }

        if (isRotating)
        {
            RotateObject(curPos);
        }

        previousX = curPos.x;
        previousZ = curPos.y;
    }

    private void RotateObject(Vector2 touchPos)
    {
        float deltaX = -(touchPos.y - previousZ) * rotationRate;
        float deltaY = -(touchPos.x - previousX) * rotationRate;

        if (!yRotation) deltaX = 0;
        if (!xRotation) deltaY = 0;
        if (invertX) deltaY *= -1;
        if (invertY) deltaX *= -1;

        transform.Rotate(deltaX, 0, deltaY, Space.World);
    }

    private void StartZoom()
    {
        zoomRoutine = StartCoroutine(ZoomRoutine());
    }

    private void StopZoom()
    {
        if (zoomRoutine != null)
            StopCoroutine(zoomRoutine);
    }

    IEnumerator ZoomRoutine()
    {
        float previousDistance = 0f, distance = 0f;

        while (true)
        {
            distance = Vector2.Distance(TouchManager.Instance.curScreenPos, TouchManager.Instance.curSecondaryPos);

            Vector3 targetPos = transform.position;

            if (distance > previousDistance)
                targetPos.y += 1f;
            else if (distance < previousDistance)
                targetPos.y -= 1f;

            targetPos.y = Mathf.Clamp(targetPos.y, minZoom, maxZoom);
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * zoomSpeed);

            previousDistance = distance;
            yield return null;
        }
    }
}
