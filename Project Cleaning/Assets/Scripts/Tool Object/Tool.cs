using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tool : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SurfaceDetection surfaceDetection;
    [SerializeField] private DraggableObject drag;


    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float rotateSpeed = 10f;

    [SerializeField] private Transform initialPosition;
    private Quaternion initialRotation;

    private void Awake()
    {
        if (surfaceDetection == null)
            Debug.LogWarning("[Tool] SurfaceDetection belum di-assign!");

        initialRotation = transform.rotation;
    }

    private void Update()
    {
        if (surfaceDetection == null)
            return;

        if (!TouchManager.Instance.isClickedOn && !TouchManager.Instance.isInteracting)
        {
            ReturnToInitial();
            return;
        }

        if (surfaceDetection.IsSurfaceDetected && drag.isDragging)
            StickToSurface();
        else
            ReturnToInitial();
    }

    private void StickToSurface()
    {
        //Debug.Log("[surface] stick to ");
        Vector3 targetPos = surfaceDetection.RaycastTipPos;
        Vector3 targetNormal = surfaceDetection.RaycastTipNormal;
        //NOTE PENTING! kalo mau ubah ke koordinat X, Y ubah ke Vector3.up!
        Quaternion targetRot = Quaternion.LookRotation(-targetNormal, Vector3.forward);
        transform.SetPositionAndRotation(
            Vector3.Lerp(transform.position, targetPos, Time.deltaTime * moveSpeed),
            Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotateSpeed)
        );
    }

    private void ReturnToInitial()
    {
        //Debug.Log("[surface] initial to ");
        transform.position = Vector3.Lerp(transform.position, initialPosition.position, Time.deltaTime * moveSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, initialRotation, Time.deltaTime * rotateSpeed);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = surfaceDetection != null && surfaceDetection.IsSurfaceDetected
            ? Color.green
            : Color.red;

        Gizmos.DrawSphere(transform.position, 0.03f);
    }
}
