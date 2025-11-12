using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FragmentIdleState : FragmentBaseState
{
    public FragmentIdleState(FragmentStateMachine stateMachine) : base(stateMachine) { }

    private bool dragJustStarted = false;
    private float initialDistanceZ;
    private FragmentStateMachine potentialTarget;

    public override void Enter()
    {
        stateMachine.Interaction.isTapAvailable = true;
        stateMachine.Interaction.isDragAvailable = true;

    }

    public override void Tick(float dt)
    {
        if (stateMachine.Interaction.isTapping && stateMachine.Interaction.isTapAvailable)
        {
            stateMachine.Interaction.ResetTap();
            stateMachine.SwitchState(new FragmentMoveToInspectState(stateMachine));
            return;
        }

        if (stateMachine.Interaction.isDragAvailable && stateMachine.Interaction.isDragging)
        {
            MoveTowardInspect();
        }

        if (!TouchManager.Instance.isInteracting && dragJustStarted)
        {
            dragJustStarted = false;

            if (potentialTarget != null)
            {
                stateMachine.SwitchState(new FragmentAssembledState(stateMachine, potentialTarget));
                potentialTarget = null;
                TouchManager.Instance.SetIsDrag(false);
            }
        }
    }

    public override void Exit()
    {
        stateMachine.Interaction.isTapAvailable = false;
        stateMachine.Interaction.isDragAvailable = false;
        stateMachine.Interaction.isDragging = false;
        potentialTarget = null;
        TouchManager.Instance.SetIsDrag(false);
    }

    private void MoveTowardInspect()
    {
        if (stateMachine.Interaction.isDragging && !dragJustStarted)
        {
            dragJustStarted = true;
            initialDistanceZ = Mathf.Abs(stateMachine.transform.position.z - stateMachine.InspectPosition.position.z);
        }

        if (stateMachine.Interaction.isDragging)
        {
            float currentDistanceZ = Mathf.Abs(stateMachine.transform.position.z - stateMachine.InspectPosition.position.z);
            if (initialDistanceZ <= 0.001f) return;

            float progress = Mathf.InverseLerp(initialDistanceZ, 0f, currentDistanceZ);
            float t = 1f - progress;

            float newY = Mathf.Lerp(stateMachine.InitialPosition.y, stateMachine.InspectPosition.position.y, 1 - t);
            if (FragmentStateMachine.CurrentInspecting != null && stateMachine != FragmentStateMachine.CurrentInspecting)
            {
                float zDist = Mathf.Abs(stateMachine.transform.position.z - FragmentStateMachine.CurrentInspecting.transform.position.z);
                if (zDist < 1.5f)
                {
                    potentialTarget = FragmentStateMachine.CurrentInspecting;
                }
                else
                {
                    potentialTarget = null;
                }
            }
            else if (currentDistanceZ < 1f)
            {
                stateMachine.SwitchState(new FragmentMoveToInspectState(stateMachine));
                TouchManager.Instance.SetIsDrag(false);
            }

            stateMachine.transform.position = Vector3.Lerp(
                stateMachine.transform.position,
                new Vector3(stateMachine.transform.position.x, newY + 2, stateMachine.transform.position.z),
                Time.deltaTime * stateMachine.Interaction.dragSpeed
            );
        }
    }
}
