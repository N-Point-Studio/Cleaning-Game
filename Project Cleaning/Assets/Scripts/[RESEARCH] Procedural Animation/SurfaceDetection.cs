using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SurfaceDetection : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform rootTransform;
    [SerializeField] private Transform currentPointTransform;

    [Header("Raycast Settings")]
    [SerializeField] private float rayLength = 10f;
    [SerializeField] private float offsetDistance = 0.05f;
    [SerializeField] private LayerMask dirtsLayerMask;

    public Vector3 RaycastTipPos { get; private set; }
    public Vector3 RaycastTipNormal { get; private set; }
    public bool IsSurfaceDetected { get; private set; }

    private void Update()
    {
        PerformRaycast();
    }

    private void PerformRaycast()
    {
        if (currentPointTransform == null)
            return;

        RaycastHit hit;
        if (Physics.Raycast(currentPointTransform.position, currentPointTransform.forward, out hit, rayLength, dirtsLayerMask))
        {
            IsSurfaceDetected = true;
            RaycastTipPos = hit.point;
            RaycastTipNormal = hit.normal;
        }
        else
        {
            IsSurfaceDetected = false;
            RaycastTipPos = Vector3.positiveInfinity;
        }
    }

    private void OnDrawGizmos()
    {
        if (currentPointTransform == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(currentPointTransform.position, currentPointTransform.forward * rayLength);

        if (IsSurfaceDetected && RaycastTipPos != Vector3.positiveInfinity)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(RaycastTipPos, 0.05f);

            Gizmos.color = Color.red;
            Gizmos.DrawRay(RaycastTipPos, RaycastTipNormal * 0.3f);

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(currentPointTransform.position, RaycastTipPos);
        }
    }
}
