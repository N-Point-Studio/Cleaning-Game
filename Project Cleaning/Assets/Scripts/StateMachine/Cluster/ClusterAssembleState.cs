using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClusterAssembleState : ClusterBaseState
{
    private float assembleSpeed = 5f;
    private float rotationSpeed = 10f;

    public ClusterAssembleState(ClusterStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        stateMachine.SetClusterState(ClusterState.Assemble);
        AssembleIntoCluster();
    }

    public override void Tick(float deltaTime) { }

    public override void Exit() { }

    private void AssembleIntoCluster()
    {
        ClusterStateMachine inspectedCluster = AssembleManager.Instance.CurrentClusterInspected;
        FragmentStateMachine inspectedFragment = AssembleManager.Instance.CurrentFragmentInspected;
        if (inspectedCluster == null && inspectedFragment == null) return;
        if (inspectedCluster != null)
        {
            foreach (var fragment in stateMachine.connectedFragments)
            {
                inspectedCluster.AddFragment(fragment);

                if (AssembleManager.Instance.TryGetAssemblePosition(fragment, out Transform correctPos))
                {
                    fragment.SwitchState(new FragmentAttachedState(fragment, correctPos));
                }
            }
            stateMachine.connectedFragments.Clear();
            Object.Destroy(stateMachine.gameObject);
            AssembleManager.Instance.SetCurrentInspectCluster(inspectedCluster);
            AssembleManager.Instance.SetCurrentInspectFragment(null);
        }

        if (inspectedFragment != null)
        {
            GameObject newClusterGO = new GameObject("Cluster_" + inspectedFragment.name + "_" + stateMachine.name);
            var newCluster = newClusterGO.AddComponent<ClusterStateMachine>();
            newCluster.transform.position = AssembleManager.Instance.InspectPosition.transform.position;
            newCluster.transform.SetParent(AssembleManager.Instance.InspectPosition.transform);

            newCluster.AddFragment(inspectedFragment);
            inspectedFragment.transform.SetParent(newCluster.transform, false);

            foreach (var fragment in stateMachine.connectedFragments)
            {
                newCluster.AddFragment(fragment);
                fragment.transform.SetParent(newCluster.transform, false);

                if (AssembleManager.Instance.TryGetAssemblePosition(fragment, out Transform correctPos))
                {
                    fragment.SwitchState(new FragmentAttachedState(fragment, correctPos));
                }
            }

            if (AssembleManager.Instance.TryGetAssemblePosition(inspectedFragment, out Transform correctPosIn))
            {
                inspectedFragment.SwitchState(new FragmentAttachedState(inspectedFragment, correctPosIn));
            }
            stateMachine.connectedFragments.Clear();
            Object.Destroy(stateMachine.gameObject);

            AssembleManager.Instance.SetCurrentInspectCluster(newCluster);
            AssembleManager.Instance.SetCurrentInspectFragment(null);
        }
    }
}
