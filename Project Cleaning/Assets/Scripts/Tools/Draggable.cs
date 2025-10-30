using UnityEngine;

[RequireComponent(typeof(InputReader))]
public class ToolDraggable : MonoBehaviour
{
    [SerializeField] private InputReader input;
    private Camera mainCamera;
    private bool isBeingDragged;
    private Vector3 offset;

    private void Start()
    {
        input = GetComponent<InputReader>();
        mainCamera = Camera.main;

        input.DragStartEvent += OnDragStart;
        input.DragEvent += OnDrag;
        input.DragEndEvent += OnDragEnd;
        input.TapEvent += OnTap;
    }

    private void OnDestroy()
    {
        input.DragStartEvent -= OnDragStart;
        input.DragEvent -= OnDrag;
        input.DragEndEvent -= OnDragEnd;
        input.TapEvent -= OnTap;
    }

    private void OnDragStart(Vector2 screenPos)
    {
        if (RaycastHitThis(screenPos))
        {
            isBeingDragged = true;
            offset = transform.position - GetWorldPosition(screenPos);
        }
    }

    private void OnDrag(Vector2 screenPos)
    {
        if (!isBeingDragged) return;
        transform.position = GetWorldPosition(screenPos) + offset;
    }

    private void OnDragEnd(Vector2 screenPos)
    {
        isBeingDragged = false;
    }

    private void OnTap(Vector2 screenPos)
    {
        if (RaycastHitThis(screenPos))
            Debug.Log($"{name} tapped!");
    }

    private Vector3 GetWorldPosition(Vector2 screenPos)
    {
        float z = mainCamera.WorldToScreenPoint(transform.position).z;
        return mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, z));
    }

    private bool RaycastHitThis(Vector2 screenPos)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        return Physics.Raycast(ray, out RaycastHit hit) && hit.transform == transform;
    }
}
