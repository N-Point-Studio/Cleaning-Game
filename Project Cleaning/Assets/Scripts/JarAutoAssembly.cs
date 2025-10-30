using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public enum JarPieceType
{
    Bottom = 0,
    Middle = 1,
    Top = 2
}

public class JarAutoAssembly : MonoBehaviour
{
    [Header("Assembly Settings")]
    public float snapDistance = 1.5f;
    public float assemblyDuration = 0.8f;

    [Header("Correct Positions & Rotations")]
    [SerializeField] private Vector3 bottomPosition = new Vector3(-0.17f, 1.67f, 0.087f);
    [SerializeField] private Vector3 middlePosition = new Vector3(-0.13f, 0.516f, 0.099f);
    [SerializeField] private Vector3 topPosition = new Vector3(0.285f, 1.617f, -0.177f);
    [SerializeField] private Vector3 correctRotation = new Vector3(-89.98f, 0f, 0f);

    [Header("Piece Settings")]
    public JarPieceType pieceType;
    public bool isAssembled = false;

    [Header("Movement")]
    public float dragSpeed = 8f;

    [Header("Input Settings")]
    public float holdTimeForDrag = 2f; // Time to hold before drag starts

    [Header("Assembled Object Settings")]
    [SerializeField] private Transform assembledJarRoot;
    [SerializeField] private Collider assembledJarCollider;
    [SerializeField] private bool disablePieceCollidersOnCompletion = true;

    private bool isDragging = false;
    private bool isHoldingForDrag = false;
    private float holdStartTime = 0f;
    private Camera mainCamera;
    private Vector3 targetPosition;
    private Vector3 originalPosition;
    private static List<JarAutoAssembly> allPieces = new List<JarAutoAssembly>();
    private static int assembledCount = 0;
    private static bool jarFullyAssembled = false;

    private Collider pieceCollider;
    private Transform originalParent;
    private bool initialAssembledColliderState;

    void Start()
    {
        originalPosition = transform.position;
        targetPosition = transform.position;
        mainCamera = Camera.main;
        pieceCollider = GetComponent<Collider>();
        originalParent = transform.parent;

        if (assembledJarRoot != null && assembledJarCollider == null)
        {
            assembledJarCollider = assembledJarRoot.GetComponent<Collider>();
        }

        if (assembledJarCollider != null)
        {
            initialAssembledColliderState = assembledJarCollider.enabled;

            if (!jarFullyAssembled)
            {
                assembledJarCollider.enabled = false;
            }
        }

        // Register this piece
        if (!allPieces.Contains(this))
            allPieces.Add(this);
    }

    void OnDestroy()
    {
        allPieces.Remove(this);
    }

    void OnMouseDown()
    {
        if (isAssembled)
        {
            // Assembled pieces can only be inspected
            Debug.Log($"Assembled piece {pieceType} clicked - triggering inspection");
            TryInspection();
            return;
        }

        // For unassembled pieces: Start hold timer
        isHoldingForDrag = true;
        holdStartTime = Time.time;
        Debug.Log($"Started hold timer for {pieceType} - hold for {holdTimeForDrag}s to drag, release quickly to inspect");

        // IMPORTANT: Prevent auto-inspection during hold timer
        // The inspection system should NOT automatically trigger until we decide
    }

    void OnMouseDrag()
    {
        // Handle dragging in two cases:
        // 1. Already in drag mode (dragging active)
        // 2. Holding for drag (will become active when timer completes)
        if (isAssembled || mainCamera == null) return;
        if (!isDragging && !isHoldingForDrag) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane dragPlane = new Plane(Vector3.up, new Vector3(0, transform.position.y, 0));

        float distance;
        if (dragPlane.Raycast(ray, out distance))
        {
            Vector3 worldPoint = ray.GetPoint(distance);
            targetPosition = worldPoint;

            if (isDragging)
            {
                Debug.Log($"Dragging {pieceType} to position: {worldPoint:F2}");
            }
        }
    }

    void OnMouseUp()
    {
        if (isHoldingForDrag)
        {
            // Check if it was a short click (< holdTimeForDrag) or long hold
            float holdDuration = Time.time - holdStartTime;
            isHoldingForDrag = false;

            if (holdDuration < holdTimeForDrag && !isDragging)
            {
                // Short click = Inspection
                Debug.Log($"Short click detected ({holdDuration:F1}s) - triggering inspection");
                TryInspection();
            }
            else if (isDragging)
            {
                // Long hold = Assembly attempt
                Debug.Log($"Long hold completed ({holdDuration:F1}s) - trying assembly");
                isDragging = false;
                TryAssemble();
            }
        }
        else if (isDragging)
        {
            // Mouse up during drag - try assembly
            isDragging = false;
            TryAssemble();
        }

        // IMPORTANT: If we were dragging and now stopped, resume any paused rotation
        if (!isDragging && !isHoldingForDrag)
        {
            ResumeInspectionRotation();
        }
    }

    /// <summary>
    /// Cancel hold operation (called when mouse exits collider during hold)
    /// </summary>
    void OnMouseExit()
    {
        if (isHoldingForDrag && !isDragging)
        {
            Debug.Log($"Mouse exited {pieceType} during hold - canceling timer");
            isHoldingForDrag = false;

            // Hide outline and resume any paused rotation since we're canceling the drag operation
            HideInspectedObjectOutline();
            ResumeInspectionRotation();
        }
    }

    /// <summary>
    /// Try to bring this piece to close-up for inspection
    /// </summary>
    void TryInspection()
    {
        if (assembledJarRoot != null && allPieces.Count > 0 && assembledCount >= allPieces.Count)
        {
            TryInspectAssembledJar();
            return;
        }

        ObjectCloseUpManager closeUpManager = FindObjectOfType<ObjectCloseUpManager>();
        if (closeUpManager != null)
        {
            Debug.Log($"Jar piece {pieceType} - bringing to close-up for inspection (assembled: {isAssembled})");

            var inspectable = GetComponent<IInspectable>();
            if (inspectable != null)
            {
                inspectable.OnInspectionStart();
                closeUpManager.BringObjectToCloseUp(transform);
            }
        }
    }

    void TryInspectAssembledJar()
    {
        if (assembledJarRoot == null)
        {
            Debug.LogWarning($"Jar is assembled but no assembledJarRoot assigned for {pieceType}");
            return;
        }

        ObjectCloseUpManager closeUpManager = FindObjectOfType<ObjectCloseUpManager>();
        if (closeUpManager == null) return;

        var inspectable = assembledJarRoot.GetComponent<IInspectable>();
        if (inspectable != null)
        {
            if (!inspectable.CanBeInspected())
            {
                Debug.Log($"Assembled jar already being inspected – skipping duplicate call");
            }
            else
            {
                inspectable.OnInspectionStart();
            }
        }

        closeUpManager.BringObjectToCloseUp(assembledJarRoot);
    }

    void Update()
    {
        // Check if hold timer has reached the threshold for starting drag
        if (isHoldingForDrag && !isDragging && !isAssembled)
        {
            float holdDuration = Time.time - holdStartTime;

            // Visual feedback for debugging
            if (holdDuration >= holdTimeForDrag)
            {
                // Time threshold reached - start dragging
                Debug.Log($"🔥 HOLD TIME REACHED ({holdDuration:F1}s) - STARTING DRAG MODE for {pieceType} 🔥");
                StartDragMode();
            }
            else
            {
                // Show countdown every 0.5 seconds
                if (Mathf.FloorToInt(holdDuration * 2) != Mathf.FloorToInt((holdDuration - Time.deltaTime) * 2))
                {
                    float remaining = holdTimeForDrag - holdDuration;
                    Debug.Log($"⏱️ Holding {pieceType} - {remaining:F1}s remaining for drag mode");
                }
            }
        }

        // Handle drag movement
        if (isDragging && !isAssembled)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * dragSpeed);

            // Check proximity to inspected objects for outline feedback
            CheckOutlineProximity();
        }
    }

    /// <summary>
    /// Start drag mode after hold threshold is reached
    /// </summary>
    void StartDragMode()
    {
        isHoldingForDrag = false; // Stop holding timer
        isDragging = true;
        targetPosition = transform.position; // Start from current position

        // IMPORTANT: Pause rotation on ANY object currently in inspection
        // This prevents the inspected object from rotating while we drag another object
        ObjectCloseUpManager closeUpManager = FindObjectOfType<ObjectCloseUpManager>();
        if (closeUpManager != null)
        {
            if (closeUpManager.HasObjectInCloseUp && closeUpManager.CurrentCloseUpObject == transform)
            {
                // If THIS object is in inspection, exit it to start dragging
                Debug.Log($"Exiting inspection mode to start dragging {pieceType}");
                closeUpManager.ExitCloseUp(true);
            }
            else if (closeUpManager.HasObjectInCloseUp)
            {
                // If ANOTHER object is in inspection, pause its rotation but keep it in inspection
                Debug.Log($"Pausing rotation on inspected object while dragging {pieceType}");
                closeUpManager.PauseRotation();
            }
        }

        Debug.Log($"Drag mode started for {pieceType} - now drag mouse to move piece for assembly");
    }

    void TryAssemble()
    {
        Vector3 correctPos = GetCorrectPosition();
        float distanceToCorrectPos = Vector3.Distance(transform.position, correctPos);

        Debug.Log($"{pieceType} distance to correct position: {distanceToCorrectPos:F2}");

        // Hide any outline that might be showing and resume rotation
        HideInspectedObjectOutline();
        ResumeInspectionRotation();

        if (distanceToCorrectPos < snapDistance)
        {
            AssembleToCorrectPosition();
        }
        else
        {
            Debug.Log($"{pieceType} too far - returning to original position");
            ReturnToOriginalPosition();
        }
    }

    /// <summary>
    /// Hide outline on inspected object when drag ends
    /// </summary>
    void HideInspectedObjectOutline()
    {
        ObjectCloseUpManager closeUpManager = FindObjectOfType<ObjectCloseUpManager>();
        if (closeUpManager == null || !closeUpManager.HasObjectInCloseUp)
            return;

        Transform inspectedObject = closeUpManager.CurrentCloseUpObject;
        if (inspectedObject == null || inspectedObject == transform)
            return;

        InspectableJar inspectableJar = inspectedObject.GetComponent<InspectableJar>();
        if (inspectableJar != null && inspectableJar.IsShowingOutline())
        {
            inspectableJar.HideOutline();
            Debug.Log($"Hidden outline on inspected object - {pieceType} drag ended");
        }
    }

    /// <summary>
    /// Resume rotation on any object currently in inspection mode
    /// </summary>
    void ResumeInspectionRotation()
    {
        ObjectCloseUpManager closeUpManager = FindObjectOfType<ObjectCloseUpManager>();
        if (closeUpManager != null && closeUpManager.HasObjectInCloseUp)
        {
            Debug.Log($"Resuming rotation on inspected object after {pieceType} drag ended");
            closeUpManager.ResumeRotation();
        }
    }

    /// <summary>
    /// Check proximity to inspected objects and show/hide outline accordingly
    /// </summary>
    void CheckOutlineProximity()
    {
        ObjectCloseUpManager closeUpManager = FindObjectOfType<ObjectCloseUpManager>();
        if (closeUpManager == null || !closeUpManager.HasObjectInCloseUp)
            return;

        Transform inspectedObject = closeUpManager.CurrentCloseUpObject;
        if (inspectedObject == null || inspectedObject == transform)
            return;

        // Get InspectableJar component from inspected object
        InspectableJar inspectableJar = inspectedObject.GetComponent<InspectableJar>();
        if (inspectableJar == null)
            return;

        // Calculate distance between dragged object and inspected object
        float distance = Vector3.Distance(transform.position, inspectedObject.position);

        // Show/hide outline based on proximity
        if (distance <= inspectableJar.outlineActivationDistance)
        {
            if (!inspectableJar.IsShowingOutline())
            {
                inspectableJar.ShowOutline();
                Debug.Log($"🎯 {pieceType} near inspected object - showing outline (distance: {distance:F1})");
            }
        }
        else
        {
            if (inspectableJar.IsShowingOutline())
            {
                inspectableJar.HideOutline();
                Debug.Log($"📤 {pieceType} moved away from inspected object - hiding outline (distance: {distance:F1})");
            }
        }
    }

    Vector3 GetCorrectPosition()
    {
        switch (pieceType)
        {
            case JarPieceType.Bottom:
                return bottomPosition;
            case JarPieceType.Middle:
                return middlePosition;
            case JarPieceType.Top:
                return topPosition;
            default:
                return transform.position;
        }
    }

    void AssembleToCorrectPosition()
    {
        Debug.Log($"Assembling {pieceType} to correct position!");

        isAssembled = true;
        isDragging = false;
        assembledCount++;

        Vector3 correctPos = GetCorrectPosition();

        // Animate to correct position and rotation
        transform.DOMove(correctPos, assemblyDuration).SetEase(Ease.OutBack);
        transform.DORotate(correctRotation, assemblyDuration).SetEase(Ease.OutBack);

        // Satisfaction effect
        transform.DOPunchScale(Vector3.one * 0.1f, 0.3f, 5);

        // Check completion
        CheckJarCompletion();
    }

    void CheckJarCompletion()
    {
        Debug.Log($"Progress: {assembledCount}/{allPieces.Count} pieces assembled");

        if (!jarFullyAssembled && allPieces.Count > 0 && assembledCount >= allPieces.Count)
        {
            jarFullyAssembled = true;

            foreach (JarAutoAssembly piece in allPieces)
            {
                if (piece != null)
                {
                    piece.HandleJarFullyAssembledState();
                }
            }

            StartCoroutine(CelebrationSequence());
        }
    }

    IEnumerator CelebrationSequence()
    {
        Debug.Log("🎉 Jar restoration complete! 🎉");

        yield return new WaitForSeconds(0.5f);

        // Simple celebration effect - just a gentle scale pulse
        foreach (JarAutoAssembly piece in allPieces)
        {
            if (piece.isAssembled)
            {
                piece.transform.DOPunchScale(Vector3.one * 0.1f, 0.5f, 3);
            }
        }

        // Optional: Add particle effects, sound, or UI feedback here
        // No rotation - jar stays in perfect final position
    }

    void HandleJarFullyAssembledState()
    {
        if (assembledJarRoot != null)
        {
            transform.SetParent(assembledJarRoot, true);
        }

        if (disablePieceCollidersOnCompletion && pieceCollider != null)
        {
            pieceCollider.enabled = false;
        }

        if (assembledJarCollider != null)
        {
            assembledJarCollider.enabled = true;
        }
    }

    void ReturnToOriginalPosition()
    {
        transform.DOMove(originalPosition, 0.5f).SetEase(Ease.OutQuad);
    }

    // Reset function for testing
    [ContextMenu("Reset Assembly")]
    public void ResetAssembly()
    {
        foreach (JarAutoAssembly piece in allPieces)
        {
            piece.isAssembled = false;
            piece.isDragging = false;
            piece.transform.position = piece.originalPosition;
            piece.transform.rotation = Quaternion.identity;
            piece.transform.SetParent(piece.originalParent, true);

            if (piece.disablePieceCollidersOnCompletion && piece.pieceCollider != null)
            {
                piece.pieceCollider.enabled = true;
            }

            if (piece.assembledJarCollider != null)
            {
                piece.assembledJarCollider.enabled = piece.initialAssembledColliderState;
            }
        }
        assembledCount = 0;
        jarFullyAssembled = false;
        Debug.Log("Assembly reset!");
    }

    void OnDrawGizmosSelected()
    {
        // Draw correct position
        Vector3 correctPos = GetCorrectPosition();
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(correctPos, 0.2f);

        // Draw snap distance
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(correctPos, snapDistance);

        // Draw line to correct position
        if (!isAssembled)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, correctPos);
        }
    }
}
