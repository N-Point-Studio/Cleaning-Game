using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FragmentUnassembleState : FragmentBaseState
{
    Transform root;

    public FragmentUnassembleState(FragmentStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        root = stateMachine.clusterRoot;

        // keluarkan diri dari cluster
        stateMachine.transform.SetParent(null, true);
        stateMachine.clusterRoot = null;

        // kalau root tinggal 1 anak → bubar cluster
        if (root.childCount == 1)
        {
            Transform last = root.GetChild(0);
            last.SetParent(null, true);
            last.GetComponent<FragmentStateMachine>().clusterRoot = null;

            GameObject.Destroy(root.gameObject);
        }
    }

    public override void Tick(float dt)
    {
        // kembali ke Idle, posisi tetap bisa digerakkan
        stateMachine.SwitchState(new FragmentIdleState(stateMachine));
    }

    public override void Exit() { }
}
