using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FragmentIdleState : FragmentBaseState
{
    public FragmentIdleState(FragmentStateMachine stateMachine) : base(stateMachine) { }

    private bool dragJustStarted = false;
    private float initialDistanceZ;
    private FragmentStateMachine potentialTarget;
    private ClusterStateMachine potentialCluster;

    public override void Enter()
    {
        stateMachine.CurrentStatus = "Idle";

        stateMachine.Interaction.isTapAvailable = true;
        stateMachine.Interaction.isDragAvailable = true;
        stateMachine.Interaction.isHoldAvailable = false;

    }

    public override void Tick(float dt)
    {
        if (stateMachine.Interaction.isTapping && stateMachine.Interaction.isTapAvailable)
        {
            stateMachine.Interaction.ResetTap();
            stateMachine.SwitchState(new FragmentMoveToInspectState(stateMachine));

            if (AssembleManager.Instance.CurrentClusterInspected != null)
                AssembleManager.Instance.CurrentClusterInspected.SwitchState(new ClusterReturningState(AssembleManager.Instance.CurrentClusterInspected));

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
                stateMachine.SwitchState(new FragmentAssembledState(stateMachine));
                potentialTarget = null;
                TouchManager.Instance.SetIsDrag(false);
            }
            else if (potentialCluster != null)
            {
                stateMachine.SwitchState(new FragmentAssembledState(stateMachine));
                potentialCluster = null;
                TouchManager.Instance.SetIsDrag(false);
            }
        }
    }

    public override void Exit()
    {
        stateMachine.Interaction.DisableAllInteraction();
        stateMachine.Interaction.isDragging = false;
        potentialTarget = null;
        TouchManager.Instance.SetIsDrag(false);
    }

    private void MoveTowardInspect()
    {
        if (AssembleManager.Instance == null || AssembleManager.Instance.InspectPosition == null)
        {
            return;
        }

        if (stateMachine.Interaction.isDragging && !dragJustStarted)
        {
            dragJustStarted = true;
            initialDistanceZ = Mathf.Abs(stateMachine.transform.position.z - AssembleManager.Instance.InspectPosition.position.z);
        }

        if (stateMachine.Interaction.isDragging)
        {
            float currentDistanceZ = Mathf.Abs(stateMachine.transform.position.z - AssembleManager.Instance.InspectPosition.position.z);
            if (initialDistanceZ <= 0.001f) return;

            float progress = Mathf.InverseLerp(initialDistanceZ, 0f, currentDistanceZ);
            float t = 1f - progress;

            // posisi
            float newY = Mathf.Lerp(stateMachine.InitialPosition.y, AssembleManager.Instance.InspectPosition.position.y, 1 - t);
            Vector3 targetPos = new Vector3(stateMachine.transform.position.x, newY + 2, stateMachine.transform.position.z);
            stateMachine.transform.position = Vector3.Lerp(stateMachine.transform.position, targetPos, Time.deltaTime * stateMachine.Interaction.dragSpeed);

            if (currentDistanceZ < 2.0f)
            {
                // if (potentialTarget != null && potentialTarget.TryGetAssemblyTarget(stateMachine, out Transform correctPos))
                // {
                //     stateMachine.transform.rotation = Quaternion.Slerp(
                //         stateMachine.transform.rotation,
                //         correctPos.rotation,
                //         Time.deltaTime * 5f
                //     );
                // }
                // else
                // {
                stateMachine.transform.rotation = Quaternion.Slerp(
                    stateMachine.transform.rotation,
                    AssembleManager.Instance.InspectPosition.transform.rotation,
                    Time.deltaTime * 5f
                );
                // }
            }

            if (AssembleManager.Instance.CurrentFragmentInspected != null && stateMachine != AssembleManager.Instance.CurrentFragmentInspected)
            {
                float zDist = Mathf.Abs(stateMachine.transform.position.z - AssembleManager.Instance.CurrentFragmentInspected.transform.position.z);
                potentialTarget = zDist < 1.5f ? AssembleManager.Instance.CurrentFragmentInspected : null;
            }
            else if (AssembleManager.Instance.CurrentClusterInspected != null)
            {
                float zDist = Mathf.Abs(stateMachine.transform.position.z - AssembleManager.Instance.CurrentClusterInspected.transform.position.z);
                potentialCluster = zDist < 1.5f ? AssembleManager.Instance.CurrentClusterInspected : null;
            }
            else if (currentDistanceZ < 1f && AssembleManager.Instance.CurrentClusterInspected == null && AssembleManager.Instance.CurrentFragmentInspected == null)
            {
                stateMachine.SwitchState(new FragmentMoveToInspectState(stateMachine));
                TouchManager.Instance.SetIsDrag(false);
            }
        }
    }
}
