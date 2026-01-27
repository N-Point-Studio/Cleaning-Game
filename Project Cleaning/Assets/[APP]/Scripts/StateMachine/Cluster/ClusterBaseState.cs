using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ClusterBaseState : State
{
    protected ClusterStateMachine stateMachine;

    public ClusterBaseState(ClusterStateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
    }
}
