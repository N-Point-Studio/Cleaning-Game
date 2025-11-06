// using UnityEngine;
// using System;

// /// <summary>
// /// Handles object selection via mouse clicks for close-up inspection
// /// </summary>
// public class ObjectSelectionHandler : MonoBehaviour
// {
//     [Header("Selection Settings")]
//     public bool enableSelection = true;
//     public bool requireInspectableComponent = false;

//     // Runtime settings
//     private LayerMask selectableLayerMask = -1;
//     private ObjectCloseUpManager closeUpManager;
//     private Camera playerCamera;

//     void Start()
//     {
//         playerCamera = Camera.main;
//         if (playerCamera == null)
//             playerCamera = FindObjectOfType<Camera>();
//     }

//     void Update()
//     {
//         // Input is now handled by ObjectCloseUpManager to avoid conflicts
//         // This prevents the selection handler from interfering with rotation
//     }

//     /// <summary>
//     /// Setup selection handler with parameters
//     /// </summary>
//     public void Setup(LayerMask layerMask, ObjectCloseUpManager manager)
//     {
//         selectableLayerMask = layerMask;
//         closeUpManager = manager;

//         Debug.Log("Object selection handler initialized");
//     }


//     /// <summary>
//     /// Try to select an object under the mouse cursor (called by manager)
//     /// </summary>
//     public void TrySelectObjectAtMousePosition()
//     {
//         // This method is kept for backwards compatibility but should use the newer overload
//         TrySelectObjectAtScreenPosition(Vector2.zero); // Will use legacy Input.mousePosition
//     }

//     /// <summary>
//     /// Try to select an object at the specified screen position (new input system)
//     /// </summary>
//     public void TrySelectObjectAtScreenPosition(Vector2 screenPosition)
//     {
//         if (closeUpManager != null && closeUpManager.IsTransitioning)
//         {
//             Debug.Log("⏳ Selection ignored - close-up transition in progress");
//             return;
//         }

//         if (playerCamera == null)
//         {
//             Debug.LogError("ObjectSelectionHandler: playerCamera is null!");
//             return;
//         }

//         // Use provided screen position if valid, otherwise fall back to TouchManager
//         Vector2 useScreenPos = (screenPosition != Vector2.zero) ? screenPosition : TouchManager.MousePosition;

//         if (useScreenPos == Vector2.zero)
//         {
//             Debug.Log("Selection skipped - no valid screen position yet");
//             return;
//         }

//         Ray ray = playerCamera.ScreenPointToRay(useScreenPos);
//         Debug.Log($"Raycast from: {ray.origin} direction: {ray.direction}");
//         Debug.Log($"Screen position: {useScreenPos}");
//         Debug.Log($"Layer mask: {selectableLayerMask} (binary: {Convert.ToString(selectableLayerMask, 2)})");

//         if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, selectableLayerMask))
//         {
//             Transform hitObject = hit.transform;
//             Debug.Log($"Raycast hit object: {hitObject.name} on layer {hitObject.gameObject.layer}");

//             // Check if object can be selected
//             if (CanSelectObject(hitObject))
//             {
//                 Debug.Log($"Object {hitObject.name} can be selected - proceeding with selection");
//                 SelectObject(hitObject);
//             }
//             else
//             {
//                 Debug.Log($"Cannot select object: {hitObject.name}");
//             }
//         }
//         else
//         {
//             Debug.Log("Raycast did not hit any objects in the selectable layer mask");
//         }
//     }

//     /// <summary>
//     /// Try to select an object under the mouse cursor (legacy method)
//     /// </summary>
//     void TrySelectObject()
//     {
//         TrySelectObjectAtMousePosition();
//     }

//     /// <summary>
//     /// Check if an object can be selected for close-up
//     /// </summary>
//     bool CanSelectObject(Transform targetObject)
//     {
//         // Check if object is active
//         if (!targetObject.gameObject.activeInHierarchy)
//             return false;

//         // Check if this is an assembled jar root - if so, handle it specially without close-up
//         if (IsAssembledJarRoot(targetObject))
//         {
//             Debug.Log($"Assembled jar detected - enabling rotation in place without close-up");
//             HandleAssembledJarClick(targetObject);
//             return false; // Prevent normal close-up selection
//         }

//         // Jar pieces can be inspected at any time (before, during, or after assembly)
//         var jarComponent = targetObject.GetComponent<JarAutoAssembly>();
//         if (jarComponent != null)
//         {
//             Debug.Log($"Jar piece {jarComponent.pieceType} can be inspected (assembled: {jarComponent.isAssembled})");
//             // No restriction - jar pieces can always be inspected
//         }

//         // If we require inspectable component, check for it
//         if (requireInspectableComponent)
//         {
//             var inspectable = targetObject.GetComponent<IInspectable>();
//             if (inspectable == null || !inspectable.CanBeInspected())
//                 return false;
//         }

//         // Additional checks can be added here
//         // For example: check if object is too far, too small, etc.

//         return true;
//     }

//     /// <summary>
//     /// Select an object for close-up inspection
//     /// </summary>
//     void SelectObject(Transform targetObject)
//     {
//         Transform selectionTarget = targetObject;
//         var jarPiece = targetObject.GetComponent<JarAutoAssembly>();

//         if (jarPiece != null) // Check if it's a jar piece first
//         {
//             // Use the JarAutoAssembly logic to determine the correct inspection target
//             // This will return the group parent if it's a partial assembly, or the assembledJarRoot if fully assembled, or the piece itself.
//             selectionTarget = jarPiece.EnsureRotationTarget(closeUpManager, false); // Pass false for startRotation as manager will handle it.
//             Debug.Log($"🔁 Jar piece detected. Selection target determined by JarAutoAssembly: {selectionTarget.name}");
//         }

//         Debug.Log($"Selected object for close-up: {selectionTarget.name}");

//         // Notify inspectable component if it exists
//         var inspectable = selectionTarget.GetComponent<IInspectable>();
//         inspectable?.OnInspectionStart();

//         // Tell the close-up manager to bring the object to camera
//         closeUpManager.BringObjectToCloseUp(selectionTarget);
//     }



//     private bool SelectionEnabled => enableSelection;
//     private LayerMask SelectableLayerMask => selectableLayerMask;

//     void OnDrawGizmosSelected()
//     {
//         // Draw selection ray in Scene view
//         if (playerCamera != null)
//         {
//             Gizmos.color = Color.red;
//             Vector3 mouseWorldPos = playerCamera.ScreenToWorldPoint(new Vector3(
//                 TouchManager.MousePosition.x,
//                 TouchManager.MousePosition.y,
//                 playerCamera.nearClipPlane
//             ));

//             Vector3 rayDirection = (mouseWorldPos - playerCamera.transform.position).normalized;
//             Gizmos.DrawRay(playerCamera.transform.position, rayDirection * 10f);
//         }
//     }

//     /// <summary>
//     /// Check if a target object is an assembled jar root
//     /// </summary>
//     bool IsAssembledJarRoot(Transform targetObject)
//     {
//         // Check if any jar piece recognizes this as their assembled jar root
//         JarAutoAssembly[] allJarPieces = FindObjectsOfType<JarAutoAssembly>();
//         foreach (var piece in allJarPieces)
//         {
//             if (piece.IsAssembledJarRoot(targetObject))
//             {
//                 return true;
//             }
//         }
//         return false;
//     }

//     /// <summary>
//     /// Handle assembled jar click without close-up animation
//     /// </summary>
//     void HandleAssembledJarClick(Transform assembledJarRoot)
//     {
//         Debug.Log($"🎯 Assembled jar clicked - enabling rotation in place");

//         // Get the ObjectCloseUpManager to manually set up rotation-only mode
//         if (closeUpManager != null)
//         {
//             // Manually set the assembled jar as current object without animation
//             closeUpManager.SetCurrentObject(assembledJarRoot);
//             Debug.Log($"✅ Set assembled jar as current object for rotation (no movement)");
//         }

//         // Start rotation immediately without any position changes
//         var smoothRotator = FindObjectOfType<SmoothObjectRotator>();
//         if (smoothRotator != null)
//         {
//             // CRITICAL: Store the current position before starting rotation
//             Vector3 currentPos = assembledJarRoot.position;
//             Debug.Log($"🔒 Locking jar position at: {currentPos}");

//             // First update the fixed position to current position
//             smoothRotator.UpdateFixedPosition(currentPos);

//             // Then start rotation (this will also set fixedPosition internally)
//             smoothRotator.StartRotating(assembledJarRoot);

//             // Double-check: Force position lock after starting rotation
//             assembledJarRoot.position = currentPos;

//             Debug.Log($"🔄 Started rotation for assembled jar - position locked at {currentPos}");
//         }

//         Debug.Log($"✅ Assembled jar ready for rotation - position absolutely locked");
//     }

//     /// <summary>
//     /// Debug method to test raycast at current mouse position
//     /// </summary>
//     [ContextMenu("Test Raycast at Mouse")]
//     public void TestRaycastAtMouse()
//     {
//         if (playerCamera == null)
//         {
//             Debug.LogError("No camera found for raycast test!");
//             return;
//         }

//         Ray ray = playerCamera.ScreenPointToRay(TouchManager.MousePosition);
//         Debug.Log($"🔍 RAYCAST TEST:");
//         Debug.Log($"    Mouse Position: {TouchManager.MousePosition}");
//         Debug.Log($"    Ray Origin: {ray.origin}");
//         Debug.Log($"    Ray Direction: {ray.direction}");
//         Debug.Log($"    Layer Mask: {selectableLayerMask} (binary: {System.Convert.ToString(selectableLayerMask, 2)})");

//         // Test raycast with infinite distance and all layers
//         RaycastHit[] allHits = Physics.RaycastAll(ray, Mathf.Infinity);
//         Debug.Log($"    Total objects hit (all layers): {allHits.Length}");

//         foreach (RaycastHit hit in allHits)
//         {
//             int layerMask = 1 << hit.transform.gameObject.layer;
//             bool inSelectable = (selectableLayerMask & layerMask) != 0;
//             Debug.Log($"    Hit: {hit.transform.name} on layer {hit.transform.gameObject.layer} (selectable: {inSelectable})");
//         }

//         // Test with selectable layer mask
//         if (Physics.Raycast(ray, out RaycastHit selectableHit, Mathf.Infinity, selectableLayerMask))
//         {
//             Debug.Log($"✅ Selectable raycast hit: {selectableHit.transform.name}");
//         }
//         else
//         {
//             Debug.Log($"❌ No selectable objects hit with current layer mask");
//         }
//     }
// }
