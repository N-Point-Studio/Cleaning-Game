using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FragmentController : MonoBehaviour
{
    [HideInInspector] public Vector3 initialPos;
    [HideInInspector] public Quaternion initialRot;

    public bool isAttached = false;
    public Transform attachPoint;

    public void SaveInitialTransform()
    {
        initialPos = transform.position;
        initialRot = transform.rotation;
    }

    public void ResetFragment()
    {
        transform.position = initialPos;
        transform.rotation = initialRot;
        isAttached = false;
    }
}
