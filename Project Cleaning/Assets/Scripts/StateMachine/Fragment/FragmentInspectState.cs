using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FragmentInspectState : FragmentBaseState
{
    private float moveSpeed = 6f;
    private bool hasReachedInspect = false;

    public FragmentInspectState(FragmentStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        stateMachine.interaction.isHoldAvailable = true;
    }


    public override void Tick(float dt)
    {
        if (stateMachine.interaction.isHolding)
        {
            stateMachine.SwitchState(new FragmentReturningState(stateMachine));
        }
    }


    public override void Exit()
    {
        stateMachine.interaction.isHoldAvailable = false;

        if (FragmentStateMachine.CurrentInspecting == stateMachine)
            FragmentStateMachine.CurrentInspecting = null;
    }
}
