// using System;
// using UnityEngine;
// using UnityEngine.InputSystem;

// public class ToolInputReader : MonoBehaviour
// {
//     public event Action<Vector2> OnTouchStart;
//     public event Action<Vector2> OnTouchMove;
//     public event Action<Vector2> OnTouchEnd;
//     public event Action<Vector2> OnTouchTap;

//     private float tapThreshold = 0.2f; // detik
//     private float touchStartTime;
//     private Vector2 startPos;
//     private bool wasPressed = false;

//     private void Update()
//     {
// #if UNITY_EDITOR
//         HandleMouseInput(); // Use TouchManager for editor testing
// #else
//         HandleTouchInput();
// #endif
//     }

//     private void HandleMouseInput()
//     {
//         if (TouchManager.IsPressed && !wasPressed)
//         {
//             startPos = TouchManager.MousePosition;
//             touchStartTime = Time.time;
//             OnTouchStart?.Invoke(TouchManager.MousePosition);
//             wasPressed = true;
//         }
//         else if (TouchManager.IsPressed && wasPressed)
//         {
//             OnTouchMove?.Invoke(TouchManager.MousePosition);
//         }
//         else if (!TouchManager.IsPressed && wasPressed)
//         {
//             OnTouchEnd?.Invoke(TouchManager.MousePosition);

//             if (Time.time - touchStartTime <= tapThreshold)
//                 OnTouchTap?.Invoke(TouchManager.MousePosition);
//             wasPressed = false;
//         }
//     }

//     private void HandleTouchInput()
//     {
//         if (Input.touchCount == 0) return;

//         Touch touch = Input.GetTouch(0);
//         Vector2 pos = touch.position;

//         switch (touch.phase)
//         {
//             case UnityEngine.TouchPhase.Began:
//                 startPos = pos;
//                 touchStartTime = Time.time;
//                 OnTouchStart?.Invoke(pos);
//                 break;

//             case UnityEngine.TouchPhase.Moved:
//             case UnityEngine.TouchPhase.Stationary:
//                 OnTouchMove?.Invoke(pos);
//                 break;

//             case UnityEngine.TouchPhase.Ended:
//             case UnityEngine.TouchPhase.Canceled:
//                 OnTouchEnd?.Invoke(pos);
//                 if (Time.time - touchStartTime <= tapThreshold)
//                     OnTouchTap?.Invoke(pos);
//                 break;
//         }
//     }

//     // Now uses TouchManager with new Input System
// }
