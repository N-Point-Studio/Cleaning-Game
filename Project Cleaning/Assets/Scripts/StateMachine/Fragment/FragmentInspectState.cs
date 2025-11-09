using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FragmentInspectState : FragmentBaseState
{
    private float moveSpeed = 6f;

    public FragmentInspectState(FragmentStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        // Kalau ada yang sedang inspect selain ini → kembalikan
        if (FragmentStateMachine.CurrentInspecting != null &&
            FragmentStateMachine.CurrentInspecting != stateMachine)
        {
            FragmentStateMachine.CurrentInspecting.SwitchState(
                new FragmentReturningState(FragmentStateMachine.CurrentInspecting)
            );
        }

        FragmentStateMachine.CurrentInspecting = stateMachine;

        stateMachine.interaction.isRotateAvailable = true;
        stateMachine.interaction.isZoomAvailable = true;
    }

    public override void Tick(float dt)
    {
        stateMachine.transform.position = Vector3.Lerp(
            stateMachine.transform.position,
            stateMachine.InspectPosition.position,
            dt * moveSpeed
        );
    }

    public override void Exit()
    {
        stateMachine.interaction.isRotateAvailable = false;
        stateMachine.interaction.isZoomAvailable = false;

        // Clear static if this object exits inspect
        if (FragmentStateMachine.CurrentInspecting == stateMachine)
            FragmentStateMachine.CurrentInspecting = null;
    }
}
