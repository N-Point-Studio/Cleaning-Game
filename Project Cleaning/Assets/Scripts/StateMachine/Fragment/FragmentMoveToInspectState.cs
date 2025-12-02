using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FragmentMoveToInspectState : FragmentBaseState
{
    private float moveSpeed = 6f;

    public FragmentMoveToInspectState(FragmentStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        stateMachine.Interaction.DisableAllInteraction();

        stateMachine.CurrentStatus = "Move To Inspect";

        if (AssembleManager.Instance.CurrentFragmentInspected != null &&
            AssembleManager.Instance.CurrentFragmentInspected != stateMachine)
        {
            AssembleManager.Instance.CurrentFragmentInspected.SwitchState(
                new FragmentReturningState(AssembleManager.Instance.CurrentFragmentInspected));
        }

        // FragmentStateMachine.CurrentInspecting = stateMachine;

        stateMachine.transform.SetParent(AssembleManager.Instance.InspectPosition);
        stateMachine.Interaction.isReturning = false;

        if (AssembleManager.Instance.CurrentClusterInspected != null)
        {
            AssembleManager.Instance.CurrentClusterInspected.SwitchState(new ClusterReturningState(AssembleManager.Instance.CurrentClusterInspected));
        }

        AssembleManager.Instance.SetCurrentInspectFragment(stateMachine);
        AssembleManager.Instance.SetCurrentInspectCluster(null);
    }

    public override void Tick(float dt)
    {
        stateMachine.transform.position = Vector3.Lerp(
            stateMachine.transform.position,
            AssembleManager.Instance.InspectPosition.position,
            dt * moveSpeed
        );

        if (Vector3.Distance(stateMachine.transform.position, AssembleManager.Instance.InspectPosition.position) < 0.01f)
        {
            stateMachine.transform.position = AssembleManager.Instance.InspectPosition.position;
            stateMachine.SwitchState(new FragmentInspectState(stateMachine));
        }
    }

    public override void Exit() { }
}

