using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class FragmentIdleState : FragmentBaseState
{
    public FragmentIdleState(FragmentStateMachine stateMachine) : base(stateMachine) { }
    private bool dragJustStarted = false;
    private float initialDistanceZ;


    public override void Enter()
    {
        stateMachine.interaction.isTapAvailable = true;
        stateMachine.interaction.isDragAvailable = true;
    }

    public override void Tick(float dt)
    {
        if (stateMachine.interaction.isTapping && stateMachine.interaction.isTapAvailable)
        {
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
        if (stateMachine.interaction.isDragging && !dragJustStarted)
        {
            dragJustStarted = true;
            initialDistanceZ = Mathf.Abs(stateMachine.transform.position.z - stateMachine.CorrectPosition.position.z);
        }

        if (stateMachine.interaction.isDragging)
        {
            float currentDistanceZ = Mathf.Abs(stateMachine.transform.position.z - stateMachine.CorrectPosition.position.z);
            if (initialDistanceZ <= 0.001f) return;

            float progress = Mathf.InverseLerp(initialDistanceZ, 0f, currentDistanceZ);
            float t = 1f - progress;

            float newY = Mathf.Lerp(stateMachine.initialPosition.y, stateMachine.CorrectPosition.position.y, 1 - t);

            if (FragmentStateMachine.CurrentInspecting != null && stateMachine != FragmentStateMachine.CurrentInspecting)
            {
                float zDist = Mathf.Abs(stateMachine.transform.position.z - stateMachine.CorrectPosition.position.z);
                if (zDist < 0.5f)
                {
                    stateMachine.SwitchState(new FragmentAssembledState(stateMachine, FragmentStateMachine.CurrentInspecting));
                    return;
                }
            }


            stateMachine.transform.position = new Vector3(stateMachine.transform.position.x, newY, stateMachine.transform.position.z);
        }

        if (!stateMachine.interaction.isDragging && dragJustStarted)
        {
            dragJustStarted = false;
        }
    }
}
