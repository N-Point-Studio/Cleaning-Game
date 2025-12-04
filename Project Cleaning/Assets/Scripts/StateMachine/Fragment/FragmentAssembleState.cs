using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FragmentAssembledState : FragmentBaseState
{
    private float assembleSpeed = 5f;
    private float rotationSpeed = 10f;

    public FragmentAssembledState(FragmentStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        stateMachine.CurrentStatus = "Assembled";
        stateMachine.Interaction.isHoldAvailable = true;
        stateMachine.Interaction.isTapAvailable = false;
        stateMachine.Interaction.isDragAvailable = false;
        stateMachine.Interaction.isReturning = false;
        AssembleIntoCluster();
    }

    public override void Tick(float deltaTime) { }

    public override void Exit()
    {
        stateMachine.Interaction.DisableAllInteraction();
    }

    private void AssembleIntoCluster()
    {
        ClusterStateMachine inspectedCluster = AssembleManager.Instance.CurrentClusterInspected;
        FragmentStateMachine inspectedFragment = AssembleManager.Instance.CurrentFragmentInspected;

        if (inspectedCluster == null && inspectedFragment == null) return;

        if (inspectedCluster != null)
        {
            inspectedCluster.AddFragment(stateMachine);
            stateMachine.transform.SetParent(inspectedCluster.transform, false);

            if (AssembleManager.Instance.TryGetAssemblePosition(stateMachine, out Transform correctPos))
            {
                stateMachine.SwitchState(new FragmentAttachedState(stateMachine, correctPos));
            }
        }

        if (inspectedFragment != null)
        {
            GameObject newClusterGO = new GameObject("Cluster_" + inspectedFragment.name + "_" + stateMachine.name);
            var newCluster = newClusterGO.AddComponent<ClusterStateMachine>();
            newCluster.transform.position = AssembleManager.Instance.InspectPosition.transform.position;
            newCluster.transform.SetParent(AssembleManager.Instance.InspectPosition.transform);

            newCluster.AddFragment(inspectedFragment);
            newCluster.AddFragment(stateMachine);

            inspectedFragment.transform.SetParent(newCluster.transform, false);
            stateMachine.transform.SetParent(newCluster.transform, false);

            if (AssembleManager.Instance.TryGetAssemblePosition(stateMachine, out Transform correctPosSM))
            {
                stateMachine.SwitchState(new FragmentAttachedState(stateMachine, correctPosSM));
            }

            if (AssembleManager.Instance.TryGetAssemblePosition(inspectedFragment, out Transform correctPosIn))
            {
                inspectedFragment.SwitchState(new FragmentAttachedState(inspectedFragment, correctPosIn));
            }

            AssembleManager.Instance.SetCurrentInspectCluster(newCluster);
            AssembleManager.Instance.SetCurrentInspectFragment(null);
        }

        stateMachine.audioSource.PlayOneShot(stateMachine.assembleSound);
        HapticManager.Instance.Light();
    }
}
