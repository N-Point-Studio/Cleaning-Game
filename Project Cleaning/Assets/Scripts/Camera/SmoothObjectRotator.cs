// using UnityEngine;
// using DG.Tweening;

// /// <summary>
// /// Smooth real-time object rotation like in cleaning games (Undusted style)
// /// Provides fluid, responsive rotation with smooth interpolation
// /// </summary>
// public class SmoothObjectRotator : MonoBehaviour
// {
//     [Header("Rotation Settings")]
//     public float rotationSensitivity = 20f; // Reduced to half for less sensitive rotation
//     public float rotationSmoothing = 8f;
//     public bool invertX = false;
//     public bool invertY = false;

//     [Header("Rotation Limits")]
//     public bool limitVerticalRotation = false;
//     public float minVerticalAngle = -45f;
//     public float maxVerticalAngle = 45f;

//     [Header("Input Detection")]
//     public float mouseSensitivityMultiplier = 1f;
//     public bool useRawInput = true; // Better for smooth rotation

//     // Rotation state
//     private Transform rotatingObject;
//     private bool isRotating = false;
//     private Vector3 lastMousePosition;

//     // Smooth rotation targets
//     private float targetHorizontalRotation = 0f;
//     private float targetVerticalRotation = 0f;
//     private float currentHorizontalRotation = 0f;
//     private float currentVerticalRotation = 0f;

//     // Initial state
//     private Quaternion initialRotation;
//     private Vector3 fixedPosition; // Keep object at fixed position during rotation
//     private bool rotationStarted = false;

//     void Update()
//     {
//         if (isRotating && rotatingObject != null)
//         {
//             HandleSmoothRotation();
//             ApplySmoothRotation();
//         }
//     }

//     /// <summary>
//     /// Start rotating an object with smooth controls
//     /// </summary>
//     public void StartRotating(Transform targetObject)
//     {
//         rotatingObject = targetObject;
//         isRotating = true;
//         rotationStarted = false;

//         // Kill any conflicting DOTween animations
//         if (targetObject != null)
//         {
//             targetObject.DOKill(false);
//             Debug.Log($"Started smooth rotation for: {targetObject.name}");
//         }

//         // Store initial rotation AND position
//         initialRotation = targetObject.rotation;
//         fixedPosition = targetObject.position; // Lock position during rotation

//         // Reset rotation values
//         currentHorizontalRotation = 0f;
//         currentVerticalRotation = 0f;
//         targetHorizontalRotation = 0f;
//         targetVerticalRotation = 0f;

//         // Initialize mouse position
//         lastMousePosition = TouchManager.MousePosition;
//     }

//     /// <summary>
//     /// Stop rotating the current object
//     /// </summary>
//     public void StopRotating()
//     {
//         isRotating = false;
//         rotatingObject = null;
//         rotationStarted = false;
//         Debug.Log("Stopped smooth rotation");
//     }

//     /// <summary>
//     /// Update the fixed position for rotation (prevents position drift)
//     /// </summary>
//     public void UpdateFixedPosition(Vector3 newPosition)
//     {
//         fixedPosition = newPosition;
//         Debug.Log($"Updated fixed rotation position to: {newPosition}");
//     }

//     // TouchManager integration removed - back to original input handling

//     /// <summary>
//     /// Handle smooth rotation input
//     /// </summary>
//     void HandleSmoothRotation()
//     {
//         Vector3 currentMousePosition = TouchManager.MousePosition;

//         if (!rotationStarted)
//         {
//             // Initialize on first frame to avoid jump
//             lastMousePosition = currentMousePosition;
//             rotationStarted = true;
//             return;
//         }

//         // Calculate mouse delta
//         Vector3 mouseDelta = currentMousePosition - lastMousePosition;

//         // Use raw mouse delta for more responsive rotation
//         float mouseX = mouseDelta.x;
//         float mouseY = mouseDelta.y;

//         // Apply sensitivity and inversion (fix direction)
//         if (invertX) mouseX = -mouseX;
//         if (invertY) mouseY = -mouseY;

//         // Apply sensitivity multiplier
//         mouseX *= mouseSensitivityMultiplier;
//         mouseY *= mouseSensitivityMultiplier;

//         // Only rotate if there's significant mouse movement
//         if (Mathf.Abs(mouseX) > 0.1f || Mathf.Abs(mouseY) > 0.1f)
//         {
//             // Calculate rotation deltas - try positive for natural rotation
//             float horizontalDelta = -mouseX * rotationSensitivity * Time.deltaTime;
//             float verticalDelta = mouseY * rotationSensitivity * Time.deltaTime; // Positive: up drag = up rotation

//             // Update target rotation
//             targetHorizontalRotation += horizontalDelta;

//             if (limitVerticalRotation)
//             {
//                 targetVerticalRotation = Mathf.Clamp(
//                     targetVerticalRotation + verticalDelta,
//                     minVerticalAngle,
//                     maxVerticalAngle
//                 );
//             }
//             else
//             {
//                 targetVerticalRotation += verticalDelta;
//             }

//         }

//         // Update last mouse position
//         lastMousePosition = currentMousePosition;
//     }

//     /// <summary>
//     /// Apply smooth interpolated rotation to object
//     /// </summary>
//     void ApplySmoothRotation()
//     {
//         if (rotatingObject == null) return;

//         // Smoothly interpolate to target rotation
//         currentHorizontalRotation = Mathf.LerpAngle(
//             currentHorizontalRotation,
//             targetHorizontalRotation,
//             rotationSmoothing * Time.deltaTime
//         );

//         currentVerticalRotation = Mathf.LerpAngle(
//             currentVerticalRotation,
//             targetVerticalRotation,
//             rotationSmoothing * Time.deltaTime
//         );

//         // Apply rotation relative to initial rotation
//         Quaternion horizontalRotation = Quaternion.AngleAxis(currentHorizontalRotation, Vector3.up);
//         Quaternion verticalRotation = Quaternion.AngleAxis(currentVerticalRotation, Vector3.right);

//         // Combine with initial rotation for natural feel
//         rotatingObject.rotation = initialRotation * horizontalRotation * verticalRotation;

//         // CRITICAL: Keep object at fixed position during rotation
//         rotatingObject.position = fixedPosition;
//     }



//     // Public properties
//     public bool IsRotating => isRotating;
//     private Transform RotatingObject => rotatingObject;
//     private Vector2 CurrentRotation => new Vector2(currentHorizontalRotation, currentVerticalRotation);
//     private Vector2 TargetRotation => new Vector2(targetHorizontalRotation, targetVerticalRotation);

//     void OnDrawGizmosSelected()
//     {
//         if (rotatingObject != null && isRotating)
//         {
//             // Draw rotation axes
//             Gizmos.color = Color.red;
//             Gizmos.DrawRay(rotatingObject.position, rotatingObject.right * 2f);

//             Gizmos.color = Color.green;
//             Gizmos.DrawRay(rotatingObject.position, rotatingObject.up * 2f);

//             Gizmos.color = Color.blue;
//             Gizmos.DrawRay(rotatingObject.position, rotatingObject.forward * 2f);
//         }
//     }
// }
