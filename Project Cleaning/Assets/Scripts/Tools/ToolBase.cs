using UnityEngine;

[RequireComponent(typeof(ToolInputReader))]
public abstract class ToolBase : MonoBehaviour
{
    protected Camera mainCamera;
    protected ToolInputReader inputHandler;
    protected bool isDragging;

    [Header("Settings")]
    [SerializeField] protected float dragDistance = 2f;
    [SerializeField] protected float moveSmoothness = 10f;
    [SerializeField] protected float returnSmoothness = 5f;
    [SerializeField] protected LayerMask draggableLayer;

    protected Transform targetObject;
    protected Vector3 dragOffset;
    protected Vector3 initialPosition;
    protected Quaternion initialRotation;
    protected bool isReturning;

    protected virtual void Awake()
    {
        mainCamera = Camera.main;
        inputHandler = GetComponent<ToolInputReader>();
    }

    protected virtual void OnEnable()
    {
        inputHandler.OnTouchStart += OnToolDragStart;
        inputHandler.OnTouchMove += OnToolDragging;
        inputHandler.OnTouchEnd += OnToolDragEnd;
        inputHandler.OnTouchTap += OnToolTap;
    }

    protected virtual void OnDisable()
    {
        inputHandler.OnTouchStart -= OnToolDragStart;
        inputHandler.OnTouchMove -= OnToolDragging;
        inputHandler.OnTouchEnd -= OnToolDragEnd;
        inputHandler.OnTouchTap -= OnToolTap;
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

    public virtual void OnToolActivate() { }
    public virtual void OnToolDeactivate() { }
    public abstract void OnToolDragStart(Vector2 screenPos);
    public abstract void OnToolDragging(Vector2 screenPos);
    public abstract void OnToolDragEnd(Vector2 screenPos);
    public abstract void OnToolTap(Vector2 screenPos);
}
