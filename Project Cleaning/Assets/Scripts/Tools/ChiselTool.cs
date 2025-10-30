using UnityEngine;

public class ChiselTool : ToolBase
{
    [Header("Settings")]
    [SerializeField] private float dragDistance = 2f;
    [SerializeField] private float moveSmoothness = 10f;
    [SerializeField] private float returnSmoothness = 5f;
    [SerializeField] private LayerMask draggableLayer;

    [Header("Debug")]
    [SerializeField] private Transform tipPoint;
    [SerializeField] private Transform lookTarget; // Transform yang selalu dihadapi
    [SerializeField] private float gizmoSize = 0.05f;
    [SerializeField] private float gizmoLineLength = 0.3f;

    private Transform targetObject;
    private Vector3 dragOffset;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private bool isReturning;

    private void Update()
    {
        if (targetObject == null) return;

        // Smooth return ke posisi & rotasi awal
        if (isReturning)
        {
            MoveTarget(initialPosition, returnSmoothness, initialRotation);

            if (Vector3.Distance(targetObject.position, initialPosition) < 0.01f &&
                Quaternion.Angle(targetObject.rotation, initialRotation) < 1f)
            {
                targetObject.position = initialPosition;
                targetObject.rotation = initialRotation;
                isReturning = false;
                targetObject = null;
            }
        }

        // Selalu arahkan tipPoint ke lookTarget jika tersedia
        if (lookTarget != null && tipPoint != null)
        {
            Vector3 dir = lookTarget.position - tipPoint.position;
            if (dir.sqrMagnitude > 0f)
            {
                // Buat rotasi targetObject sehingga tipPoint.forward mengarah ke lookTarget
                Quaternion lookRot = Quaternion.FromToRotation(tipPoint.forward, dir.normalized) * targetObject.rotation;
                targetObject.rotation = Quaternion.Slerp(targetObject.rotation, lookRot, Time.deltaTime * moveSmoothness);
            }
        }
    }



    public override void OnToolDragStart(Vector2 screenPos)
    {
        if (Physics.Raycast(mainCamera.ScreenPointToRay(screenPos), out RaycastHit hit, Mathf.Infinity, draggableLayer))
        {
            targetObject = hit.transform;
            initialPosition = targetObject.position;
            initialRotation = targetObject.rotation;
            dragOffset = targetObject.position - hit.point;
            isDragging = true;
            isReturning = false;
        }
    }

    public override void OnToolDragging(Vector2 screenPos)
    {
        if (!isDragging || targetObject == null) return;

        Vector3 targetPos = GetWorldPoint(screenPos, dragDistance) + dragOffset;
        MoveTarget(targetPos, moveSmoothness);
    }

    public override void OnToolDragEnd(Vector2 screenPos)
    {
        if (!isDragging || targetObject == null) return;

        isDragging = false;
        isReturning = true;
    }

    private void MoveTarget(Vector3 position, float speed, Quaternion? rotation = null)
    {
        targetObject.position = Vector3.Lerp(targetObject.position, position, Time.deltaTime * speed);
        if (rotation.HasValue)
            targetObject.rotation = Quaternion.Slerp(targetObject.rotation, rotation.Value, Time.deltaTime * speed);
    }

    private void OnDrawGizmos()
    {
        if (tipPoint == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(tipPoint.position, gizmoSize);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(tipPoint.position, tipPoint.position + tipPoint.forward * gizmoLineLength);

        // Garis ke lookTarget
        if (lookTarget != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(tipPoint.position, lookTarget.position);
        }
    }
}
