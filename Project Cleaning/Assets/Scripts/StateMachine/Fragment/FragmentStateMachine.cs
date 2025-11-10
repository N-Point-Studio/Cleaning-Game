using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;


public class FragmentStateMachine : StateMachine
{
    public FragmentInteraction interaction { get; private set; }
    public Camera MainCamera { get; private set; }
    public Transform InspectPosition;
    public Transform CorrectPosition;
    public Vector3 initialPosition { get; private set; }
    public Quaternion initialRotation { get; private set; }
    public static FragmentStateMachine CurrentInspecting;
    public Transform clusterRoot; // null = tidak di-cluster
    public bool IsInCluster => clusterRoot != null;

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
