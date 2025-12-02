using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClusterFinishState : ClusterBaseState
{
    public ClusterFinishState(ClusterStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        stateMachine.SetClusterState(ClusterState.Finished);
        stateMachine.BoxCollider.enabled = true;
        stateMachine.Interaction.DisableAllInteraction();

        foreach (var frag in stateMachine.connectedFragments)
        {
            frag.SwitchState(new FragmentFinishState(frag));
        }

    }
    public override void Tick(float deltaTime)
    {

    }

    public override void Exit()
    {

    }
}
