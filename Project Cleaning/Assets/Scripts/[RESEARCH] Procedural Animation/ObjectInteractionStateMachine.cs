using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class ObjectInteractionState : MonoBehaviour
{
    public Collider CurrentIntersectingCollider { get; private set; }
    public Vector3 ClosestPointOnCollider { get; private set; } = Vector3.positiveInfinity;

    [Header("References")]
    [SerializeField] private Transform rootTransform;
    [SerializeField] private Transform currentPointTransform;

    [SerializeField] private float offsetDistance = 0.05f;

    public Vector3 RaycastTipPos { get; private set; }
    public Vector3 RaycastTipNormal { get; private set; }
    private float pointHeight;

    private int dirtsLayer;

    private void Awake()
    {
        dirtsLayer = LayerMask.NameToLayer("Dirts");
        pointHeight = ClosestPointOnCollider.y;
    }

    private void Update()
    {
        RaycastHit hit;

        if (Physics.Raycast(currentPointTransform.position, currentPointTransform.forward, out hit, 10))
        {
            RaycastTipPos = hit.point;
            RaycastTipNormal = hit.normal;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == dirtsLayer)
        {
            CurrentIntersectingCollider = other;
            SetClosestPoint();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other == CurrentIntersectingCollider)
        {
            SetClosestPoint();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other == CurrentIntersectingCollider)
        {
            CurrentIntersectingCollider = null;
            ClosestPointOnCollider = Vector3.positiveInfinity;
        }
    }


    private void SetClosestPoint()
    {
        if (CurrentIntersectingCollider == null || currentPointTransform == null)
            return;

        // ClosestPointOnCollider = CurrentIntersectingCollider.ClosestPoint(currentPointTransform.position);
        ClosestPointOnCollider = GetClosestPointOnCollider(CurrentIntersectingCollider, new Vector3(currentPointTransform.position.x, currentPointTransform.position.y, currentPointTransform.position.z));

        // Vector3 rayDir = (currentPointTransform.position - ClosestPointOnCollider).normalized;
        // currentPointTransform.position = ClosestPointOnCollider + rayDir * offsetDistance;
    }

    private Vector3 GetClosestPointOnCollider(Collider intersectingCollider, Vector3 positionToCheck)
    {
        return intersectingCollider.ClosestPoint(positionToCheck);
    }

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
}
