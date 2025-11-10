using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class FragmentIdleState : FragmentBaseState
{
    public FragmentIdleState(FragmentStateMachine stateMachine) : base(stateMachine) { }
    private bool dragJustStarted = false;
    private float initialDistanceZ;
    private bool isCloseToAssemble = false;


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
        // Debug.Log("Position: " + stateMachine.transform.position);
        if (stateMachine.interaction.isDragging && !dragJustStarted)
        {
            dragJustStarted = true;
            initialDistanceZ = Mathf.Abs(stateMachine.transform.position.z - stateMachine.InspectPosition.position.z);
        }

        if (stateMachine.interaction.isDragging)
        {
            float currentDistanceZ = Mathf.Abs(stateMachine.transform.position.z - stateMachine.InspectPosition.position.z);
            if (initialDistanceZ <= 0.001f) return;

            float progress = Mathf.InverseLerp(initialDistanceZ, 0f, currentDistanceZ);
            float t = 1f - progress;

            float newY = Mathf.Lerp(stateMachine.initialPosition.y, stateMachine.InspectPosition.position.y, 1 - t);

            //gabungin di sini
            isCloseToAssemble = currentDistanceZ <= 1;


            stateMachine.transform.position = new Vector3(stateMachine.transform.position.x, newY, stateMachine.transform.position.z);
        }

        if (!stateMachine.interaction.isDragging && dragJustStarted)
        {
            dragJustStarted = false;
            isCloseToAssemble = false;
        }
    }
}
