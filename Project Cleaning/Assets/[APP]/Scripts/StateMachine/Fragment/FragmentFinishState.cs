using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FragmentFinishState : FragmentBaseState
{
    public FragmentFinishState(FragmentStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        stateMachine.Interaction.DisableAllInteraction();
    }

    public override void Exit()
    {

    }

    public override void Tick(float deltaTime)
    {

    }
}
