using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;

[RequireComponent(typeof(FragmentInteraction))]
public class FragmentStateMachine : StateMachine
{
    public enum FragmentStatus
    {
        Parent,
        Attached
    }

    public List<AssemblyTarget> assemblyTargets = new List<AssemblyTarget>();
    public List<FragmentStateMachine> StateMachineConnected = new List<FragmentStateMachine>();
    public FragmentStateMachine[] ListOfStateMachineConnected;

    public FragmentInteraction Interaction { get; private set; }
    public Camera MainCamera { get; private set; }
    public Transform InspectPosition;
    public Vector3 InitialPosition { get; private set; }
    public Quaternion InitialRotation { get; private set; }

    public static FragmentStateMachine CurrentInspecting;
    public FragmentStateMachine ParentFragment { get; private set; }
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

    public void SetParentFragment(FragmentStateMachine parent)
    {
        ParentFragment = parent;
        transform.SetParent(parent != null ? parent.transform : null);
        if (parent != null && !parent.StateMachineConnected.Contains(this))
            parent.StateMachineConnected.Add(this);
    }

}
