using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FragmentSlot : MonoBehaviour
{
    public Transform attachPoint;
    private Camera cam;

    void OnEnable()
    {
        TouchManager.OnTapped += Tapped;
    }

    void OnDisable()
    {
        TouchManager.OnTapped -= Tapped;
    }

    private void Start()
    {
        cam = Camera.main;
    }

    private void Tapped()
    {
        if (!TouchManager.Instance.isInteracting)
        {
            Ray ray = cam.ScreenPointToRay(TouchManager.Instance.tapPosition);
            // Debug.Log("Position " + TouchManager.Instance.tapPosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                FragmentController frag = hit.transform.GetComponentInChildren<FragmentController>();
                if (frag != null)
                {
                    Debug.Log("Fragment yang di tap: " + frag.name);
                    AssembleManager.Instance.InspectFragment(frag);
                }
            }
        }
    }
}
