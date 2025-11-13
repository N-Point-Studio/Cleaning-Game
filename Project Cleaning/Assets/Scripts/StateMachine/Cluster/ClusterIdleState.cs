using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClusterIdleState : ClusterBaseState
{
    public ClusterIdleState(ClusterStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        stateMachine.SetClusterState(ClusterState.Idle);
        Debug.Log("Cluster state: Idle");

        stateMachine.Interaction.isTapAvailable = true;
        stateMachine.Interaction.isDragAvailable = true;
    }

    public override void Tick(float deltaTime)
    {
        if (stateMachine.Interaction.isTapping && stateMachine.Interaction.isTapAvailable)
        {
            stateMachine.Interaction.ResetTap();
            stateMachine.SwitchState(new ClusterMoveToInspect(stateMachine));
            return;
        }

        if (stateMachine.Interaction.isDragAvailable && stateMachine.Interaction.isDragging)
        {
            // MoveTowardInspect();
        }
    }

    public override void Exit()
    {
        TouchManager.Instance.SetIsDrag(false);
        stateMachine.Interaction.isTapAvailable = false;
        stateMachine.Interaction.isDragAvailable = false;
        // stateMachine.Interaction.isDragging = false;
    }
}
