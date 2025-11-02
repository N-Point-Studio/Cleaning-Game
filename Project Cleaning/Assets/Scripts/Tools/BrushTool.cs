using UnityEngine;

public class BrushTool : ToolBase
{
    [Header("Brush References")]
    [SerializeField] private Transform tipPoint;
    [SerializeField] private Transform lookTarget;

    [SerializeField] private float gizmosRange = 10f;
    [SerializeField] private float rotateSmoothness = 20f; // max rotation speed (degrees/sec)
    [SerializeField] private float movementSmoothness = 10f;
    [SerializeField] private float positionThreshold = 0.001f; // minimal perubahan posisi
    [SerializeField] private float normalDamping = 10f; // smoothing faktor normal permukaan

    private Vector3 smoothedNormal = Vector3.zero;

    private void Update()
    {
        if (targetObject == null) return;
        // Debug.Log("Position: " + targetObject.position);

        // --- Handle return to initial position ---
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

        // --- Drag rotation (lookTarget) ---
        if (isDragging && lookTarget != null && tipPoint != null)
        {
            Vector3 dir = (lookTarget.position - tipPoint.position).normalized;
            Quaternion targetRot = Quaternion.FromToRotation(tipPoint.forward, dir) * targetObject.rotation;

            targetObject.rotation = Quaternion.Slerp(
                targetObject.rotation,
                targetRot,
                Time.deltaTime * rotateSmoothness
            );
        }

        // --- Nempel ke permukaan objek bertag "Dirts" ---
        if (isDragging && tipPoint != null)
        {
            Ray ray = new Ray(tipPoint.position, tipPoint.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, gizmosRange))
            {
                if (hit.collider.CompareTag("Dirts"))
                {
                    // Debug: lihat apa yang dideteksi
                    Debug.Log($"Detected Dirts: {hit.collider.name}");

                    // --- Posisikan brush agar nempel di permukaan ---
                    Vector3 targetPos = hit.point - tipPoint.forward * 0.02f;

                    // if (Vector3.Distance(targetObject.position, targetPos) > positionThreshold)
                    // {
                    //     Debug.Log($"Distance " + (Vector3.Distance(targetObject.position, targetPos)) + "with " + (targetPos));
                    //     targetObject.position = Vector3.MoveTowards(
                    //         targetObject.position,
                    //         targetPos,
                    //         movementSmoothness * Time.deltaTime
                    //     );
                    // }

                    // Vector3 targetPos = hit.point - tipPoint.forward * 0.02f; // offset kecil biar ga tembus
                    targetObject.position = Vector3.Lerp(
                        targetObject.position,
                        targetPos,
                        Time.deltaTime * moveSmoothness
                    );

                    // if (Vector3.Distance(targetObject.position, targetPos) > positionThreshold)
                    // {
                    //     // t = 1 → langsung nempel; t kecil → lebih halus
                    //     float t = 0.1f; // bisa disesuaikan
                    //     targetObject.position = Vector3.Lerp(targetObject.position, targetPos, t);
                    // }

                    // --- Smoothing normal permukaan ---
                    if (smoothedNormal == Vector3.zero) smoothedNormal = hit.normal;
                    smoothedNormal = Vector3.Lerp(smoothedNormal, hit.normal, Time.deltaTime * normalDamping);

                    // --- Rotasi brush sesuai normal permukaan ---
                    Quaternion surfaceRot = Quaternion.LookRotation(-smoothedNormal, Vector3.up);
                    targetObject.rotation = Quaternion.RotateTowards(
                        targetObject.rotation,
                        surfaceRot,
                        rotateSmoothness * Time.deltaTime
                    );
                }
            }
        }
    }

    public override void OnToolDragStart(Vector2 screenPos)
    {
        if (isReturning) { return; }
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
        if (isReturning) { return; }
        if (!isDragging || targetObject == null) return;

        Vector3 targetPos = GetWorldPoint(screenPos, dragDistance) + dragOffset;
        MoveTarget(targetPos, moveSmoothness);
    }

    public override void OnToolDragEnd(Vector2 screenPos)
    {
        if (isReturning) { return; }
        if (!isDragging || targetObject == null) return;

        isDragging = false;
        isReturning = true;
    }

    public override void OnToolTap(Vector2 screenPos)
    {
        if (isReturning) { return; }
        Debug.Log("Brush clicked");
    }

    // private void OnDrawGizmos()
    // {
    //     if (tipPoint == null) return;
    //     Vector3 forwardDir = tipPoint.forward * gizmosRange;
    //     Gizmos.color = Color.green;
    //     Gizmos.DrawLine(tipPoint.position, tipPoint.position + forwardDir);
    // }
}
