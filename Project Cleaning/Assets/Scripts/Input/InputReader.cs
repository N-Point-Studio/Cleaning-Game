// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.InputSystem;

// public class InputReader : MonoBehaviour, InputSystem.IInputActions
// {
//     public InputSystem inputSystem;
//     public Vector3 curScreenPos;
//     Camera camera;
//     public bool isDragging;

//     private Vector3 WorldPos
//     {
//         get
//         {
//             float z = camera.WorldToScreenPoint(transform.position).z;
//             return camera.ScreenToWorldPoint(curScreenPos + new Vector3(0, 0, z));
//         }
//     }

//     private bool isClickedOn
//     {
//         get
//         {
//             Ray ray = camera.ScreenPointToRay(curScreenPos);
//             RaycastHit hit;
//             if (Physics.Raycast(ray, out hit))
//             {
//                 return hit.transform == transform;
//             }
//             return false;
//         }
//     }

//     void OnEnable()
//     {
//         inputSystem.Input.Enable();
//     }

//     void OnDisable()
//     {
//         inputSystem.Input.Disable();
//     }

//     private void Awake()
//     {
//         camera = Camera.main;
//         inputSystem = new InputSystem();
//         inputSystem.Input.SetCallbacks(this);
//     }

//     public void OnPress(InputAction.CallbackContext context)
//     {
//         if (context.performed)
//         {
//             Debug.Log("Press performed");
//             if (isClickedOn) StartCoroutine(Drag());
//         }
//         else if (context.canceled)
//         {
//             Debug.Log("Press Canceled");
//             isDragging = false;
//         }
//     }

//     public void OnScreenPos(InputAction.CallbackContext context)
//     {
//         if (!context.performed) return;
//         Debug.Log("Screen Pos performed");
//         curScreenPos = context.ReadValue<Vector2>();
//     }

//     private IEnumerator Drag()
//     {
//         isDragging = true;
//         Vector3 offset = transform.position - WorldPos;
//         while (isDragging)
//         {
//             transform.position = WorldPos + offset;
//             yield return null;
//         }
//     }

//     public void OnPrimaryFingerPos(InputAction.CallbackContext context)
//     {
//         throw new System.NotImplementedException();
//     }

//     public void OnSecondaryFingerPos(InputAction.CallbackContext context)
//     {
//         throw new System.NotImplementedException();
//     }

//     public void OnSecondaryTouchContact(InputAction.CallbackContext context)
//     {
//         throw new System.NotImplementedException();
//     }
// }