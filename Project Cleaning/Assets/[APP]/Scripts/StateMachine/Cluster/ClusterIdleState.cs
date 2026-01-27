using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClusterIdleState : ClusterBaseState
{
    private bool dragJustStarted = false;
    private float initialDistanceZ;
    private FragmentStateMachine potentialFragmentTarget;
    private ClusterStateMachine potentialClusterTarget;

    public ClusterIdleState(ClusterStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        stateMachine.SetClusterState(ClusterState.Idle);
        Debug.Log("Cluster state: Idle");

        stateMachine.Interaction.isTapAvailable = true;
        stateMachine.Interaction.isDragAvailable = true;
        stateMachine.Interaction.isHoldAvailable = false;
    }

    public override void Tick(float deltaTime)
    {
        if (stateMachine.Interaction.isTapping && stateMachine.Interaction.isTapAvailable)
        {
            stateMachine.Interaction.ResetTap();
            stateMachine.SwitchState(new ClusterMoveToInspect(stateMachine));
            if (AssembleManager.Instance.CurrentFragmentInspected != null)
                AssembleManager.Instance.CurrentFragmentInspected.SwitchState(
                    new FragmentReturningState(AssembleManager.Instance.CurrentFragmentInspected)
                );

            return;
        }
        if (stateMachine.Interaction.isDragAvailable && stateMachine.Interaction.isDragging)
        {
            MoveTowardInspect();
        }
        if (!TouchManager.Instance.isInteracting && dragJustStarted)
        {
            dragJustStarted = false;

            if (potentialFragmentTarget != null || potentialClusterTarget != null)
            {
                stateMachine.SwitchState(new ClusterAssembleState(stateMachine));
                potentialFragmentTarget = null;
                potentialClusterTarget = null;
                TouchManager.Instance.SetIsDrag(false);
            }
        }
    }

    public override void Exit()
    {
        TouchManager.Instance.SetIsDrag(false);
        stateMachine.Interaction.DisableAllInteraction();
        stateMachine.Interaction.isDragging = false;

        potentialFragmentTarget = null;
        potentialClusterTarget = null;
    }

    private void MoveTowardInspect()
    {
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

            float newY = Mathf.Lerp(stateMachine.InitialPosition.y, AssembleManager.Instance.InspectPosition.position.y, 1 - t);
            Vector3 targetPos = new Vector3(stateMachine.transform.position.x, newY + 2f, stateMachine.transform.position.z);
            stateMachine.transform.position = Vector3.Lerp(
                stateMachine.transform.position,
                targetPos,
                Time.deltaTime * stateMachine.Interaction.dragSpeed
            );

            if (currentDistanceZ < 2.0f)
            {
                stateMachine.transform.rotation = Quaternion.Slerp(
                    stateMachine.transform.rotation,
                    AssembleManager.Instance.InspectPosition.transform.rotation,
                    Time.deltaTime * 5f
                );
            }

            if (AssembleManager.Instance.CurrentFragmentInspected != null)
            {
                float zDist = Mathf.Abs(stateMachine.transform.position.z -
                                        AssembleManager.Instance.CurrentFragmentInspected.transform.position.z);
                potentialFragmentTarget = zDist < 1.5f ? AssembleManager.Instance.CurrentFragmentInspected : null;
            }

            if (AssembleManager.Instance.CurrentClusterInspected != null &&
                AssembleManager.Instance.CurrentClusterInspected != stateMachine)
            {
                float zDist = Mathf.Abs(stateMachine.transform.position.z -
                                        AssembleManager.Instance.CurrentClusterInspected.transform.position.z);
                potentialClusterTarget = zDist < 1.5f ? AssembleManager.Instance.CurrentClusterInspected : null;
            }

            if (currentDistanceZ < 1f &&
                AssembleManager.Instance.CurrentClusterInspected == null &&
                AssembleManager.Instance.CurrentFragmentInspected == null)
            {
                stateMachine.SwitchState(new ClusterMoveToInspect(stateMachine));
                TouchManager.Instance.SetIsDrag(false);
            }
        }
    }
}
