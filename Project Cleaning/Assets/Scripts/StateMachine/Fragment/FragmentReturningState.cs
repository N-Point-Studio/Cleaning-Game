using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FragmentReturningState : FragmentBaseState
{
    private float moveSpeed = 6f;
    private Vector3 targetPosition;
    private Quaternion targetRotation;

    public FragmentReturningState(FragmentStateMachine stateMachine) : base(stateMachine)
    {
        targetPosition = stateMachine.InitialPosition;
        targetRotation = stateMachine.InitialRotation;
    }

    public override void Enter()
    {

        stateMachine.audioSource.PlayOneShot(stateMachine.putSound);
        HapticManager.Instance.Light();
        Debug.Log(stateMachine.name + "is returning");
        stateMachine.Interaction.DisableAllInteraction();
        stateMachine.CurrentStatus = "Returning";
        stateMachine.transform.SetParent(null);

        if (AssembleManager.Instance.CurrentFragmentInspected == stateMachine)
        {
            AssembleManager.Instance.SetCurrentInspectFragment(null);
        }

        if (AssembleManager.Instance.CurrentClusterInspected != null)
        {
            AssembleManager.Instance.CurrentClusterInspected.DestroyingCluster();
        }
    }

    public override void Tick(float dt)
    {
        stateMachine.transform.position = Vector3.Lerp(
            stateMachine.transform.position,
            targetPosition,
            dt * moveSpeed
        );

        stateMachine.transform.rotation = Quaternion.Lerp(
            stateMachine.transform.rotation,
            targetRotation,
            dt * moveSpeed
        );

        if (Vector3.Distance(stateMachine.transform.position, targetPosition) < 0.01f)
        {
            stateMachine.transform.position = targetPosition;
            stateMachine.transform.rotation = targetRotation;
            stateMachine.SwitchState(new FragmentIdleState(stateMachine));
        }
    }

    public override void Exit() { }
}
