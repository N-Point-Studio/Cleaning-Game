// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// public class ToolManager : MonoBehaviour
// {
//     [SerializeField] private ToolBase activeTool;

//     public void SetActiveTool(ToolBase tool)
//     {
//         if (activeTool == tool) return;

//         if (activeTool != null)
//             activeTool.OnToolDeactivate();

//         activeTool = tool;

//         if (activeTool != null)
//             activeTool.OnToolActivate();
//     }

//     private void Update()
//     {
//         if (activeTool == null || Input.touchCount == 0)
//             return;

//         HandleTouchInput();
//     }

//     private void HandleTouchInput()
//     {
//         Touch touch = Input.GetTouch(0);
//         Vector2 pos = touch.position;

//         switch (touch.phase)
//         {
//             case TouchPhase.Began:
//                 activeTool.OnToolDragStart(pos);
//                 break;

//             case TouchPhase.Moved:
//             case TouchPhase.Stationary:
//                 activeTool.OnToolDragging(pos);
//                 break;

//             case TouchPhase.Ended:
//             case TouchPhase.Canceled:
//                 activeTool.OnToolDragEnd(pos);
//                 break;
//         }
//     }
// }


