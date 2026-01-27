using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClusterReturningState : ClusterBaseState
{
    public ClusterReturningState(ClusterStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        Debug.Log("Cluster state: returning");

        stateMachine.SetClusterState(ClusterState.Return);
        stateMachine.transform.SetParent(null);
        stateMachine.Interaction.DisableAllInteraction();


        // Debug.Log($"Entering returning cluster state with: {stateMachine.InitialPosition}");
    }

    public override void Tick(float deltaTime)
    {
        stateMachine.transform.position = Vector3.Lerp(
            stateMachine.transform.position,
            stateMachine.InitialPosition,
            deltaTime * stateMachine.moveSpeed
        );

        stateMachine.transform.rotation = Quaternion.Lerp(
            stateMachine.transform.rotation,
            stateMachine.InitialRotation,
            deltaTime * stateMachine.moveSpeed
        );

        if (Vector3.Distance(stateMachine.transform.position, stateMachine.InitialPosition) < 0.01f)
        {
            stateMachine.transform.position = stateMachine.InitialPosition;
            stateMachine.transform.rotation = stateMachine.InitialRotation;
            stateMachine.SwitchState(new ClusterIdleState(stateMachine));
        }
    }

    public override void Exit()
    {
        stateMachine.Interaction.DisableAllInteraction();
    }
}
