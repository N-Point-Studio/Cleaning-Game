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
        // stateMachine.interaction.isRotateAvailable = true;
        // stateMachine.interaction.isZoomAvailable = true;
        stateMachine.interaction.isHoldAvailable = true;
    }


    public override void Tick(float dt)
    {
        // stateMachine.interaction.HandleRotate();

        if (stateMachine.interaction.isHolding)
        {
            stateMachine.SwitchState(new FragmentReturningState(stateMachine));
        }
    }


    public override void Exit()
    {
        // stateMachine.interaction.isRotateAvailable = false;
        // stateMachine.interaction.isZoomAvailable = false;
        stateMachine.interaction.isHoldAvailable = false;

        if (FragmentStateMachine.CurrentInspecting == stateMachine)
            FragmentStateMachine.CurrentInspecting = null;
    }
}
