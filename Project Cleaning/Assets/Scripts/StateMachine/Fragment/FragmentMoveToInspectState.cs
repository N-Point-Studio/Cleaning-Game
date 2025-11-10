using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FragmentMoveToInspectState : FragmentBaseState
{
    private float moveSpeed = 6f;

    public FragmentMoveToInspectState(FragmentStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        // Jika ada fragment lain yang sedang di-inspect, kembalikan dulu
        if (FragmentStateMachine.CurrentInspecting != null &&
            FragmentStateMachine.CurrentInspecting != stateMachine)
        {
            FragmentStateMachine.CurrentInspecting.SwitchState(
                new FragmentReturningState(FragmentStateMachine.CurrentInspecting));
        }

        FragmentStateMachine.CurrentInspecting = stateMachine;

        // --- Tambahan: set parent ke InspectPosition ---
        stateMachine.transform.SetParent(stateMachine.InspectPosition);
    }

    public override void Tick(float dt)
    {
        stateMachine.transform.position = Vector3.Lerp(
            stateMachine.transform.position,
            stateMachine.InspectPosition.position,
            dt * moveSpeed
        );

        if (Vector3.Distance(stateMachine.transform.position, stateMachine.InspectPosition.position) < 0.01f)
        {
            stateMachine.transform.position = stateMachine.InspectPosition.position;
            stateMachine.SwitchState(new FragmentInspectState(stateMachine));
        }
    }

    public override void Exit() { }
}

