using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SurfaceDetection : MonoBehaviour
{
    public enum CollisionToolsType
    {
        Texture,
        Mesh,
    }

    [Header("References")]
    [SerializeField] private Transform currentPointTransform;
    [Header("Raycast Settings")]
    [SerializeField] private float rayLength = 10f;
    [SerializeField] private LayerMask dirtsLayerMask;
    [SerializeField] private CollisionToolsType surfaceType = CollisionToolsType.Texture;
    public Vector3 RaycastTipPos { get; private set; }
    public Vector3 RaycastTipNormal { get; private set; }
    public bool IsSurfaceDetected { get; private set; }
    public Clean CleaningSurface { get; private set; }
    public Vector2 TextureSurface { get; private set; }
    public CleanMesh MudObject { get; private set; }

    public bool isUsed = false;

    void Awake()
    {
        CleaningSurface = null;
    }

    private void Update()
    {
        // Debug.Log("is used? " + isUsed);
        if (isUsed) PerformRaycast();
        Debug.Log("[surface] from here: " + IsSurfaceDetected);
    }

    private void PerformRaycast()
    {
        if (currentPointTransform == null)
            return;

        if (!TouchManager.Instance.isClickedOn)
        {
            IsSurfaceDetected = false;
            return;
        }

        RaycastHit hit;
        if (Physics.Raycast(currentPointTransform.position, currentPointTransform.forward, out hit, rayLength, dirtsLayerMask))
        {
            switch (surfaceType)
            {
                case CollisionToolsType.Mesh:
                    var mud = hit.collider.GetComponent<CleanMesh>();
                    EssentialDetecting(hit);
                    MudObject = mud;
                    break;
                case CollisionToolsType.Texture:
                    var clean = hit.collider.GetComponent<Clean>();
                    EssentialDetecting(hit);
                    CleaningSurface = clean;
                    break;
            }
        }
        else
        {
            IsSurfaceDetected = false;
            RaycastTipPos = Vector3.positiveInfinity;
            CleaningSurface = null;
            MudObject = null;
        }
    }

    private void EssentialDetecting(RaycastHit hit)
    {
        IsSurfaceDetected = true;
        RaycastTipPos = hit.point;
        RaycastTipNormal = hit.normal;
        TextureSurface = hit.textureCoord;
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
