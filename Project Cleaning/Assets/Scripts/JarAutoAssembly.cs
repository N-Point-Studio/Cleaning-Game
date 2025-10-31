using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
#if UNITY_EDITOR
using UnityEditor;
#endif

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

    [Header("Inspection Assembly Settings")]
    [Tooltip("Enable snap-to-position during inspection mode")]
    public bool enableInspectionSnap = true;
    [Tooltip("Snap distance for inspection mode assembly")]
    public float inspectionSnapDistance = 0.8f;
    [Tooltip("Visual feedback when near snap zone during inspection")]
    public bool showInspectionSnapFeedback = true;

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
    private static bool jarFullyAssembled = false;
    private static bool staticsInitialized = false;

    public static bool IsJarFullyAssembled => jarFullyAssembled;

    private Collider pieceCollider;
    private Transform originalParent;
    private bool initialAssembledColliderState;
    private Vector3 correctLocalPosition;
    private Quaternion correctLocalRotation;
    private bool localOffsetsComputed = false;
    private InspectableJar inspectableComponent;
    private bool initialCanBeInspected = true;
    private bool initialScriptEnabled;

    private static void EnsureStaticState()
    {
        if (staticsInitialized)
            return;

        allPieces = new List<JarAutoAssembly>();
        jarFullyAssembled = false;
        staticsInitialized = true;
    }

    private static int GetAssembledPieceCount()
    {
        CleanupNullPieces();

        int count = 0;

        for (int i = 0; i < allPieces.Count; i++)
        {
            JarAutoAssembly piece = allPieces[i];
            if (piece != null && piece.isAssembled)
            {
                count++;
            }
        }

        return count;
    }

    private static void CleanupNullPieces()
    {
        for (int i = allPieces.Count - 1; i >= 0; i--)
        {
            if (allPieces[i] == null)
            {
                allPieces.RemoveAt(i);
            }
        }
    }

    void Awake()
    {
        EnsureStaticState();
    }

    void Start()
    {
        originalPosition = transform.position;
        targetPosition = transform.position;
        mainCamera = Camera.main;
        pieceCollider = GetComponent<Collider>();
        originalParent = transform.parent;
        inspectableComponent = GetComponent<InspectableJar>();
        initialScriptEnabled = enabled;
        if (inspectableComponent != null)
        {
            initialCanBeInspected = inspectableComponent.canBeInspected;
        }

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

        ComputeLocalAssemblyOffsets();
    }

    void OnDestroy()
    {
        allPieces.Remove(this);
    }

    void OnMouseDown()
    {
        if (jarFullyAssembled && assembledJarRoot != null)
        {
            TryInspectAssembledJar();
            return;
        }

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

        // NEW: Check for inspection mode assembly if we were being dragged during inspection
        if (!isDragging && !isHoldingForDrag)
        {
            TryInspectionAssembly();
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
        ObjectCloseUpManager closeUpManager = FindObjectOfType<ObjectCloseUpManager>();
        if (closeUpManager != null && !jarFullyAssembled)
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

        Debug.Log($"🎯 Assembled jar clicked - enabling rotation in place");

        // Make sure the collider is enabled for proper raycast detection
        if (assembledJarCollider != null)
        {
            assembledJarCollider.enabled = true;
        }

        // Get the ObjectCloseUpManager to manually set up rotation-only mode
        ObjectCloseUpManager closeUpManager = FindObjectOfType<ObjectCloseUpManager>();
        if (closeUpManager != null)
        {
            // Manually set the assembled jar as current object without animation
            closeUpManager.SetCurrentObject(assembledJarRoot);
            Debug.Log($"✅ Set assembled jar as current object for rotation (no movement)");
        }

        // Start rotation immediately without any position changes
        var smoothRotator = FindObjectOfType<SmoothObjectRotator>();
        if (smoothRotator != null)
        {
            // Update rotator to use current position (no movement)
            smoothRotator.UpdateFixedPosition(assembledJarRoot.position);
            smoothRotator.StartRotating(assembledJarRoot);
            Debug.Log($"🔄 Started rotation for assembled jar at current position");
        }

        Debug.Log($"✅ Assembled jar ready for rotation - no position changes");
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
        Vector3 currentPos = transform.position;

        Vector3 currentPlanar = new Vector3(currentPos.x, 0f, currentPos.z);
        Vector3 targetPlanar = new Vector3(correctPos.x, 0f, correctPos.z);

        float planarDistance = Vector3.Distance(currentPlanar, targetPlanar);
        float verticalOffset = Mathf.Abs(currentPos.y - correctPos.y);

        Debug.Log($"{pieceType} planar distance: {planarDistance:F2}, vertical offset: {verticalOffset:F2}");

        // Hide any outline that might be showing and resume rotation
        HideInspectedObjectOutline();
        ResumeInspectionRotation();

        if (planarDistance < snapDistance)
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
    /// Try to assemble pieces during inspection mode
    /// </summary>
    void TryInspectionAssembly()
    {
        // Only attempt inspection assembly if we're in inspection mode and feature is enabled
        if (!enableInspectionSnap || isAssembled) return;

        ObjectCloseUpManager closeUpManager = FindObjectOfType<ObjectCloseUpManager>();
        if (closeUpManager == null || !closeUpManager.HasObjectInCloseUp) return;

        // Check if there's another jar piece currently being inspected that we can assemble to
        Transform inspectedObject = closeUpManager.CurrentCloseUpObject;
        if (inspectedObject == null || inspectedObject == transform) return;

        JarAutoAssembly inspectedPiece = inspectedObject.GetComponent<JarAutoAssembly>();
        if (inspectedPiece == null || !inspectedPiece.isAssembled) return;

        // Calculate the correct position for this piece relative to the inspected piece
        Vector3 correctInspectionPos = GetCorrectInspectionPosition(inspectedPiece);
        float distance = Vector3.Distance(transform.position, correctInspectionPos);

        Debug.Log($"Inspection assembly check: {pieceType} -> {inspectedPiece.pieceType}, distance: {distance:F2}, snap threshold: {inspectionSnapDistance}");

        if (distance <= inspectionSnapDistance)
        {
            AssembleToInspectionPosition(inspectedPiece, correctInspectionPos);
        }
        else
        {
            Debug.Log($"Inspection assembly: {pieceType} too far from {inspectedPiece.pieceType} (distance: {distance:F2})");
        }
    }

    /// <summary>
    /// Calculate where this piece should be positioned relative to an inspected piece
    /// FIXED: Use consistent shared inspection offset for all pieces
    /// </summary>
    Vector3 GetCorrectInspectionPosition(JarAutoAssembly relativeToPiece)
    {
        // CRITICAL FIX: Get the SHARED inspection offset from the first assembled piece
        // This ensures ALL pieces use the same inspection offset, preventing position scatter
        Vector3 sharedInspectionOffset = GetSharedInspectionOffset();

        // Get our base assembly position (without any inspection offsets)
        Vector3 thisBasePos = GetBaseCorrectWorldPosition();

        // Apply the shared inspection offset consistently to all pieces
        Vector3 correctInspectionPos = thisBasePos + sharedInspectionOffset;

        Debug.Log($"🔧 FIXED Inspection position calculation for {pieceType}:");
        Debug.Log($"    This base pos: {thisBasePos}");
        Debug.Log($"    Shared inspection offset: {sharedInspectionOffset}");
        Debug.Log($"    Final inspection pos: {correctInspectionPos}");

        return correctInspectionPos;
    }

    /// <summary>
    /// Get the shared inspection offset used by all assembled pieces
    /// This ensures consistent positioning across all pieces during inspection assembly
    /// </summary>
    Vector3 GetSharedInspectionOffset()
    {
        // Find the first assembled piece that has a cached inspection offset
        foreach (JarAutoAssembly piece in allPieces)
        {
            if (piece != null && piece.isAssembled && piece.inspectionOffset != Vector3.zero)
            {
                Debug.Log($"📏 Using shared inspection offset from {piece.pieceType}: {piece.inspectionOffset}");
                return piece.inspectionOffset;
            }
        }

        // If no piece has a cached offset yet, use the current inspection mode offset
        ObjectCloseUpManager closeUpManager = FindObjectOfType<ObjectCloseUpManager>();
        if (closeUpManager != null && closeUpManager.HasObjectInCloseUp)
        {
            Transform inspected = closeUpManager.CurrentCloseUpObject;
            if (inspected != null)
            {
                JarAutoAssembly inspectedPiece = inspected.GetComponent<JarAutoAssembly>();
                if (inspectedPiece != null && inspectedPiece.isAssembled)
                {
                    Vector3 inspectedBasePos = inspectedPiece.GetBaseCorrectWorldPosition();
                    Vector3 currentInspectionOffset = inspected.position - inspectedBasePos;
                    Debug.Log($"📏 Calculating shared inspection offset from currently inspected {inspectedPiece.pieceType}: {currentInspectionOffset}");
                    return currentInspectionOffset;
                }
            }
        }

        // Fallback to zero offset
        Debug.Log("📏 No shared inspection offset found, using zero offset");
        return Vector3.zero;
    }

    /// <summary>
    /// Assemble this piece to the correct position relative to an inspected piece
    /// FIXED: Last piece adapts to existing pieces' position and scale
    /// </summary>
    void AssembleToInspectionPosition(JarAutoAssembly relativeToPiece, Vector3 targetPosition)
    {
        Debug.Log($"🔧 Assembling {pieceType} to inspection position relative to {relativeToPiece.pieceType}!");

        // CRITICAL: Set this piece as assembled BEFORE animation
        // This prevents GetCorrectPosition from applying wrong offsets during animation
        isAssembled = true;

        // NEW LOGIC: Adapt this piece to match existing assembled pieces' positioning
        Vector3 adaptedTargetPosition = AdaptToExistingPiecesPosition(targetPosition);

        // Cache the adapted inspection offset for this piece
        Vector3 basePos = GetBaseCorrectWorldPosition();
        Vector3 adaptedInspectionOffset = adaptedTargetPosition - basePos;
        CacheInspectionOffsets(adaptedInspectionOffset, Quaternion.identity);

        Debug.Log($"📦 Adapted target position for {pieceType}: {adaptedTargetPosition}");
        Debug.Log($"📦 Cached adapted inspection offset: {adaptedInspectionOffset}");

        // Animate to the adapted target position (following existing pieces)
        transform.DOMove(adaptedTargetPosition, assemblyDuration).SetEase(Ease.OutBack);

        // Calculate and apply correct rotation
        Quaternion correctRot = GetCorrectRotation();
        transform.DORotateQuaternion(correctRot, assemblyDuration).SetEase(Ease.OutBack);

        // Add assembly effect
        transform.DOPunchScale(Vector3.one * 0.1f, 0.3f, 5);

        // Hide any outline that was showing
        HideInspectedObjectOutline();

        // Check if jar is now complete
        CheckJarCompletion();

        Debug.Log($"✅ {pieceType} assembled and adapted to existing pieces!");
    }

    /// <summary>
    /// Adapt this piece's position to match existing assembled pieces' positioning and scale
    /// The last piece follows the position and scale established by pieces 1 & 2
    /// </summary>
    Vector3 AdaptToExistingPiecesPosition(Vector3 originalTargetPosition)
    {
        // Find the first assembled piece to use as reference
        JarAutoAssembly referencePiece = null;
        foreach (JarAutoAssembly piece in allPieces)
        {
            if (piece != null && piece != this && piece.isAssembled)
            {
                referencePiece = piece;
                break;
            }
        }

        if (referencePiece == null)
        {
            Debug.Log($"🔄 No reference piece found, using original position: {originalTargetPosition}");
            return originalTargetPosition;
        }

        // Calculate the inspection offset that the reference piece is using
        Vector3 referenceBasePos = referencePiece.GetBaseCorrectWorldPosition();
        Vector3 referenceCurrentPos = referencePiece.transform.position;
        Vector3 referenceInspectionOffset = referenceCurrentPos - referenceBasePos;

        // Apply the SAME inspection offset to this piece's base position
        Vector3 myBasePos = GetBaseCorrectWorldPosition();
        Vector3 adaptedPosition = myBasePos + referenceInspectionOffset;

        Debug.Log($"🔄 Adapting {pieceType} to follow {referencePiece.pieceType}:");
        Debug.Log($"    - Reference base: {referenceBasePos}");
        Debug.Log($"    - Reference current: {referenceCurrentPos}");
        Debug.Log($"    - Reference offset: {referenceInspectionOffset}");
        Debug.Log($"    - My base: {myBasePos}");
        Debug.Log($"    - My adapted position: {adaptedPosition}");
        Debug.Log($"    - Original target was: {originalTargetPosition}");

        return adaptedPosition;
    }

    /// <summary>
    /// DEPRECATED: Old alignment method - now pieces follow existing instead of leading
    /// </summary>
    void AlignAllPiecesToNewAssembly(Vector3 newInspectionOffset)
    {
        // This method is no longer used with the new "last piece follows" logic
        Debug.Log($"⚠️ AlignAllPiecesToNewAssembly called but deprecated - using follow logic instead");
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
        if (IsJarFullyAssembled)
            return;

        ObjectCloseUpManager closeUpManager = FindObjectOfType<ObjectCloseUpManager>();
        if (closeUpManager != null && closeUpManager.HasObjectInCloseUp)
        {
            Debug.Log($"Resuming rotation on inspected object after {pieceType} drag ended");
            closeUpManager.ResumeRotation();
        }
    }

    /// <summary>
    /// Check proximity to inspected objects and show/hide outline accordingly
    /// Also provides visual feedback for inspection snap zones
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

        // Check for inspection snap zone if enabled
        bool inSnapZone = false;
        if (enableInspectionSnap && showInspectionSnapFeedback && !isAssembled)
        {
            JarAutoAssembly inspectedPiece = inspectedObject.GetComponent<JarAutoAssembly>();
            if (inspectedPiece != null && inspectedPiece.isAssembled)
            {
                Vector3 correctInspectionPos = GetCorrectInspectionPosition(inspectedPiece);
                float snapDistance = Vector3.Distance(transform.position, correctInspectionPos);

                if (snapDistance <= inspectionSnapDistance)
                {
                    inSnapZone = true;
                    Debug.Log($"🎯 {pieceType} in inspection SNAP ZONE for {inspectedPiece.pieceType} (snap distance: {snapDistance:F2})");
                }
            }
        }

        // Show/hide outline based on proximity or snap zone
        bool shouldShowOutline = (distance <= inspectableJar.outlineActivationDistance) || inSnapZone;

        if (shouldShowOutline)
        {
            if (!inspectableJar.IsShowingOutline())
            {
                inspectableJar.ShowOutline();
                if (inSnapZone)
                {
                    Debug.Log($"🎯 {pieceType} near SNAP ZONE - showing outline");
                }
                else
                {
                    Debug.Log($"🎯 {pieceType} near inspected object - showing outline (distance: {distance:F1})");
                }
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
        Vector3 target = GetBaseCorrectWorldPosition();

        ObjectCloseUpManager closeUpManager = FindObjectOfType<ObjectCloseUpManager>();
        if (closeUpManager != null && closeUpManager.HasObjectInCloseUp)
        {
            Transform inspected = closeUpManager.CurrentCloseUpObject;
            if (inspected != null && inspected != assembledJarRoot)
            {
                JarAutoAssembly inspectedPiece = inspected.GetComponent<JarAutoAssembly>();
                if (inspectedPiece != null)
                {
                    if (inspectedPiece == this)
                    {
                        // This piece is the one being inspected - use its cached inspection offset
                        target += inspectionOffset;
                        Debug.Log($"🎯 Using cached inspection offset for {pieceType}: {inspectionOffset}");
                    }
                    else if (inspectedPiece.isAssembled)
                    {
                        // FIXED: Use shared inspection offset for consistency
                        // This ensures all pieces maintain the same relative positioning
                        Vector3 sharedOffset = GetSharedInspectionOffset();
                        target += sharedOffset;
                        Debug.Log($"🔧 FIXED: Using shared inspection offset for {pieceType}: {sharedOffset}");
                    }
                    else
                    {
                        // Fallback to the original logic for unassembled pieces
                        target += inspectedPiece.GetAssemblyOffset();
                        Debug.Log($"📎 Using assembly offset for {pieceType}: {inspectedPiece.GetAssemblyOffset()}");
                    }
                }
            }
        }

        return target;
    }

    Quaternion GetCorrectRotation()
    {
        Quaternion target = GetBaseCorrectWorldRotation();

        ObjectCloseUpManager closeUpManager = FindObjectOfType<ObjectCloseUpManager>();
        if (closeUpManager != null && closeUpManager.HasObjectInCloseUp)
        {
            Transform inspected = closeUpManager.CurrentCloseUpObject;
            if (inspected != null && inspected != assembledJarRoot)
            {
                JarAutoAssembly inspectedPiece = inspected.GetComponent<JarAutoAssembly>();
                if (inspectedPiece != null)
                {
                    if (inspectedPiece == this)
                        target = inspectionRotationOffset * target;
                    else
                        target = inspectedPiece.GetAssemblyRotationOffset() * target;
                }
            }
        }

        return target;
    }

    internal Vector3 GetBaseCorrectWorldPosition()
    {
        if (!localOffsetsComputed)
        {
            ComputeLocalAssemblyOffsets();
        }

        if (assembledJarRoot != null)
        {
            return assembledJarRoot.TransformPoint(correctLocalPosition);
        }

        return correctLocalPosition;
    }

    internal Quaternion GetBaseCorrectWorldRotation()
    {
        if (!localOffsetsComputed)
        {
            ComputeLocalAssemblyOffsets();
        }

        if (assembledJarRoot != null)
        {
            return assembledJarRoot.rotation * correctLocalRotation;
        }

        return correctLocalRotation;
    }

    private Vector3 inspectionOffset = Vector3.zero;
    private Quaternion inspectionRotationOffset = Quaternion.identity;

    public void CacheInspectionOffsets(Vector3 offset, Quaternion rotationOffset)
    {
        inspectionOffset = offset;
        inspectionRotationOffset = rotationOffset;
    }

    private Vector3 GetAssemblyOffset()
    {
        return inspectionOffset;
    }

    private Quaternion GetAssemblyRotationOffset()
    {
        return inspectionRotationOffset == Quaternion.identity ? Quaternion.identity : inspectionRotationOffset;
    }

    Vector3 GetConfiguredWorldPosition()
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

    void ComputeLocalAssemblyOffsets()
    {
        Vector3 worldTarget = GetConfiguredWorldPosition();
        Quaternion worldRotation = Quaternion.Euler(correctRotation);

        if (assembledJarRoot != null)
        {
            correctLocalPosition = assembledJarRoot.InverseTransformPoint(worldTarget);
            correctLocalRotation = Quaternion.Inverse(assembledJarRoot.rotation) * worldRotation;
        }
        else
        {
            correctLocalPosition = worldTarget;
            correctLocalRotation = worldRotation;
        }

        localOffsetsComputed = true;
    }

    void AssembleToCorrectPosition()
    {
        Debug.Log($"Assembling {pieceType} to correct position!");

        isDragging = false;

        // NEW LOGIC: If there are already assembled pieces, adapt to their position
        Vector3 correctPos = GetCorrectPosition();
        Vector3 adaptedPos = AdaptToExistingPiecesPosition(correctPos);

        // Cache the adapted offset
        Vector3 basePos = GetBaseCorrectWorldPosition();
        Vector3 adaptedOffset = adaptedPos - basePos;

        // Set as assembled and cache the adapted offset
        isAssembled = true;
        CacheInspectionOffsets(adaptedOffset, Quaternion.identity);

        // Use MarkAssembled with the adapted position
        transform.DOKill();
        transform.DOMove(adaptedPos, assemblyDuration).SetEase(Ease.OutBack);

        Quaternion correctRot = GetCorrectRotation();
        transform.DORotateQuaternion(correctRot, assemblyDuration).SetEase(Ease.OutBack);
        transform.DOPunchScale(Vector3.one * 0.1f, 0.3f, 5);

        Debug.Log($"📦 {pieceType} assembled to adapted position: {adaptedPos}");

        ObjectCloseUpManager closeUpManager = FindObjectOfType<ObjectCloseUpManager>();
        if (closeUpManager != null && closeUpManager.HasObjectInCloseUp)
        {
            JarAutoAssembly inspectedPiece = closeUpManager.CurrentCloseUpObject.GetComponent<JarAutoAssembly>();
            if (inspectedPiece != null && inspectedPiece != this && !inspectedPiece.isAssembled)
            {
                inspectedPiece.MarkAssembled(false);
            }
        }

        // Check completion
        CheckJarCompletion();
    }

    void CheckJarCompletion()
    {
        int assembledPieces = GetAssembledPieceCount();
        Debug.Log($"Progress: {assembledPieces}/{allPieces.Count} pieces assembled");

        if (jarFullyAssembled || allPieces.Count == 0 || assembledPieces < allPieces.Count)
            return;

        jarFullyAssembled = true;

        HandleAllPiecesOnCompletion();

        StartCoroutine(CelebrationSequence());

        ObjectCloseUpManager closeUpManager = FindObjectOfType<ObjectCloseUpManager>();

        if (assembledJarRoot != null)
        {
            Debug.Log($"🔧 ASSEMBLY COMPLETION: Bringing assembled jar to close-up: {assembledJarRoot.name}");

            // CRITICAL: Clear any individual pieces from close-up first
            // This prevents the individual pieces from staying in close-up after assembly
            if (closeUpManager != null && closeUpManager.HasObjectInCloseUp)
            {
                Transform currentObject = closeUpManager.CurrentCloseUpObject;
                Debug.Log($"🔍 Current object in close-up before clearing: {currentObject?.name ?? "null"}");

                var currentPieceComponent = currentObject?.GetComponent<JarAutoAssembly>();

                if (currentPieceComponent != null && !currentPieceComponent.IsAssembledJarRoot(currentObject))
                {
                    Debug.Log($"❌ CLEARING individual piece {currentObject.name} from close-up before showing assembled jar");
                    closeUpManager.ClearCurrentObject();
                    Debug.Log($"✅ Current object cleared. HasObjectInCloseUp is now: {closeUpManager.HasObjectInCloseUp}");
                }
                else
                {
                    Debug.Log($"⚠️ Current object {currentObject?.name} is either null or already the assembled jar root");
                }
            }
            else
            {
                Debug.Log("ℹ️ No object currently in close-up, proceeding to bring assembled jar");
            }

            // FIXED: Disable normal inspection for assembled jar to prevent position changes
            var parentInspectable = assembledJarRoot.GetComponent<InspectableJar>();
            if (parentInspectable != null)
            {
                parentInspectable.SetInspectable(false);
                Debug.Log($"🔒 DISABLED normal inspection for assembled jar {assembledJarRoot.name} - prevents position changes");
            }
            else
            {
                Debug.LogWarning($"⚠️ No InspectableJar component found on assembled jar root: {assembledJarRoot.name}");
            }

            // CRITICAL FIX: Set up rotation-only mode and manually set as current close-up object
            if (assembledJarCollider != null)
            {
                assembledJarCollider.enabled = true;

                // CRITICAL: Manually set the assembled jar as the current close-up object
                // This enables rotation without calling BringObjectToCloseUp which moves it
                if (closeUpManager != null)
                {
                    // Use reflection to directly set the current object without position animation
                    try
                    {
                        // Try to set it directly - this may require the close-up manager to have a setter
                        var currentObjectProperty = closeUpManager.GetType().GetProperty("CurrentCloseUpObject");
                        var hasObjectProperty = closeUpManager.GetType().GetProperty("HasObjectInCloseUp");

                        if (currentObjectProperty != null && hasObjectProperty != null)
                        {
                            currentObjectProperty.SetValue(closeUpManager, assembledJarRoot);
                            hasObjectProperty.SetValue(closeUpManager, true);
                            Debug.Log($"🎯 MANUALLY set ParentJar as current close-up object without animation");
                        }
                        else
                        {
                            Debug.LogWarning($"⚠️ Could not access ObjectCloseUpManager properties - using alternative approach");
                        }
                    }
                    catch
                    {
                        Debug.LogWarning($"⚠️ Reflection approach failed - ParentJar may still go through normal selection");
                    }
                }

                Debug.Log($"✅ ENABLED ParentJar collider {assembledJarCollider.name} for rotation");
                Debug.Log($"🔒 Set up ROTATION-ONLY mode for ParentJar");
                Debug.Log($"    - ParentJar should now be clickable for rotation");
                Debug.Log($"    - Position changes prevented by manual close-up setup");
            }
            else
            {
                Debug.LogWarning($"⚠️ No assembled jar collider found for {assembledJarRoot.name}");
            }

            // Now bring the assembled jar to close-up
            if (closeUpManager != null)
            {
                Debug.Log($"🎯 BRINGING ASSEMBLED JAR ROOT to close-up for inspection: {assembledJarRoot.name}");

                // Start inspection on assembled jar
                var assembledInspectable = assembledJarRoot.GetComponent<IInspectable>();
                if (assembledInspectable != null)
                {
                    assembledInspectable.OnInspectionStart();
                    Debug.Log($"✅ Started inspection on assembled jar: {assembledJarRoot.name}");
                }

                // FIXED: Don't call BringObjectToCloseUp as it moves the assembled jar and all pieces
                // Just keep pieces in their current inspection positions
                Debug.Log($"🔒 KEEPING ALL PIECES IN CURRENT INSPECTION POSITIONS");
                Debug.Log($"    - Not moving assembled jar to avoid position changes");
                Debug.Log($"    - Pieces remain exactly where they are assembled");
                Debug.Log($"    - Rotation will work on assembled jar in current position");
            }
            else
            {
                Debug.LogError("❌ CloseUpManager is null!");
            }
        }
        else
        {
            Debug.LogError("❌ AssembledJarRoot is null!");
        }
    }

    IEnumerator CelebrationSequence()
    {
        Debug.Log("🎉 Jar restoration complete! 🎉");

        yield return new WaitForSeconds(0.5f);

        // Simple celebration effect - just a gentle scale pulse
        foreach (JarAutoAssembly piece in allPieces)
        {
            if (piece != null && piece.isAssembled)
            {
                piece.transform.DOPunchScale(Vector3.one * 0.1f, 0.5f, 3);
            }
        }

        // Optional: Add particle effects, sound, or UI feedback here
        // No rotation - jar stays in perfect final position
    }

    void HandleJarFullyAssembledState(bool includeTransformAdjustments)
    {
        ObjectCloseUpManager closeUpManager = FindObjectOfType<ObjectCloseUpManager>();
        if (closeUpManager != null && closeUpManager.CurrentCloseUpObject == transform)
        {
            closeUpManager.PauseRotation();
        }

        // FIXED: Don't re-parent pieces during completion to avoid position changes
        // Keep pieces exactly where they are assembled in inspection mode
        if (includeTransformAdjustments && assembledJarRoot != null)
        {
            Debug.Log($"🔒 Skipping re-parenting {pieceType} to keep position fixed");
            // Comment out re-parenting to prevent position drift
            // transform.SetParent(assembledJarRoot, true);
        }

        if (disablePieceCollidersOnCompletion && pieceCollider != null)
        {
            pieceCollider.enabled = false;
        }

        if (assembledJarCollider != null)
        {
            assembledJarCollider.enabled = true;
        }

        // Prevent individual inspection/rotation once the jar is complete
        if (inspectableComponent != null)
        {
            inspectableComponent.SetInspectable(false);
            if (inspectableComponent.IsBeingInspected())
            {
                inspectableComponent.OnInspectionEnd();
            }
        }

        enabled = false;
    }

    void HandleAllPiecesOnCompletion()
    {
        JarAutoAssembly[] snapshot = allPieces.ToArray();

        for (int i = 0; i < snapshot.Length; i++)
        {
            if (snapshot[i] != null)
            {
                snapshot[i].HandleJarFullyAssembledState(false);
            }
        }

        for (int i = 0; i < snapshot.Length; i++)
        {
            if (snapshot[i] != null)
            {
                snapshot[i].HandleJarFullyAssembledState(true);
            }
        }
    }

    void ReturnToOriginalPosition()
    {
        transform.DOMove(originalPosition, 0.5f).SetEase(Ease.OutQuad);
    }

    void MarkAssembled(bool animateToCorrectPosition)
    {
        if (isAssembled)
            return;

        isAssembled = true;

        Vector3 correctPos = GetCorrectPosition();
        Quaternion correctRot = GetCorrectRotation();

        transform.DOKill();

        if (animateToCorrectPosition)
        {
            transform.DOMove(correctPos, assemblyDuration).SetEase(Ease.OutBack);
            transform.DORotateQuaternion(correctRot, assemblyDuration).SetEase(Ease.OutBack);

            transform.DOPunchScale(Vector3.one * 0.1f, 0.3f, 5);
        }
        else
        {
            transform.position = correctPos;
            transform.rotation = correctRot;
        }
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
            piece.localOffsetsComputed = false;
            piece.ComputeLocalAssemblyOffsets();

            if (piece.disablePieceCollidersOnCompletion && piece.pieceCollider != null)
            {
                piece.pieceCollider.enabled = true;
            }

            if (piece.assembledJarCollider != null)
            {
                piece.assembledJarCollider.enabled = piece.initialAssembledColliderState;
            }

            if (piece.inspectableComponent != null)
            {
                piece.inspectableComponent.SetInspectable(piece.initialCanBeInspected);
                if (piece.inspectableComponent.IsBeingInspected())
                {
                    piece.inspectableComponent.OnInspectionEnd();
                }
            }

            piece.enabled = piece.initialScriptEnabled;
        }
        jarFullyAssembled = false;
        Debug.Log("Assembly reset!");
    }

    void OnDrawGizmosSelected()
    {
        // Draw correct position
        Vector3 correctPos = Application.isPlaying ? GetBaseCorrectWorldPosition() : GetConfiguredWorldPosition();
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(correctPos, 0.2f);

        // Draw ground assembly snap distance
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(correctPos, snapDistance);

        // Draw line to correct position
        if (!isAssembled)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, correctPos);
        }

        // Draw inspection snap zones if in play mode and inspection snap is enabled
        if (Application.isPlaying && enableInspectionSnap)
        {
            ObjectCloseUpManager closeUpManager = FindObjectOfType<ObjectCloseUpManager>();
            if (closeUpManager != null && closeUpManager.HasObjectInCloseUp)
            {
                Transform inspectedObject = closeUpManager.CurrentCloseUpObject;
                if (inspectedObject != null && inspectedObject != transform)
                {
                    JarAutoAssembly inspectedPiece = inspectedObject.GetComponent<JarAutoAssembly>();
                    if (inspectedPiece != null && inspectedPiece.isAssembled)
                    {
                        Vector3 inspectionSnapPos = GetCorrectInspectionPosition(inspectedPiece);

                        // Draw inspection snap position
                        Gizmos.color = Color.cyan;
                        Gizmos.DrawWireSphere(inspectionSnapPos, 0.15f);

                        // Draw inspection snap distance
                        Gizmos.color = Color.blue;
                        Gizmos.DrawWireSphere(inspectionSnapPos, inspectionSnapDistance);

                        // Draw line from current position to inspection snap position
                        if (!isAssembled)
                        {
                            Gizmos.color = Color.magenta;
                            Gizmos.DrawLine(transform.position, inspectionSnapPos);
                        }

                        // Label for clarity
#if UNITY_EDITOR
                        Handles.Label(inspectionSnapPos + Vector3.up * 0.5f,
                            $"Inspection Snap\n{pieceType} -> {inspectedPiece.pieceType}");
#endif
                    }
                }
            }
        }
    }

    public bool IsAssembledJarRoot(Transform target)
    {
        return assembledJarRoot != null && target == assembledJarRoot;
    }
}
