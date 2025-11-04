using UnityEngine;

public class DraggableObject : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform GrabPosition;
    private Vector3 initialPosition;
    private float grabOffset;
    public bool isDragging = false;
    public bool isReturning = false;


    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        initialPosition = transform.position;
        grabOffset = Vector3.Distance(transform.position, GrabPosition.position);
    }

    private void Update()
    {
        if (TouchManager.Instance == null) return;
        if (!TouchManager.Instance.isInteracting)
        {
            Ray ray = mainCamera.ScreenPointToRay(TouchManager.Instance.curScreenPos);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.transform == transform)
                {
                    isDragging = true;
                    isReturning = false;
                    TouchManager.Instance.TouchUsed(true);
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
            isReturning = true;
            TouchManager.Instance.TouchUsed(false);
        }

        if (isReturning)
        {
            MoveBack(10);
            if (Vector3.Distance(transform.position, initialPosition) < 0.01f)
            {
                isReturning = false;
                isDragging = false;
            }
            return;
        }
    }

    private void MoveOnScreen(Vector2 screenPos, float speed)
    {
        Debug.Log("Stop Move");
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, mainCamera.WorldToScreenPoint(initialPosition).z));
        Vector3 target = new Vector3(worldPos.x, worldPos.y + grabOffset, initialPosition.z);
        transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * speed);
    }

    private void MoveBack(float speed)
    {
        Debug.Log("Stop Back");

        transform.position = Vector3.Lerp(transform.position, initialPosition, Time.deltaTime * speed);
    }
}
