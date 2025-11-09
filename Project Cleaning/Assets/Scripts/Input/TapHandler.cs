using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TapHandler : MonoBehaviour
{
    private Camera cam;

    private void OnEnable()
    {
        TouchManager.OnTapped += HandleTap;
    }

    private void OnDisable()
    {
        TouchManager.OnTapped -= HandleTap;
    }

    private void Start()
    {
        cam = Camera.main;
    }

    void HandleTap()
    {
        Ray ray = cam.ScreenPointToRay(TouchManager.Instance.tapPosition);
        if (Physics.Raycast(ray, out RaycastHit hit) && hit.transform == transform)
        {
            Debug.Log(name + " TAPPED");
            AssembleManager.Instance.InspectFragment(GetComponent<FragmentController>());
        }
    }
}
