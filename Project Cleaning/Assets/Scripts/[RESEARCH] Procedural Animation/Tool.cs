using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tool : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SurfaceDetection surfaceDetection;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float rotateSpeed = 10f;

    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private void Awake()
    {
        if (surfaceDetection == null)
            Debug.LogWarning("[Tool] SurfaceDetection belum di-assign!");

        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    private void Update()
    {
        if (surfaceDetection == null)
            return;

        if (surfaceDetection.IsSurfaceDetected)
            StickToSurface();
        else
            ReturnToInitial();
    }

    private void StickToSurface()
    {
        Vector3 targetPos = surfaceDetection.RaycastTipPos;
        Vector3 targetNormal = surfaceDetection.RaycastTipNormal;
        Quaternion targetRot = Quaternion.LookRotation(-targetNormal, Vector3.up);
        transform.SetPositionAndRotation(
            Vector3.Lerp(transform.position, targetPos, Time.deltaTime * moveSpeed),
            Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotateSpeed)
        );
    }

    private void ReturnToInitial()
    {
        transform.position = Vector3.Lerp(transform.position, initialPosition, Time.deltaTime * moveSpeed);
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
