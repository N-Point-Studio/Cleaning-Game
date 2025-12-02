using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class FragmentBaseState : State
{
    protected FragmentStateMachine stateMachine;

    public FragmentBaseState(FragmentStateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
    }
}

