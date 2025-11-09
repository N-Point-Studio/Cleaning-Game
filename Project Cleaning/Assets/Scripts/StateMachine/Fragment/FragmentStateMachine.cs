using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;


public class FragmentStateMachine : StateMachine
{
    public FragmentInteraction interaction { get; private set; }
    public Camera MainCamera { get; private set; }
    public Transform InspectPosition;
    public Vector3 initialPosition { get; private set; }
    public Quaternion initialRotation { get; private set; }
    public static FragmentStateMachine CurrentInspecting;


    // public float initialY;

    // public Transform InspectPosition;

    private void Awake()
    {
        MainCamera = Camera.main;
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        // initialY = transform.position.y;
    }

    private void Start()
    {
        interaction = GetComponent<FragmentInteraction>();
        SwitchState(new FragmentIdleState(this));
    }
}

// public class FragmentStateMachine : StateMachine
// {
//     public static FragmentStateMachine CurrentInspecting;

//     public FragmentInteraction interaction { get; private set; }
// public Transform InspectPosition { get; set; }
// public Vector3 initialPosition { get; private set; }
// public Quaternion initialRotation { get; private set; }

//     private void Start()
//     {
//         interaction = GetComponent<FragmentInteraction>();

//         // Save starting transform for returning
// initialPosition = transform.position;
// initialRotation = transform.rotation;

//         SwitchState(new FragmentIdleState(this));
//     }
// }
