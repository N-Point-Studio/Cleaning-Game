using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ToolBase : MonoBehaviour
{
    protected Camera mainCamera;
    protected bool isDragging;

    protected virtual void Awake()
    {
        mainCamera = Camera.main;
    }
    public virtual void OnToolActivate() { }
    public virtual void OnToolDeactivate() { }
    public virtual void OnToolDragStart(Vector2 screenPos) { }
    public virtual void OnToolDragging(Vector2 screenPos) { }
    public virtual void OnToolDragEnd(Vector2 screenPos) { }
    public virtual void OnToolTap(Vector2 screenPos) { }

    protected Vector3 GetWorldPoint(Vector2 screenPos, float distance)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        return ray.GetPoint(distance);
    }
}
