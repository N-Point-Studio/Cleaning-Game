using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClusterReturningState : ClusterBaseState
{
    public ClusterReturningState(ClusterStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        stateMachine.SetClusterState(ClusterState.Return);
        stateMachine.transform.SetParent(null);
        Debug.Log("Entering returing cluster state with: " + stateMachine.InitialPosition);
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

        if (Vector3.Distance(stateMachine.transform.position, stateMachine.transform.position) < 0.01f)
        {
            Debug.Log("DEKET: " + Vector3.Distance(stateMachine.transform.position, stateMachine.transform.position));
            stateMachine.transform.position = stateMachine.transform.position;
            stateMachine.transform.rotation = stateMachine.transform.rotation;
            stateMachine.SwitchState(new ClusterIdleState(stateMachine));
        }
    }

    public override void Exit()
    {
    }
}
