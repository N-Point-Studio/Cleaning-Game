using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FragmentAssembledState : FragmentBaseState
{
    private FragmentStateMachine targetFragment;

    public FragmentAssembledState(FragmentStateMachine stateMachine, FragmentStateMachine target)
        : base(stateMachine)
    {
        targetFragment = target;
    }

    public override void Enter()
    {
        stateMachine.Interaction.isHoldAvailable = true;

        if (targetFragment.StateMachineConnected.Count == 0 && stateMachine.StateMachineConnected.Count == 0)
        {
            targetFragment.StateMachineConnected.Add(stateMachine);
            stateMachine.transform.SetParent(targetFragment.transform);
            if (targetFragment.TryGetAssemblyTarget(stateMachine, out Transform correctPos))
            {
                stateMachine.transform.position = correctPos.position;
                stateMachine.transform.rotation = correctPos.rotation;
            }
        }

        else if (targetFragment.StateMachineConnected.Count > 0 && stateMachine.StateMachineConnected.Count == 0)
        {
            targetFragment.StateMachineConnected.Add(stateMachine);
            Transform parent = targetFragment.transform;
            stateMachine.transform.SetParent(parent);
            if (targetFragment.TryGetAssemblyTarget(stateMachine, out Transform correctPos))
            {
                stateMachine.transform.position = correctPos.position;
                stateMachine.transform.rotation = correctPos.rotation;
            }
        }

        else if (stateMachine.StateMachineConnected.Count > 0)
        {
            foreach (var connected in stateMachine.StateMachineConnected)
            {
                targetFragment.StateMachineConnected.Add(connected);
                Transform parent = targetFragment.transform;
                connected.transform.SetParent(parent);
                if (targetFragment.TryGetAssemblyTarget(connected, out Transform correctPos))
                {
                    connected.transform.position = correctPos.position;
                    connected.transform.rotation = correctPos.rotation;
                }
            }

            if (!targetFragment.StateMachineConnected.Contains(stateMachine))
            {
                targetFragment.StateMachineConnected.Add(stateMachine);
                stateMachine.transform.SetParent(targetFragment.transform);

                if (targetFragment.TryGetAssemblyTarget(stateMachine, out Transform correctPos))
                {
                    stateMachine.transform.position = correctPos.position;
                    stateMachine.transform.rotation = correctPos.rotation;
                }
            }
            stateMachine.StateMachineConnected.Clear();
        }
    }

    public override void Tick(float deltaTime)
    {
        if (stateMachine.Interaction.isHolding && targetFragment.Interaction.isHoldAvailable)
        {
            ReturnFragment();
        }
    }

    public override void Exit()
    {
        stateMachine.Interaction.isHoldAvailable = false;
    }

    private void ReturnFragment()
    {
        if (targetFragment != null && targetFragment.StateMachineConnected.Contains(stateMachine))
        {
            targetFragment.StateMachineConnected.Remove(stateMachine);
        }

        stateMachine.transform.SetParent(null);
        stateMachine.SwitchState(new FragmentReturningState(stateMachine));
    }
}
