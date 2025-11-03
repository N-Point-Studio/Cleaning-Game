using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
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

    [Header("Final Assembly Positions & Rotations")]
    [Tooltip("Exact final positions and rotations when all pieces are assembled")]
    [SerializeField] private Vector3 finalBottomPosition = new Vector3(-0.0399f, 4.12828f, -1.46515f);
    [SerializeField] private Vector3 finalMiddlePosition = new Vector3(0f, 2.97428f, -1.45318f);
    [SerializeField] private Vector3 finalTopPosition = new Vector3(0.415f, 4.07528f, -1.72915f);
    [SerializeField] private Vector3 finalRotation = new Vector3(-89.98f, 0f, 0f);

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

    // Original behavior restored - no input variables needed
    private static List<JarAutoAssembly> allPieces = new List<JarAutoAssembly>();
    private static bool jarFullyAssembled = false;
    private static bool staticsInitialized = false;
    private static GameObject currentPartialAssemblyParent = null;

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

        // Input handled by TouchManager - original OnMouseDown behavior preserved
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

        // Clean up temporary parent if this was the last piece being used
        if (allPieces.Count == 0)
        {
            CleanupTemporaryAssemblyParent();
        }
    }

    void OnEnable()
    {
        TouchManager.OnMouseDown += HandleMouseDown;
        TouchManager.OnMouseUp += HandleMouseUp;
        TouchManager.OnMouseDrag += HandleMouseDrag;
    }

    void OnDisable()
    {
        TouchManager.OnMouseDown -= HandleMouseDown;
        TouchManager.OnMouseUp -= HandleMouseUp;
        TouchManager.OnMouseDrag -= HandleMouseDrag;
    }

    bool IsClickedOn(Vector2 screenPos)
    {
        if (mainCamera == null) return false;

        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            return hit.transform == transform;
        }
        return false;
    }

    void HandleMouseDown(Vector2 screenPos)
    {
        // Check if this object was clicked
        if (!IsClickedOn(screenPos)) return;

        // FIXED: When jar is fully assembled, individual pieces should not respond to clicks
        // Only the assembled jar root should handle mouse events
        if (jarFullyAssembled)
        {
            Debug.Log($"🚫 Individual piece {pieceType} clicked but jar is fully assembled - ignoring click");
            Debug.Log($"🎯 Click should be handled by assembled jar root instead");
            return;
        }

        if (isAssembled)
        {
            // Check if this is part of a partial assembly (multiple pieces assembled)
            List<JarAutoAssembly> assembledPieces = GetAssembledPieces();

            if (assembledPieces.Count > 1 && assembledPieces.Count < allPieces.Count)
            {
                Debug.Log($"🔗 Partial assembly piece {pieceType} clicked - will rotate as GROUP with {assembledPieces.Count} pieces");
                Debug.Log($"🔒 Individual piece rotation blocked - only group rotation allowed");
            }
            else if (assembledPieces.Count == allPieces.Count)
            {
                Debug.Log($"🏺 Fully assembled jar piece {pieceType} clicked");
            }
            else
            {
                Debug.Log($"🧩 Single assembled piece {pieceType} clicked");
            }

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

    void HandleMouseDrag(Vector2 screenPos)
    {
        // Only handle drag for this specific object if it's being held or dragged
        if (!isHoldingForDrag && !isDragging) return;

        // Handle dragging in two cases:
        // 1. Already in drag mode (dragging active)
        // 2. Holding for drag (will become active when timer completes)
        if (isAssembled || mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(screenPos);
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

    void HandleMouseUp(Vector2 screenPos)
    {
        // Only handle mouse up for this specific object if it was being held or dragged
        if (!isHoldingForDrag && !isDragging) return;

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
    /// Get all pieces that are currently assembled together
    /// </summary>
    List<JarAutoAssembly> GetAssembledPieces()
    {
        List<JarAutoAssembly> assembled = new List<JarAutoAssembly>();

        foreach (JarAutoAssembly piece in allPieces)
        {
            if (piece != null && piece.isAssembled)
            {
                assembled.Add(piece);
            }
        }

        return assembled;
    }

    /// <summary>
    /// Set up rotation for partially assembled jar pieces as a group
    /// </summary>
    void SetupPartialAssemblyRotation(List<JarAutoAssembly> assembledPieces, ObjectCloseUpManager closeUpManager)
    {
        Debug.Log($"🔗 Setting up partial assembly rotation for {assembledPieces.Count} pieces");

        // Create a temporary parent object for just the assembled pieces
        GameObject tempParent = CreateTemporaryAssemblyParent(assembledPieces);
        if (tempParent != null)
        {
            // Start inspection on all assembled pieces for visual feedback
            foreach (JarAutoAssembly piece in assembledPieces)
            {
                var inspectable = piece.GetComponent<IInspectable>();
                if (inspectable != null)
                {
                    inspectable.OnInspectionStart();
                }
            }

            // Set up rotation-only mode for the partial assembly using temp parent
            closeUpManager.SetCurrentObject(tempParent.transform);

            // Enable rotation at current position
            var smoothRotator = FindObjectOfType<SmoothObjectRotator>();
            if (smoothRotator != null)
            {
                smoothRotator.UpdateFixedPosition(tempParent.transform.position);
                smoothRotator.StartRotating(tempParent.transform);
                Debug.Log($"🔄 Started partial assembly rotation at position: {tempParent.transform.position}");
            }

            Debug.Log($"✅ Partial assembly ({assembledPieces.Count} pieces) ready for group rotation");
            Debug.Log($"🔒 Unassembled pieces will remain independent");
        }
        else
        {
            Debug.LogWarning("⚠️ Failed to create temporary parent - falling back to individual piece inspection");

            // Fallback to normal individual piece inspection
            var inspectable = GetComponent<IInspectable>();
            if (inspectable != null)
            {
                inspectable.OnInspectionStart();
                closeUpManager.BringObjectToCloseUp(transform);
            }
        }
    }

    /// <summary>
    /// Create a temporary parent object that contains only the assembled pieces
    /// This allows partial assembly rotation without affecting unassembled pieces
    /// </summary>
    GameObject CreateTemporaryAssemblyParent(List<JarAutoAssembly> assembledPieces)
    {
        // Clean up any existing temporary parent
        CleanupTemporaryAssemblyParent();

        if (assembledPieces.Count == 0)
            return null;

        // Calculate center position of assembled pieces
        Vector3 centerPosition = Vector3.zero;
        foreach (JarAutoAssembly piece in assembledPieces)
        {
            centerPosition += piece.transform.position;
        }
        centerPosition /= assembledPieces.Count;

        // Create temporary parent object
        currentPartialAssemblyParent = new GameObject("PartialAssemblyParent");
        currentPartialAssemblyParent.transform.position = centerPosition;

        // Store original parents and re-parent assembled pieces to temp parent
        foreach (JarAutoAssembly piece in assembledPieces)
        {
            piece.transform.SetParent(currentPartialAssemblyParent.transform, true);
        }

        Debug.Log($"🏗️ Created temporary parent for {assembledPieces.Count} assembled pieces at {centerPosition}");
        return currentPartialAssemblyParent;
    }

    /// <summary>
    /// Clean up temporary assembly parent and restore original hierarchy
    /// </summary>
    static void CleanupTemporaryAssemblyParent()
    {
        if (currentPartialAssemblyParent != null)
        {
            // Restore all children to their original parents
            for (int i = currentPartialAssemblyParent.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = currentPartialAssemblyParent.transform.GetChild(i);
                JarAutoAssembly piece = child.GetComponent<JarAutoAssembly>();
                if (piece != null)
                {
                    child.SetParent(piece.originalParent, true);
                }
            }

            // Destroy the temporary parent
            if (Application.isPlaying)
                Object.Destroy(currentPartialAssemblyParent);
            else
                Object.DestroyImmediate(currentPartialAssemblyParent);

            currentPartialAssemblyParent = null;
            Debug.Log($"🗑️ Cleaned up temporary assembly parent");
        }
    }

    /// <summary>
    /// Try to bring this piece to close-up for inspection
    /// If this piece is part of a partial assembly, rotate the whole assembled group
    /// </summary>
    void TryInspection()
    {
        ObjectCloseUpManager closeUpManager = FindObjectOfType<ObjectCloseUpManager>();
        if (closeUpManager != null && !jarFullyAssembled)
        {
            // Check if this piece is part of a partial assembly (multiple pieces assembled together)
            List<JarAutoAssembly> assembledPieces = GetAssembledPieces();

            if (assembledPieces.Count > 1 && isAssembled)
            {
                Debug.Log($"🔗 Partial assembly detected! {assembledPieces.Count} pieces assembled together");
                SetupPartialAssemblyRotation(assembledPieces, closeUpManager);
            }
            else
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

        // Clean up temporary parent if it's no longer being used for rotation
        if (currentPartialAssemblyParent != null)
        {
            ObjectCloseUpManager closeUpManager = FindObjectOfType<ObjectCloseUpManager>();
            if (closeUpManager == null || !closeUpManager.HasObjectInCloseUp ||
                closeUpManager.CurrentCloseUpObject != currentPartialAssemblyParent.transform)
            {
                CleanupTemporaryAssemblyParent();
            }
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

        // Adapt this piece to match existing assembled pieces' positioning
        Vector3 adaptedTargetPosition = AdaptToExistingPiecesPosition(targetPosition);

        // Cache the adapted inspection offset for this piece
        Vector3 basePos = GetBaseCorrectWorldPosition();
        Vector3 adaptedInspectionOffset = adaptedTargetPosition - basePos;
        CacheInspectionOffsets(adaptedInspectionOffset, Quaternion.identity);

        Debug.Log($"📦 Adapted target position for {pieceType}: {adaptedTargetPosition}");
        Debug.Log($"📦 Cached adapted inspection offset: {adaptedInspectionOffset}");

        // FIRST: Reset all existing assembled pieces to default rotation (keep their positions)
        ResetAllAssembledPiecesToDefaultRotation();

        // THEN: Animate to the adapted target position (shared inspection position)
        transform.DOMove(adaptedTargetPosition, assemblyDuration).SetEase(Ease.OutBack);

        // FIXED: Get base rotation without any inspection offsets to ensure default snap behavior
        Quaternion correctRot = GetBaseCorrectWorldRotation();
        transform.DORotateQuaternion(correctRot, assemblyDuration).SetEase(Ease.OutBack);

        // Add assembly effect
        transform.DOPunchScale(Vector3.one * 0.1f, 0.3f, 5);

        Debug.Log($"🔄 {pieceType} rotating to base assembly rotation during inspection assembly: {correctRot.eulerAngles}");

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

    /// <summary>
    /// Get the final assembly position for this piece type when all pieces are assembled
    /// </summary>
    Vector3 GetFinalAssemblyPosition()
    {
        switch (pieceType)
        {
            case JarPieceType.Bottom:
                return finalBottomPosition;
            case JarPieceType.Middle:
                return finalMiddlePosition;
            case JarPieceType.Top:
                return finalTopPosition;
            default:
                return transform.position;
        }
    }

    /// <summary>
    /// Enforce the exact final positions and rotations for all pieces when jar is completed
    /// </summary>
    void EnforceFinalAssemblyPositions()
    {
        Debug.Log("🎯 Enforcing final assembly positions and rotations for all pieces");

        // Calculate center position of all final piece positions
        Vector3 centerPosition = Vector3.zero;
        int assembledCount = 0;

        foreach (JarAutoAssembly piece in allPieces)
        {
            if (piece != null && piece.isAssembled)
            {
                Vector3 finalPos = piece.GetFinalAssemblyPosition();
                Quaternion finalRot = Quaternion.Euler(finalRotation);

                Debug.Log($"🔧 Setting {piece.pieceType} to final position: {finalPos}, rotation: {finalRotation}");

                // Kill any ongoing animations
                piece.transform.DOKill();

                // Set exact final position and rotation with smooth animation
                piece.transform.DOMove(finalPos, assemblyDuration * 0.5f).SetEase(Ease.OutQuad);
                piece.transform.DORotateQuaternion(finalRot, assemblyDuration * 0.5f).SetEase(Ease.OutQuad);

                // Add to center calculation
                centerPosition += finalPos;
                assembledCount++;
            }
        }

        // Calculate and set the assembled jar root position to the center of all pieces
        if (assembledCount > 0 && assembledJarRoot != null)
        {
            centerPosition /= assembledCount;
            assembledJarRoot.position = centerPosition;
            Debug.Log($"🎯 Updated assembled jar root position to center of pieces: {centerPosition}");
        }

        Debug.Log("✅ All pieces moved to final assembly positions and rotations");
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

        // Get the shared inspection position where other pieces are assembled
        Vector3 correctPos = GetCorrectPosition();
        Vector3 adaptedPos = AdaptToExistingPiecesPosition(correctPos);

        // Cache the adapted offset for inspection assembly positioning
        Vector3 basePos = GetBaseCorrectWorldPosition();
        Vector3 adaptedOffset = adaptedPos - basePos;

        // Set as assembled and cache the adapted offset
        isAssembled = true;
        CacheInspectionOffsets(adaptedOffset, Quaternion.identity);

        // FIRST: Reset all existing assembled pieces to default rotation (keep their positions)
        ResetAllAssembledPiecesToDefaultRotation();

        // THEN: Assemble this piece to the shared inspection position
        transform.DOKill();
        transform.DOMove(adaptedPos, assemblyDuration).SetEase(Ease.OutBack);

        // FIXED: Get base rotation without any inspection offsets to ensure default snap behavior
        Quaternion correctRot = GetBaseCorrectWorldRotation();
        transform.DORotateQuaternion(correctRot, assemblyDuration).SetEase(Ease.OutBack);
        transform.DOPunchScale(Vector3.one * 0.1f, 0.3f, 5);

        Debug.Log($"🔄 {pieceType} rotating to base assembly rotation: {correctRot.eulerAngles}");

        Debug.Log($"📦 {pieceType} assembled to shared inspection position: {adaptedPos}");

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

    /// <summary>
    /// Reset all currently assembled pieces to their default rotation only
    /// Keeps pieces in their inspection assembly positions but resets rotation to default
    /// </summary>
    void ResetAllAssembledPiecesToDefaultRotation()
    {
        Debug.Log($"🔄 Resetting all assembled pieces to default rotation (keeping inspection positions) before {pieceType} assembly");

        foreach (JarAutoAssembly piece in allPieces)
        {
            if (piece != null && piece != this && piece.isAssembled)
            {
                // Get the base assembly rotation (but keep current position for inspection mode)
                Quaternion defaultRot = piece.GetBaseCorrectWorldRotation();

                Debug.Log($"    - Resetting {piece.pieceType} rotation:");
                Debug.Log($"        Position: {piece.transform.position} (keeping current)");
                Debug.Log($"        Rotation: {piece.transform.rotation.eulerAngles} -> {defaultRot.eulerAngles}");

                // Animate to default rotation ONLY (keep current position)
                piece.transform.DOKill();
                piece.transform.DORotateQuaternion(defaultRot, assemblyDuration).SetEase(Ease.OutBack);

                // Clear only the rotation offset, keep position offset for inspection mode
                piece.CacheInspectionOffsets(piece.inspectionOffset, Quaternion.identity);
            }
        }
    }

    void CheckJarCompletion()
    {
        int assembledPieces = GetAssembledPieceCount();
        Debug.Log($"Progress: {assembledPieces}/{allPieces.Count} pieces assembled");

        if (jarFullyAssembled || allPieces.Count == 0 || assembledPieces < allPieces.Count)
            return;

        // Clean up any partial assembly parent before completing
        CleanupTemporaryAssemblyParent();

        jarFullyAssembled = true;

        // CRITICAL: Enforce exact final positions and rotations before any other processing
        EnforceFinalAssemblyPositions();

        HandleAllPiecesOnCompletion();

        // Start celebration after final positions are set
        StartCoroutine(CelebrationSequenceAfterFinalPositions());

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

            // FIXED: Keep assembled jar inspectable for rotation - same as individual pieces
            var parentInspectable = assembledJarRoot.GetComponent<InspectableJar>();
            if (parentInspectable != null)
            {
                parentInspectable.SetInspectable(true);
                Debug.Log($"✅ KEPT assembled jar inspectable for rotation - same as individual pieces");
            }
            else
            {
                Debug.LogWarning($"⚠️ No InspectableJar component found on assembled jar root: {assembledJarRoot.name}");
            }

            // Enable assembled jar collider for interaction
            if (assembledJarCollider != null)
            {
                assembledJarCollider.enabled = true;
                Debug.Log($"✅ ENABLED assembled jar collider for rotation interaction");
            }
            else
            {
                Debug.LogWarning($"⚠️ No assembled jar collider found for {assembledJarRoot.name}");
            }

            // NEW: Automatically enable rotation after jar completion (no click needed)
            StartCoroutine(EnableAssembledJarRotationAfterDelay());

            Debug.Log($"✅ ASSEMBLED JAR SETUP COMPLETE - rotation will be automatically enabled");
        }
        else
        {
            Debug.LogError("❌ AssembledJarRoot is null!");
        }
    }


    IEnumerator CelebrationSequenceAfterFinalPositions()
    {
        Debug.Log("🎉 Jar restoration complete! 🎉");

        // Wait for the final position animations to complete
        yield return new WaitForSeconds(assemblyDuration * 0.5f + 0.1f);

        // Simple celebration effect - just a gentle scale pulse that doesn't affect position
        foreach (JarAutoAssembly piece in allPieces)
        {
            if (piece != null && piece.isAssembled)
            {
                piece.transform.DOPunchScale(Vector3.one * 0.05f, 0.3f, 2);
            }
        }

        Debug.Log("✅ Jar pieces are now in perfect final positions!");
        // Optional: Add particle effects, sound, or UI feedback here
        // No rotation or position changes - jar stays in exact final position
    }

    /// <summary>
    /// Automatically enable rotation for the assembled jar after a short delay
    /// This eliminates the need for the user to click first
    /// </summary>
    IEnumerator EnableAssembledJarRotationAfterDelay()
    {
        // Wait for all animations and celebration to complete
        yield return new WaitForSeconds(assemblyDuration * 0.5f + 0.5f);

        // FIXED: Add proper null checks to prevent MissingReferenceException
        if (assembledJarRoot == null)
        {
            Debug.LogWarning("⚠️ AssembledJarRoot is null - cannot enable automatic rotation");
            yield break;
        }

        // Additional check to ensure the object hasn't been destroyed
        if (assembledJarRoot.gameObject == null)
        {
            Debug.LogWarning("⚠️ AssembledJarRoot GameObject has been destroyed - cannot enable automatic rotation");
            yield break;
        }

        Debug.Log("🔄 Auto-enabling assembled jar rotation - no click needed!");

        ObjectCloseUpManager closeUpManager = FindObjectOfType<ObjectCloseUpManager>();
        if (closeUpManager == null)
        {
            Debug.LogWarning("⚠️ ObjectCloseUpManager not found - cannot enable rotation");
            yield break;
        }

        // Set up rotation-only mode WITHOUT bringing to close-up
        // The jar rotates at its current assembled position
        closeUpManager.SetCurrentObject(assembledJarRoot);

        // Enable rotation at current position (no movement)
        var smoothRotator = FindObjectOfType<SmoothObjectRotator>();
        if (smoothRotator != null)
        {
            // Start rotation at the jar's current assembled position
            smoothRotator.UpdateFixedPosition(assembledJarRoot.position);
            smoothRotator.StartRotating(assembledJarRoot);

            Debug.Log("✅ Assembled jar rotation AUTO-ENABLED at position: " + assembledJarRoot.position);
        }
        else
        {
            Debug.LogWarning("⚠️ SmoothObjectRotator not found - rotation may not work properly");
        }

        // Start inspection for visual feedback (but no position changes)
        var assembledInspectable = assembledJarRoot.GetComponent<IInspectable>();
        if (assembledInspectable != null)
        {
            assembledInspectable.OnInspectionStart();
            Debug.Log("✅ Visual feedback enabled for assembled jar rotation");
        }
        else
        {
            Debug.LogWarning("⚠️ No IInspectable component found on assembled jar root");
        }

        Debug.Log("🎯 Assembled jar is now ready for immediate rotation!");
    }

    void HandleJarFullyAssembledState(bool includeTransformAdjustments)
    {
        ObjectCloseUpManager closeUpManager = FindObjectOfType<ObjectCloseUpManager>();
        if (closeUpManager != null && closeUpManager.CurrentCloseUpObject == transform)
        {
            closeUpManager.PauseRotation();
        }

        // Clean up any partial assembly parent when jar is fully assembled
        CleanupTemporaryAssemblyParent();

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
        // FIXED: Use base rotation to ensure default snap behavior
        Quaternion correctRot = GetBaseCorrectWorldRotation();

        transform.DOKill();

        if (animateToCorrectPosition)
        {
            transform.DOMove(correctPos, assemblyDuration).SetEase(Ease.OutBack);
            transform.DORotateQuaternion(correctRot, assemblyDuration).SetEase(Ease.OutBack);

            transform.DOPunchScale(Vector3.one * 0.1f, 0.3f, 5);
            Debug.Log($"🔄 {pieceType} (MarkAssembled) rotating to base assembly rotation: {correctRot.eulerAngles}");
        }
        else
        {
            transform.position = correctPos;
            transform.rotation = correctRot;
        }
    }

    // Test function to verify final positions
    [ContextMenu("Test Final Positions")]
    public void TestFinalPositions()
    {
        Debug.Log("🧪 Testing Final Assembly Positions:");
        Debug.Log($"Bottom: {finalBottomPosition} (Target: -0.0399, 4.12828, -1.46515)");
        Debug.Log($"Middle: {finalMiddlePosition} (Target: 0, 2.97428, -1.45318)");
        Debug.Log($"Top: {finalTopPosition} (Target: 0.415, 4.07528, -1.72915)");
        Debug.Log($"Final Rotation: {finalRotation} (Target: -89.98, 0, 0)");

        // Immediately set all pieces to final positions for testing
        foreach (JarAutoAssembly piece in allPieces)
        {
            if (piece != null)
            {
                Vector3 finalPos = piece.GetFinalAssemblyPosition();
                Quaternion finalRot = Quaternion.Euler(finalRotation);

                piece.transform.position = finalPos;
                piece.transform.rotation = finalRot;

                Debug.Log($"Set {piece.pieceType} to: Position {finalPos}, Rotation {finalRot.eulerAngles}");
            }
        }
    }


    // Reset function for testing
    [ContextMenu("Reset Assembly")]
    public void ResetAssembly()
    {
        // Clean up any temporary assembly parent
        CleanupTemporaryAssemblyParent();

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
