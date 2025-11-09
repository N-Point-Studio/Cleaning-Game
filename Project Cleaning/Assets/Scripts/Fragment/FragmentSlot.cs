using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FragmentSlot : MonoBehaviour
{
    [SerializeField] private Transform InspectPosition;
    [SerializeField] private DraggableObject drag;
    private Camera cam;
    private float baseY;

    private float initialDistanceZ;
    private bool dragJustStarted = false;

    private bool isCloseToAssemble = false;


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
        baseY = transform.position.y; // simpan Y awal
    }


    private void Update()
    {
        if (drag != null)
        {

            MoveTowardsInspect();
        }

    }

    private void Tapped()
    {
        if (!TouchManager.Instance.isInteracting)
        {
            Ray ray = cam.ScreenPointToRay(TouchManager.Instance.tapPosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                FragmentController frag = hit.transform.GetComponentInChildren<FragmentController>();
                if (frag != null)
                {
                    // Debug.Log("Fragment yang di tap: " + frag.name);
                    AssembleManager.Instance.InspectFragment(frag);
                }
            }
        }
    }

    void MoveTowardsInspect()
    {
        if (drag.isDragging && !dragJustStarted)
        {
            dragJustStarted = true;
            initialDistanceZ = Mathf.Abs(transform.position.z - InspectPosition.position.z);
        }
        if (drag.isDragging)
        {

            float currentDistanceZ = Mathf.Abs(transform.position.z - InspectPosition.position.z);
            if (initialDistanceZ <= 0.001f) return;
            float progress = Mathf.InverseLerp(initialDistanceZ, 0f, currentDistanceZ);
            float t = 1f - progress;

            float newY = Mathf.Lerp(baseY, InspectPosition.position.y, 1 - t);

            // isCloseToAssemble = currentDistanceZ <= 2;
            // Debug.Log("CLOSE TO ASSEMBLE: " + isCloseToAssemble);
            FragmentController frag = GetComponentInChildren<FragmentController>();
            if (frag != null) // kalau ada fragment
            {
                // AssembleManager.Instance.TryAssembleFragment(frag);
            }

            transform.position = new Vector3(
                transform.position.x,
                newY,
                transform.position.z
            );
        }
        if (!drag.isDragging && dragJustStarted)
        {
            dragJustStarted = false;
            isCloseToAssemble = false;
        }
    }

}
