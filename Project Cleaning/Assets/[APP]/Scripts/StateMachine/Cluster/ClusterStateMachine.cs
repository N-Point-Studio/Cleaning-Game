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

    [field: SerializeField] public Vector3 InitialPosition { get; set; }
    [field: SerializeField] public Quaternion InitialRotation { get; set; }

    public float moveSpeed = 6f;

    public bool isInspecting = false;

    private void Awake()
    {
        Camera = Camera.main;
    }

    private void Start()
    {
        Interaction = GetComponent<FragmentInteraction>();
        BoxCollider = GetComponent<BoxCollider>();
        BoxCollider.size = new Vector3(2.5f, 2.5f, 2.5f);
        SwitchState(new ClusterCreatedState(this));
        AssembleManager.Instance.RegisterCluster(this, true);
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
        Interaction.SetInitialPos(position);
    }

    public void AddFragment(FragmentStateMachine fragment)
    {
        if (!connectedFragments.Contains(fragment))
        {
            connectedFragments.Add(fragment);
            fragment.transform.SetParent(this.transform);
        }
    }

    public void RemovingFragment(FragmentStateMachine fragment)
    {
        if (connectedFragments.Contains(fragment))
        {
            connectedFragments.Remove(fragment);
            fragment.SwitchState(new FragmentUnassembleState(fragment));
        }
    }

    public void DestroyingCluster()
    {
        if (connectedFragments.Count <= 0)
        {
            Destroy(gameObject);
        }
    }

    public void DisableAllInteraction()
    {
        Interaction.isDragAvailable = false;
        Interaction.isTapAvailable = false;
        Interaction.isHoldAvailable = false;
    }

}
