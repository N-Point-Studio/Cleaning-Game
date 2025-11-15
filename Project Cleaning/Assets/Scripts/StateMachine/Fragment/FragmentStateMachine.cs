using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;

[RequireComponent(typeof(FragmentInteraction))]
public class FragmentStateMachine : StateMachine
{
    public List<FragmentStateMachine> StateMachineConnected = new List<FragmentStateMachine>();
    public FragmentInteraction Interaction { get; private set; }
    public Camera MainCamera { get; private set; }
    [SerializeField] public Vector3 InitialPosition { get; set; }
    [SerializeField] public Quaternion InitialRotation { get; set; }

    public string CurrentStatus;

    private void Awake()
    {
        MainCamera = Camera.main;
        InitialPosition = transform.position;
        InitialRotation = transform.rotation;
    }

    private void Start()
    {
        Interaction = GetComponent<FragmentInteraction>();
        Interaction.SetInitialPos(InitialPosition);
        SwitchState(new FragmentIdleState(this));
    }

    public void DisableAllInteraction()
    {
        Interaction.isDragAvailable = false;
        Interaction.isTapAvailable = false;
        Interaction.isHoldAvailable = false;
    }
}
