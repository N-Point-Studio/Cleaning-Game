using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FragmentAssembledState : FragmentBaseState
{
    private FragmentStateMachine targetFragment;
    private float assembleSpeed = 5f;
    private float rotationSpeed = 10f;

    public FragmentAssembledState(FragmentStateMachine stateMachine, FragmentStateMachine target)
        : base(stateMachine)
    {
        targetFragment = target;
    }

    public override void Enter()
    {
        stateMachine.CurrentStatus = "Assembled";
        stateMachine.Interaction.isHoldAvailable = true;
        stateMachine.Interaction.isReturning = false;

        if (targetFragment.StateMachineConnected.Count == 0 && stateMachine.StateMachineConnected.Count == 0)
        {
            targetFragment.StateMachineConnected.Add(stateMachine);
            stateMachine.transform.SetParent(targetFragment.transform);
            MoveSmoothlyToTarget(stateMachine, targetFragment);
        }

        else if (targetFragment.StateMachineConnected.Count > 0 && stateMachine.StateMachineConnected.Count == 0)
        {
            targetFragment.StateMachineConnected.Add(stateMachine);
            stateMachine.transform.SetParent(targetFragment.transform);
            MoveSmoothlyToTarget(stateMachine, targetFragment);
        }

        else if (stateMachine.StateMachineConnected.Count > 0)
        {
            if (!targetFragment.StateMachineConnected.Contains(stateMachine))
            {
                targetFragment.StateMachineConnected.Add(stateMachine);
                stateMachine.transform.SetParent(targetFragment.transform);
                MoveSmoothlyToTarget(stateMachine, targetFragment);
            }
            var connectedCopy = new List<FragmentStateMachine>(stateMachine.StateMachineConnected);

            foreach (var connected in connectedCopy)
            {
                targetFragment.StateMachineConnected.Add(connected);
                connected.transform.SetParent(targetFragment.transform);
                MoveSmoothlyToTarget(connected, targetFragment);
            }

            stateMachine.StateMachineConnected.Clear();
        }
    }

    public override void Tick(float deltaTime)
    {
        if (stateMachine.Interaction.isHolding)
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
        targetFragment.StateMachineConnected.Remove(stateMachine);
        stateMachine.transform.SetParent(null);
        stateMachine.SwitchState(new FragmentReturningState(stateMachine));
    }

    private void MoveSmoothlyToTarget(FragmentStateMachine fragment, FragmentStateMachine target)
    {
        if (target.TryGetAssemblyTarget(fragment, out Transform correctPos))
        {
            fragment.StartCoroutine(SmoothMoveCoroutine(fragment.transform, correctPos));
        }
    }

    private IEnumerator SmoothMoveCoroutine(Transform fragment, Transform target)
    {
        Vector3 startPos = fragment.position;
        Quaternion startRot = fragment.rotation;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * assembleSpeed;

            fragment.position = Vector3.Lerp(startPos, target.position, t);
            fragment.rotation = Quaternion.Slerp(startRot, target.rotation, t * rotationSpeed / assembleSpeed);

            yield return null;
        }

        fragment.position = target.position;
        fragment.rotation = target.rotation;
    }
}
