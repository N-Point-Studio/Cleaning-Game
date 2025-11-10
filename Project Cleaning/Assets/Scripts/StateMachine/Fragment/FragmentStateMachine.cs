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
    public Transform clusterRoot;
    public bool IsClusterRoot => clusterRoot != null && clusterRoot.GetComponent<FragmentCluster>()?.fragments[0] == this;


    private void Awake()
    {
        MainCamera = Camera.main;
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    private void Start()
    {
        interaction = GetComponent<FragmentInteraction>();
        SwitchState(new FragmentIdleState(this));
    }
}
