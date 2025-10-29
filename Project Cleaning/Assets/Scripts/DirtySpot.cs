using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DirtySpot : MonoBehaviour
{
    private bool isCleaned = false;
    private Renderer rend;
    private Collider col;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        col = GetComponent<Collider>();
    }

    void OnMouseDown()
    {
        if (isCleaned) return;
        CleanSpot();
    }

    void CleanSpot()
    {
        isCleaned = true;

        // Disable visuals
        if (rend != null) rend.enabled = false;

        // Disable collider so it can’t be clicked again
        if (col != null) col.enabled = false;

        // Notify manager
        CleaningManager.Instance.SpotCleaned();
    }
}
