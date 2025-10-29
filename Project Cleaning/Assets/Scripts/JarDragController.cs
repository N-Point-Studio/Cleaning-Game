using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class JarDragController : MonoBehaviour
{
    public Vector3 originalPosition;
    [Header("Movement Settings")]
    public float dragSpeed = 8f; // Adjust this to control movement speed
    private bool isDragging = false;
    private Camera mainCamera;
    private Vector3 targetPosition;

    void Start()
    {
        originalPosition = transform.position;
        targetPosition = transform.position;
        mainCamera = Camera.main;
    }

    void OnMouseDown()
    {
        isDragging = true;
    }

    void OnMouseDrag()
    {
        if (!isDragging || mainCamera == null) return;

        // Get mouse position in world space
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        // Create a plane at the jar's current Y position
        Plane dragPlane = new Plane(Vector3.up, new Vector3(0, transform.position.y, 0));

        float distance;
        if (dragPlane.Raycast(ray, out distance))
        {
            Vector3 worldPoint = ray.GetPoint(distance);
            targetPosition = worldPoint; // Set target instead of direct position
        }
    }

    void Update()
    {
        // Smoothly move to target position when dragging
        if (isDragging)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * dragSpeed);
        }
    }

    void OnMouseUp()
    {
        isDragging = false;

        // Try to insert the jar
        JarInsertionController insertController = FindObjectOfType<JarInsertionController>();
        if (insertController != null)
        {
            insertController.TryInsert(transform);
        }
    }
}