using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FragmentAssembledState : FragmentBaseState
{
    private FragmentStateMachine other;

    public FragmentAssembledState(FragmentStateMachine self, FragmentStateMachine other) : base(self)
    {
        this.other = other;
    }

    public override void Enter()
    {
        Transform rootA = stateMachine.clusterRoot;
        Transform rootB = other.clusterRoot;

        // CASE 1: Kedua belum punya cluster → buat baru
        if (rootA == null && rootB == null)
        {
            Transform newRoot = new GameObject("Cluster").transform;
            newRoot.position = stateMachine.CorrectPosition.position;

            stateMachine.transform.SetParent(newRoot, true);
            other.transform.SetParent(newRoot, true);

            stateMachine.clusterRoot = newRoot;
            other.clusterRoot = newRoot;
        }
        // CASE 2: Only self has cluster → join other into self cluster
        else if (rootA != null && rootB == null)
        {
            other.transform.SetParent(rootA, true);
            other.clusterRoot = rootA;
        }
        // CASE 3: Only other has cluster → join self into other cluster
        else if (rootA == null && rootB != null)
        {
            stateMachine.transform.SetParent(rootB, true);
            stateMachine.clusterRoot = rootB;
        }
        // CASE 4: Both have clusters → merge clusters
        else if (rootA != rootB)
        {
            // pindahkan semua child dari rootB ke rootA
            while (rootB.childCount > 0)
            {
                Transform child = rootB.GetChild(0);
                child.SetParent(rootA, true);
                child.GetComponent<FragmentStateMachine>().clusterRoot = rootA;
            }
            GameObject.Destroy(rootB.gameObject);
        }

        // jadikan cluster ini sebagai yang diinspect
        FragmentStateMachine.CurrentInspecting = stateMachine;
    }

    public override void Tick(float dt)
    {
        // kalau salah satu hold → keluar dari cluster
        if (stateMachine.interaction.isHolding)
            stateMachine.SwitchState(new FragmentUnassembleState(stateMachine));

        if (other.interaction.isHolding)
            other.SwitchState(new FragmentUnassembleState(other));
    }

    public override void Exit() { }
}
