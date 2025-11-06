// using System.Collections.Generic;
// using UnityEngine;

// public partial class JarAutoAssembly : MonoBehaviour
// {
//     bool IsClickedOn(Vector2 screenPos, out RaycastHit hit)
//     {
//         hit = new RaycastHit();
//         if (mainCamera == null) return false;

//         Ray ray = mainCamera.ScreenPointToRay(screenPos);
//         if (!Physics.Raycast(ray, out hit))
//             return false;

//         if (jarFullyAssembled)
//         {
//             if (hit.transform == assembledJarRoot)
//             {
//                 // If we hit the assembled jar, find which piece is closest to the hit point.
//                 float minDistance = float.MaxValue;
//                 JarAutoAssembly closestPiece = null;
//                 foreach (var piece in allPieces)
//                 {
//                     if (piece == null) continue;

//                     // Use collider bounds for a more accurate check
//                     Collider pieceCollider = piece.GetComponent<Collider>();
//                     Vector3 closestPoint = (pieceCollider != null) ? pieceCollider.ClosestPoint(hit.point) : piece.transform.position;
//                     float dist = Vector3.Distance(hit.point, closestPoint);

//                     if (dist < minDistance)
//                     {
//                         minDistance = dist;
//                         closestPiece = piece;
//                     }
//                 }

//                 if (closestPiece == this)
//                 {
//                     Debug.Log($"Closest piece to click on assembled jar is {pieceType}");
//                     return true;
//                 }
//                 return false;
//             }
//         }

//         return hit.transform == transform;
//     }

//     void HandleMouseDown(Vector2 screenPos)
//     {
//         ObjectCloseUpManager closeUpManager = FindObjectOfType<ObjectCloseUpManager>();
//         if (closeUpManager != null && closeUpManager.IsTransitioning)
//         {
//             Debug.Log($"⏳ Close-up transition active - ignoring click on {pieceType}");
//             return;
//         }

//         // Check if this object was clicked
//         if (!IsClickedOn(screenPos, out _)) return;

//         awaitingReassemblyDecision = false;
//         reassemblyHoldTriggered = false;

//         if (isAssembled)
//         {
//             // Check if this is part of a partial assembly (multiple pieces assembled)
//             List<JarAutoAssembly> assembledPieces = GetAssembledPieces();

//             if (assembledPieces.Count > 1 && assembledPieces.Count < allPieces.Count)
//             {
//                 Debug.Log($"🔗 Partial assembly piece {pieceType} clicked - will rotate as GROUP with {assembledPieces.Count} pieces");
//                 Debug.Log($"🔒 Individual piece rotation blocked - only group rotation allowed");
//             }
//             else if (assembledPieces.Count == allPieces.Count)
//             {
//                 Debug.Log($"🏺 Fully assembled jar piece {pieceType} clicked");
//             }
//             else
//             {
//                 Debug.Log($"🧩 Single assembled piece {pieceType} clicked");
//             }

//             if (!allowReassemblyAfterCompletion && jarFullyAssembled)
//             {
//                 Debug.Log($"🚫 Reassembly disabled - ignoring hold on {pieceType}");
//                 return;
//             }

//             awaitingReassemblyDecision = true;
//             reassemblyHoldTriggered = false;
//             isHoldingForDrag = true;
//             holdStartTime = Time.time;
//             reassemblyHoldStartScreenPos = screenPos;

//             Debug.Log($"Hold {pieceType} for {holdTimeForDrag}s to disassemble. Release quickly to inspect.");
//             return;
//         }

//         // For unassembled pieces: Start hold timer
//         isHoldingForDrag = true;
//         holdStartTime = Time.time;
//         Debug.Log($"Started hold timer for {pieceType} - hold for {holdTimeForDrag}s to drag, release quickly to inspect");

//         // IMPORTANT: Prevent auto-inspection during hold timer
//         // The inspection system should NOT automatically trigger until we decide
//     }

//     void HandleMouseDrag(Vector2 screenPos)
//     {
//         if (awaitingReassemblyDecision && isAssembled)
//         {
//             if (Vector2.Distance(screenPos, reassemblyHoldStartScreenPos) >= 20f)
//             {
//                 awaitingReassemblyDecision = false;
//                 isHoldingForDrag = false;
//                 reassemblyHoldTriggered = false;
//                 Debug.Log($"Drag detected on assembled {pieceType} - canceling reassembly hold");
//                 return;
//             }
//         }

//         // Only handle drag for this specific object if it's being held or dragged
//         if (!isHoldingForDrag && !isDragging) return;

//         // Handle dragging in two cases:
//         // 1. Already in drag mode (dragging active)
//         // 2. Holding for drag (will become active when timer completes)
//         if (isAssembled || mainCamera == null) return;

//         Ray ray = mainCamera.ScreenPointToRay(screenPos);
//         Plane dragPlane = new Plane(Vector3.up, new Vector3(0, transform.position.y, 0));

//         float distance;
//         if (dragPlane.Raycast(ray, out distance))
//         {
//             Vector3 worldPoint = ray.GetPoint(distance);
//             targetPosition = worldPoint;

//             if (isDragging)
//             {
//                 Debug.Log($"Dragging {pieceType} to position: {worldPoint:F2}");
//             }
//         }
//     }

//     void HandleMouseUp(Vector2 screenPos)
//     {
//         if (awaitingReassemblyDecision)
//         {
//             float holdDuration = Time.time - holdStartTime;
//             awaitingReassemblyDecision = false;
//             isHoldingForDrag = false;

//             if (reassemblyHoldTriggered)
//             {
//                 return;
//             }

//             if (holdDuration < holdTimeForDrag)
//             {
//                 if (jarFullyAssembled)
//                 {
//                     Debug.Log($"Short click on fully assembled {pieceType} - rotation handled by assembled jar root");
//                 }
//                 else if (inspectableComponent == null || inspectableComponent.CanBeInspected())
//                 {
//                     Debug.Log($"Short click detected ({holdDuration:F1}s) on assembled {pieceType} - triggering inspection");
//                     TryInspection();
//                 }
//                 else
//                 {
//                     Debug.Log($"Inspection disabled for assembled {pieceType} - ignoring short click");
//                 }
//             }

//             reassemblyHoldTriggered = false;

//             return;
//         }

//         // Only handle mouse up for this specific object if it was being held or dragged
//         if (!isHoldingForDrag && !isDragging) return;

//         if (isHoldingForDrag)
//         {
//             // Check if it was a short click (< holdTimeForDrag) or long hold
//             float holdDuration = Time.time - holdStartTime;
//             isHoldingForDrag = false;

//             if (holdDuration < holdTimeForDrag && !isDragging)
//             {
//                 // Short click = Inspection
//                 Debug.Log($"Short click detected ({holdDuration:F1}s) - triggering inspection");
//                 TryInspection();
//             }
//             else if (isDragging)
//             {
//                 // Long hold = Assembly attempt
//                 Debug.Log($"Long hold completed ({holdDuration:F1}s) - trying assembly");
//                 isDragging = false;
//                 TryAssemble();
//             }
//         }
//         else if (isDragging)
//         {
//             // Mouse up during drag - try assembly
//             isDragging = false;
//             TryAssemble();
//         }

//         // IMPORTANT: If we were dragging and now stopped, resume any paused rotation
//         if (!isDragging && !isHoldingForDrag)
//         {
//             ResumeInspectionRotation();
//         }

//         // NEW: Check for inspection mode assembly if we were being dragged during inspection
//         if (!isDragging && !isHoldingForDrag)
//         {
//             TryInspectionAssembly();
//         }
//     }

//     /// <summary>
//     /// Cancel hold operation (called when mouse exits collider during hold)
//     /// </summary>
//     void OnMouseExit()
//     {
//         if (isHoldingForDrag && !isDragging)
//         {
//             Debug.Log($"Mouse exited {pieceType} during hold - canceling timer");
//             isHoldingForDrag = false;

//             // Hide outline and resume any paused rotation since we're canceling the drag operation
//             HideInspectedObjectOutline();
//             ResumeInspectionRotation();
//         }
//     }

//     /// <summary>
//     /// Start drag mode after hold threshold is reached
//     /// </summary>
//     void StartDragMode()
//     {
//         isHoldingForDrag = false; // Stop holding timer
//         isDragging = true;
//         targetPosition = transform.position; // Start from current position

//         // IMPORTANT: Pause rotation on ANY object currently in inspection
//         // This prevents the inspected object from rotating while we drag another object
//         ObjectCloseUpManager closeUpManager = FindObjectOfType<ObjectCloseUpManager>();
//         if (closeUpManager != null)
//         {
//             if (closeUpManager.HasObjectInCloseUp && closeUpManager.CurrentCloseUpObject == transform)
//             {
//                 // If THIS object is in inspection, exit it to start dragging
//                 Debug.Log($"Exiting inspection mode to start dragging {pieceType}");
//                 closeUpManager.ExitCloseUp(true);
//             }
//             else if (closeUpManager.HasObjectInCloseUp)
//             {
//                 // If ANOTHER object is in inspection, pause its rotation but keep it in inspection
//                 Debug.Log($"Pausing rotation on inspected object while dragging {pieceType}");
//                 closeUpManager.PauseRotation();
//             }
//         }

//         Debug.Log($"Drag mode started for {pieceType} - now drag mouse to move piece for assembly");
//     }

//     void Update()
//     {
//         if (isHoldingForDrag && !isDragging && !isAssembled)
//         {
//             float holdDuration = Time.time - holdStartTime;

//             if (holdDuration >= holdTimeForDrag)
//             {
//                 Debug.Log($"🔥 HOLD TIME REACHED ({holdDuration:F1}s) - STARTING DRAG MODE for {pieceType} 🔥");
//                 StartDragMode();
//             }
//             else
//             {
//                 if (Mathf.FloorToInt(holdDuration * 2) != Mathf.FloorToInt((holdDuration - Time.deltaTime) * 2))
//                 {
//                     float remaining = holdTimeForDrag - holdDuration;
//                     Debug.Log($"⏱️ Holding {pieceType} - {remaining:F1}s remaining for drag mode");
//                 }
//             }
//         }

//         if (awaitingReassemblyDecision && isAssembled && !reassemblyHoldTriggered)
//         {
//             float holdDuration = Time.time - holdStartTime;

//             if (holdDuration >= holdTimeForDrag)
//             {
//                 Debug.Log($"♻️ HOLD TIME REACHED ({holdDuration:F1}s) - Triggering disassembly of last piece.");
//                 awaitingReassemblyDecision = false;
//                 isHoldingForDrag = false;
//                 reassemblyHoldTriggered = true;

//                 // Pause rotation if the clicked object is being inspected
//                 ObjectCloseUpManager closeUpManager = FindObjectOfType<ObjectCloseUpManager>();
//                 if (closeUpManager != null && closeUpManager.CurrentCloseUpObject == transform)
//                 {
//                     closeUpManager.PauseRotation();
//                 }

//                 DisassembleLastPiece();
//             }
//             else if (Mathf.FloorToInt(holdDuration * 2) != Mathf.FloorToInt((holdDuration - Time.deltaTime) * 2))
//             {
//                 float remaining = holdTimeForDrag - holdDuration;
//                 Debug.Log($"⏱️ Holding assembled {pieceType} - {remaining:F1}s remaining to disassemble");
//             }
//         }

//         if (isDragging && !isAssembled)
//         {
//             transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * dragSpeed);
//             CheckOutlineProximity();
//         }

//         if (JarAutoAssembly.currentPartialAssemblyParent != null)
//         {
//             ObjectCloseUpManager closeUpManager = FindObjectOfType<ObjectCloseUpManager>();
//             if (closeUpManager == null || !closeUpManager.HasObjectInCloseUp)
//             {
//                 // If there's no close up object at all, we can safely clean up.
//                 CleanupTemporaryAssemblyParent();
//             }
//             else
//             {
//                 // Check if the current close up object is another jar piece.
//                 bool isInspectingAnotherJarPiece = closeUpManager.CurrentCloseUpObject.GetComponent<JarAutoAssembly>() != null;

//                 // Only clean up if the current object is NOT the parent AND it's NOT another jar piece.
//                 // This allows switching inspection to another piece for assembly without destroying the parent.
//                 if (closeUpManager.CurrentCloseUpObject != JarAutoAssembly.currentPartialAssemblyParent.transform && !isInspectingAnotherJarPiece)
//                 {
//                     CleanupTemporaryAssemblyParent();
//                 }
//             }
//         }
//     }
// }
