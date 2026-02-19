using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AssemblyTarget
{
    public AssemblyTarget(FragmentStateMachine frag, CustomTransform transform)
    {
        this.targetFragment = frag;
        this.correctPosition = transform;
    }
    public FragmentStateMachine targetFragment;
    public CustomTransform correctPosition;
}
