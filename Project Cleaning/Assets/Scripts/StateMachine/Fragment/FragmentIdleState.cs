using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FragmentIdleState : FragmentBaseState
{
    public FragmentIdleState(FragmentStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        stateMachine.interaction.isTapAvailable = true;
        stateMachine.interaction.isDragAvailable = true;

        // stateMachine.interaction.isHoldAvailable = false;
        // stateMachine.interaction.isDragAvailable = false;
        // stateMachine.interaction.isRotateAvailable = false;

    }

    public override void Tick(float dt)
    {
        if (stateMachine.interaction.isTapping && stateMachine.interaction.isTapAvailable)
        {
            // Debug.Log("idle is tapped");
            stateMachine.interaction.ResetTap();
            stateMachine.SwitchState(new FragmentMoveToInspectState(stateMachine));
        }

        if (stateMachine.interaction.isDragAvailable && stateMachine.interaction.isDragging)
        {
            MoveTowardInspect();
        }
    }

    public override void Exit()
    {
        stateMachine.interaction.isTapAvailable = false;
        stateMachine.interaction.isDragAvailable = false;
        stateMachine.interaction.isDragging = false;
    }

    public void MoveTowardInspect()
    {
        // Debug.Log("Position: " + stateMachine.transform.position);
    }
}
