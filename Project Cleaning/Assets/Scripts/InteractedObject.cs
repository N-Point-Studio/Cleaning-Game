using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractedObject : MonoBehaviour
{
    private Camera cam;

    private void Awake()
    {
        cam = Camera.main;
    }
    void Start()
    {

    }

    void Update()
    {
        //Debug.Log("Snack");
        if (TouchManager.Instance.isDragging || TouchManager.Instance.isInteracting || TouchManager.Instance.isZooming || TouchManager.Instance.isRotating || !TouchManager.Instance.isClickedOn) return;
        Clicked();
    }

    private void Clicked()
    {
        Ray ray = cam.ScreenPointToRay(TouchManager.Instance.tapPosition);
        if (Physics.Raycast(ray, out RaycastHit hit) && hit.transform == transform)
        {
            Debug.Log("Clicked on Snack Object");
            UIManager.Instance.ShowSetting(true);
        }
    }
}
