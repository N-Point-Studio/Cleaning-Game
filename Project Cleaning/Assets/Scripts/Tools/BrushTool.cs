using UnityEngine;

public class BrushTool : ToolBase
{
    [Header("Brush References")]
    [SerializeField] private Transform tipPoint;
    [SerializeField] private Transform lookTarget;

    [SerializeField] private float gizmosRange = 10f;
    [SerializeField] private float rotateSmoothness = 10f;

    private void Update()
    {
        if (targetObject == null) return;

        if (isReturning)
        {
            MoveTarget(initialPosition, returnSmoothness, initialRotation);

            bool posClose = Vector3.Distance(targetObject.position, initialPosition) < 0.01f;
            bool rotClose = Quaternion.Angle(targetObject.rotation, initialRotation) < 1f;

            if (posClose && rotClose)
            {
                targetObject.position = initialPosition;
                targetObject.rotation = initialRotation;
                isReturning = false;
                isDragging = false;
                targetObject = null;
            }

            return;
        }

        if (isDragging && lookTarget != null && tipPoint != null)
        {
            Vector3 dir = (lookTarget.position - tipPoint.position).normalized;
            Vector3 localTip = tipPoint.localPosition;
            Quaternion targetRot = Quaternion.FromToRotation(tipPoint.forward, dir) * targetObject.rotation;

            targetObject.rotation = Quaternion.Slerp(
                targetObject.rotation,
                targetRot,
                Time.deltaTime * rotateSmoothness
            );
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

    public override void OnToolTap(Vector2 screenPos)
    {
        Debug.Log("Brush clicked");
    }

    private void OnDrawGizmos()
    {
        if (tipPoint == null) return;
        Vector3 forwardDir = tipPoint.forward * gizmosRange;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(tipPoint.position, tipPoint.position + forwardDir);

        // if (lookTarget != null)
        // {
        //     Gizmos.color = Color.cyan;
        //     Gizmos.DrawLine(tipPoint.position, lookTarget.position);
        //     Gizmos.DrawSphere(lookTarget.position, 0.015f);
        // }
    }
}
