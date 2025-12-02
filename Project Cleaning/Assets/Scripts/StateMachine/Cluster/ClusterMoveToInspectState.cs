using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClusterMoveToInspect : ClusterBaseState
{
    public ClusterMoveToInspect(ClusterStateMachine stateMachine) : base(stateMachine) { }
    public override void Enter()
    {
        stateMachine.Interaction.DisableAllInteraction();

        Debug.Log("Cluster state: Move to inspect");

        stateMachine.SetClusterState(ClusterState.MoveToInspect);

        var manager = stateMachine.assembleManager;
        var inspectedFragment = manager.CurrentFragmentInspected;
        var inspectedCluster = manager.CurrentClusterInspected;

        if (inspectedFragment != null)
        {
            inspectedFragment.SwitchState(new FragmentReturningState(inspectedFragment));
        }

        if (inspectedCluster != null)
        {
            inspectedCluster.SwitchState(new ClusterReturningState(inspectedCluster));
        }

        AssembleManager.Instance.SetCurrentInspectFragment(null);
        AssembleManager.Instance.SetCurrentInspectCluster(stateMachine);

        stateMachine.transform.SetParent(manager.InspectPosition);
        stateMachine.Interaction.isReturning = false;
    }

    public override void Tick(float deltaTime)
    {
        stateMachine.transform.position = Vector3.Lerp(
            stateMachine.transform.position,
            AssembleManager.Instance.InspectPosition.position,
            deltaTime * stateMachine.moveSpeed
        );

        if (Vector3.Distance(stateMachine.transform.position, AssembleManager.Instance.InspectPosition.position) < .01f)
        {
            stateMachine.transform.position = AssembleManager.Instance.InspectPosition.position;
            stateMachine.SwitchState(new ClusterInspectState(stateMachine));
        }
    }

    public override void Exit()
    {
        stateMachine.Interaction.DisableAllInteraction();
    }
}
