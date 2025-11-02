using UnityEngine;

public class ChiselTool : ToolBase
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

        if (isDragging && lookTarget != null && tipPoint != null)
        {
            RotateTowardsLookTarget();
        }

        if (isDragging && tipPoint != null)
        {
            HandleSurfaceDetection();
        }
    }

    private void RotateTowardsLookTarget()
    {
        Vector3 dir = (lookTarget.position - tipPoint.position).normalized;
        Quaternion targetRot = Quaternion.FromToRotation(tipPoint.forward, dir) * targetObject.rotation;
        targetObject.rotation = Quaternion.Slerp(targetObject.rotation, targetRot, Time.deltaTime * rotateSmoothness);
    }

    private void HandleSurfaceDetection()
    {
        Ray ray = new Ray(tipPoint.position, tipPoint.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, gizmosRange))
        {
            if (!hit.collider.CompareTag("Dirts")) return;

            Debug.Log($"Detected Dirts: {hit.collider.name}");
            Vector3 targetPos = hit.point - tipPoint.forward * 0.02f;
            targetObject.position = Vector3.Lerp(targetObject.position, targetPos, Time.deltaTime * movementSmoothness);

            smoothedNormal = smoothedNormal == Vector3.zero ? hit.normal : Vector3.Lerp(smoothedNormal, hit.normal, Time.deltaTime * normalDamping);

            Quaternion surfaceRot = Quaternion.LookRotation(-smoothedNormal, Vector3.up);
            targetObject.rotation = Quaternion.RotateTowards(targetObject.rotation, surfaceRot, rotateSmoothness * Time.deltaTime);
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

        Vector3 targetPos = GetWorldPoint(screenPos, dragDistance) + dragOffset;
        MoveTarget(targetPos, movementSmoothness);
    }

    public override void OnToolDragEnd(Vector2 screenPos)
    {
        if (isReturning || !isDragging || targetObject == null) return;

        isDragging = false;
        isReturning = true;
        TouchManager.Instance.TouchUsed(false);
    }

    public override void OnToolTap(Vector2 screenPos)
    {
        if (isReturning) return;
        Debug.Log("Brush clicked");
    }

    private void ResetDrag()
    {
        isReturning = false;
        isDragging = false;
        targetObject = null;
    }

    private void OnDrawGizmos()
    {
        if (tipPoint == null) return;

        Vector3 direction = Vector3.forward;
        // Vector3 direction = mainCamera.transform.forward;
        Vector3 endPoint = tipPoint.position + direction * gizmosRange;

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(tipPoint.position, endPoint);
        Gizmos.DrawSphere(endPoint, 0.05f);
    }
}
