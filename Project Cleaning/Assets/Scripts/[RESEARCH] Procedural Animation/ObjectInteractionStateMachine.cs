using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

[ExecuteAlways]
public class ObjectInteractionState : ObjectInteractionBase
{
    [Header("Constraints")]
    [SerializeField] private TwoBoneIKConstraint IKConstraint;

    [Header("References")]
    [SerializeField] private Transform rootTransform;
    [SerializeField] private Transform currentPointTransform;
    [SerializeField] private float offsetDistance = 0.05f;

    public Vector3 RaycastTipPos { get; private set; }
    public Vector3 RaycastTipNormal { get; private set; }
    public Collider CurrentIntersectingCollider { get; private set; }
    public Vector3 ClosestPointOnCollider { get; private set; } = Vector3.positiveInfinity;
    public Transform CurrentIkTargetTransform { get; private set; }
    public Transform CurrentIkHintTransform { get; private set; }

    private float pointHeight;
    private int dirtsLayer;

    private void OnDrawGizmos()
    {
        if (CurrentIntersectingCollider != null && ClosestPointOnCollider != Vector3.positiveInfinity)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(ClosestPointOnCollider, 0.03f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(currentPointTransform.position, ClosestPointOnCollider);
        }
    }

    private void Awake()
    {
        dirtsLayer = LayerMask.NameToLayer("Dirts");
        pointHeight = ClosestPointOnCollider.y;
    }

    //pindahin
    private void Update()
    {
        RaycastHit hit;

        if (Physics.Raycast(currentPointTransform.position, currentPointTransform.forward, out hit, 10))
        {
            RaycastTipPos = hit.point;
            RaycastTipNormal = hit.normal;
        }
    }

    private void SetClosestPoint()
    {
        if (CurrentIntersectingCollider == null || currentPointTransform == null)
            return;

        // ClosestPointOnCollider = CurrentIntersectingCollider.ClosestPoint(currentPointTransform.position);

        ClosestPointOnCollider = GetClosestPointOnCollider(CurrentIntersectingCollider, new Vector3(currentPointTransform.position.x, currentPointTransform.position.y, currentPointTransform.position.z));
        // Vector3 rayDir = currentPointTransform.position - ClosestPointOnCollider;
        // Vector3 normalizeRayDir = rayDir.normalized;
        // Vector3 offset = normalizeRayDir * offsetDistance;

        // Vector3 offsetPosition = ClosestPointOnCollider + offset;

        // CurrentIkTargetTransform.position = offsetPosition;

        // Vector3 rayDir = (currentPointTransform.position - ClosestPointOnCollider).normalized;
        // currentPointTransform.position = ClosestPointOnCollider + rayDir * offsetDistance;
    }

    // private void SetCurrentIKTarget()
    // {
    //     CurrentIkTargetTransform = IKConstraint.data.target.transform;
    //     CurrentIkHintTransform = IKConstraint.data.hint.transform;
    // }

    private Vector3 GetClosestPointOnCollider(Collider intersectingCollider, Vector3 positionToCheck)
    {
        return intersectingCollider.ClosestPoint(positionToCheck);
    }

    public override void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.layer == dirtsLayer)
        {
            CurrentIntersectingCollider = collider;
            // SetCurrentIKTarget();
            SetClosestPoint();
        }
    }

    public override void OnTriggerStay(Collider collider)
    {
        if (collider == CurrentIntersectingCollider)
        {
            SetClosestPoint();
        }
    }

    public override void OnTriggerExit(Collider collider)
    {
        if (collider == CurrentIntersectingCollider)
        {
            CurrentIntersectingCollider = null;
            ClosestPointOnCollider = Vector3.positiveInfinity;
        }
    }
}
