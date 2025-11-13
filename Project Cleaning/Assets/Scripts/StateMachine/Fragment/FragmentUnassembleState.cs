using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FragmentUnassembleState : FragmentBaseState
{
    Transform root;

    public FragmentUnassembleState(FragmentStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        stateMachine.SwitchState(new FragmentReturningState(stateMachine));
    }

    public override void Tick(float dt) { }

    public override void Exit() { }
}
