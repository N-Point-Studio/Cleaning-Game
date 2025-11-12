using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClusterIdleState : ClusterBaseState
{
    public ClusterIdleState(ClusterStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        stateMachine.SetClusterState(ClusterState.Idle);

        stateMachine.Interaction.isTapAvailable = true;
        stateMachine.Interaction.isDragAvailable = true;
    }

    public override void Tick(float deltaTime)
    {
        if (stateMachine.Interaction.isTapping && stateMachine.Interaction.isTapAvailable)
        {
            stateMachine.Interaction.ResetTap();
            //pindah ke moveToInspect
            stateMachine.SwitchState(new ClusterMoveToInspect(stateMachine));
            return;
        }
    }

    public override void Exit()
    {
        stateMachine.Interaction.isTapAvailable = false;
        stateMachine.Interaction.isDragAvailable = false;
    }
}
