using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class CassetteDrag : MonoBehaviour
{

    public Vector3 originalPosition;

    private bool isDragging = false;

    void Start()
    {
        originalPosition = transform.position;
    }

    void OnMouseDown()
    {
        // Begin drag
        isDragging = true;
    }

    void OnMouseDrag()
    {
        if (!isDragging) return;

        // Move object to pointer position
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, Vector3.zero);
        float distance;
        if (plane.Raycast(ray, out distance))
        {
            Vector3 worldPoint = ray.GetPoint(distance);
            transform.position = worldPoint;
        }
    }

    void OnMouseUp()
    {
        isDragging = false;

        // Now we will check if close enough and trigger insertion
        FindObjectOfType<CasseteInsertionController>().TryInsert(transform);
    }
}
