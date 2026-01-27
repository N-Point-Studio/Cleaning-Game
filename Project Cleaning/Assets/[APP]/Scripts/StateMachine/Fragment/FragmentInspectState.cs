using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FragmentInspectState : FragmentBaseState
{
    private float moveSpeed = 6f;
    private bool hasReachedInspect = false;

    public FragmentInspectState(FragmentStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        stateMachine.CurrentStatus = "Inspect";

        stateMachine.Interaction.isHoldAvailable = true;
        stateMachine.Interaction.isTapAvailable = false;
        stateMachine.Interaction.isDragAvailable = false;

        // AssembleManager.Instance.SetCurrentInspectCluster(null);

    }


    public override void Tick(float dt)
    {
        Debug.Log("inspect" + stateMachine.name + " containing: " + stateMachine.StateMachineConnected.Count);
        if (stateMachine.Interaction.isHolding && !TouchManager.Instance.isInteracting)
        {
            Holding();
        }
    }

    public override void Exit()
    {
        stateMachine.Interaction.DisableAllInteraction();


        // if (AssembleManager.Instance.CurrentFragmentInspected == stateMachine)
        // {
        //     // FragmentStateMachine.CurrentInspecting = null;
        //     AssembleManager.Instance.SetCurrentInspectFragment(null);
        // }
    }

    private void Holding()
    {
        var connectedList = stateMachine.StateMachineConnected;
        if (connectedList != null && connectedList.Count > 0)
        {
            if (connectedList.Count == 1)
            {
                var single = connectedList[0];
                if (single != null && single.CurrentStatus == "Assemble")
                {
                    single.SwitchState(new FragmentMoveToInspectState(single));
                }
            }
            else
            {
                FragmentStateMachine parent = connectedList[0];
                if (parent != null)
                {

                    foreach (var frag in connectedList)
                    {
                        if (frag == parent) continue;

                        frag.transform.SetParent(parent.transform);
                        parent.StateMachineConnected.Add(frag);
                    }
                    parent.SwitchState(new FragmentMoveToInspectState(parent));
                }
            }
        }
        else
        {
            stateMachine.SwitchState(new FragmentReturningState(stateMachine));
        }
        stateMachine.StateMachineConnected.Clear();
    }
}
