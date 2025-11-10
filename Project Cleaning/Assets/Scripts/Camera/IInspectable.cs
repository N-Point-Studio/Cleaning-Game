using UnityEngine;

/// <summary>
/// Interface for objects that can be brought to camera for inspection
/// </summary>
public interface IInspectable
{
    /// <summary>
    /// Whether this object can currently be inspected
    /// </summary>
    bool CanBeInspected();

    /// <summary>
    /// The ideal scale multiplier when inspecting this object
    /// (1.0 = normal size, 2.0 = double size, etc.)
    /// </summary>
    float GetIdealInspectionScale();

    /// <summary>
    /// Called when inspection starts
    /// </summary>
    void OnInspectionStart();

    /// <summary>
    /// Called when inspection ends
    /// </summary>
    void OnInspectionEnd();

    /// <summary>
    /// Get the transform that should be moved for inspection
    /// (usually the object itself, but could be a parent)
    /// </summary>
    Transform GetInspectionTarget();
}


// private void Update()
//     {
//         if (TouchManager.Instance.isInteracting) return;
//         if (!TouchManager.Instance.isClickedOn)
//         {
//             isRotating = false;
//             TouchManager.Instance.IsRotate(false);

//             fingerOnObject = false;
//             return;
//         }

//         Vector2 curPos = TouchManager.Instance.curScreenPos;
//         Ray ray = cam.ScreenPointToRay(curPos);

//         if (!fingerOnObject)
//         {
//             if (Physics.Raycast(ray, out RaycastHit hit) && hit.transform == transform)
//             {
//                 fingerOnObject = true;
//                 previousX = curPos.x;
//                 previousZ = curPos.y;
//                 return;
//             }
//             return;
//         }
//         float moveDist = Vector2.Distance(new Vector2(previousX, previousZ), curPos);
//         if (moveDist > dragThreshold)
//         {
//             isRotating = true;
//             TouchManager.Instance.IsRotate(true);

//         }

//         if (isRotating)
//         {
//             RotateObject(curPos);
//         }
//     }

//     private void RotateObject(Vector2 touchPos)
//     {
//         float deltaX = -(touchPos.y - previousZ) * rotationRate;
//         float deltaY = -(touchPos.x - previousX) * rotationRate;

//         if (!yRotation) deltaX = 0;
//         if (!xRotation) deltaY = 0;
//         if (invertX) deltaY *= -1;
//         if (invertY) deltaX *= -1;

//         transform.Rotate(deltaX, 0, deltaY, Space.World);

//         previousX = touchPos.x;
//         previousZ = touchPos.y;
//     }
