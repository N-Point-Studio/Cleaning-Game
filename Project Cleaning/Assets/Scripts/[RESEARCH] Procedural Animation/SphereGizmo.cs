using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphereGizmo : MonoBehaviour
{
    public static bool Enabled = true;

    public enum GizmoShape
    {
        Sphere,
        Cube
    }

    [Header("Gizmo Settings")]
    public GizmoShape gizmoType = GizmoShape.Sphere;
    public float size = 0.1f;
    public Color color = Color.red;

    private void OnDrawGizmos()
    {
        if (!Enabled) return;

        Gizmos.color = color;

        switch (gizmoType)
        {
            case GizmoShape.Sphere:
                Gizmos.DrawSphere(transform.position, size);
                break;

            case GizmoShape.Cube:
                Gizmos.DrawCube(transform.position, Vector3.one * size);
                break;
        }
    }
}
