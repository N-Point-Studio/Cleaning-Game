using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClusterCreatedState : ClusterBaseState
{
    public ClusterCreatedState(ClusterStateMachine stateMachine) : base(stateMachine) { }
    public override void Enter()
    {
        stateMachine.DisableAllInteraction();

        Debug.Log("Cluster state: created");

        stateMachine.SetClusterState(ClusterState.Created);
        stateMachine.BoxCollider.enabled = false;

        AssembleManager.Instance.SetCurrentInspectFragment(null);
        AssembleManager.Instance.SetCurrentInspectCluster(stateMachine);

        stateMachine.SetInitialPosition(stateMachine.connectedFragments[0].InitialPosition,
        stateMachine.connectedFragments[0].InitialRotation
        );

        stateMachine.Interaction.SetInitialPos(stateMachine.connectedFragments[0].InitialPosition);

        Debug.Log("initial position dari " + stateMachine.connectedFragments[0].name + " adalah " + stateMachine.connectedFragments[0].InitialPosition);
    }
    public override void Tick(float deltaTime)
    {

    }
    public override void Exit()
    {
        stateMachine.BoxCollider.enabled = true;
    }
}
