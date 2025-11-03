using UnityEngine;

public class BrushTool : ToolBase
{
    private Vector3 smoothedNormal = Vector3.zero;

    private void Update()
    {
        if (!TouchManager.Instance.isInteracting)
        {
            Ray ray = mainCamera.ScreenPointToRay(TouchManager.Instance.curScreenPos);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.transform == this.transform)
                {
                    Debug.Log("Brush selected!");
                    TouchManager.Instance.TouchUsed(true);
                    OnToolDragStart(TouchManager.Instance.curScreenPos);
                }
            }
        }
        else if (isDragging)
        {
            OnToolDragging(TouchManager.Instance.curScreenPos);
        }
        if (!TouchManager.Instance.isInteracting && isDragging)
        {
            Debug.Log("Brush drag end!");
            OnToolDragEnd(TouchManager.Instance.curScreenPos);
        }

        if (isReturning)
        {
            MoveTarget(initialPosition, returnSmoothness, initialRotation);

            bool posClose = Vector3.Distance(targetObject.position, initialPosition) < 0.01f;
            bool rotClose = Quaternion.Angle(targetObject.rotation, initialRotation) < 1f;

            if (posClose && rotClose)
            {
                targetObject.position = initialPosition;
                targetObject.rotation = initialRotation;
                ResetDrag();
            }

            return;
        }

        // if (isDragging && lookTarget != null && tipPoint != null)
        // {
        //     RotateTowardsLookTarget();
        // }

        if (isDragging && tipPoint != null)
        {
            HandleScreenSurfaceDetection();
        }
    }

    private void RotateTowardsLookTarget()
    {
        Vector3 dir = (lookTarget.position - tipPoint.position).normalized;
        Quaternion targetRot = Quaternion.FromToRotation(tipPoint.forward, dir) * targetObject.rotation;
        targetObject.rotation = Quaternion.Slerp(targetObject.rotation, targetRot, Time.deltaTime * rotateSmoothness);
    }

    private void HandleScreenSurfaceDetection()
    {
        if (tipPoint == null || mainCamera == null) return;
        Vector3 screenPos = mainCamera.WorldToScreenPoint(tipPoint.position);
        screenPos.z += gizmosRange;
        Vector3 worldEnd = mainCamera.ScreenToWorldPoint(screenPos);

        Vector3 rayDir = (worldEnd - tipPoint.position).normalized;
        Ray ray = new Ray(tipPoint.position, rayDir);


        if (Physics.Raycast(ray, out RaycastHit hit, gizmosRange))
        {
            if (!hit.collider.CompareTag("Dirts")) { isSurfaceDeteced = false; return; }

            isSurfaceDeteced = true;
            Debug.Log($"[SCREEN] Detected Dirts: {hit.collider.name}");
            Vector3 targetPos = (hit.point - tipPoint.position).normalized;

            targetObject.position = Vector3.Lerp(targetObject.position, hit.point, Time.deltaTime * movementSmoothness);
            Quaternion targetRot = Quaternion.FromToRotation(tipPoint.forward, targetPos) * targetObject.rotation;
            targetObject.rotation = Quaternion.Slerp(targetObject.rotation, targetRot, rotateSmoothness * Time.deltaTime);
        }
    }

    public override void OnToolDragStart(Vector2 screenPos)
    {
        if (isReturning) return;

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
        if (isReturning || !isDragging || targetObject == null) return;
        Vector3 targetPosTouch = GetWorldPoint(screenPos, dragDistance) + dragOffset;
        MoveTarget(targetPosTouch, movementSmoothness);
    }

    public override void OnToolDragEnd(Vector2 screenPos)
    {
        if (isReturning || !isDragging || targetObject == null) return;

        isDragging = false;
        isReturning = true;
        TouchManager.Instance.TouchUsed(false);
    }

    private void OnDrawGizmos()
    {
        if (tipPoint == null || mainCamera == null) return;
        Vector3 screenPos = mainCamera.WorldToScreenPoint(tipPoint.position);
        screenPos.z += gizmosRange;
        Vector3 worldEnd = mainCamera.ScreenToWorldPoint(screenPos);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(tipPoint.position, worldEnd);
        Gizmos.DrawSphere(worldEnd, 0.05f);
    }
}
