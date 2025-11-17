using UnityEngine;

public abstract class ToolBase : MonoBehaviour
{
    [SerializeField] protected Camera mainCamera;
    protected bool isDragging;

    [Header("Tool References")]
    [SerializeField] protected Transform tipPoint;
    [SerializeField] protected Transform lookTarget;

    [Header("Tool Settings")]
    [SerializeField] protected float gizmosRange = 10f;
    [SerializeField] protected float rotateSmoothness = 20f;
    [SerializeField] protected float movementSmoothness = 10f;
    [SerializeField] protected float normalDamping = 10f;

    [Header("Settings")]
    [SerializeField] protected float dragDistance = 2f;
    [SerializeField] protected float returnSmoothness = 5f;
    [SerializeField] protected LayerMask draggableLayer;
    protected Transform targetObject;
    protected Vector3 dragOffset;
    protected Vector3 initialPosition;
    protected Quaternion initialRotation;
    protected bool isReturning;
    protected bool isSurfaceDeteced = false;

    protected virtual void Awake()
    {
        mainCamera = Camera.main;
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    protected Vector3 GetWorldPoint(Vector2 screenPos, float distance)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        return ray.GetPoint(distance);
    }

    protected void MoveTarget(Vector3 position, float speed, Quaternion? rotation = null)
    {
        if (targetObject == null) return;

        targetObject.position = Vector3.Lerp(targetObject.position, position, Time.deltaTime * speed);
        if (rotation.HasValue)
            targetObject.rotation = Quaternion.Slerp(targetObject.rotation, rotation.Value, Time.deltaTime * speed);
    }

    protected void ResetDrag()
    {
        isReturning = false;
        isDragging = false;
        targetObject = null;
    }

    public abstract void OnToolDragStart(Vector2 screenPos);
    public abstract void OnToolDragging(Vector2 screenPos);
    public abstract void OnToolDragEnd(Vector2 screenPos);
}
