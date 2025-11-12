using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(FragmentInteraction), typeof(BoxCollider))]
public class ClusterStateMachine : StateMachine
{
    public ClusterState CurrentState { get; set; }

    public AssembleManager assembleManager = AssembleManager.Instance;

    public List<AssemblyTarget> assemblyTargets = new();
    public List<FragmentStateMachine> connectedFragments = new();
    public FragmentInteraction Interaction { get; private set; }

    public Camera Camera { get; private set; }
    public BoxCollider BoxCollider;

    [SerializeField] public Vector3 InitialPosition { get; set; }
    [SerializeField] public Quaternion InitialRotation { get; set; }

    public float moveSpeed = 6f;

    public bool isInspecting = false;

    private void Awake()
    {
        Camera = Camera.main;

        //temporary
        InitialPosition = transform.position;
        InitialRotation = transform.rotation;

    }

    private void Start()
    {
        Interaction = GetComponent<FragmentInteraction>();
        BoxCollider = GetComponent<BoxCollider>();
        BoxCollider.size = new Vector3(2.5f, 2.5f, 2.5f);
        // SwitchState(new ClusterCreatedState(this));
        SwitchState(new ClusterIdleState(this));
    }

    public void SetClusterState(ClusterState state)
    {
        CurrentState = state;
    }

    public bool TryGetAssemblePosition(FragmentStateMachine other, out Transform correctPos)
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

    public void SetInitialPosition(Vector3 position, Quaternion rotation)
    {
        InitialPosition = position;
        InitialRotation = rotation;
    }

    public void AddFragment(FragmentStateMachine fragment)
    {
        if (!connectedFragments.Contains(fragment))
        {
            connectedFragments.Add(fragment);
            fragment.transform.SetParent(this.transform);
        }
    }

}
