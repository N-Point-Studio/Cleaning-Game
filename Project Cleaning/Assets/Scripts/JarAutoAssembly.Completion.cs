// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using DG.Tweening;
// #if UNITY_EDITOR
// using UnityEditor;
// #endif

// public partial class JarAutoAssembly : MonoBehaviour
// {
//     void CheckJarCompletion()
//     {
//         int assembledPieces = GetAssembledPieceCount();
//         Debug.Log($"Progress: {assembledPieces}/{allPieces.Count} pieces assembled");

//         if (jarFullyAssembled || allPieces.Count == 0 || assembledPieces < allPieces.Count)
//             return;

//         // Clean up any partial assembly parent before completing
//         CleanupTemporaryAssemblyParent();

//         jarFullyAssembled = true;

//         // CRITICAL: Enforce exact final positions and rotations before any other processing
//         EnforceFinalAssemblyPositions();

//         HandleAllPiecesOnCompletion();

//         // Start celebration after final positions are set
//         StartCoroutine(CelebrationSequenceAfterFinalPositions());

//         ObjectCloseUpManager closeUpManager = FindObjectOfType<ObjectCloseUpManager>();

//         if (assembledJarRoot != null)
//         {
//             Debug.Log($"🔧 ASSEMBLY COMPLETION: Bringing assembled jar to close-up: {assembledJarRoot.name}");

//             // CRITICAL: Clear any individual pieces from close-up first
//             // This prevents the individual pieces from staying in close-up after assembly
//             if (closeUpManager != null && closeUpManager.HasObjectInCloseUp)
//             {
//                 Transform currentObject = closeUpManager.CurrentCloseUpObject;
//                 Debug.Log($"🔍 Current object in close-up before clearing: {currentObject?.name ?? "null"}");

//                 var currentPieceComponent = currentObject?.GetComponent<JarAutoAssembly>();

//                 if (currentPieceComponent != null && !currentPieceComponent.IsAssembledJarRoot(currentObject))
//                 {
//                     Debug.Log($"❌ CLEARING individual piece {currentObject.name} from close-up before showing assembled jar");
//                     closeUpManager.ClearCurrentObject();
//                     Debug.Log($"✅ Current object cleared. HasObjectInCloseUp is now: {closeUpManager.HasObjectInCloseUp}");
//                 }
//                 else
//                 {
//                     Debug.Log($"⚠️ Current object {currentObject?.name} is either null or already the assembled jar root");
//                 }
//             }
//             else
//             {
//                 Debug.Log("ℹ️ No object currently in close-up, proceeding to bring assembled jar");
//             }

//             // FIXED: Keep assembled jar inspectable for rotation - same as individual pieces
//             var parentInspectable = assembledJarRoot.GetComponent<InspectableJar>();
//             if (parentInspectable != null)
//             {
//                 parentInspectable.SetInspectable(true);
//                 Debug.Log("✅ KEPT assembled jar inspectable for rotation - same as individual pieces");
//             }
//             else
//             {
//                 Debug.LogWarning($"⚠️ No InspectableJar component found on assembled jar root: {assembledJarRoot.name}");
//             }

//             // Enable assembled jar collider for interaction
//             if (assembledJarCollider != null)
//             {
//                 assembledJarCollider.enabled = true;
//                 Debug.Log("✅ ENABLED assembled jar collider for rotation interaction");
//             }
//             else
//             {
//                 Debug.LogWarning($"⚠️ No assembled jar collider found for {assembledJarRoot.name}");
//             }

//             // NEW: Automatically enable rotation after jar completion (no click needed)
//             StartCoroutine(EnableAssembledJarRotationAfterDelay());

//             Debug.Log("✅ ASSEMBLED JAR SETUP COMPLETE - rotation will be automatically enabled");
//         }
//         else
//         {
//             Debug.LogError("❌ AssembledJarRoot is null!");
//         }
//     }


//     IEnumerator CelebrationSequenceAfterFinalPositions()
//     {
//         Debug.Log("🎉 Jar restoration complete! 🎉");

//         // Wait for the final position animations to complete
//         yield return new WaitForSeconds(assemblyDuration * 0.5f + 0.1f);

//         // Simple celebration effect - just a gentle scale pulse that doesn't affect position
//         foreach (JarAutoAssembly piece in allPieces)
//         {
//             if (piece != null && piece.isAssembled)
//             {
//                 piece.transform.DOPunchScale(Vector3.one * 0.05f, 0.3f, 2);
//             }
//         }

//         Debug.Log("✅ Jar pieces are now in perfect final positions!");
//         // Optional: Add particle effects, sound, or UI feedback here
//         // No rotation or position changes - jar stays in exact final position
//     }

//     /// <summary>
//     /// Automatically enable rotation for the assembled jar after a short delay
//     /// This eliminates the need for the user to click first
//     /// </summary>
//     IEnumerator EnableAssembledJarRotationAfterDelay()
//     {
//         // Wait for all animations and celebration to complete
//         yield return new WaitForSeconds(assemblyDuration * 0.5f + 0.5f);

//         // FIXED: Add proper null checks to prevent MissingReferenceException
//         if (assembledJarRoot == null)
//         {
//             Debug.LogWarning("⚠️ AssembledJarRoot is null - cannot enable automatic rotation");
//             yield break;
//         }

//         // Additional check to ensure the object hasn't been destroyed
//         if (assembledJarRoot.gameObject == null)
//         {
//             Debug.LogWarning("⚠️ AssembledJarRoot GameObject has been destroyed - cannot enable automatic rotation");
//             yield break;
//         }

//         Debug.Log("🔄 Auto-enabling assembled jar rotation - no click needed!");

//         ObjectCloseUpManager closeUpManager = FindObjectOfType<ObjectCloseUpManager>();
//         if (closeUpManager == null)
//         {
//             Debug.LogWarning("⚠️ ObjectCloseUpManager not found - cannot enable rotation");
//             yield break;
//         }

//         // Set up rotation-only mode WITHOUT bringing to close-up
//         // The jar rotates at its current assembled position
//         closeUpManager.SetCurrentObject(assembledJarRoot);

//         // Enable rotation at current position (no movement)
//         var smoothRotator = FindObjectOfType<SmoothObjectRotator>();
//         if (smoothRotator != null)
//         {
//             // Start rotation at the jar's current assembled position
//             smoothRotator.UpdateFixedPosition(assembledJarRoot.position);
//             smoothRotator.StartRotating(assembledJarRoot);

//             Debug.Log("✅ Assembled jar rotation AUTO-ENABLED at position: " + assembledJarRoot.position);
//         }
//         else
//         {
//             Debug.LogWarning("⚠️ SmoothObjectRotator not found - rotation may not work properly");
//         }

//         // Start inspection for visual feedback (but no position changes)
//         var assembledInspectable = assembledJarRoot.GetComponent<IInspectable>();
//         if (assembledInspectable != null)
//         {
//             assembledInspectable.OnInspectionStart();
//             Debug.Log("✅ Visual feedback enabled for assembled jar rotation");
//         }
//         else
//         {
//             Debug.LogWarning("⚠️ No IInspectable component found on assembled jar root");
//         }

//         Debug.Log("🎯 Assembled jar is now ready for immediate rotation!");
//     }

//     void HandleJarFullyAssembledState(bool includeTransformAdjustments)
//     {
//         ObjectCloseUpManager closeUpManager = FindObjectOfType<ObjectCloseUpManager>();
//         if (closeUpManager != null && closeUpManager.CurrentCloseUpObject == transform)
//         {
//             closeUpManager.PauseRotation();
//         }

//         // Clean up any partial assembly parent when jar is fully assembled
//         CleanupTemporaryAssemblyParent();

//         // FIXED: Don't re-parent pieces during completion to avoid position changes
//         // Keep pieces exactly where they are assembled in inspection mode
//         if (includeTransformAdjustments && assembledJarRoot != null)
//         {
//             Debug.Log($"🔒 Skipping re-parenting {pieceType} to keep position fixed");
//             // Comment out re-parenting to prevent position drift
//             // transform.SetParent(assembledJarRoot, true);
//         }

//         if (pieceCollider != null)
//         {
//             if (allowReassemblyAfterCompletion)
//             {
//                 pieceCollider.enabled = true;
//             }
//             else if (disablePieceCollidersOnCompletion)
//             {
//                 pieceCollider.enabled = false;
//             }
//         }

//         if (assembledJarCollider != null)
//         {
//             assembledJarCollider.enabled = true;
//         }

//         // Prevent individual inspection/rotation once the jar is complete
//         if (inspectableComponent != null)
//         {
//             inspectableComponent.SetInspectable(false);
//             if (inspectableComponent.IsBeingInspected())
//             {
//                 inspectableComponent.OnInspectionEnd();
//             }
//         }

//         if (!allowReassemblyAfterCompletion)
//         {
//             enabled = false;
//         }
//         else if (!enabled)
//         {
//             enabled = true;
//         }
//     }

//     void HandleAllPiecesOnCompletion()
//     {
//         JarAutoAssembly[] snapshot = allPieces.ToArray();

//         for (int i = 0; i < snapshot.Length; i++)
//         {
//             if (snapshot[i] != null)
//             {
//                 snapshot[i].HandleJarFullyAssembledState(false);
//             }
//         }

//         for (int i = 0; i < snapshot.Length; i++)
//         {
//             if (snapshot[i] != null)
//             {
//                 snapshot[i].HandleJarFullyAssembledState(true);
//             }
//         }
//     }

//     void ReturnToOriginalPosition()
//     {
//         transform.DOMove(originalPosition, 0.5f).SetEase(Ease.OutQuad);
//     }

//     void MarkAssembled(bool animateToCorrectPosition)
//     {
//         if (isAssembled)
//             return;

//         isAssembled = true;

//         if (!assemblyOrder.Contains(this))
//         {
//             assemblyOrder.Add(this);
//             Debug.Log($"Added {pieceType} to assembly order via MarkAssembled. Total: {assemblyOrder.Count}");
//         }

//         Vector3 correctPos = GetCorrectPosition();
//         // FIXED: Use base rotation to ensure default snap behavior
//         Quaternion correctRot = GetBaseCorrectWorldRotation();

//         transform.DOKill();

//         if (animateToCorrectPosition)
//         {
//             transform.DOMove(correctPos, assemblyDuration).SetEase(Ease.OutBack);
//             transform.DORotateQuaternion(correctRot, assemblyDuration).SetEase(Ease.OutBack);

//             transform.DOPunchScale(Vector3.one * 0.1f, 0.3f, 5);
//             Debug.Log($"🔄 {pieceType} (MarkAssembled) rotating to base assembly rotation: {correctRot.eulerAngles}");
//         }
//         else
//         {
//             transform.position = correctPos;
//             transform.rotation = correctRot;
//         }
//     }

//     // Test function to verify final positions
//     [ContextMenu("Test Final Positions")]
//     public void TestFinalPositions()
//     {
//         Debug.Log("🧪 Testing Final Assembly Positions:");
//         Debug.Log($"Bottom: {finalBottomPosition} (Target: -0.0399, 4.12828, -1.46515)");
//         Debug.Log($"Middle: {finalMiddlePosition} (Target: 0, 2.97428, -1.45318)");
//         Debug.Log($"Top: {finalTopPosition} (Target: 0.415, 4.07528, -1.72915)");
//         Debug.Log($"Final Rotation: {finalRotation} (Target: -89.98, 0, 0)");

//         // Immediately set all pieces to final positions for testing
//         foreach (JarAutoAssembly piece in allPieces)
//         {
//             if (piece != null)
//             {
//                 Vector3 finalPos = piece.GetFinalAssemblyPosition();
//                 Quaternion finalRot = Quaternion.Euler(finalRotation);

//                 piece.transform.position = finalPos;
//                 piece.transform.rotation = finalRot;

//                 Debug.Log($"Set {piece.pieceType} to: Position {finalPos}, Rotation {finalRot.eulerAngles}");
//             }
//         }
//     }


//     // Reset function for testing
//     [ContextMenu("Reset Assembly")]
//     public void ResetAssembly()
//     {
//         // Clean up any temporary assembly parent
//         CleanupTemporaryAssemblyParent();
//         assemblyOrder.Clear();

//         foreach (JarAutoAssembly piece in allPieces)
//         {
//             piece.isAssembled = false;
//             piece.isDragging = false;
//             piece.transform.position = piece.originalPosition;
//             piece.transform.rotation = Quaternion.identity;
//             piece.transform.SetParent(piece.originalParent, true);
//             piece.localOffsetsComputed = false;
//             piece.ComputeLocalAssemblyOffsets();

//             if (piece.disablePieceCollidersOnCompletion && piece.pieceCollider != null)
//             {
//                 piece.pieceCollider.enabled = true;
//             }

//             if (piece.assembledJarCollider != null)
//             {
//                 piece.assembledJarCollider.enabled = piece.initialAssembledColliderState;
//             }

//             if (piece.inspectableComponent != null)
//             {
//                 piece.inspectableComponent.SetInspectable(piece.initialCanBeInspected);
//                 if (piece.inspectableComponent.IsBeingInspected())
//                 {
//                     piece.inspectableComponent.OnInspectionEnd();
//                 }
//             }

//             piece.enabled = piece.initialScriptEnabled;
//         }
//         jarFullyAssembled = false;
//         Debug.Log("Assembly reset!");
//     }

//     void OnDrawGizmosSelected()
//     {
//         // Draw correct position
//         Vector3 correctPos = Application.isPlaying ? GetBaseCorrectWorldPosition() : GetConfiguredWorldPosition();
//         Gizmos.color = Color.green;
//         Gizmos.DrawWireSphere(correctPos, 0.2f);

//         // Draw ground assembly snap distance
//         Gizmos.color = Color.yellow;
//         Gizmos.DrawWireSphere(correctPos, snapDistance);

//         // Draw line to correct position
//         if (!isAssembled)
//         {
//             Gizmos.color = Color.red;
//             Gizmos.DrawLine(transform.position, correctPos);
//         }

//         // Draw inspection snap zones if in play mode
//         if (Application.isPlaying)
//         {
//             ObjectCloseUpManager closeUpManager = FindObjectOfType<ObjectCloseUpManager>();
//             if (closeUpManager != null && closeUpManager.HasObjectInCloseUp)
//             {
//                 Transform inspectedObject = closeUpManager.CurrentCloseUpObject;
//                 if (inspectedObject != null && inspectedObject != transform)
//                 {
//                     JarAutoAssembly inspectedPiece = inspectedObject.GetComponent<JarAutoAssembly>();
//                     if (inspectedPiece != null && inspectedPiece.isAssembled)
//                     {
//                         Vector3 inspectionSnapPos = GetCorrectInspectionPosition(inspectedPiece);

//                         // Draw inspection snap position
//                         Gizmos.color = Color.cyan;
//                         Gizmos.DrawWireSphere(inspectionSnapPos, 0.15f);

//                         // Draw inspection snap distance
//                         Gizmos.color = Color.blue;
//                         Gizmos.DrawWireSphere(inspectionSnapPos, snapDistance);

//                         // Draw line from current position to inspection snap position
//                         if (!isAssembled)
//                         {
//                             Gizmos.color = Color.magenta;
//                             Gizmos.DrawLine(transform.position, inspectionSnapPos);
//                         }

//                         // Label for clarity
// #if UNITY_EDITOR
//                         Handles.Label(inspectionSnapPos + Vector3.up * 0.5f,
//                             $"Inspection Snap\n{pieceType} -> {inspectedPiece.pieceType}");
// #endif
//                     }
//                 }
//             }
//         }
//     }

//     public bool IsAssembledJarRoot(Transform target)
//     {
//         return assembledJarRoot != null && target == assembledJarRoot;
//     }

//     void TryInspectionAssembly()
//     {
//         // Only attempt inspection assembly if we're not already assembled
//         if (isAssembled) return;

//         ObjectCloseUpManager closeUpManager = FindObjectOfType<ObjectCloseUpManager>();
//         if (closeUpManager == null || !closeUpManager.HasObjectInCloseUp) return;

//         // Check if there's another jar piece currently being inspected that we can assemble to
//         Transform inspectedObject = closeUpManager.CurrentCloseUpObject;
//         if (inspectedObject == null || inspectedObject == transform) return;

//         JarAutoAssembly inspectedPiece = inspectedObject.GetComponent<JarAutoAssembly>();
//         if (inspectedPiece == null || !inspectedPiece.isAssembled) return;

//         // Calculate the correct position for this piece relative to the inspected piece
//         Vector3 correctInspectionPos = GetCorrectInspectionPosition(inspectedPiece);
//         float distance = Vector3.Distance(transform.position, correctInspectionPos);

//         Debug.Log($"Inspection assembly check: {pieceType} -> {inspectedPiece.pieceType}, distance: {distance:F2}, snap threshold: {snapDistance}");

//         if (distance <= snapDistance)
//         {
//             AssembleToInspectionPosition(inspectedPiece, correctInspectionPos);
//         }
//         else
//         {
//             Debug.Log($"Inspection assembly: {pieceType} too far from {inspectedPiece.pieceType} (distance: {distance:F2})");
//         }
//     }

//     /// <summary>
//     /// Calculate where this piece should be positioned relative to an inspected piece
//     /// FIXED: Use consistent shared inspection offset for all pieces
//     /// </summary>
//     Vector3 GetCorrectInspectionPosition(JarAutoAssembly relativeToPiece)
//     {
//         // CRITICAL FIX: Get the SHARED inspection offset from the first assembled piece
//         // This ensures ALL pieces use the same inspection offset, preventing position scatter
//         Vector3 sharedInspectionOffset = GetSharedInspectionOffset();

//         // Get our base assembly position (without any inspection offsets)
//         Vector3 thisBasePos = GetBaseCorrectWorldPosition();

//         // Apply the shared inspection offset consistently to all pieces
//         Vector3 correctInspectionPos = thisBasePos + sharedInspectionOffset;

//         Debug.Log($"🔧 FIXED Inspection position calculation for {pieceType}:");
//         Debug.Log($"    This base pos: {thisBasePos}");
//         Debug.Log($"    Shared inspection offset: {sharedInspectionOffset}");
//         Debug.Log($"    Final inspection pos: {correctInspectionPos}");

//         return correctInspectionPos;
//     }

//     /// <summary>
//     /// Get the shared inspection offset used by all assembled pieces
//     /// This ensures consistent positioning across all pieces during inspection assembly
//     /// </summary>
//     Vector3 GetSharedInspectionOffset()
//     {
//         // Find the first assembled piece that has a cached inspection offset
//         foreach (JarAutoAssembly piece in allPieces)
//         {
//             if (piece != null && piece.isAssembled && piece.inspectionOffset != Vector3.zero)
//             {
//                 Debug.Log($"📏 Using shared inspection offset from {piece.pieceType}: {piece.inspectionOffset}");
//                 return piece.inspectionOffset;
//             }
//         }

//         // If no piece has a cached offset yet, use the current inspection mode offset
//         ObjectCloseUpManager closeUpManager = FindObjectOfType<ObjectCloseUpManager>();
//         if (closeUpManager != null && closeUpManager.HasObjectInCloseUp)
//         {
//             Transform inspected = closeUpManager.CurrentCloseUpObject;
//             if (inspected != null)
//             {
//                 JarAutoAssembly inspectedPiece = inspected.GetComponent<JarAutoAssembly>();
//                 if (inspectedPiece != null && inspectedPiece.isAssembled)
//                 {
//                     Vector3 inspectedBasePos = inspectedPiece.GetBaseCorrectWorldPosition();
//                     Vector3 currentInspectionOffset = inspected.position - inspectedBasePos;
//                     Debug.Log($"📏 Calculating shared inspection offset from currently inspected {inspectedPiece.pieceType}: {currentInspectionOffset}");
//                     return currentInspectionOffset;
//                 }
//             }
//         }

//         // Fallback to zero offset
//         Debug.Log("📏 No shared inspection offset found, using zero offset");
//         return Vector3.zero;
//     }

//     /// <summary>
//     /// Assemble this piece to the correct position relative to an inspected piece
//     /// FIXED: Last piece adapts to existing pieces' position and scale
//     /// </summary>
//     void AssembleToInspectionPosition(JarAutoAssembly relativeToPiece, Vector3 targetPosition)
//     {
//         Debug.Log($"🔧 Assembling {pieceType} to inspection position relative to {relativeToPiece.pieceType}!");

//         // CRITICAL: Set this piece as assembled BEFORE animation
//         // This prevents GetCorrectPosition from applying wrong offsets during animation
//         isAssembled = true;

//         if (!assemblyOrder.Contains(this))
//         {
//             assemblyOrder.Add(this);
//             Debug.Log($"Added {pieceType} to assembly order. Total: {assemblyOrder.Count}");
//         }

//         // Adapt this piece to match existing assembled pieces' positioning
//         Vector3 adaptedTargetPosition = AdaptToExistingPiecesPosition(targetPosition);

//         // Cache the adapted inspection offset for this piece
//         Vector3 basePos = GetBaseCorrectWorldPosition();
//         Vector3 adaptedInspectionOffset = adaptedTargetPosition - basePos;
//         CacheInspectionOffsets(adaptedInspectionOffset, Quaternion.identity);

//         Debug.Log($"📦 Adapted target position for {pieceType}: {adaptedTargetPosition}");
//         Debug.Log($"📦 Cached adapted inspection offset: {adaptedInspectionOffset}");

//         // FIRST: Reset all existing assembled pieces to default rotation (keep their positions)
//         ResetAllAssembledPiecesToDefaultRotation();

//         // THEN: Animate to the adapted target position (shared inspection position)
//         transform.DOMove(adaptedTargetPosition, assemblyDuration).SetEase(Ease.OutBack);

//         // FIXED: Get base rotation without any inspection offsets to ensure default snap behavior
//         Quaternion correctRot = GetBaseCorrectWorldRotation();
//         transform.DORotateQuaternion(correctRot, assemblyDuration).SetEase(Ease.OutBack);

//         // Add assembly effect
//         transform.DOPunchScale(Vector3.one * 0.1f, 0.3f, 5);

//         Debug.Log($"🔄 {pieceType} rotating to base assembly rotation during inspection assembly: {correctRot.eulerAngles}");

//         // Hide any outline that was showing
//         HideInspectedObjectOutline();

//         // Check if jar is now complete
//         CheckJarCompletion();

//         Debug.Log($"✅ {pieceType} assembled and adapted to existing pieces!");
//     }

//     /// <summary>
//     /// Adapt this piece's position to match existing assembled pieces' positioning and scale
//     /// The last piece follows the position and scale established by pieces 1 & 2
//     /// </summary>
//     Vector3 AdaptToExistingPiecesPosition(Vector3 originalTargetPosition)
//     {
//         // Find the first assembled piece to use as reference
//         JarAutoAssembly referencePiece = null;
//         foreach (JarAutoAssembly piece in allPieces)
//         {
//             if (piece != null && piece != this && piece.isAssembled)
//             {
//                 referencePiece = piece;
//                 break;
//             }
//         }

//         if (referencePiece == null)
//         {
//             Debug.Log($"🔄 No reference piece found, using original position: {originalTargetPosition}");
//             return originalTargetPosition;
//         }

//         // Calculate the inspection offset that the reference piece is using
//         Vector3 referenceBasePos = referencePiece.GetBaseCorrectWorldPosition();
//         Vector3 referenceCurrentPos = referencePiece.transform.position;
//         Vector3 referenceInspectionOffset = referenceCurrentPos - referenceBasePos;

//         // Apply the SAME inspection offset to this piece's base position
//         Vector3 myBasePos = GetBaseCorrectWorldPosition();
//         Vector3 adaptedPosition = myBasePos + referenceInspectionOffset;

//         Debug.Log($"🔄 Adapting {pieceType} to follow {referencePiece.pieceType}:");
//         Debug.Log($"    - Reference base: {referenceBasePos}");
//         Debug.Log($"    - Reference current: {referenceCurrentPos}");
//         Debug.Log($"    - Reference offset: {referenceInspectionOffset}");
//         Debug.Log($"    - My base: {myBasePos}");
//         Debug.Log($"    - My adapted position: {adaptedPosition}");
//         Debug.Log($"    - Original target was: {originalTargetPosition}");

//         return adaptedPosition;
//     }


//     /// <summary>
//     /// Hide outline on inspected object when drag ends
//     /// </summary>
//     void HideInspectedObjectOutline()
//     {
//         ObjectCloseUpManager closeUpManager = FindObjectOfType<ObjectCloseUpManager>();
//         if (closeUpManager == null || !closeUpManager.HasObjectInCloseUp)
//             return;

//         Transform inspectedObject = closeUpManager.CurrentCloseUpObject;
//         if (inspectedObject == null || inspectedObject == transform)
//             return;

//         InspectableJar inspectableJar = inspectedObject.GetComponent<InspectableJar>();
//         if (inspectableJar != null && inspectableJar.IsShowingOutline())
//         {
//             inspectableJar.HideOutline();
//             Debug.Log($"Hidden outline on inspected object - {pieceType} drag ended");
//         }
//     }

//     /// <summary>
//     /// Resume rotation on any object currently in inspection mode
//     /// </summary>
//     void ResumeInspectionRotation()
//     {
//         if (IsJarFullyAssembled)
//             return;

//         ObjectCloseUpManager closeUpManager = FindObjectOfType<ObjectCloseUpManager>();
//         if (closeUpManager != null && closeUpManager.HasObjectInCloseUp)
//         {
//             Debug.Log($"Resuming rotation on inspected object after {pieceType} drag ended");
//             closeUpManager.ResumeRotation();
//         }
//     }

//     /// <summary>
//     /// Check proximity to inspected objects and show/hide outline accordingly
//     /// Also provides visual feedback for inspection snap zones
//     /// </summary>
//     void CheckOutlineProximity()
//     {
//         ObjectCloseUpManager closeUpManager = FindObjectOfType<ObjectCloseUpManager>();
//         if (closeUpManager == null || !closeUpManager.HasObjectInCloseUp)
//             return;

//         Transform inspectedObject = closeUpManager.CurrentCloseUpObject;
//         if (inspectedObject == null || inspectedObject == transform)
//             return;

//         // Get InspectableJar component from inspected object
//         InspectableJar inspectableJar = inspectedObject.GetComponent<InspectableJar>();
//         if (inspectableJar == null)
//             return;

//         // Calculate distance between dragged object and inspected object
//         float distance = Vector3.Distance(transform.position, inspectedObject.position);

//         // Check for inspection snap zone
//         bool inSnapZone = false;
//         if (!isAssembled)
//         {
//             JarAutoAssembly inspectedPiece = inspectedObject.GetComponent<JarAutoAssembly>();
//             if (inspectedPiece != null && inspectedPiece.isAssembled)
//             {
//                 Vector3 correctInspectionPos = GetCorrectInspectionPosition(inspectedPiece);
//                 float snapDist = Vector3.Distance(transform.position, correctInspectionPos);

//                 if (snapDist <= snapDistance)
//                 {
//                     inSnapZone = true;
//                     Debug.Log($"🎯 {pieceType} in inspection SNAP ZONE for {inspectedPiece.pieceType} (snap distance: {snapDist:F2})");
//                 }
//             }
//         }

//         // Show/hide outline based on proximity or snap zone
//         bool shouldShowOutline = (distance <= inspectableJar.outlineActivationDistance) || inSnapZone;

//         if (shouldShowOutline)
//         {
//             if (!inspectableJar.IsShowingOutline())
//             {
//                 inspectableJar.ShowOutline();
//                 if (inSnapZone)
//                 {
//                     Debug.Log($"🎯 {pieceType} near SNAP ZONE - showing outline");
//                 }
//                 else
//                 {
//                     Debug.Log($"🎯 {pieceType} near inspected object - showing outline (distance: {distance:F1})");
//                 }
//             }
//         }
//         else
//         {
//             if (inspectableJar.IsShowingOutline())
//             {
//                 inspectableJar.HideOutline();
//                 Debug.Log($"📤 {pieceType} moved away from inspected object - hiding outline (distance: {distance:F1})");
//             }
//         }
//     }

//     Vector3 GetCorrectPosition()
//     {
//         Vector3 target = GetBaseCorrectWorldPosition();

//         ObjectCloseUpManager closeUpManager = FindObjectOfType<ObjectCloseUpManager>();
//         if (closeUpManager != null && closeUpManager.HasObjectInCloseUp)
//         {
//             Transform inspected = closeUpManager.CurrentCloseUpObject;
//             if (inspected != null && inspected != assembledJarRoot)
//             {
//                 JarAutoAssembly inspectedPiece = inspected.GetComponent<JarAutoAssembly>();
//                 if (inspectedPiece != null)
//                 {
//                     if (inspectedPiece == this)
//                     {
//                         // This piece is the one being inspected - use its cached inspection offset
//                         target += inspectionOffset;
//                         Debug.Log($"🎯 Using cached inspection offset for {pieceType}: {inspectionOffset}");
//                     }
//                     else if (inspectedPiece.isAssembled)
//                     {
//                         // FIXED: Use shared inspection offset for consistency
//                         // This ensures all pieces maintain the same relative positioning
//                         Vector3 sharedOffset = GetSharedInspectionOffset();
//                         target += sharedOffset;
//                         Debug.Log($"🔧 FIXED: Using shared inspection offset for {pieceType}: {sharedOffset}");
//                     }
//                     else
//                     {
//                         // Fallback to the original logic for unassembled pieces
//                         target += inspectedPiece.GetAssemblyOffset();
//                         Debug.Log($"📎 Using assembly offset for {pieceType}: {inspectedPiece.GetAssemblyOffset()}");
//                     }
//                 }
//             }
//         }

//         return target;
//     }

//     Quaternion GetCorrectRotation()
//     {
//         Quaternion target = GetBaseCorrectWorldRotation();

//         ObjectCloseUpManager closeUpManager = FindObjectOfType<ObjectCloseUpManager>();
//         if (closeUpManager != null && closeUpManager.HasObjectInCloseUp)
//         {
//             Transform inspected = closeUpManager.CurrentCloseUpObject;
//             if (inspected != null && inspected != assembledJarRoot)
//             {
//                 JarAutoAssembly inspectedPiece = inspected.GetComponent<JarAutoAssembly>();
//                 if (inspectedPiece != null)
//                 {
//                     if (inspectedPiece == this)
//                         target = inspectionRotationOffset * target;
//                     else
//                         target = inspectedPiece.GetAssemblyRotationOffset() * target;
//                 }
//             }
//         }

//         return target;
//     }

//     internal Vector3 GetBaseCorrectWorldPosition()
//     {
//         if (!localOffsetsComputed)
//         {
//             ComputeLocalAssemblyOffsets();
//         }

//         if (assembledJarRoot != null)
//         {
//             return assembledJarRoot.TransformPoint(correctLocalPosition);
//         }

//         return correctLocalPosition;
//     }

//     internal Quaternion GetBaseCorrectWorldRotation()
//     {
//         if (!localOffsetsComputed)
//         {
//             ComputeLocalAssemblyOffsets();
//         }

//         if (assembledJarRoot != null)
//         {
//             return assembledJarRoot.rotation * correctLocalRotation;
//         }

//         return correctLocalRotation;
//     }

//     private Vector3 inspectionOffset = Vector3.zero;
//     private Quaternion inspectionRotationOffset = Quaternion.identity;

//     public void CacheInspectionOffsets(Vector3 offset, Quaternion rotationOffset)
//     {
//         inspectionOffset = offset;
//         inspectionRotationOffset = rotationOffset;
//     }

//     private Vector3 GetAssemblyOffset()
//     {
//         return inspectionOffset;
//     }

//     private Quaternion GetAssemblyRotationOffset()
//     {
//         return inspectionRotationOffset == Quaternion.identity ? Quaternion.identity : inspectionRotationOffset;
//     }

//     Vector3 GetConfiguredWorldPosition()
//     {
//         Vector3 referenceWorldPosition;

//         switch (pieceType)
//         {
//             case JarPieceType.Bottom:
//                 referenceWorldPosition = bottomPosition;
//                 break;
//             case JarPieceType.Middle:
//                 referenceWorldPosition = middlePosition;
//                 break;
//             case JarPieceType.Top:
//                 referenceWorldPosition = topPosition;
//                 break;
//             default:
//                 referenceWorldPosition = transform.position;
//                 break;
//         }

//         if (!autoScaleConfiguredPositions)
//             return referenceWorldPosition;

//         float scaleRatio = GetScaleRatio();
//         if (Mathf.Approximately(scaleRatio, 1f))
//             return referenceWorldPosition;

//         if (assembledJarRoot != null)
//         {
//             Vector3 localReference = assembledJarRoot.InverseTransformPoint(referenceWorldPosition);
//             Vector3 scaledLocal = localReference * scaleRatio;
//             return assembledJarRoot.TransformPoint(scaledLocal);
//         }

//         return referenceWorldPosition * scaleRatio;
//     }

//     /// <summary>
//     /// Get the final assembly position for this piece type when all pieces are assembled
//     /// </summary>
//     Vector3 GetFinalAssemblyPosition()
//     {
//         Vector3 referenceWorldPosition;

//         switch (pieceType)
//         {
//             case JarPieceType.Bottom:
//                 referenceWorldPosition = finalBottomPosition;
//                 break;
//             case JarPieceType.Middle:
//                 referenceWorldPosition = finalMiddlePosition;
//                 break;
//             case JarPieceType.Top:
//                 referenceWorldPosition = finalTopPosition;
//                 break;
//             default:
//                 referenceWorldPosition = transform.position;
//                 break;
//         }

//         if (!autoScaleConfiguredPositions)
//             return referenceWorldPosition;

//         float scaleRatio = GetScaleRatio();
//         if (Mathf.Approximately(scaleRatio, 1f))
//             return referenceWorldPosition;

//         if (assembledJarRoot != null)
//         {
//             Vector3 localReference = assembledJarRoot.InverseTransformPoint(referenceWorldPosition);
//             Vector3 scaledLocal = localReference * scaleRatio;
//             return assembledJarRoot.TransformPoint(scaledLocal);
//         }

//         return referenceWorldPosition * scaleRatio;
//     }

//     float GetScaleRatio()
//     {
//         if (!autoScaleConfiguredPositions)
//             return 1f;

//         if (referencePieceScale <= Mathf.Epsilon)
//             return 1f;

//         Vector3 lossyScale = transform.lossyScale;

//         float uniformScale = (Mathf.Abs(lossyScale.x) + Mathf.Abs(lossyScale.y) + Mathf.Abs(lossyScale.z)) / 3f;
//         if (Mathf.Approximately(uniformScale, 0f))
//             return 1f;

//         return uniformScale / referencePieceScale;
//     }

//     /// <summary>
//     /// Enforce the exact final positions and rotations for all pieces when jar is completed
//     /// </summary>
//     void EnforceFinalAssemblyPositions()
//     {
//         Debug.Log("🎯 Enforcing final assembly positions and rotations for all pieces");

//         // Get the inspection position from the ObjectCloseUpManager
//         ObjectCloseUpManager closeUpManager = FindObjectOfType<ObjectCloseUpManager>();
//         if (closeUpManager == null)
//         {
//             Debug.LogError("ObjectCloseUpManager not found! Cannot align to inspection position.");
//             return;
//         }
//         // Use the default GetCloseUpPosition which doesn't require a target
//         Vector3 inspectionCenter = closeUpManager.GetCloseUpPosition();

//         // Calculate the current center of the hardcoded final positions
//         Vector3 originalFinalCenter = Vector3.zero;
//         int assembledCount = 0;
//         foreach (JarAutoAssembly piece in allPieces)
//         {
//             if (piece != null && piece.isAssembled)
//             {
//                 originalFinalCenter += piece.GetFinalAssemblyPosition();
//                 assembledCount++;
//             }
//         }

//         if (assembledCount > 0)
//         {
//             originalFinalCenter /= assembledCount;
//         }

//         // Calculate the offset needed to move the original center to the inspection center
//         Vector3 positionOffset = inspectionCenter - originalFinalCenter;
//         Debug.Log($"Aligning assembled jar to inspection position. Offset: {positionOffset}");

//         // Move all pieces with the calculated offset
//         int cascadeIndex = 0;

//         foreach (JarAutoAssembly piece in allPieces)
//         {
//             if (piece != null && piece.isAssembled)
//             {
//                 Vector3 finalPos = piece.GetFinalAssemblyPosition() + positionOffset; // Apply offset
//                 Quaternion finalRot = Quaternion.Euler(finalRotation);

//                 Debug.Log($"🔧 Setting {piece.pieceType} to final position: {finalPos}, rotation: {finalRotation}");

//                 // Kill any ongoing animations
//                 piece.transform.DOKill();

//                 Vector3 originalScale = piece.transform.localScale;
//                 Sequence completionSequence = DOTween.Sequence();

//                 float cascadeDelay = Mathf.Max(0f, cascadeIndex * completionCascadeDelay);

//                 completionSequence.Append(
//                     piece.transform.DOMove(finalPos, completionMoveDuration)
//                         .SetEase(completionMoveEase)
//                 );

//                 completionSequence.Join(
//                     piece.transform.DORotateQuaternion(finalRot, completionMoveDuration)
//                         .SetEase(completionRotateEase)
//                 );

//                 if (completionScaleOvershoot > 0f && completionScaleDuration > 0f)
//                 {
//                     float bounceDelay = Mathf.Max(0f, completionMoveDuration - completionScaleDuration);
//                     Tween scaleTween = piece.transform
//                         .DOScale(originalScale * (1f + completionScaleOvershoot), completionScaleDuration)
//                         .SetEase(Ease.OutSine)
//                         .SetLoops(2, LoopType.Yoyo)
//                         .SetDelay(bounceDelay);

//                     completionSequence.Join(scaleTween);
//                 }

//                 if (cascadeDelay > 0f)
//                 {
//                     completionSequence.SetDelay(cascadeDelay);
//                 }

//                 completionSequence.OnComplete(() =>
//                 {
//                     piece.transform.position = finalPos;
//                     piece.transform.rotation = finalRot;
//                     piece.transform.localScale = originalScale;
//                 })
//                 .Play();
//                 cascadeIndex++;
//             }
//         }

//         // Set the assembled jar root position to the inspection center
//         if (assembledCount > 0 && assembledJarRoot != null)
//         {
//             assembledJarRoot.DOKill();

//             Sequence rootSequence = DOTween.Sequence();
//             float rootDelay = Mathf.Max(0f, (cascadeIndex - 1) * completionCascadeDelay);
//             rootSequence.Append(
//                 assembledJarRoot.DOMove(inspectionCenter, completionMoveDuration)
//                     .SetEase(completionMoveEase)
//             );
//             if (rootDelay > 0f)
//                 rootSequence.SetDelay(rootDelay);

//             rootSequence.OnComplete(() =>
//             {
//                 assembledJarRoot.position = inspectionCenter;
//             })
//             .Play();

//             Debug.Log($"🎯 Updated assembled jar root position to inspection center: {inspectionCenter}");
//         }

//         Debug.Log("✅ All pieces moved to final assembly positions and rotations, aligned with inspection view.");
//     }


//     void ComputeLocalAssemblyOffsets()
//     {
//         Vector3 worldTarget = GetConfiguredWorldPosition();
//         Quaternion worldRotation = Quaternion.Euler(correctRotation);

//         if (assembledJarRoot != null)
//         {
//             correctLocalPosition = assembledJarRoot.InverseTransformPoint(worldTarget);
//             correctLocalRotation = Quaternion.Inverse(assembledJarRoot.rotation) * worldRotation;
//         }
//         else
//         {
//             correctLocalPosition = worldTarget;
//             correctLocalRotation = worldRotation;
//         }

//         localOffsetsComputed = true;
//     }

//     void AssembleToCorrectPosition()
//     {
//         Debug.Log($"Assembling {pieceType} to correct position!");

//         isDragging = false;

//         // Get the shared inspection position where other pieces are assembled
//         Vector3 correctPos = GetCorrectPosition();
//         Vector3 adaptedPos = AdaptToExistingPiecesPosition(correctPos);

//         // Cache the adapted offset for inspection assembly positioning
//         Vector3 basePos = GetBaseCorrectWorldPosition();
//         Vector3 adaptedOffset = adaptedPos - basePos;

//         // Set as assembled and cache the adapted offset
//         isAssembled = true;

//         if (!assemblyOrder.Contains(this))
//         {
//             assemblyOrder.Add(this);
//             Debug.Log($"Added {pieceType} to assembly order. Total: {assemblyOrder.Count}");
//         }

//         CacheInspectionOffsets(adaptedOffset, Quaternion.identity);

//         // FIRST: Reset all existing assembled pieces to default rotation (keep their positions)
//         ResetAllAssembledPiecesToDefaultRotation();

//         // THEN: Assemble this piece to the shared inspection position
//         transform.DOKill();
//         transform.DOMove(adaptedPos, assemblyDuration).SetEase(Ease.OutBack);

//         // FIXED: Get base rotation without any inspection offsets to ensure default snap behavior
//         Quaternion correctRot = GetBaseCorrectWorldRotation();
//         transform.DORotateQuaternion(correctRot, assemblyDuration).SetEase(Ease.OutBack);
//         transform.DOPunchScale(Vector3.one * 0.1f, 0.3f, 5);

//         Debug.Log($"🔄 {pieceType} rotating to base assembly rotation: {correctRot.eulerAngles}");

//         Debug.Log($"📦 {pieceType} assembled to shared inspection position: {adaptedPos}");

//         ObjectCloseUpManager closeUpManager = FindObjectOfType<ObjectCloseUpManager>();
//         if (closeUpManager != null && closeUpManager.HasObjectInCloseUp)
//         {
//             JarAutoAssembly inspectedPiece = closeUpManager.CurrentCloseUpObject.GetComponent<JarAutoAssembly>();
//             if (inspectedPiece != null && inspectedPiece != this && !inspectedPiece.isAssembled)
//             {
//                 inspectedPiece.MarkAssembled(false);
//             }
//         }

//         // Check completion
//         CheckJarCompletion();
//     }

//     /// <summary>
//     /// Reset all currently assembled pieces to their default rotation only
//     /// Keeps pieces in their inspection assembly positions but resets rotation to default
//     /// </summary>
//         void ResetAllAssembledPiecesToDefaultRotation()
//         {
//             Debug.Log($"🔄 Resetting all assembled pieces to default rotation (keeping inspection positions) before {pieceType} assembly");

//             // If a temporary parent is being used for group rotation, reset ITS rotation.
//             if (JarAutoAssembly.currentPartialAssemblyParent != null)
//             {
//                 Debug.Log($"Found partial assembly parent. Resetting its rotation to default.");
//                 JarAutoAssembly.currentPartialAssemblyParent.transform.DOKill();
//                 // The parent's default rotation should be identity, as the pieces have their own world rotations baked in.
//                 JarAutoAssembly.currentPartialAssemblyParent.transform.DORotateQuaternion(Quaternion.identity, assemblyDuration).SetEase(Ease.OutBack);

//                 // Also clear the inspection rotation offsets on the children, as the parent is now reset
//                 foreach (Transform child in JarAutoAssembly.currentPartialAssemblyParent.transform)
//                 {
//                     JarAutoAssembly childPiece = child.GetComponent<JarAutoAssembly>();
//                     if (childPiece != null)
//                     {
//                         childPiece.CacheInspectionOffsets(childPiece.inspectionOffset, Quaternion.identity);
//                     }
//                 }
//             }
//             else // Fallback to old logic if no parent is found
//             {
//                 Debug.Log("No partial assembly parent found. Resetting rotation of individual pieces.");
//                 foreach (JarAutoAssembly piece in allPieces)
//                 {
//                     if (piece != null && piece != this && piece.isAssembled)
//                     {
//                         Quaternion defaultRot = piece.GetBaseCorrectWorldRotation();
//                         Debug.Log($"    - Resetting {piece.pieceType} rotation to {defaultRot.eulerAngles}");

//                         piece.transform.DOKill();
//                         piece.transform.DORotateQuaternion(defaultRot, assemblyDuration).SetEase(Ease.OutBack);
//                         piece.CacheInspectionOffsets(piece.inspectionOffset, Quaternion.identity);
//                     }
//                 }
//             }
//         }
// }
