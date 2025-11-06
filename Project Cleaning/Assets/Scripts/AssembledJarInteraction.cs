// using UnityEngine;
// using UnityEngine.InputSystem;

// [System.Serializable]
// public class AssembledJarInteraction : MonoBehaviour
// {
//     [Header("Debug")]
//     public bool enableDebugLogs = true;

//     void Start()
//     {
//         // Original behavior restored - no input variables needed
//     }

//     void OnEnable()
//     {
//         TouchManager.OnMouseDown += HandleMouseDown;
//     }

//     void OnDisable()
//     {
//         TouchManager.OnMouseDown -= HandleMouseDown;
//     }

//     bool IsClickedOn(Vector2 screenPos)
//     {
//         Camera camera = Camera.main;
//         if (camera == null) return false;

//         Ray ray = camera.ScreenPointToRay(screenPos);
//         if (Physics.Raycast(ray, out RaycastHit hit))
//         {
//             return hit.transform == transform;
//         }
//         return false;
//     }

//     void HandleMouseDown(Vector2 screenPos)
//     {
//         // Check if this object was clicked
//         if (!IsClickedOn(screenPos)) return;

//         // Only respond to clicks when the jar is fully assembled
//         if (!JarAutoAssembly.IsJarFullyAssembled)
//         {
//             if (enableDebugLogs)
//                 Debug.Log("🚫 Assembled jar clicked but jar is not fully assembled yet - ignoring click");
//             return;
//         }

//         if (enableDebugLogs)
//             Debug.Log("🎯 Assembled jar clicked - enabling immediate rotation at current position");

//         EnableImmediateRotation();
//     }

//     void EnableImmediateRotation()
//     {
//         if (enableDebugLogs)
//             Debug.Log("🔄 Assembled jar - enabling rotation in place (no close-up needed)");

//         // FIXED: Add null checks to prevent MissingReferenceException
//         if (transform == null)
//         {
//             Debug.LogWarning("⚠️ Transform is null - cannot enable rotation");
//             return;
//         }

//         if (gameObject == null)
//         {
//             Debug.LogWarning("⚠️ GameObject has been destroyed - cannot enable rotation");
//             return;
//         }

//         ObjectCloseUpManager closeUpManager = FindObjectOfType<ObjectCloseUpManager>();
//         if (closeUpManager == null)
//         {
//             Debug.LogWarning("⚠️ No ObjectCloseUpManager found in scene");
//             return;
//         }

//         // Set up rotation-only mode WITHOUT bringing to close-up
//         // The jar rotates at its current assembled position
//         closeUpManager.SetCurrentObject(transform);

//         // Enable rotation at current position (no movement)
//         var smoothRotator = FindObjectOfType<SmoothObjectRotator>();
//         if (smoothRotator != null)
//         {
//             // Start rotation at the jar's current assembled position
//             smoothRotator.UpdateFixedPosition(transform.position);
//             smoothRotator.StartRotating(transform);

//             if (enableDebugLogs)
//                 Debug.Log("✅ Assembled jar rotation enabled at position: " + transform.position);
//         }
//         else
//         {
//             Debug.LogWarning("⚠️ No SmoothObjectRotator found in scene");
//         }

//         // Start inspection for visual feedback (but no position changes)
//         var assembledInspectable = GetComponent<IInspectable>();
//         if (assembledInspectable != null)
//         {
//             assembledInspectable.OnInspectionStart();
//             if (enableDebugLogs)
//                 Debug.Log("✅ Visual feedback enabled for assembled jar rotation");
//         }
//         else
//         {
//             Debug.LogWarning("⚠️ No IInspectable component found on assembled jar");
//         }
//     }

//     void OnDrawGizmosSelected()
//     {
//         // Visual indicator that this object handles assembled jar interaction
//         Gizmos.color = Color.cyan;
//         Gizmos.DrawWireCube(transform.position, transform.localScale * 1.1f);

//         if (JarAutoAssembly.IsJarFullyAssembled)
//         {
//             Gizmos.color = Color.green;
//             Gizmos.DrawWireSphere(transform.position, 0.5f);
//         }
//     }
// }