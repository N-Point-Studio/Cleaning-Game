// using UnityEngine;

// [RequireComponent(typeof(FragmentController))]
// public class FragmentDraggable : MonoBehaviour
// {
//     private Camera cam;
//     private FragmentController fc;
//     private Vector3 offset;
//     private float zDepth;

//     private void Start()
//     {
//         cam = Camera.main;
//         fc = GetComponent<FragmentController>();
//     }

//     private void Update()
//     {
//         if (TouchManager.Instance == null) return;
//         if (!TouchManager.Instance.isInteracting)
//         {
//             Ray ray = cam.ScreenPointToRay(TouchManager.Instance.curScreenPos);
//             if (Physics.Raycast(ray, out RaycastHit hit))
//             {
//                 if (hit.transform == transform)
//                 {
//                     Debug.Log("ADA KOCAK");
//                     // isDragging = true;
//                     // isReturning = false;
//                     TouchManager.Instance.TouchUsed(true);
//                     OnMouseDown();
//                     // if (surface != null) surface.isUsed = true;
//                 }
//             }
//         }
//     }

//     private void OnMouseDown()
//     {
//         Debug.Log("On Mouse Down");
//         // Jika belum di-inspect → kita inspect dulu
//         if (!fc.isAttached)
//         {
//             AssembleManager.Instance.Inspect(fc);
//         }

//         // Hitung offset posisi untuk drag
//         zDepth = cam.WorldToScreenPoint(transform.position).z;
//         offset = transform.position - GetMouseWorldPosition();
//     }

//     private void OnMouseDrag()
//     {
//         if (!fc.isAttached) // hanya fragment yang belum terpasang yang bisa di-drag
//             transform.position = GetMouseWorldPosition() + offset;
//     }

//     private void OnMouseUp()
//     {
//         // Saat mouse dilepas → cek apakah bisa attach
//         AssembleManager.Instance.TryAttach(fc);
//     }

//     Vector3 GetMouseWorldPosition()
//     {
//         Vector3 mousePoint = Input.mousePosition;
//         mousePoint.z = zDepth;
//         return cam.ScreenToWorldPoint(mousePoint);
//     }
// }
