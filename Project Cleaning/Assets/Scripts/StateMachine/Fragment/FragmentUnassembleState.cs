using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FragmentUnassembleState : FragmentBaseState
{
    Transform root;

    public FragmentUnassembleState(FragmentStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        var cluster = AssembleManager.Instance.CurrentClusterInspected;
        if (cluster != null && cluster.connectedFragments.Count > 0)
        {
            FragmentStateMachine referenceFragment = cluster.connectedFragments[0];
            cluster.SetInitialPosition(
                referenceFragment.InitialPosition,
                referenceFragment.InitialRotation
            );
        }

        stateMachine.SwitchState(new FragmentReturningState(stateMachine));
    }
    public override void Tick(float dt) { }

    public override void Exit() { }
}
