using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Modules;

public class ClusterInspectState : ClusterBaseState
{
    public ClusterInspectState(ClusterStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        AppLogger.Log("Cluster state: inspect");
        stateMachine.Interaction.DisableAllInteraction();


        stateMachine.SetClusterState(ClusterState.Inspect);
        stateMachine.BoxCollider.enabled = false;

        foreach (var frag in stateMachine.connectedFragments)
        {
            frag.Interaction.isHoldAvailable = true;
        }
    }

    public override void Tick(float deltaTime)
    {
    }

    public override void Exit()
    {
        stateMachine.BoxCollider.enabled = true;
        stateMachine.Interaction.DisableAllInteraction();
    }

}

