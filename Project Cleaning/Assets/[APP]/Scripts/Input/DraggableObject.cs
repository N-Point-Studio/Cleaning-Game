using UnityEngine;

public class DraggableObject : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform GrabPosition;
    [SerializeField] private SurfaceDetection surface = null;
    private Vector3 initialPosition;
    private float grabOffset;
    public bool isDragging = false;
    public bool isReturning = false;
    public bool isTapping = false;
    private Collider Collider;


    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        initialPosition = transform.position;
        grabOffset = Vector3.Distance(transform.position, GrabPosition.position);
        Collider = GetComponent<Collider>();
    }

    private void Update()
    {

        if (TouchManager.Instance == null) return;

        if (!TouchManager.Instance.isInteracting && !TouchManager.Instance.isRotating)
        {
            Ray ray = mainCamera.ScreenPointToRay(TouchManager.Instance.curScreenPos);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.transform == transform)
                {
                    isDragging = true;
                    isTapping = true;
                    isReturning = false;
                    TouchManager.Instance.TouchUsed(true);
                    if (surface != null) surface.isUsed = true;
                }
            }
        }
        else if (isDragging)
        {
            MoveOnScreen(TouchManager.Instance.curScreenPos, 10f);
        }

        if (!TouchManager.Instance.isInteracting && isDragging)
        {
            isDragging = false;
            isTapping = false;
            isReturning = true;
            Collider.enabled = false;
            TouchManager.Instance.TouchUsed(false);
        }

        if (isReturning)
        {
            MoveBack(10);
            if (Vector3.Distance(transform.position, initialPosition) < 0.01f)
            {
                isReturning = false;
                isDragging = false;
                isTapping = false;
                Collider.enabled = true;
                // surface?.isUsed = false;
                if (surface != null) surface.isUsed = false;
            }
            return;
        }
    }

    private void MoveOnScreen(Vector2 screenPos, float speed)
    {
        if (TouchManager.Instance.curScreenPos != Vector3.zero)
        {
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, mainCamera.WorldToScreenPoint(initialPosition).z));
            Vector3 target = new Vector3(worldPos.x, initialPosition.y, worldPos.z + grabOffset);
            transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * speed);
        }
        else
        {
            MoveBack(10);
        }
    }

    private void MoveBack(float speed)
    {
        transform.position = Vector3.Lerp(transform.position, initialPosition, Time.deltaTime * speed);
    }
}
