using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;

[RequireComponent(typeof(FragmentInteraction))]
public class FragmentStateMachine : StateMachine
{
    public List<AssemblyTarget> assemblyTargets = new List<AssemblyTarget>();
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
        SwitchState(new FragmentIdleState(this));
    }

    public bool TryGetAssemblyTarget(FragmentStateMachine other, out Transform correctPos)
    {
        foreach (var target in assemblyTargets)
        {
            if (target.targetFragment == other)
            {
                correctPos = target.correctPosition;
                return true;
            }
        }
        correctPos = null;
        return false;
    }
}
