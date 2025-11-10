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
        Transform clusterTransform;

        if (targetFragment.clusterRoot != null)
        {
            clusterTransform = targetFragment.clusterRoot;
        }
        else
        {
            GameObject cluster = new GameObject("Cluster_" + targetFragment.name);
            clusterTransform = cluster.transform;
            clusterTransform.position = targetFragment.transform.position;
            clusterTransform.rotation = targetFragment.transform.rotation;
            targetFragment.transform.SetParent(clusterTransform);
            targetFragment.clusterRoot = clusterTransform;
        }

        if (targetFragment.transform.parent != clusterTransform)
            targetFragment.transform.SetParent(clusterTransform);

        stateMachine.transform.SetParent(clusterTransform);

        stateMachine.transform.position = stateMachine.CorrectPosition.position;
        stateMachine.transform.rotation = stateMachine.CorrectPosition.rotation;

        targetFragment.transform.position = targetFragment.CorrectPosition.position;
        targetFragment.transform.rotation = targetFragment.CorrectPosition.rotation;

        stateMachine.clusterRoot = clusterTransform;

        stateMachine.SwitchState(new FragmentInspectState(stateMachine));

        Debug.Log($"{stateMachine.name} attached to cluster {clusterTransform.name}");

        FragmentStateMachine.CurrentInspecting = stateMachine;
    }

    public override void Tick(float deltaTime) { }

    public override void Exit() { }
}
