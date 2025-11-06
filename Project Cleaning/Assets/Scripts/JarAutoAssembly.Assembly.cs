// using System.Collections.Generic;
// using UnityEngine;
// using DG.Tweening;

// public partial class JarAutoAssembly : MonoBehaviour
// {
//     void TryAssemble()
//     {
//         Vector3 correctPos = GetCorrectPosition();
//         Vector3 currentPos = transform.position;

//         Vector3 currentPlanar = new Vector3(currentPos.x, 0f, currentPos.z);
//         Vector3 targetPlanar = new Vector3(correctPos.x, 0f, correctPos.z);

//         float planarDistance = Vector3.Distance(currentPlanar, targetPlanar);
//         float verticalOffset = Mathf.Abs(currentPos.y - correctPos.y);

//         Debug.Log($"{pieceType} planar distance: {planarDistance:F2}, vertical offset: {verticalOffset:F2}");

//         // Hide any outline that might be showing and resume rotation
//         HideInspectedObjectOutline();
//         ResumeInspectionRotation();

//         if (planarDistance < snapDistance)
//         {
//             AssembleToCorrectPosition();
//         }
//         else
//         {
//             Debug.Log($"{pieceType} too far - returning to original position");
//             ReturnToOriginalPosition();
//         }
//     }

//     /// <summary>
//     /// Try to assemble pieces during inspection mode
//     /// </summary>
// }
